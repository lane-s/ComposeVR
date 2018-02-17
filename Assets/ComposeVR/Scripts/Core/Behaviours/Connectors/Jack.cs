using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRTK;
using System;
using System.Linq;

namespace ComposeVR {
    public class JackEventArgs : EventArgs {
        private Jack otherJack;

        public JackEventArgs(Jack other) {
            otherJack = other;
        }

        public Jack Other {
            get { return otherJack;  }
            set { otherJack = value;  }
        }
    }

    /// <summary>
    /// Jacks provide a way to connect different modules.
    /// 
    /// A Free Jack has nothing plugged into it and will deploy a plug if the user's controller is detected nearby
    /// 
    /// A Jack that is WaitingForGrab has deployed a plug but is waiting for the user to grab it before deploying the plug on the other end of the cord.
    /// 
    /// A Jack that is Blocked has a plug inside of it already.
    /// 
    /// Every jack owns two plugs and a cord, though they are not active until deployed from the jack.
    /// 
    /// </summary>
    public sealed class Jack : MonoBehaviour {

        public Transform PlugPrefab;
        public Transform CordPrefab;

        public Vector3 ShrinkPlugScale;
        public Transform PlugStart;
        public Transform PlugSnapPoint;
        public Transform CordOrigin;

        public SimpleTrigger ControllerDetector;
        public SimpleTrigger PlugDetector;
        public SimpleTrigger BlockingDetector;

        public float ExtendDistance;
        public float ExtendSpeed;

        public EventHandler<JackEventArgs> OtherJackConnected;
        public EventHandler<JackEventArgs> OtherJackDisconnected;

        public enum State {Free, WaitingForGrab, Blocked}
        private State state;

        private Plug primaryPlug;
        private Plug secondaryPlug;
        private Cord cord;

        private Vector3 normalPlugScale;

        private List<VRTK_InteractGrab> nearbyControllers;

        private int numBlockers;

        private void Awake() {
            ControllerDetector.TriggerEnter += OnControllerEnterArea;
            ControllerDetector.TriggerExit += OnControllerLeaveArea;

            PlugDetector.TriggerEnter += OnPlugEnterArea;
            PlugDetector.TriggerExit += OnPlugLeaveArea;

            BlockingDetector.TriggerEnter += OnBlockerEnterArea;
            BlockingDetector.TriggerExit += OnBlockerLeaveArea;

            nearbyControllers = new List<VRTK_InteractGrab>();

            primaryPlug = Instantiate(PlugPrefab, PlugStart.position, PlugStart.rotation).GetComponent<Plug>();
            secondaryPlug = Instantiate(PlugPrefab, PlugStart.position, PlugStart.rotation).GetComponent<Plug>();

            primaryPlug.SetConnectedPlug(secondaryPlug);
            secondaryPlug.SetConnectedPlug(primaryPlug);

            primaryPlug.OriginJack = this;
            secondaryPlug.DestinationJack = this;

            primaryPlug.gameObject.SetActive(false);
            secondaryPlug.gameObject.SetActive(false);
            normalPlugScale = primaryPlug.GetComponent<Plug>().PlugTransform.localScale;

            cord = Instantiate(CordPrefab).GetComponent<Cord>();
            cord.SetCordEnds(secondaryPlug.CordAttachPoint, primaryPlug.CordAttachPoint);
            cord.gameObject.SetActive(false);

            state = State.Free;

            StartCoroutine(FSM());
        }

        void OnBlockerEnterArea(object sender, SimpleTriggerEventArgs e) {
            numBlockers += 1;
        }

        void OnBlockerLeaveArea(object sender, SimpleTriggerEventArgs e) {
            numBlockers -= 1;
        }

        void OnControllerEnterArea(object sender, SimpleTriggerEventArgs e) {
            VRTK_InteractGrab controller = e.other.GetComponentInParent<VRTK_InteractGrab>();
            if (controller != null) {
                nearbyControllers.Add(controller);
            }                
        }

        void OnControllerLeaveArea(object sender, SimpleTriggerEventArgs e) {
            VRTK_InteractGrab controller = e.other.GetComponentInParent<VRTK_InteractGrab>();
            if (controller != null) {
                nearbyControllers.Remove(controller);
            }                
        }

