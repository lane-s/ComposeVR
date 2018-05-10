using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRTK;

namespace ComposeVR
{
    public class CordDispenser : MonoBehaviour
    {

        public Plug PlugPrefab;
        public Cord CordPrefab;
        public SimpleTrigger ControllerDetectorPrefab;

        public Transform PlugStart;
        public Transform PlugConnectionPoint;

        public SimpleTrigger BlockerDetector;
        public Transform ControllerDetectorVolume;

        public float ExtendDistance;
        public float ExtendSpeed;
        public float ActivationWaitTime;

        private Plug primaryPlug;
        private Plug secondaryPlug;
        private Cord cord;

        private SimpleTrigger controllerDetector;
        private List<VRTK_InteractGrab> nearbyControllers;

        private int numBlockers;

        public enum State { Free, WaitingForGrab, Blocked }
        private State state;

        void Awake()
        {
            BlockerDetector.TriggerEnter += OnBlockerEnterArea;
            BlockerDetector.TriggerExit += OnBlockerLeaveArea;

            nearbyControllers = new List<VRTK_InteractGrab>();
        }

        private void Start()
        {
            controllerDetector = Instantiate(ControllerDetectorPrefab);
            controllerDetector.TriggerEnter += OnControllerEnterArea;
            controllerDetector.TriggerExit += OnControllerLeaveArea;

            CreateCord();
            state = State.Free;
            StartCoroutine(FSM());
        }

        private void OnEnable()
        {
            state = State.Blocked;
            StartCoroutine(UnblockAfterDelay());
        }

        private IEnumerator UnblockAfterDelay()
        {
            yield return new WaitForSeconds(ActivationWaitTime);
            state = State.Free;
        }

        // Update is called once per frame
        void Update()
        {
            controllerDetector.transform.position = ControllerDetectorVolume.position;
            controllerDetector.transform.rotation = ControllerDetectorVolume.rotation;

            if (Input.GetKeyDown(KeyCode.W))
            {
                Debug.Log(transform.parent.gameObject.name + " state: " + state);
            }
        }

        /// <summary>
        /// Keep track of objects that block the jack, but ignore plugs from the cord that the jack currently manages
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void OnBlockerEnterArea(object sender, SimpleTriggerEventArgs e)
        {
            Plug p = e.other.GetComponent<Plug>();
            if (p == null && e.other.GetComponent<ActorSubObject>())
            {
                p = e.other.GetComponent<ActorSubObject>().Actor.GetComponent<Plug>();
            }

            if (p != null && (p.Equals(primaryPlug) || p.Equals(secondaryPlug)))
            {
                return;
            }

            numBlockers += 1;
        }

        void OnBlockerLeaveArea(object sender, SimpleTriggerEventArgs e)
        {
            Plug p = e.other.GetComponent<Plug>();
            if (p == null && e.other.GetComponent<ActorSubObject>())
            {
                p = e.other.GetComponent<ActorSubObject>().Actor.GetComponent<Plug>();
            }

            if (p != null && (p.Equals(primaryPlug) || p.Equals(secondaryPlug)))
            {
                return;
            }

            numBlockers -= 1;
            numBlockers = Math.Max(numBlockers, 0);
        }

        public void OnBlockerDestroyed()
        {
            numBlockers -= 1;
            numBlockers = Mathf.Max(numBlockers, 0);
        }

        /// <summary>
        /// Keep track of nearby controllers
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void OnControllerEnterArea(object sender, SimpleTriggerEventArgs e)
        {
            VRTK_InteractGrab controller = e.other.GetComponentInParent<VRTK_InteractGrab>();
            if (controller != null && !nearbyControllers.Contains(controller))
            {
                nearbyControllers.Add(controller);
            }
        }

