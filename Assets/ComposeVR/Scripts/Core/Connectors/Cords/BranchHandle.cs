using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRTK;

namespace ComposeVR
{
    public class CordNode
    {
        public Cord Cord;
        public Transform transform;

        public CordNode(Cord cord, Transform nodeInCord)
        {
            this.Cord = cord;
            this.transform = nodeInCord;
        }
    }
    public class CordJunction
    {
        public CordNode A;
        public CordNode B;

        public float Flow;

        public CordJunction(CordNode A, CordNode B, float Flow)
        {
            this.A = A;
            this.B = B;

            this.Flow = Flow;
        }
    }

    public class ConnectedDataEndpoints
    {
        public HashSet<IPhysicalDataInput> Inputs;
        public List<PhysicalDataOutput> Outputs;

        public ConnectedDataEndpoints(HashSet<IPhysicalDataInput> Inputs, List<PhysicalDataOutput> Outputs)
        {
            this.Inputs = Inputs;
            this.Outputs = Outputs;
        }
    }

    [RequireComponent(typeof(VRTK_InteractableObject))]
    [RequireComponent(typeof(CordFollower))]
    public class BranchHandle : MonoBehaviour
    {

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

        private CordFollower cordFollower;
        private Transform controllerToTrack;
        private bool trackController = true;

        private const float UNGRABBABLE_TIME = 0.35f;
        private float ungrabbableTimeElapsed;
        private bool ungrabbablePeriodOver;

        private VRTK_InteractGrab grabber;
        private delegate void PlugGrabbed();

        public Transform TrackedController
        {
            get
            {
                return controllerToTrack;
            }
            set
            {
                controllerToTrack = value;
            }
        }

        public bool IsTrackingController()
        {
            return controllerToTrack != null;
        }

        public Cord SourceCord
        {
            get { return sourceCord; }
            set
            {
                sourceCord = value;
                cordFollower.SetCord(sourceCord);
            }
        }

        public CordNode BranchNode
        {
            get { return branchNode; }
        }

        // Use this for initialization
        void Awake()
        {
            GetComponent<VRTK_InteractableObject>().InteractableObjectGrabbed += OnGrabbed;
            GetComponent<VRTK_InteractableObject>().InteractableObjectUngrabbed += OnUngrabbed;

            GetComponent<VRTK_InteractableObject>().isGrabbable = false;

            cordFollower = GetComponent<CordFollower>();
        }

        private void OnGrabbed(object sender, InteractableObjectEventArgs e)
        {
            grabber = e.interactingObject.GetComponent<VRTK_InteractGrab>();
            if (branchNode != null)
            {
                VRTK_ControllerEvents controllerEvents = e.interactingObject.GetComponent<VRTK_ControllerEvents>();
                controllerEvents.ButtonOnePressed += OnDisconnectButtonPressed;
            }
            else
            {

                //Force the controller to let go of the branch handle
                grabber.ForceRelease();
                GetComponent<VRTK_InteractableObject>().isGrabbable = false;

                Plug branchPlug = CreatePlug();
                Cord branchCord;

                //Create a cord between the plug and the branch handle
                branchCord = Instantiate(CordPrefab).GetComponent<Cord>();
                branchCord.Color = sourceCord.Color;
                branchCord.Connect(transform, branchPlug.CordAttachPoint);
                branchCord.Flow = 0;
                branchNode = new CordNode(branchCord, transform);

                cordJunction = SplitCord(sourceCord);
                StopMovementAlongCord();

                grabber.ForceGrab(branchPlug.GetComponent<VRTK_InteractableObject>(), () =>
                {
                    GetComponent<VRTK_InteractableObject>().isGrabbable = true;
                    branchPlug.EnableSnapping();
                });
            }
        }

        private CordJunction SplitCord(Cord cord)
        {
            Cord splitCord = Instantiate(CordPrefab).GetComponent<Cord>();
            return cord.SplitByBranchHandle(this, closestCordPointIndex, splitCord);
        }

        private void StopMovementAlongCord()
        {
            trackController = false;
            cordFollower.enabled = false;
        }

        private void OnUngrabbed(object sender, InteractableObjectEventArgs e)
        {
            GetComponent<Rigidbody>().isKinematic = true;
            if (branchNode != null)
            {
                e.interactingObject.GetComponent<VRTK_ControllerEvents>().ButtonOnePressed -= OnDisconnectButtonPressed;
            }
        }

