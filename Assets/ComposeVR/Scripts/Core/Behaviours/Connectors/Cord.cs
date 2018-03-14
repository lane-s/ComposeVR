using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using VRTK;

namespace ComposeVR {
    [RequireComponent(typeof(LineRenderer))]
    public sealed class Cord : MonoBehaviour {

        public Transform BranchHandlePrefab;
        public ParticleSystem CordCollapseParticleSystemPrefab;
        public Texture FlowTexture;

        public float SegmentLength = 0.005f;
        public float RelaxAmount = 0.05f;
        public float RelaxTime = 10.0f;
        public float PruneDistance = 0.01f;
        public float Flow = 1.0f;
        public float OnEmission = 0.55f;
        public float OffEmission = 0.1f;

        public int RelaxIterationsPerFrame = 2;

        private const float BOUNDING_BOX_PADDING = 0.01f;
        private Transform A;
        private Transform B;

        private LineRenderer lineRenderer;
        private List<Vector3> path;
        private Vector3 lastPos;
        private float timeRelaxed;
        private Color cordColor;

        private int collapseStart;
        private int collapseEnd;
        private bool collapsing = false;
        private float collapseAccel = 0.01f;
        private const float COLLAPSE_ROTATION_SPEED = 20f;
        private Vector3 plugALookVector;
        private Vector3 plugBLookVector;
        private ParticleSystem collapseParticles;
        private const int COLLAPSE_PARTICLE_FIRE_COUNT = 20; //How small should the cord be when the collapse particle system fires

        private bool updateLine;

        private BoxCollider boundingBox;
        private List<Transform> nearbyControllers;
        private List<BranchHandle> branchHandles;

        private bool allowBranching;

        void Awake() {
            lineRenderer = GetComponent<LineRenderer>();

            path = new List<Vector3>();

            SetColor(new Color(UnityEngine.Random.value, UnityEngine.Random.value, UnityEngine.Random.value));

            timeRelaxed = RelaxTime;

            SimpleTrigger boundingBoxTrigger = GetComponentInChildren<SimpleTrigger>();
            boundingBoxTrigger.TriggerEnter += OnControllerEnterArea;
            boundingBoxTrigger.TriggerExit += OnControllerLeaveArea;

            nearbyControllers = new List<Transform>();
            branchHandles = new List<BranchHandle>();

            lineRenderer.material.SetFloat("_EmissionGain", OffEmission);
            collapseParticles = Instantiate(CordCollapseParticleSystemPrefab);
            collapseParticles.gameObject.SetActive(false);
        }