        void OnControllerLeaveArea(object sender, SimpleTriggerEventArgs e)
        {
            VRTK_InteractGrab controller = e.other.GetComponentInParent<VRTK_InteractGrab>();
            if (controller != null)
            {
                nearbyControllers.Remove(controller);
            }
        }
        /// <summary>
        /// Creates a new cord consisting of two connected plugs and a actual cord object. This is the cord that the jack will deploy when the user's controller comes near
        /// </summary>
        private void CreateCord()
        {
            primaryPlug = Instantiate(PlugPrefab, PlugStart.position, PlugStart.rotation);
            primaryPlug.name = "Primary";

            secondaryPlug = Instantiate(PlugPrefab, PlugStart.position, PlugStart.rotation);
            secondaryPlug.name = "Secondary";

            secondaryPlug.transform.SetParent(PlugStart);

            cord = Instantiate(CordPrefab).GetComponent<Cord>();
            cord.Connect(secondaryPlug.CordAttachPoint, primaryPlug.CordAttachPoint);

            cord.Flow = 1;

            if (GetComponent<PhysicalDataInput>() != null)
            {
                cord.Flow = -cord.Flow;
            }
            cord.Flowing = true;

            cord.gameObject.SetActive(false);
            primaryPlug.gameObject.SetActive(false);
            secondaryPlug.gameObject.SetActive(false);
        }

        private IEnumerator FSM()
        {
            while (true)
            {
                yield return StartCoroutine(state.ToString());
            }
        }

        /// <summary>
        /// In the Free state, the dispenser checks to see if any empty hand is nearby. If so, extend a plug
        /// 
        /// </summary>
        /// <returns></returns>
        private IEnumerator Free()
        {
            while (state == State.Free)
            {
                if (nearbyControllers.Count > 0)
                {
                    bool holdingObject = false;

                    foreach (VRTK_InteractGrab c in nearbyControllers)
                    {
                        GameObject grabbedObject = c.GetGrabbedObject();
                        if (grabbedObject != null)
                        {
                            holdingObject = true;
                            break;
                        }
                    }

                    if (!holdingObject && numBlockers == 0)
                    {
                        state = State.WaitingForGrab;
                        break;
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
        private IEnumerator WaitingForGrab()
        {
            cord.gameObject.SetActive(true);
            StartCoroutine(ExtendPlug(primaryPlug, PlugStart.position + PlugStart.forward * ExtendDistance));

            while (state == State.WaitingForGrab)
            {
                if (primaryPlug.GetComponent<VRTK_InteractableObject>().IsGrabbed())
                {

                    StartCoroutine(ExtendPlug(secondaryPlug, PlugConnectionPoint.position));
                    secondaryPlug.transform.rotation *= Quaternion.AngleAxis(180.0f, Vector3.up);

                    Debug.Log("Forcing secondary plug lock");
                    GetComponent<SocketPlugReceptacle>().ForcePlugLockAndConnect(secondaryPlug);

                    CreateCord();
                    state = State.Blocked;
                }
                else if (nearbyControllers.Count == 0)
                {
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
        private IEnumerator Blocked()
        {
            while (state == State.Blocked)
            {
                yield return new WaitForEndOfFrame();
            }
        }

        private IEnumerator ExtendPlug(Plug p, Vector3 target)
        {
            Debug.Log("Extending plug: " + p.name);
            p.gameObject.SetActive(true);
            p.transform.position = PlugStart.position;
            p.transform.rotation = PlugStart.rotation;
            p.PlugTransform.position = PlugStart.position;
            p.PlugTransform.rotation = PlugStart.rotation;

            p.GetComponent<SnapToTargetPosition>().SnapToTarget(target, ExtendSpeed);

            p.GetComponent<VRTK_InteractableObject>().isGrabbable = true;

            while (!p.GetComponent<SnapToTargetPosition>().HasReachedTarget)
            {
                yield return new WaitForEndOfFrame();
            }
            Debug.Log("Finished extending: " + p.name);
        }

        private IEnumerator RetractPlug(Plug p)
        {

            p.GetComponent<SnapToTargetPosition>().SnapToTarget(PlugStart.position, ExtendSpeed);

            p.GetComponent<VRTK_InteractableObject>().isGrabbable = false;

            while (!p.GetComponent<SnapToTargetPosition>().HasReachedTarget)
            {
                yield return new WaitForEndOfFrame();
            }

            p.gameObject.SetActive(false);

            if (primaryPlug.Equals(p))
            {
                cord.gameObject.SetActive(false);
            }

            state = State.Free;
        }

        public State GetState()
        {
            return state;
        }

        public void Block()
        {
            if (state == State.Free)
            {
                state = State.Blocked;
            }
        }

        public void Unblock()
        {
            if (state == State.Blocked)
            {
                state = State.Free;
            }
        }
    }
}