        void OnPlugEnterArea(object sender, SimpleTriggerEventArgs e) {
            OwnedObject o = e.other.GetComponent<OwnedObject>();
            if(o != null) {
                Plug p = o.Owner.GetComponent<Plug>();
                if(p != null) {
                    p.AddNearbyJack(this);
                }
            }
        }

        void OnPlugLeaveArea(object sender, SimpleTriggerEventArgs e) {
            OwnedObject o = e.other.GetComponent<OwnedObject>();
            if(o != null) {
                Plug p = o.Owner.GetComponent<Plug>();
                if(p != null) {
                    p.RemoveNearbyJack(this);
                }
            }
        }

        public Jack.State GetState() {
            return state;
        }

        public void SetState(Jack.State s) {
            state = s;
        }

        private IEnumerator FSM() {
            while (true) {
                yield return StartCoroutine(state.ToString());
            }
        }

        private IEnumerator Free() {
            while (state == State.Free) {
                if(nearbyControllers.Count > 0) {
                    bool holdingPlug = false;

                    foreach(VRTK_InteractGrab c in nearbyControllers) {
                        GameObject grabbedObject = c.GetGrabbedObject();
                        if(grabbedObject != null && grabbedObject.GetComponent<Plug>()) {
                            holdingPlug = true;
                            break;
                        }
                    }

                    if (!holdingPlug && numBlockers == 0) {
                        state = State.WaitingForGrab;
                        break;
                    }
                }
                yield return new WaitForEndOfFrame();
            }
        }

        private IEnumerator WaitingForGrab() {
            primaryPlug.OriginJack = this;
            cord.gameObject.SetActive(true);
            StartCoroutine(ExtendPlug(primaryPlug, PlugStart.position + PlugStart.forward * ExtendDistance));

            bool grabbed = false;

            while (state == State.WaitingForGrab) {
                if (primaryPlug.GetComponent<VRTK_InteractableObject>().IsGrabbed()) {
                    StartCoroutine(ExtendPlug(secondaryPlug, PlugSnapPoint.position));
                    secondaryPlug.DestinationJack = this;
                    secondaryPlug.transform.rotation *= Quaternion.AngleAxis(180.0f, secondaryPlug.transform.up);

                    state = State.Blocked;
                }else if(nearbyControllers.Count == 0) {
                    StartCoroutine(RetractPlug(primaryPlug.GetComponent<Plug>()));
                    state = State.Free;
                }

                yield return new WaitForEndOfFrame();
            }

        }

        private IEnumerator Blocked() {
            while (state == State.Blocked) {
                yield return new WaitForEndOfFrame();
            }
        }

        private IEnumerator ExtendPlug(Plug p, Vector3 target) {
            p.gameObject.SetActive(true);
            p.transform.position = PlugStart.position;
            p.transform.rotation = PlugStart.rotation;

            p.PlugTransform.GetComponent<SnapToTargetPosition>().SnapToTarget(target, ExtendSpeed);
            p.PlugTransform.localScale = ShrinkPlugScale;
            p.PlugTransform.GetComponent<Scalable>().TargetScale = normalPlugScale;

            p.PlugTransform.GetComponent<SnapToTargetRotation>().enabled = false;

            p.GetComponent<VRTK_InteractableObject>().isGrabbable = true;

            while (!p.PlugTransform.GetComponent<SnapToTargetPosition>().HasReachedTarget) {
                yield return new WaitForEndOfFrame();
            }

        }

        private IEnumerator RetractPlug(Plug p) {

            p.PlugTransform.GetComponent<SnapToTargetPosition>().SnapToTarget(PlugStart.position, ExtendSpeed);
            p.PlugTransform.GetComponent<Scalable>().TargetScale = ShrinkPlugScale;

            p.GetComponent<VRTK_InteractableObject>().isGrabbable = false;

            while (!p.PlugTransform.GetComponent<SnapToTargetPosition>().HasReachedTarget) {
                yield return new WaitForEndOfFrame();
            }

            p.gameObject.SetActive(false);

            if (primaryPlug.Equals(p)) {
                cord.gameObject.SetActive(false);
            }

            state = State.Free;
        }

        public void ConnecToJack(Jack other) {
            if(OtherJackConnected != null) {
                OtherJackConnected(this, new JackEventArgs(other));
            }
        }

        public void DisconnectJack(Jack other) {
            if(OtherJackDisconnected != null) {
                OtherJackDisconnected(this, new JackEventArgs(other));
            }
        }
    }
}