        // Update is called once per frame
        void Update() {
            if (A != null && B != null && !collapsing) {
                //Start a path from the origin
                if (path.Count == 0) {
                    path.Add(A.position);
                }

                if (!lastPos.Equals(B.position)) {

                    updateLine = false;

                    //Extend path if the end moves
                    if (path.Count > 0) {
                        if (Vector3.Distance(path.Last(), B.position) > SegmentLength) {
                            path.Add(B.position);
                            updateLine = true;
                            timeRelaxed = 0;
                        }
                    }
                }

                //Add on to the beginning of the cord if the start point is not at the beginning of the cord path
                if (A.gameObject.activeSelf && path[0] != A.position) {
                    if (Vector3.Distance(path[0], A.position) > SegmentLength) {
                        path.Insert(0, A.position);
                        updateLine = true;
                        timeRelaxed = 0;
                    }
                }

                if (timeRelaxed < RelaxTime) {
                    timeRelaxed += Time.deltaTime;

                    for (int i = 0; i < RelaxIterationsPerFrame; i++) {
                        RelaxPath();
                    }

                    updateLine = true;
                }

                if (updateLine) {
                    UpdateLine();
                    UpdateBoundingBox();
                }

                if (nearbyControllers.Count > 0) {
                    UpdateBranchHandles();
                }

                lastPos = B.position;

                FlowEffect();
            }
            else if(collapsing) {
                if(collapseStart >= collapseEnd || collapseStart >= path.Count - 1 || collapseEnd <= 1) {
                    OnCollapseFinished();
                }
                else {
                    //Only render the portion of the path that has not yet been collapsed
                    lineRenderer.positionCount = collapseEnd - collapseStart + 1;
                    Vector3[] collapsingPath = new Vector3[lineRenderer.positionCount];

                    int j = 0;
                    for(int i = collapseStart; i <= collapseEnd; i++) {
                        collapsingPath[j] = path[i];
                        j += 1;
                    }

                    lineRenderer.SetPositions(collapsingPath);

                    //Rotate plugs to point away from their movement direction
                    Plug plugA = GetPlugFromCordNode(A);
                    if(plugA != null) {
                        if (plugALookVector != Vector3.zero) {
                            plugA.PlugTransform.transform.rotation = Quaternion.Slerp(plugA.PlugTransform.transform.rotation, Quaternion.LookRotation(plugALookVector), COLLAPSE_ROTATION_SPEED * Time.deltaTime);
                        }
                        plugA.GetComponent<CordFollower>().Speed += collapseAccel;
                    }

                     Plug plugB = GetPlugFromCordNode(B);
                    if(plugB != null) {
                        if (plugBLookVector != Vector3.zero) {
                            plugB.PlugTransform.transform.rotation = Quaternion.Slerp(plugB.PlugTransform.transform.rotation, Quaternion.LookRotation(plugBLookVector), COLLAPSE_ROTATION_SPEED * Time.deltaTime);
                        }
                        plugB.GetComponent<CordFollower>().Speed += collapseAccel;
                    }

                    //Play the collapse particle effect when the cord is almost completely collapsed
                    if(lineRenderer.positionCount < COLLAPSE_PARTICLE_FIRE_COUNT && collapseParticles != null && !collapseParticles.gameObject.activeSelf) {
                        collapseParticles.gameObject.SetActive(true);
                        if(plugA != null && plugB != null) {
                            collapseParticles.transform.position = GetPointAtIndex((collapseEnd - collapseStart) / 2 + collapseStart);
                        }else if(plugA != null) {
                            collapseParticles.transform.position = GetPointAtIndex(path.Count - 1);
                        }
                        else {
                            collapseParticles.transform.position = GetPointAtIndex(0);
                        }

                        collapseParticles.Play();
                    }
                }
            }
        }

        float flowTextureOffset = 0;
        private void FlowEffect() {
            flowTextureOffset = Mathf.Repeat(flowTextureOffset - Time.deltaTime*Flow, 1);
            lineRenderer.material.mainTextureOffset = new Vector2(flowTextureOffset, 0);
        }

        private BoxCollider GetBoundingBox() {
            if (boundingBox == null) {
                boundingBox = GetComponentInChildren<BoxCollider>();
            }
            return boundingBox;
        }

        private void RelaxPath() {
            for(int i = 0; i < path.Count; i++) {
                if(i != 0 && i != path.Count - 1) {
                    //Take the average of the current point and the adjacent points on the path
                    Vector3 targetPosition = (path[i - 1] + path[i] + path[i + 1]) / 3;

                    //Smoothly move towards this position
                    path[i] = Vector3.Lerp(path[i], targetPosition, RelaxAmount);
                }
            }

            //Prune unecessary points
            for(int i = 0; i < path.Count; i++) {
                if(i != 0 && i != path.Count - 1) {
                    if(Vector3.Distance(path[i - 1], path[i]) < PruneDistance) {
                        path.RemoveAt(i);
                    }
                }
            }

        }

        private void UpdateLine() {
            lineRenderer.positionCount = path.Count;
            lineRenderer.SetPositions(path.ToArray());
        }