        private void OnDisconnectButtonPressed(object sender, ControllerInteractionEventArgs e)
        {
            grabber.ForceRelease();
            GetComponent<VRTK_InteractableObject>().enabled = false;
            GetComponent<MeshRenderer>().enabled = false;
            GetComponent<Collider>().enabled = false;

            DisconnectBranchFromJunction();

            sourceCord = MergeJunction();
            sourceCord.AllowBranching(false);

            Plug disconnectedPlug = CreatePlug();
            if (branchNode.Cord.StartNode.Equals(branchNode.transform))
            {
                branchNode.Cord.Connect(disconnectedPlug.CordAttachPoint, branchNode.Cord.EndNode);
            }
            else
            {
                branchNode.Cord.Connect(branchNode.Cord.StartNode, disconnectedPlug.CordAttachPoint);
            }

            grabber.ForceGrab(disconnectedPlug.GetComponent<VRTK_InteractableObject>(), () =>
            {
                disconnectedPlug.EnableSnapping();
                sourceCord.AllowBranching(true);
                Destroy(this.gameObject);
            });
        }

        void Update()
        {
            if (ungrabbableTimeElapsed < UNGRABBABLE_TIME)
            {
                ungrabbableTimeElapsed += Time.deltaTime;
            }
            else if (!ungrabbablePeriodOver)
            {
                GetComponent<VRTK_InteractableObject>().isGrabbable = true;
                ungrabbablePeriodOver = true;
            }

            if (trackController)
            {
                if (controllerToTrack != null)
                {
                    Vector3 diff = controllerToTrack.position - sourceCord.GetPathPointAtIndex(closestCordPointIndex);
                    controllerDistance = diff.sqrMagnitude;

                    if (controllerDistance < ShowDistanceSquared && closestCordPointIndex >= ClosestBranchPointToEnd && closestCordPointIndex <= sourceCord.NumPoints - ClosestBranchPointToEnd)
                    {
                        if (!wasShowing)
                        {
                            ShowHandle();
                        }

                        MoveToTargetPoint();
                    }
                    else if (wasShowing)
                    {
                        HideHandle();
                    }
                }
            }
        }

        private void ShowHandle()
        {
            GetComponent<MeshRenderer>().enabled = true;
            GetComponent<Collider>().enabled = true;

            cordFollower.TeleportToPoint(closestCordPointIndex);

            wasShowing = true;
        }

        private void HideHandle()
        {
            GetComponent<MeshRenderer>().enabled = false;
            GetComponent<Collider>().enabled = false;
            wasShowing = false;
        }

        private void MoveToTargetPoint()
        {
            cordFollower.SetTargetPoint(closestCordPointIndex);
            transform.rotation = Quaternion.LookRotation(controllerToTrack.position - transform.position);
        }

        public void SetClosestPoint(int index, float distance)
        {
            closestCordPointIndex = index;
            controllerDistance = distance;
        }

        public float GetControllerSquareDistance()
        {
            if (controllerToTrack == null)
            {
                return Mathf.Infinity;
            }

            return controllerDistance;
        }

        public CordNode GetDownstreamJunctionNode(bool reverseFlow)
        {
            float workingFlow = cordJunction.Flow;
            if (reverseFlow)
            {
                workingFlow = -workingFlow;
            }

            if (cordJunction != null)
            {
                if (workingFlow > 0)
                {
                    return cordJunction.B;
                }
                else
                {
                    return cordJunction.A;
                }
            }
            Debug.LogError("Cannot get DownstreamCord for BranchHandle that has not yet split the source cord");
            return null;
        }

        /// <summary>
        /// A BranchHandle exists at the intersection of three cords:
        /// The two cords in the CordJunction that was formed by splitting the source cord as well as the newly created branch cord
        /// 
        /// This method takes a cord that is about to collapse as the parameter and merges the other two cords of the handle together.
        /// </summary>
        /// <param name="collapsedCord"></param>
        public void MergeRemainingCords(Cord collapsedCord)
        {

            if (collapsedCord.Equals(branchNode.Cord))
            {
                //If the branch cord has collapsed we merge the junction and end up with the same cord that was split by the BranchHandle
                MergeJunction();
            }
            else
            {
                //Otherwise one side of the junction has collapsed so we merge the branch cord with the other side of the junction
                List<Vector3> mergedPath = new List<Vector3>();
                CordNode toMerge = collapsedCord.Equals(cordJunction.A.Cord) ? cordJunction.B : cordJunction.A;

                Transform branchEnd = branchNode.transform.Equals(branchNode.Cord.StartNode) ? branchNode.Cord.EndNode : branchNode.Cord.StartNode;

                if (toMerge.transform.Equals(toMerge.Cord.StartNode))
                {
                    toMerge.Cord.Connect(branchEnd, toMerge.Cord.EndNode);
                    mergedPath.AddRange(branchNode.Cord.Path);
                    mergedPath.AddRange(toMerge.Cord.Path);
                }
                else
                {
                    toMerge.Cord.Connect(toMerge.Cord.StartNode, branchEnd);
                    mergedPath.AddRange(toMerge.Cord.Path);
                    mergedPath.AddRange(branchNode.Cord.Path);
                }

                toMerge.Cord.Path = mergedPath;

                if (branchEnd.GetComponent<BranchHandle>())
                {
                    BranchHandle startHandle = branchEnd.GetComponent<BranchHandle>();
                    startHandle.ReplaceCord(branchNode.Cord, toMerge.Cord);
                }

                branchNode.Cord.DestroyCord();
            }
        }

