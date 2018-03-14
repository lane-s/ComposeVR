using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRTK;

namespace ComposeVR {
    public class CordNode {
        public Cord cord;
        public Transform nodeInCord;

        public CordNode(Cord cord, Transform nodeInCord) {
            this.cord = cord;
            this.nodeInCord = nodeInCord;
        }
    }
    public class CordJunction {
        public CordNode A;
        public CordNode B;

        public float Flow;

        public CordJunction(CordNode A, CordNode B, float Flow) {
            this.A = A;
            this.B = B;

            this.Flow = Flow;
        }
    }

    [RequireComponent(typeof(VRTK_InteractableObject))]
    [RequireComponent(typeof(CordFollower))]
    public class BranchHandle : MonoBehaviour {

        public Transform PlugPrefab;
        public Transform CordPrefab;

        public float ShowDistanceSquared = 0.8f;
        public float SnapSpeed = 0.5f;
        public int ClosestBranchPointToEnd = 12;

        private int closestCordPointIndex;
        private bool wasShowing;

        private float controllerDistance;
        private Cord sourceCord;    //The cord that created the BranchHandle

        private CordJunction cordJunction; //Junction where the sourceCord was split by the BranchHandle

        private CordNode branchNode;
        private bool cordStartPoint = true;

        private CordFollower cordFollower;

        private Transform controllerToTrack;
        private Plug connectedPlug;
        private bool trackController = true;

        private const float UNGRABBABLE_TIME = 0.35f;
        private float ungrabbableTimeElapsed;
        private bool ungrabbablePeriodOver;

        // Use this for initialization
        void Awake() {
            connectedPlug = Instantiate(PlugPrefab).GetComponent<Plug>();
            connectedPlug.transform.parent = transform;
            connectedPlug.transform.localPosition = Vector3.zero;
            connectedPlug.transform.localRotation = Quaternion.identity;
            connectedPlug.gameObject.SetActive(false);

            GetComponent<VRTK_InteractableObject>().InteractableObjectGrabbed += OnGrabbed;
            GetComponent<VRTK_InteractableObject>().InteractableObjectUngrabbed += OnUngrabbed;

            GetComponent<VRTK_InteractableObject>().isGrabbable = false;

            cordFollower = GetComponent<CordFollower>();
        }

        void Update() {
            if (ungrabbableTimeElapsed < UNGRABBABLE_TIME) {
                ungrabbableTimeElapsed += Time.deltaTime;
            }
            else if(!ungrabbablePeriodOver){
                GetComponent<VRTK_InteractableObject>().isGrabbable = true;
                ungrabbablePeriodOver = true;
            }

            if (trackController) {
                if (controllerToTrack != null) {
                    Vector3 diff = controllerToTrack.position - sourceCord.GetPointAtIndex(closestCordPointIndex);
                    controllerDistance = diff.sqrMagnitude;

                    if (controllerDistance < ShowDistanceSquared && closestCordPointIndex >= ClosestBranchPointToEnd && closestCordPointIndex <= sourceCord.GetLength() - ClosestBranchPointToEnd) {
                        if (!wasShowing) {
                            ShowHandle();                
                        }

                        MoveToTargetPoint();
                    }
                    else if (wasShowing) {
                        HideHandle();
                    }
                }
            }
        }

        private void ShowHandle() {
            GetComponent<MeshRenderer>().enabled = true;
            GetComponent<Collider>().enabled = true;

            cordFollower.TeleportToPoint(closestCordPointIndex);

            wasShowing = true;
        }

        private void HideHandle() {
            GetComponent<MeshRenderer>().enabled = false;
            GetComponent<Collider>().enabled = false;
            wasShowing = false;
        }

        private void MoveToTargetPoint() {
            cordFollower.SetTargetPoint(closestCordPointIndex);
            transform.rotation = Quaternion.LookRotation(controllerToTrack.position - transform.position);
        }

        public void TrackController(Transform controller) {
            controllerToTrack = controller;
        }

        public Transform GetTrackedController() {
            return controllerToTrack;
        }

        public bool IsTrackingController() {
            return controllerToTrack != null;
        }

        public void SetClosestPoint(int index, float distance) {
            closestCordPointIndex = index;
            controllerDistance = distance;
        }

        public float GetControllerSquareDistance() {
            if(controllerToTrack == null) {
                return Mathf.Infinity;
            }

            return controllerDistance;
        }

        public void SetSourceCord(Cord c) {
            sourceCord = c;
            cordFollower.SetCord(c);
        }

        public CordNode GetBranchNode() {
            return branchNode;
        }

        public Cord GetSourceCord() {
            return sourceCord;
        }

        public CordNode GetUpstreamNode() {
            if(cordJunction != null) {
                if (cordJunction.Flow > 0) {
                    return cordJunction.A;
                }
                else {
                    return cordJunction.B;
                }
            }
            Debug.LogError("Cannot get UpstreamCord for BranchHandle that has not yet split the source cord");
            return null;
        }

        public CordNode GetDownstreamNode() {
            if(cordJunction != null) {
                if(cordJunction.Flow > 0) {
                    return cordJunction.B;
                }
                else {
                    return cordJunction.A;
                }
            }
            Debug.LogError("Cannot get DownstreamCord for BranchHandle that has not yet split the source cord");
            return null;
        }

        public bool IsCordStartPoint() {
            return cordStartPoint;
        }

        VRTK_InteractGrab grabber;
        private void OnGrabbed(object sender, InteractableObjectEventArgs e) {
            if(branchNode != null) {

            }
            else {
                grabber = e.interactingObject.GetComponent<VRTK_InteractGrab>();

                //Force the controller to let go of the branch handle
                grabber.ForceRelease();
                GetComponent<VRTK_InteractableObject>().isGrabbable = false;

                //And grab the plug instead
                connectedPlug.gameObject.SetActive(true);
                connectedPlug.DisableSnapping();
                connectedPlug.transform.SetParent(null);
                connectedPlug.GetComponent<VRTK_InteractableObject>().ForceStopInteracting();

                Cord branchCord;

                //Create a cord between the plug and the branch handle
                branchCord = Instantiate(CordPrefab).GetComponent<Cord>();
                branchCord.SetColor(sourceCord.GetColor());
                branchCord.ConnectCord(transform, connectedPlug.CordAttachPoint);
                branchCord.Flow = 0;

                branchNode = new CordNode(branchCord, transform);

                //Split the original cord
                Cord splitCord = Instantiate(CordPrefab).GetComponent<Cord>();
                cordJunction = sourceCord.SplitByBranchHandle(this, closestCordPointIndex, splitCord);
                
                trackController = false;
                cordFollower.enabled = false;

                StartCoroutine(GrabPlug());

            }
        }

        private void OnUngrabbed(object sender, InteractableObjectEventArgs e) {
            GetComponent<Rigidbody>().isKinematic = true;
        }

        private IEnumerator GrabPlug() {
            while (!connectedPlug.GetComponent<VRTK_InteractableObject>().IsGrabbed()) {
                connectedPlug.transform.rotation = Quaternion.LookRotation(grabber.controllerAttachPoint.transform.forward);
                connectedPlug.transform.position = grabber.controllerAttachPoint.transform.position;
                grabber.AttemptGrab();
                yield return new WaitForEndOfFrame();
            }

            yield return new WaitForEndOfFrame();

            connectedPlug.EnableSnapping();
            GetComponent<VRTK_InteractableObject>().isGrabbable = true;
            yield return null;
        }
    }
}