        private void UpdateBoundingBox() {
            float maxX, maxY, maxZ, minX, minY, minZ;

            maxX = maxY = maxZ = Mathf.NegativeInfinity;
            minX = minY = minZ = Mathf.Infinity;

            for(int i = 0; i < path.Count; i++) {
                minX = Mathf.Min(path[i].x, minX)-BOUNDING_BOX_PADDING/2;
                minY = Mathf.Min(path[i].y, minY)-BOUNDING_BOX_PADDING/2;
                minZ = Mathf.Min(path[i].z, minZ)-BOUNDING_BOX_PADDING/2;

                maxX = Mathf.Max(path[i].x, maxX)+BOUNDING_BOX_PADDING/2;
                maxY = Mathf.Max(path[i].y, maxY)+BOUNDING_BOX_PADDING/2;
                maxZ = Mathf.Max(path[i].z, maxZ)+BOUNDING_BOX_PADDING/2;
            }

            Vector3 min = new Vector3(minX, minY, minZ);
            Vector3 max = new Vector3(maxX, maxY, maxZ);

            transform.GetChild(0).position = (min + max) / 2;
            transform.GetChild(0).SetGlobalScale(max - min);
        }

        /// <summary>
        /// For each nearby controller, update the point on the cord that is closest to the controller/
        /// 
        /// If the distance between the controller and the closest point on the cord is below a threshold, show a handle to allow the user to branch off of this cord
        /// 
        /// Currently we use a brute force method as it seems to be ok for the number of points that are on a normal cord segment. If performance of this method becomes a problem, we will have to use space partitioning to reduce the search time
        /// </summary>
        private void UpdateBranchHandles() {
            for(int i = 0; i < path.Count; i++) {
                for(int j = 0; j < nearbyControllers.Count; j++) {
                    if (branchHandles[j].GetTrackedController() != null) {
                        Vector3 diff = branchHandles[j].GetTrackedController().position - path[i];

                        if (diff.sqrMagnitude <= branchHandles[j].GetControllerSquareDistance()) {
                            branchHandles[j].SetClosestPoint(i, diff.sqrMagnitude);
                        }
                    }
                }
            }
        }

        public Vector3 GetPointAtIndex(int i) {
            if(i >= path.Count) {
                i = path.Count - 1;
            }else if(i < 0) {
                i = 0;
            }

            return path[i];
        }

        public void ConnectCord(Transform start, Transform end) {
            A = start;
            B = end;

            if (A.GetComponent<OwnedObject>()) {
                Transform owner = A.GetComponent<OwnedObject>().Owner;
                if (owner.GetComponent<Plug>()) {
                    Plug p = owner.GetComponent<Plug>();
                    p.SetCord(this);

                    if (p.IsPluggedIn()) {
                        if (p.DestinationJack.GetComponent<InputJack>()) {
                            Flow = -1;
                        }else if (p.DestinationJack.GetComponent<OutputJack>()) {
                            Flow = 1;
                        }
                    }
                }
            }

            if (B.GetComponent<OwnedObject>()) {
                Transform owner = B.GetComponent<OwnedObject>().Owner;
                if (owner.GetComponent<Plug>()) {
                    Plug p = owner.GetComponent<Plug>();
                    p.SetCord(this);

                    if (p.IsPluggedIn()) {
                        if (p.DestinationJack.GetComponent<InputJack>()) {
                            Flow = 1;
                        }else if (p.DestinationJack.GetComponent<OutputJack>()) {
                            Flow = -1;
                        }
                    }
                }
            }

            if(Flow != 0) {
                SetFlowing(true);
            }
        }

        private bool flowing;

        public void SetFlowing(bool flowing) {
            this.flowing = flowing;

            if (flowing) {
                lineRenderer.material.mainTexture = FlowTexture;
                lineRenderer.material.SetFloat("_EmissionGain", OnEmission);
            }
            else {
                lineRenderer.material.mainTexture = null;
                lineRenderer.material.SetFloat("_EmissionGain", OffEmission);
            }
            flowTextureOffset = 0;
        }

        public void AllowBranching(bool allow) {
            allowBranching = allow;
            for(int i = 0; i < branchHandles.Count; i++) {
                if (branchHandles[i] != null) {
                    branchHandles[i].gameObject.SetActive(allowBranching);
                }
            }
        }

        public float GetLength() {
            return path.Count;
        }

        private void OnControllerEnterArea(object sender, SimpleTriggerEventArgs e) {
            if (!nearbyControllers.Contains(e.other.transform)) {
                nearbyControllers.Add(e.other.transform);
                CreateBranchHandles(e.other.transform);
            }                
        }

