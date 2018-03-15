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
        private bool trackController = true;

        private const float UNGRABBABLE_TIME = 0.35f;
        private float ungrabbableTimeElapsed;
        private bool ungrabbablePeriodOver;

        // Use this for initialization
        void Awake() {
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

        public void SetBranchCord(Cord cord) {
            branchNode.cord = cord;
        }

        public Cord GetSourceCord() {
            return sourceCord;
        }

        public CordNode GetDownstreamNode(bool reverseFlow) {
            float workingFlow = cordJunction.Flow;
            if (reverseFlow) {
                workingFlow = -workingFlow;
            }

            if(cordJunction != null) {
                if(workingFlow > 0) {
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

        /// <summary>
        /// A BranchHandle exists at the intersection of three cords:
        /// The two cords in the CordJunction that was formed by splitting the source cord as well as the newly created branch cord
        /// 
        /// This method takes a cord that is about to collapse as the parameter and merges the other two cords of the handle together.
        /// </summary>
        /// <param name="collapsedCord"></param>
        public void MergeRemainingCords(Cord collapsedCord) {

            if (collapsedCord.Equals(branchNode.cord)) {
                //If the branch cord has collapsed we merge the junction and end up with the same cord that was split by the BranchHandle
                MergeJunction();
            }else {
                //Otherwise one side of the junction has collapsed so we merge the branch cord with the other side of the junction
                List<Vector3> mergedPath = new List<Vector3>();
                CordNode toMerge = collapsedCord.Equals(cordJunction.A.cord) ? cordJunction.B : cordJunction.A;

                Transform branchEnd = branchNode.nodeInCord.Equals(branchNode.cord.GetCordStart()) ? branchNode.cord.GetCordEnd() : branchNode.cord.GetCordStart();

                if (toMerge.nodeInCord.Equals(toMerge.cord.GetCordStart())) {
                    toMerge.cord.ConnectCord(branchEnd, toMerge.cord.GetCordEnd());
                    mergedPath.AddRange(branchNode.cord.GetPath());
                    mergedPath.AddRange(toMerge.cord.GetPath());
                }
                else {
                    toMerge.cord.ConnectCord(toMerge.cord.GetCordStart(), branchEnd);
                    mergedPath.AddRange(toMerge.cord.GetPath());
                    mergedPath.AddRange(branchNode.cord.GetPath());
                }

                toMerge.cord.SetPath(mergedPath);

                if (branchEnd.GetComponent<BranchHandle>()) {
                    BranchHandle startHandle = branchEnd.GetComponent<BranchHandle>();
                    startHandle.ReplaceCord(branchNode.cord, toMerge.cord);
                }
                
                branchNode.cord.DestroyCord();
            }
        }

        private Cord MergeJunction() {
            List<Vector3> mergedPath = new List<Vector3>();
            //If the branch cord is collapsing then we merge the two cords in the junction
            mergedPath.AddRange(cordJunction.A.cord.GetPath());
            mergedPath.AddRange(cordJunction.B.cord.GetPath());

            cordJunction.A.cord.ConnectCord(cordJunction.A.cord.GetCordStart(), cordJunction.B.cord.GetCordEnd());
            cordJunction.A.cord.SetPath(mergedPath);

            if (cordJunction.B.cord.GetCordEnd().GetComponent<BranchHandle>()) {
                BranchHandle endHandle = cordJunction.B.cord.GetCordEnd().GetComponent<BranchHandle>();
                endHandle.ReplaceCord(cordJunction.B.cord, cordJunction.A.cord);
            }

            cordJunction.B.cord.DestroyCord();

            return cordJunction.A.cord;
        }

        public void ReplaceCord(Cord toReplace, Cord replacement) {
            if (branchNode != null && branchNode.cord.Equals(toReplace)) {
                branchNode.cord = replacement;
            }else if (cordJunction != null && cordJunction.A != null && cordJunction.A.cord.Equals(toReplace)) {
                cordJunction.A.cord = replacement;
            }
            else if(cordJunction != null && cordJunction.B != null && cordJunction.B.cord.Equals(toReplace)){
                cordJunction.B.cord = replacement; 
            }
        }

        VRTK_InteractGrab grabber;
        private void OnGrabbed(object sender, InteractableObjectEventArgs e) {
            grabber = e.interactingObject.GetComponent<VRTK_InteractGrab>();
            if(branchNode != null) {
                VRTK_ControllerEvents controllerEvents = e.interactingObject.GetComponent<VRTK_ControllerEvents>();
                controllerEvents.ButtonOnePressed += OnDisconnectButtonPressed;
            }
            else {

                //Force the controller to let go of the branch handle
                grabber.ForceRelease();
                GetComponent<VRTK_InteractableObject>().isGrabbable = false;

                Plug branchPlug = CreatePlug();

                Cord branchCord;

                //Create a cord between the plug and the branch handle
                branchCord = Instantiate(CordPrefab).GetComponent<Cord>();
                branchCord.SetColor(sourceCord.GetColor());
                branchCord.ConnectCord(transform, branchPlug.CordAttachPoint);
                branchCord.Flow = 0;

                branchNode = new CordNode(branchCord, transform);

                Cord splitCord = Instantiate(CordPrefab).GetComponent<Cord>();
                cordJunction = sourceCord.SplitByBranchHandle(this, closestCordPointIndex, splitCord); //The source cord is split into two cords connected by a cordJunction
                
                trackController = false;
                cordFollower.enabled = false;

                StartCoroutine(GrabPlug(branchPlug, false));
            }
        }

        private void OnUngrabbed(object sender, InteractableObjectEventArgs e) {
            GetComponent<Rigidbody>().isKinematic = true;
            if(branchNode != null) {
                e.interactingObject.GetComponent<VRTK_ControllerEvents>().ButtonOnePressed -= OnDisconnectButtonPressed;
            }
        }

        private Cord merged;

        private void OnDisconnectButtonPressed(object sender, ControllerInteractionEventArgs e) {
            grabber.ForceRelease();
            GetComponent<VRTK_InteractableObject>().enabled = false;
            GetComponent<MeshRenderer>().enabled = false;
            GetComponent<Collider>().enabled = false;

            merged = MergeJunction();
            merged.AllowBranching(false);

            Plug disconnectedPlug = CreatePlug();
            if (branchNode.cord.GetCordStart().Equals(branchNode.nodeInCord)) {
                branchNode.cord.ConnectCord(disconnectedPlug.CordAttachPoint, branchNode.cord.GetCordEnd());
            }
            else {
                branchNode.cord.ConnectCord(branchNode.cord.GetCordStart(), disconnectedPlug.CordAttachPoint);
            } 

            StartCoroutine(GrabPlug(disconnectedPlug, true));
        }

        private Plug CreatePlug() {
            Plug p = Instantiate(PlugPrefab).GetComponent<Plug>();
            p.transform.position = transform.position;
            p.transform.rotation = transform.rotation;
            p.DisableSnapping();
            p.GetComponent<VRTK_InteractableObject>().ForceStopInteracting();
            return p;
        }

        private IEnumerator GrabPlug(Plug p, bool destroyOnFinished) {
            while (!p.GetComponent<VRTK_InteractableObject>().IsGrabbed()) {
                p.transform.rotation = Quaternion.LookRotation(grabber.controllerAttachPoint.transform.forward);
                p.transform.position = grabber.controllerAttachPoint.transform.position;
                grabber.AttemptGrab();
                yield return new WaitForEndOfFrame();
            }

            yield return new WaitForEndOfFrame();

            p.EnableSnapping();
            GetComponent<VRTK_InteractableObject>().isGrabbable = true;

            if (destroyOnFinished) {
                merged.AllowBranching(true);
                Destroy(this.gameObject);
            }
            yield return null;
        }
    }
}