using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRTK;

namespace ComposeVR {
    [RequireComponent(typeof(VRTK_InteractableObject))]
    public class BranchHandle : MonoBehaviour {

        public Transform PlugPrefab;
        public Transform CordPrefab;

        public float ShowDistanceSquared = 0.8f;
        public int ClosestBranchPointToEnd = 12;

        private int closestPointIndex;
        private float controllerDistance;

        private Plug connectedPlug;
        private Cord sourceCord;
        private LinkedListNode<BranchNode> nodeInSourceCord;

        private Cord branchCord;
        private bool cordStartPoint = true;

        private Transform controllerToTrack;
        private bool trackController = true;


        // Use this for initialization
        void Awake() {
            connectedPlug = Instantiate(PlugPrefab).GetComponent<Plug>();
            connectedPlug.transform.parent = transform;
            connectedPlug.transform.localPosition = Vector3.zero;
            connectedPlug.transform.localRotation = Quaternion.identity;
            connectedPlug.gameObject.SetActive(false);

            GetComponent<VRTK_InteractableObject>().InteractableObjectGrabbed += OnGrabbed;
        }

        void Update() {
            if (trackController) {
                if (controllerToTrack != null) {
                    Vector3 diff = controllerToTrack.position - sourceCord.GetPointAtIndex(closestPointIndex);
                    controllerDistance = diff.sqrMagnitude;

                    if (controllerDistance < ShowDistanceSquared && closestPointIndex >= ClosestBranchPointToEnd && closestPointIndex <= sourceCord.GetLength() - ClosestBranchPointToEnd) {
                        GetComponent<MeshRenderer>().enabled = true;
                        GetComponent<Collider>().enabled = true;

                        transform.position = sourceCord.GetPointAtIndex(closestPointIndex);
                        transform.rotation = Quaternion.LookRotation(controllerToTrack.position - transform.position);
                    }
                    else {
                        GetComponent<MeshRenderer>().enabled = false;
                        GetComponent<Collider>().enabled = false;
                    }
                }
            }
            else {
                transform.position = sourceCord.GetPointAtIndex(closestPointIndex);
            }
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
            closestPointIndex = index;
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

        public void OnGrabbed(object sender, InteractableObjectEventArgs e) {
            if(branchCord != null) {
                //Replace this branch handle with a plug
            }
            else {
                VRTK_InteractGrab grabber = e.interactingObject.GetComponent<VRTK_InteractGrab>();

                //Force the controller to let go of the branch handle
                GetComponent<VRTK_InteractableObject>().ForceStopInteracting();
                GetComponent<VRTK_InteractableObject>().isGrabbable = false;

                //And grab the plug instead
                connectedPlug.gameObject.SetActive(true);
                connectedPlug.transform.SetParent(null);
                grabber.AttemptGrab();

                //Create a cord between the plug and the branch handle
                branchCord = Instantiate(CordPrefab).GetComponent<Cord>();
                branchCord.ConnectCord(transform, connectedPlug.CordAttachPoint);
                branchCord.Flow = 0;

                nodeInSourceCord = sourceCord.InsertBranchNode(this, closestPointIndex);
                trackController = false;
            }
        }
    }
}