        private void CreateBranchHandles(Transform trackedController) {
            if(nearbyControllers.Count > branchHandles.Count) {
                branchHandles.Add((Instantiate(BranchHandlePrefab) as Transform).GetComponent<BranchHandle>());
                branchHandles[branchHandles.Count - 1].SetSourceCord(this);
                branchHandles[branchHandles.Count - 1].gameObject.SetActive(allowBranching);
                branchHandles[branchHandles.Count - 1].TrackController(trackedController);
            }
        } 

        private void OnControllerLeaveArea(object sender, SimpleTriggerEventArgs e) {
            int index = nearbyControllers.IndexOf(e.other.transform);
            if (index != -1) {
                nearbyControllers.RemoveAt(index);
                Destroy(branchHandles[index].gameObject);
                branchHandles.RemoveAt(index);
            }
        }

        public void SetPath(List<Vector3> path) {
            this.path = path;
            UpdateLine();
            UpdateBoundingBox();
        }

        public List<Vector3> GetPath() {
            return path;
        }

        /// <summary>
        /// This method searches the network of cords connected to this cord in the direction determined by reverseFlow.
        /// 
        /// For a given cord, flow is positive if the cord flows from A -> B (i.e. A is the output and B is the input)
        /// Flow is negative if the cords flows from B -> A
        /// 
        /// When searching, we look for nodes that are 'downstream' from the start point. This means we only take paths with positive flow during our search. 
        /// This is reversed when searching for output jacks 
        /// 
        /// </summary>
        /// <param name="reverseFlow"></param>
        /// <param name="cordPos"></param>
        /// <returns></returns>
        public HashSet<Jack> GetConnectedJacks(bool reverseFlow, Transform start) {
            HashSet<Jack> results = new HashSet<Jack>();

            float workingFlow = Flow; 
            if (reverseFlow) {
                workingFlow = -workingFlow;
            }

            if (workingFlow > 0 && !B.Equals(start)) {

                if (B.GetComponent<BranchHandle>()) {
                    BranchHandle handle = B.GetComponent<BranchHandle>();
                    CordNode downStreamNode = handle.GetDownstreamNode(reverseFlow);
                    results.UnionWith(downStreamNode.cord.GetConnectedJacks(reverseFlow, downStreamNode.nodeInCord));

                    CordNode branchNode = handle.GetBranchNode();
                    results.UnionWith(branchNode.cord.GetConnectedJacks(reverseFlow, branchNode.nodeInCord));
                }else if (B.GetComponent<OwnedObject>()) {
                    Transform owner = B.GetComponent<OwnedObject>().Owner;

                    if (owner.GetComponent<Plug>()) {
                        Plug plug = owner.GetComponent<Plug>();

                        if(plug.DestinationJack != null) {
                            results.Add(plug.DestinationJack);
                        }
                    }
                }

            }
            else if(workingFlow < 0 && !A.Equals(start)){

                if (A.GetComponent<BranchHandle>()) {
                    BranchHandle handle = A.GetComponent<BranchHandle>();
                    CordNode downstreamNode = handle.GetDownstreamNode(reverseFlow);
                    results.UnionWith(downstreamNode.cord.GetConnectedJacks(reverseFlow, downstreamNode.nodeInCord));

                    CordNode branchNode = handle.GetBranchNode();
                    results.UnionWith(branchNode.cord.GetConnectedJacks(reverseFlow, branchNode.nodeInCord));
                }else if (A.GetComponent<OwnedObject>()) {
                    Transform owner = A.GetComponent<OwnedObject>().Owner;

                    if (owner.GetComponent<Plug>()) {
                        Plug plug = owner.GetComponent<Plug>();

                        if(plug.DestinationJack != null) {
                            results.Add(plug.DestinationJack);
                        }
                    }
                }
            }

            return results;
        }