        private Cord MergeJunction()
        {
            List<Vector3> mergedPath = new List<Vector3>();
            //If the branch cord is collapsing then we merge the two cords in the junction
            mergedPath.AddRange(cordJunction.A.Cord.Path);
            mergedPath.AddRange(cordJunction.B.Cord.Path);

            cordJunction.A.Cord.Connect(cordJunction.A.Cord.StartNode, cordJunction.B.Cord.EndNode);
            cordJunction.A.Cord.Path = mergedPath;

            if (cordJunction.B.Cord.EndNode.GetComponent<BranchHandle>())
            {
                BranchHandle endHandle = cordJunction.B.Cord.EndNode.GetComponent<BranchHandle>();
                endHandle.ReplaceCord(cordJunction.B.Cord, cordJunction.A.Cord);
            }

            cordJunction.B.Cord.DestroyCord();

            return cordJunction.A.Cord;
        }

        public void ReplaceCord(Cord toReplace, Cord replacement)
        {
            if (branchNode != null && branchNode.Cord.Equals(toReplace))
            {
                branchNode.Cord = replacement;
            }
            else if (cordJunction != null && cordJunction.A != null && cordJunction.A.Cord.Equals(toReplace))
            {
                cordJunction.A.Cord = replacement;
            }
            else if (cordJunction != null && cordJunction.B != null && cordJunction.B.Cord.Equals(toReplace))
            {
                cordJunction.B.Cord = replacement;
            }
        }

        private Plug CreatePlug()
        {
            Plug p = Instantiate(PlugPrefab).GetComponent<Plug>();
            p.transform.position = transform.position;
            p.transform.rotation = transform.rotation;
            p.DisableSnapping();
            p.GetComponent<VRTK_InteractableObject>().ForceStopInteracting();
            return p;
        }

        /// <summary>
        /// When a plug begins touching the handle, see if it is being held by a controller. 
        /// If it is, subscribe to button presses from the controller
        /// </summary>
        /// <param name="collision"></param>
        private void OnTriggerEnter(Collider other)
        {
            Plug touchingPlug = other.transform.GetComponentInActor<Plug>();

            if (touchingPlug != null)
            {
                VRTK_InteractableObject plugInteractable = touchingPlug.GetComponent<VRTK_InteractableObject>();
                ObservePlugGrabbedState(plugInteractable, true);

                if (plugInteractable.IsGrabbed())
                {
                    GameObject grabbingController = touchingPlug.GetComponent<VRTK_InteractableObject>().GetGrabbingObject();
                    grabbingController.GetComponent<VRTK_ControllerEvents>().ButtonOnePressed += OnMergeButtonPressed;
                }
            }
        }

        /// <summary>
        /// Stop listening for button presses if the plug stops touching the handle
        /// </summary>
        /// <param name="collision"></param>
        private void OnTriggerExit(Collider other)
        {
            Plug touchingPlug = other.transform.GetComponentInActor<Plug>();

            if (touchingPlug != null)
            {
                VRTK_InteractableObject plugInteractable = touchingPlug.GetComponent<VRTK_InteractableObject>();
                ObservePlugGrabbedState(plugInteractable, false);

                if (plugInteractable.IsGrabbed())
                {
                    GameObject grabbingController = touchingPlug.GetComponent<VRTK_InteractableObject>().GetGrabbingObject();
                    grabbingController.GetComponent<VRTK_ControllerEvents>().ButtonOnePressed -= OnMergeButtonPressed;
                }
            }
        }

        private void ObservePlugGrabbedState(VRTK_InteractableObject plugInteractable, bool observe)
        {
            if (observe)
            {
                plugInteractable.InteractableObjectGrabbed += OnTouchingPlugGrabbed;
                plugInteractable.InteractableObjectUngrabbed += OnTouchingPlugUngrabbed;
            }
            else
            {
                plugInteractable.InteractableObjectGrabbed -= OnTouchingPlugGrabbed;
                plugInteractable.InteractableObjectUngrabbed -= OnTouchingPlugUngrabbed;
            }
        }

