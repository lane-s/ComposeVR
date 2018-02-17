using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRTK;
using System;
using System.Linq;

namespace ComposeVR {
    /// <summary>
    /// Jacks provide a way to connect different modules.
    /// 
    /// A Free Jack has nothing plugged into it and will deploy a plug if the user's controller is detected nearby
    /// 
    /// A Jack that is WaitingForGrab has deployed a plug but is waiting for the user to grab it before deploying the plug on the other end of the cord.
    /// 
    /// A Jack that is Blocked has a plug inside of it already. Plugging into a blocked jack merges two or more plugs 
    /// 
    /// </summary>
    public sealed class Jack : MonoBehaviour {

        public Transform PlugPrefab;
        public Vector3 ShrinkPlugScale;
        public Transform PlugStart;
        public Transform PlugSnapPoint;
        public Transform CordOrigin;
        public SimpleTrigger ControllerDetector;
        public SimpleTrigger PlugDetector;

        public float ExtendDistance;
        public float ExtendSpeed;

        public enum State {Free, WaitingForGrab, Blocked}
        private State state;

        private Plug primaryPlug;
        private Plug secondaryPlug;

        //Nearby plugs that originate from other jacks
        private List<VRTK_InteractGrab> nearbyControllers;

        private Vector3 normalPlugScale;

        private int numNearbyPlugs;

        private void Awake() {
            ControllerDetector.TriggerEnter += OnControllerEnterArea;
            ControllerDetector.TriggerExit += OnControllerLeaveArea;

            PlugDetector.TriggerEnter += OnPlugEnterArea;
            PlugDetector.TriggerExit += OnPlugLeaveArea;

            nearbyControllers = new List<VRTK_InteractGrab>();

            primaryPlug = Instantiate(PlugPrefab).GetComponent<Plug>();
            secondaryPlug = Instantiate(PlugPrefab).GetComponent<Plug>();

            primaryPlug.SetSecondaryPlug(secondaryPlug);
            primaryPlug.OriginJack = this;

            normalPlugScale = primaryPlug.GetComponent<Plug>().PlugTransform.localScale;

            primaryPlug.gameObject.SetActive(false);
            secondaryPlug.gameObject.SetActive(false);

            state = State.Free;

            StartCoroutine(FSM());
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
                    numNearbyPlugs += 1;
                    p.AddNearbyJack(this);
                }
            }
        }

        void OnPlugLeaveArea(object sender, SimpleTriggerEventArgs e) {
            OwnedObject o = e.other.GetComponent<OwnedObject>();
            if(o != null) {
                Plug p = o.Owner.GetComponent<Plug>();
                if(p != null) {
                    numNearbyPlugs += 1;
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

                    if (!holdingPlug) {
                        //Deploy a plug and wait for it to be grabbed
                        state = State.WaitingForGrab;
                        break;
                    }
                }
                yield return new WaitForEndOfFrame();
            }
        }

        private IEnumerator WaitingForGrab() {
            StartCoroutine(ExtendPlug(primaryPlug, PlugStart.position + PlugStart.forward * ExtendDistance));

            bool grabbed = false;

            while (state == State.WaitingForGrab) {
                if (primaryPlug.GetComponent<VRTK_InteractableObject>().IsGrabbed()) {
                    StartCoroutine(ExtendPlug(secondaryPlug, PlugSnapPoint.position));
                    secondaryPlug.transform.rotation *= Quaternion.AngleAxis(180.0f, secondaryPlug.transform.up);
                    secondaryPlug.DestinationJack = this;

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

            state = State.Free;
        }
    }
}