        /// <summary>
        /// Splits the cord so that this cord contains the points before splitPoint and splitCord contains the points after splitPoint
        /// </summary>
        /// <param name="handle"></param>
        /// <param name="splitPointIndex"></param>
        /// <param name="splitCord"></param>
        /// <returns></returns>
        public CordJunction SplitByBranchHandle(BranchHandle handle, int splitPointIndex, Cord splitCord) {

            //Get the points after the splitPoint and assign them to the splitCord
            List<Vector3> splitPath = new List<Vector3>();
            for(int i = splitPointIndex; i < path.Count; i++) {
                splitPath.Add(path[i]);
            }

            splitCord.SetColor(cordColor);
            splitCord.Flow = 0;
            splitCord.ConnectCord(handle.transform, B);
            splitCord.SetPath(splitPath);
            splitCord.AllowBranching(true);

            int combinedPathLength = path.Count;

            //Exclude the points in the splitCord from this cord
            for(int i = 0; i < combinedPathLength - splitPointIndex; i++) {
                path.RemoveAt(path.Count - 1);
            }
            B = handle.transform;

            branchHandles.Remove(handle);
            CreateBranchHandles(handle.GetTrackedController());
            UpdateBoundingBox();

            CordNode JunctionA = new CordNode(this, handle.transform);
            CordNode JunctionB = new CordNode(splitCord, handle.transform);

            return new CordJunction(JunctionA, JunctionB, Flow);
        }

        public Transform GetCordStart() {
            return A;
        }

        public Transform GetCordEnd() {
            return B;
        }

        public Color GetColor() {
            return cordColor;
        }

        public void SetColor(Color color) {
            cordColor = color;
            if (lineRenderer) {
                lineRenderer.material.SetColor("_TintColor", cordColor);
            }
        }

        public void Collapse() {
            Flow = 0;
            SetFlowing(false);

            collapsing = true;

            Plug plugA = GetPlugFromCordNode(A);
            Plug plugB = GetPlugFromCordNode(B);

            collapseStart = 0;
            collapseEnd = path.Count - 1;

            if(plugA != null) {
                CordFollower followerA = plugA.GetComponent<CordFollower>();
                followerA.enabled = true;
                followerA.TeleportToPoint(0);
                followerA.SetTargetPoint(path.Count - 1);
                followerA.NextPointReached += OnStartPointConsumed;
                plugA.GetComponent<VRTK_InteractableObject>().isGrabbable = false;
            }

            if(plugB != null) {
                CordFollower followerB = plugB.GetComponent<CordFollower>();
                followerB.enabled = true;
                followerB.TeleportToPoint(path.Count - 1);
                followerB.SetTargetPoint(0);
                followerB.NextPointReached += OnEndPointConsumed;
                plugB.GetComponent<VRTK_InteractableObject>().isGrabbable = false;
            }
        }

        private void OnCollapseFinished() {

            if (A.GetComponent<BranchHandle>()) {
                A.GetComponent<BranchHandle>().MergeJunction();
            }
            else {
                Plug p = GetPlugFromCordNode(A);
                if(p != null) {
                    Destroy(p.gameObject);
                }
            }

            if (B.GetComponent<BranchHandle>()) {
                B.GetComponent<BranchHandle>().MergeJunction();
            }
            else {
                Plug p = GetPlugFromCordNode(B);
                if(p != null) {
                    Destroy(p.gameObject);
                }
            }

            DestroyCord();
        }

        public void DestroyCord() {
            foreach(BranchHandle b in branchHandles) {
                Destroy(b.gameObject);
            }

            Destroy(gameObject);
        }

        private void OnStartPointConsumed(object sender, CordFollowerEventArgs e) {
            collapseStart += 1;
            plugALookVector = -e.NextMoveVector;
        }

        private void OnEndPointConsumed(object sender, CordFollowerEventArgs e) {
            collapseEnd -= 1;
            plugBLookVector = -e.NextMoveVector;
        }

        public Plug GetPlugFromCordNode(Transform cordAttachPoint) {
            if (cordAttachPoint.GetComponent<OwnedObject>()) {
                Transform owner = cordAttachPoint.GetComponent<OwnedObject>().Owner;
                if (owner.GetComponent<Plug>()) {
                    return owner.GetComponent<Plug>();
                }
            }

            return null;    
        }

    }
}
