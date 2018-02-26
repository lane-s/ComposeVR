using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRTK;
using System;
using System.Linq;

namespace ComposeVR {
    public class JackEventArgs : EventArgs {
        private Cord connectedCord;
        private LinkedListNode<BranchNode> plugEnd;

        public JackEventArgs(Cord connectedCord, LinkedListNode<BranchNode> plugEnd) {
            this.connectedCord = connectedCord;
            this.plugEnd = plugEnd;
        }

        public Cord ConnectedCord {
            get { return connectedCord;  }
            set { connectedCord = value;  }
        }

        public LinkedListNode<BranchNode> PlugNodeInCord {
            get { return plugEnd; }
            set { plugEnd = value; }
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
        public Transform PlugConnectionPoint;
        public Transform CordOrigin;

        public SimpleTrigger ControllerDetector;
        public SimpleTrigger PlugDetector;
        public SimpleTrigger BlockingDetector;

        public float ExtendDistance;
        public float ExtendSpeed;

        public EventHandler<JackEventArgs> PlugConnected;
        public EventHandler<JackEventArgs> PlugDisconnected;

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

            CreateCord();

            state = State.Free;

            StartCoroutine(FSM());
        }

        /// <summary>
        /// Creates a new cord consisting of two connected plugs and a actual cord object. This is the cord that the jack will deploy when the user's controller comes near
        /// </summary>
        private void CreateCord() {
            primaryPlug = Instantiate(PlugPrefab, PlugStart.position, PlugStart.rotation).GetComponent<Plug>();
            secondaryPlug = Instantiate(PlugPrefab, PlugStart.position, PlugStart.rotation).GetComponent<Plug>();

            secondaryPlug.DestinationJack = this;

            primaryPlug.gameObject.SetActive(false);
            secondaryPlug.gameObject.SetActive(false);
            normalPlugScale = primaryPlug.GetComponent<Plug>().PlugTransform.localScale;

            cord = Instantiate(CordPrefab).GetComponent<Cord>();
            cord.ConnectCord(secondaryPlug.CordAttachPoint, primaryPlug.CordAttachPoint);

            cord.Flow = 1;

            if(GetComponent<InputJack>() != null) {
                cord.Flow = -cord.Flow;
            }
            cord.SetFlowing(true);
                
            cord.gameObject.SetActive(false);
        }

        /// <summary>
        /// Keep track of objects that block the jack, but ignore plugs from the cord that the jack currently manages
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void OnBlockerEnterArea(object sender, SimpleTriggerEventArgs e) {
            Plug p = e.other.GetComponent<Plug>();
            if(p == null && e.other.GetComponent<OwnedObject>()){
                p = e.other.GetComponent<OwnedObject>().Owner.GetComponent<Plug>();
            }

            if (p != null && (p.Equals(primaryPlug) || p.Equals(secondaryPlug))) {
                return;
            }

            numBlockers += 1;
        }

        void OnBlockerLeaveArea(object sender, SimpleTriggerEventArgs e) {
            Plug p = e.other.GetComponent<Plug>();
            if(p == null && e.other.GetComponent<OwnedObject>()){
                p = e.other.GetComponent<OwnedObject>().Owner.GetComponent<Plug>();
            }

            if (p != null && (p.Equals(primaryPlug) || p.Equals(secondaryPlug))) {
                return;
            }

            numBlockers -= 1;
            numBlockers = Math.Max(numBlockers, 0);
        }

        public void OnBlockerDestroyed() {
            numBlockers -= 1;
            numBlockers = Mathf.Max(numBlockers, 0);
        }

        /// <summary>
        /// Keep track of nearby controllers
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void OnControllerEnterArea(object sender, SimpleTriggerEventArgs e) {
            VRTK_InteractGrab controller = e.other.GetComponentInParent<VRTK_InteractGrab>();
            if (controller != null && !nearbyControllers.Contains(controller)) {
                nearbyControllers.Add(controller);
            }                
        }

        void OnControllerLeaveArea(object sender, SimpleTriggerEventArgs e) {
            VRTK_InteractGrab controller = e.other.GetComponentInParent<VRTK_InteractGrab>();
            if (controller != null) {
                nearbyControllers.Remove(controller);
            }                
        }

        /// <summary>
        /// Keep track of nearby plugs
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
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

        /// <summary>
        /// In the Free state, the jack checks to see if any empty hand is nearby. If so, extend a plug
        /// 
        /// This is the only state where a jack can be plugged into
        /// </summary>
        /// <returns></returns>
        private IEnumerator Free() {
            while (state == State.Free) {
                if(nearbyControllers.Count > 0) {
                    bool holdingObject = false;

                    foreach(VRTK_InteractGrab c in nearbyControllers) {
                        GameObject grabbedObject = c.GetGrabbedObject();
                        if(grabbedObject != null) {
                            holdingObject = true;
                            break;
                        }
                    }

                    if (!holdingObject && numBlockers == 0) {
                        state = State.WaitingForGrab;
                        break;
                    }
                }
                yield return new WaitForEndOfFrame();
            }
        }

        /// <summary>
        /// When the jack is WaitingForGrab, it checks if the plug that it extended is grabbed. If it is, it extends the other plug on the same cord, releasing it into the world. It creates a new cord to replace that one.
        /// If there are no controllers in the area, then the plug retracts and the jack goes back to the Free state
        /// </summary>
        /// <returns></returns>
        private IEnumerator WaitingForGrab() {
            cord.gameObject.SetActive(true);
            StartCoroutine(ExtendPlug(primaryPlug, PlugStart.position + PlugStart.forward * ExtendDistance));

            while (state == State.WaitingForGrab) {
                if (primaryPlug.GetComponent<VRTK_InteractableObject>().IsGrabbed()) {
                    StartCoroutine(ExtendPlug(secondaryPlug, PlugConnectionPoint.position));
                    secondaryPlug.DestinationJack = this;
                    secondaryPlug.transform.rotation *= Quaternion.AngleAxis(180.0f, secondaryPlug.transform.up);

                    CreateCord();
                    state = State.Blocked;
                }else if(nearbyControllers.Count == 0) {
                    StartCoroutine(RetractPlug(primaryPlug.GetComponent<Plug>()));
                    state = State.Free;
                }

                yield return new WaitForEndOfFrame();
            }

        }

        /// <summary>
        /// A blocked jack has no behaviour. It waits for the blocking plug to set the state back to Free.
        /// </summary>
        /// <returns></returns>
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

        public void Connect(Cord connectedCord, LinkedListNode<BranchNode> plugNodeInCord) {
            if(PlugConnected != null) {
                PlugConnected(this, new JackEventArgs(connectedCord, plugNodeInCord));
            }
        }

        public void Disconnect(Cord connectedCord, LinkedListNode<BranchNode> plugNodeInCord) {
            if(PlugDisconnected != null) {
                PlugDisconnected(this, new JackEventArgs(connectedCord, plugNodeInCord));
            }
        }
    }
}