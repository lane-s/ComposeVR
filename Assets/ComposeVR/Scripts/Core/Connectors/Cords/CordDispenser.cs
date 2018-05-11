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

        public Transform ControllerDetectorVolume;

        public float ExtendDistance;
        public float ExtendSpeed;
        public float ActivationWaitTime;

        private Plug primaryPlug;
        private Plug secondaryPlug;
        private Cord cord;

        private SimpleTrigger controllerDetector;
        private List<VRTK_InteractGrab> nearbyControllers;

        public GameObject primaryPlugAttach;
        public GameObject secondaryPlugAttach;

        public enum State { Free, WaitingForGrab, Blocked }
        private State state;

        void Awake()
        {
            nearbyControllers = new List<VRTK_InteractGrab>();
        }

        private GameObject CreatePlugAttach(string name)
        {
            GameObject plugAttach = new GameObject(name);
            plugAttach.transform.position = PlugStart.position;
            plugAttach.transform.rotation = PlugStart.rotation;
            plugAttach.transform.SetParent(transform);
            plugAttach.AddComponent<SnapToTargetPosition>();
            return plugAttach;
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
            primaryPlugAttach = CreatePlugAttach("PrimaryPlugAttach");
            secondaryPlugAttach = CreatePlugAttach("SecondaryPlugAttach");

            primaryPlug = Instantiate(PlugPrefab, PlugStart.position, PlugStart.rotation);
            primaryPlug.name = "Primary";
            primaryPlug.GetComponent<VRTK_TransformFollow>().gameObjectToFollow = primaryPlugAttach;

            secondaryPlug = Instantiate(PlugPrefab, PlugStart.position, PlugStart.rotation);
            secondaryPlug.name = "Secondary";
            secondaryPlug.GetComponent<VRTK_TransformFollow>().gameObjectToFollow = secondaryPlugAttach;

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

                    if (!holdingObject)
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
            StartCoroutine(ExtendPlug(primaryPlug, primaryPlugAttach, PlugStart.position + PlugStart.forward * ExtendDistance));

            while (state == State.WaitingForGrab)
            {
                if (primaryPlug.GetComponent<VRTK_InteractableObject>().IsGrabbed())
                {
                    Destroy(primaryPlugAttach);

                    InteractableObjectEventHandler destroyPlugAttach = null;
                    VRTK_InteractableObject secondaryPlugInteractable = secondaryPlug.GetComponent<VRTK_InteractableObject>();

                    destroyPlugAttach = (object sender, InteractableObjectEventArgs e) =>
                    {
                        Destroy(secondaryPlugAttach);
                        secondaryPlugInteractable.InteractableObjectGrabbed -= destroyPlugAttach;
                    };

                    secondaryPlugInteractable.InteractableObjectGrabbed += destroyPlugAttach;

                    StartCoroutine(ExtendPlug(secondaryPlug, secondaryPlugAttach, PlugConnectionPoint.position));
                    secondaryPlugAttach.transform.rotation *= Quaternion.AngleAxis(180.0f, Vector3.up);

                    GetComponent<SocketPlugReceptacle>().ForcePlugLockAndConnect(secondaryPlug);

                    CreateCord();
                    state = State.Blocked;
                }
                else if (nearbyControllers.Count == 0)
                {
                    StartCoroutine(RetractPlug(primaryPlug, primaryPlugAttach));
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

        private IEnumerator ExtendPlug(Plug p, GameObject plugAttach, Vector3 target)
        {
            p.gameObject.SetActive(true);
            plugAttach.transform.position = PlugStart.position;
            plugAttach.transform.rotation = PlugStart.rotation;
            p.PlugTransform.position = PlugStart.position;
            p.PlugTransform.rotation = PlugStart.rotation;

            plugAttach.GetComponent<SnapToTargetPosition>().SnapToTarget(target, ExtendSpeed);

            p.GetComponent<VRTK_InteractableObject>().isGrabbable = true;

            while (plugAttach != null && !plugAttach.GetComponent<SnapToTargetPosition>().HasReachedTarget)
            {
                yield return new WaitForEndOfFrame();
            }
        }

        private IEnumerator RetractPlug(Plug p, GameObject plugAttach)
        {

            plugAttach.GetComponent<SnapToTargetPosition>().SnapToTarget(PlugStart.position, ExtendSpeed);
            p.GetComponent<VRTK_InteractableObject>().isGrabbable = false;

            while (plugAttach != null && !plugAttach.GetComponent<SnapToTargetPosition>().HasReachedTarget)
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