        /// <summary>
        /// Handle the edge case where a plug is grabbed or ungrabbed while touching a BranchHandle
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnTouchingPlugGrabbed(object sender, InteractableObjectEventArgs e)
        {
            e.interactingObject.GetComponent<VRTK_ControllerEvents>().ButtonOnePressed += OnMergeButtonPressed;
        }

        private void OnTouchingPlugUngrabbed(object sender, InteractableObjectEventArgs e)
        {
            e.interactingObject.GetComponent<VRTK_ControllerEvents>().ButtonOnePressed -= OnMergeButtonPressed;
        }

        private void OnMergeButtonPressed(object sender, ControllerInteractionEventArgs e)
        {
            VRTK_InteractGrab grabbingController = e.controllerReference.actual.GetComponentInChildren<VRTK_InteractGrab>();
            Plug grabbedPlug = grabbingController.GetGrabbedObject().GetComponent<Plug>();

            Cord connectedCord = grabbedPlug.ConnectedCord;
            bool replaceCordStart = connectedCord.StartNode.Equals(grabbedPlug.CordAttachPoint);

            if (replaceCordStart)
            {
                connectedCord.Connect(transform, connectedCord.EndNode);
                if (connectedCord.EndNode.GetComponent<BranchHandle>() != null)
                {
                    connectedCord.Flow = -1;
                }
            }
            else
            {
                connectedCord.Connect(connectedCord.StartNode, transform);
                if (connectedCord.StartNode.GetComponent<BranchHandle>() != null)
                {
                    connectedCord.Flow = 1;
                }
            }

            branchNode = new CordNode(connectedCord, transform);

            grabbingController.ForceRelease();
            grabbedPlug.DestroyPlug();

            cordJunction = SplitCord(sourceCord);
            StopMovementAlongCord();

            ConnectBranchToJunction();
        }

        private void ConnectBranchToJunction()
        {
            ConnectedDataEndpoints endpoints = GetDataEndpointsConnectedByHandle();

            for (int i = 0; i < endpoints.Outputs.Count; i++)
            {
                endpoints.Outputs[i].ConnectInputs(endpoints.Inputs);
            }
        }

        private void DisconnectBranchFromJunction()
        {
            ConnectedDataEndpoints endpoints = GetDataEndpointsConnectedByHandle();

            for (int i = 0; i < endpoints.Outputs.Count; i++)
            {
                endpoints.Outputs[i].DisconnectInputs(endpoints.Inputs);
            }
        }

        ConnectedDataEndpoints GetDataEndpointsConnectedByHandle()
        {
            bool branchFlowsIntoJunction = branchNode.Cord.EndNode.Equals(transform) ? branchNode.Cord.Flow > 0 : branchNode.Cord.Flow < 0;

            HashSet<PhysicalDataEndpoint> receptaclesConnectedToBranch = branchNode.Cord.GetConnectedEndpoints(branchFlowsIntoJunction, transform);
            HashSet<PhysicalDataEndpoint> receptaclesConnectedToJunction = GetDownstreamJunctionNode(!branchFlowsIntoJunction).Cord.GetConnectedEndpoints(!branchFlowsIntoJunction, transform);

            HashSet<IPhysicalDataInput> connectedInputs = new HashSet<IPhysicalDataInput>();
            List<PhysicalDataOutput> outputs = new List<PhysicalDataOutput>();

            foreach (PhysicalDataEndpoint receptacle in receptaclesConnectedToBranch)
            {
                PhysicalDataOutput output = receptacle.GetComponent<PhysicalDataOutput>();
                if (output != null)
                {
                    outputs.Add(output);
                }
                else
                {
                    PhysicalDataInput input = receptacle.GetComponent<PhysicalDataInput>();
                    if (input != null)
                    {
                        connectedInputs.UnionWith(input.GetConnectedInputs());
                    }
                }

            }

            foreach (PhysicalDataEndpoint receptacle in receptaclesConnectedToJunction)
            {
                PhysicalDataOutput output = receptacle.GetComponent<PhysicalDataOutput>();
                if (output != null)
                {
                    outputs.Add(output);
                }
                else
                {
                    PhysicalDataInput input = receptacle.GetComponent<PhysicalDataInput>();
                    if (input != null)
                    {
                        connectedInputs.UnionWith(input.GetConnectedInputs());
                    }
                }
            }

            return new ConnectedDataEndpoints(connectedInputs, outputs);
        }
    }
}