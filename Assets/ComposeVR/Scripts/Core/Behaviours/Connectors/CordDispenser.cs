using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRTK;

namespace ComposeVR {
    public class CordDispenser : MonoBehaviour {

        public Plug PlugPrefab;
        public Cord CordPrefab;
        public SimpleTrigger ControllerDetectorPrefab;

        public Transform PlugStart;
        public Transform SecondaryPlugTarget;

        public SimpleTrigger BlockerDetector;
        public Transform ControllerDetectorVolume;
        public Vector3 ShrinkPlugScale;

        public float ExtendDistance;
        public float ExtendSpeed;


        private Plug primaryPlug;
        private Plug secondaryPlug;
        private Cord cord;
        
        private SimpleTrigger controllerDetector;
        private List<VRTK_InteractGrab> nearbyControllers;

        private int numBlockers;

        public enum State {Free, WaitingForGrab, Blocked}
        private State state;

        private Vector3 normalPlugScale;

        void Awake() {
            controllerDetector = Instantiate(ControllerDetectorPrefab);

            controllerDetector.TriggerEnter += OnControllerEnterArea;
            controllerDetector.TriggerExit += OnControllerLeaveArea;

            CreateCord();

            BlockerDetector.TriggerEnter += OnBlockerEnterArea;
            BlockerDetector.TriggerExit += OnBlockerLeaveArea;

            nearbyControllers = new List<VRTK_InteractGrab>();

            state = State.Free;

            StartCoroutine(FSM());
        }

        // Update is called once per frame
        void Update() {
            controllerDetector.transform.position = ControllerDetectorVolume.position;
            controllerDetector.transform.rotation = ControllerDetectorVolume.rotation;

            if (Input.GetKeyDown(KeyCode.W)) {
                Debug.Log(transform.parent.gameObject.name+" state: "+state);
            }
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
        /// Creates a new cord consisting of two connected plugs and a actual cord object. This is the cord that the jack will deploy when the user's controller comes near
        /// </summary>
        private void CreateCord() {
            primaryPlug = Instantiate(PlugPrefab, PlugStart.position, PlugStart.rotation);
            secondaryPlug = Instantiate(PlugPrefab, PlugStart.position, PlugStart.rotation);

            secondaryPlug.transform.SetParent(PlugStart);
            secondaryPlug.ShrinkCollider();

            secondaryPlug.DestinationJack = GetComponent<Jack>();

            normalPlugScale = primaryPlug.GetComponent<Plug>().PlugTransform.localScale;

            cord = Instantiate(CordPrefab).GetComponent<Cord>();
            cord.ConnectCord(secondaryPlug.CordAttachPoint, primaryPlug.CordAttachPoint);

            cord.Flow = 1;

            if(GetComponent<InputJack>() != null) {
                cord.Flow = -cord.Flow;
            }
            cord.SetFlowing(true);
                
            cord.gameObject.SetActive(false);
            primaryPlug.gameObject.SetActive(false);
            secondaryPlug.gameObject.SetActive(false);
        }

        private IEnumerator FSM() {
            while (true) {
                yield return StartCoroutine(state.ToString());
            }
        }

        /// <summary>
        /// In the Free state, the dispenser checks to see if any empty hand is nearby. If so, extend a plug
        /// 
        /// </summary>
        /// <returns></returns>
        private IEnumerator Free() {
            while (state == State.Free) {
                if(nearbyControllers.Count > 0) {
                    bool holdingObject = false;

                    foreach(VRTK_InteractGrab c in nearbyControllers) {
                        GameObject grabbedObject = c.GetGrabbedObject();
                        if(grabbedObject != null) {
                            Debug.Log("Extend blocked by " + grabbedObject.name);
                            holdingObject = true;
                            break;
                        }
                    }

                    if (!holdingObject && numBlockers == 0) {
                        state = State.WaitingForGrab;
                        break;
                    }
                    else {
                        Debug.Log("Can't extend. " + numBlockers + " blockers");
                    }
                }
                yield return new WaitForEndOfFrame();
            }
        }

        /// <summary>
        /// When the dispenser is WaitingForGrab, it checks if the plug that it extended is grabbed. If it is, it extends the other plug on the same cord, releasing it into the world. It creates a new cord to replace that one.
        /// If there are no controllers in the area, then the plug retracts and the dispenser goes back to the Free state
        /// </summary>
        /// <returns></returns>
        private IEnumerator WaitingForGrab() {
            cord.gameObject.SetActive(true);
            StartCoroutine(ExtendPlug(primaryPlug, PlugStart.position + PlugStart.forward * ExtendDistance));

            while (state == State.WaitingForGrab) {
                if (primaryPlug.GetComponent<VRTK_InteractableObject>().IsGrabbed()) {
                    StartCoroutine(ExtendPlug(secondaryPlug, SecondaryPlugTarget.position));
                    secondaryPlug.DestinationJack = GetComponent<Jack>();

                    if (GetComponent<Jack>()) {
                        GetComponent<Jack>().Block();
                    }

                    secondaryPlug.transform.rotation *= Quaternion.AngleAxis(180.0f, Vector3.up);

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
        /// A blocked dispenser waits for the blocking plug to leave
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
            p.PlugTransform.position = PlugStart.position;
            p.PlugTransform.rotation = PlugStart.rotation;

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

        public State GetState() {
            return state;
        }

        public void Block() {
            Debug.Log("Dispenser blocked by plug");
            if (state == State.Free) {
                state = State.Blocked;
            }
        }

        public void Unblock() {
            if (state == State.Blocked) {
                state = State.Free;
            }
        }
    }
}
