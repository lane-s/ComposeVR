using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRTK;

namespace ComposeVR {
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

        private Plug connectedPlug;
        private Cord sourceCord;
        private LinkedListNode<BranchNode> nodeInSourceCord;

        private Cord branchCord;
        private bool cordStartPoint = true;

        private CordFollower cordFollower;

        private Transform controllerToTrack;
        private bool trackController = true;

        private bool grabbable;

        // Use this for initialization
        void Awake() {
            connectedPlug = Instantiate(PlugPrefab).GetComponent<Plug>();
            connectedPlug.transform.parent = transform;
            connectedPlug.transform.localPosition = Vector3.zero;
            connectedPlug.transform.localRotation = Quaternion.identity;
            connectedPlug.gameObject.SetActive(false);

            GetComponent<VRTK_InteractableObject>().InteractableObjectGrabbed += OnGrabbed;

            GetComponent<VRTK_InteractableObject>().isGrabbable = false;
            StartCoroutine(AllowGrab());

            cordFollower = GetComponent<CordFollower>();
        }

        private IEnumerator AllowGrab() {
            yield return new WaitForSeconds(0.8f);
            GetComponent<VRTK_InteractableObject>().isGrabbable = true;
        }

        void Update() {
            if (!grabbable) {
                grabbable = true;
                StartCoroutine(AllowGrab());
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
            else {
                MoveToTargetPoint();
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

        public Cord GetBranchCord() {
            return branchCord;
        }

        public Cord GetSourceCord() {
            return sourceCord;
        }

        public bool IsCordStartPoint() {
            return cordStartPoint;
        }

        public LinkedListNode<BranchNode> GetNodeInSourceCord() {
            return nodeInSourceCord;
        }

        VRTK_InteractGrab grabber;
        public void OnGrabbed(object sender, InteractableObjectEventArgs e) {
            if(branchCord != null) {
                //Replace this branch handle with a plug
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

                //Create a cord between the plug and the branch handle
                branchCord = Instantiate(CordPrefab).GetComponent<Cord>();
                branchCord.SetColor(sourceCord.GetColor());
                branchCord.ConnectCord(transform, connectedPlug.CordAttachPoint);
                branchCord.Flow = 0;

                nodeInSourceCord = sourceCord.InsertBranchNode(this, closestCordPointIndex);
                trackController = false;

                StartCoroutine(GrabPlug());
            }
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
            yield return null;
        }
    }
}