using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using VRTK;

namespace ComposeVR {
    public class BranchNode {
        public BranchHandle handle;
        public int cordPosition;

        public BranchNode(BranchHandle handle, int cordPosition) {
            this.handle = handle;
            this.cordPosition = cordPosition;
        }
    }

    [RequireComponent(typeof(LineRenderer))]
    public sealed class Cord : MonoBehaviour {

        public Transform BranchHandlePrefab;
        public Texture FlowTexture;

        public float SegmentLength = 0.005f;
        public float RelaxAmount = 0.05f;
        public float RelaxTime = 10.0f;
        public float PruneDistance = 0.01f;
        public float Flow = 1.0f;
        public float OnEmission = 0.55f;
        public float OffEmission = 0.1f;

        public int RelaxIterationsPerFrame = 2;

        private const float BOUNDING_BOX_PADDING = 0.1f;
        private Transform A;
        private Transform B;

        private LineRenderer lineRenderer;
        private List<Vector3> path;
        private Vector3 lastPos;
        private float timeRelaxed;
        private Color cordColor;

        private bool updateLine;

        private BoxCollider boundingBox;
        private List<Transform> nearbyControllers;
        private List<BranchHandle> branchHandles;

        private LinkedList<BranchNode> branches;

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

            InitializeBranchList();            
        }

        private void InitializeBranchList() {
            branches = new LinkedList<BranchNode>();

            BranchNode A = new BranchNode(null, int.MinValue);
            BranchNode B = new BranchNode(null, int.MaxValue);

            branches.AddFirst(A);
            branches.AddLast(B);
        }

        // Update is called once per frame
        void Update() {
            if (A != null && B != null) {
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
        }

        float flowVal = 0;
        private void FlowEffect() {
            flowVal = Mathf.Repeat(flowVal - Time.deltaTime*Flow, 1);
            lineRenderer.material.mainTextureOffset = new Vector2(flowVal, 0);
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

            Vector3 min = transform.InverseTransformPoint(new Vector3(minX, minY, minZ));
            Vector3 max = transform.InverseTransformPoint(new Vector3(maxX, maxY, maxZ));

            GetBoundingBox().center = (min + max) / 2;
            GetBoundingBox().size = max - min;
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
                    p.SetPlugNodeInCord(branches.First);
                }
            }

            if (B.GetComponent<OwnedObject>()) {
                Transform owner = B.GetComponent<OwnedObject>().Owner;
                if (owner.GetComponent<Plug>()) {
                    Plug p = owner.GetComponent<Plug>();
                    p.SetCord(this);
                    p.SetPlugNodeInCord(branches.Last);
                }
            }
        }

        public void SetFlowing(bool flowing) {
            if (flowing) {
                lineRenderer.material.mainTexture = FlowTexture;
                lineRenderer.material.SetFloat("_EmissionGain", OnEmission);
            }
            else {
                lineRenderer.material.mainTexture = null;
                lineRenderer.material.SetFloat("_EmissionGain", OffEmission);
            }
            flowVal = 0;
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

                if(nearbyControllers.Count > branchHandles.Count) {
                    branchHandles.Add((Instantiate(BranchHandlePrefab) as Transform).GetComponent<BranchHandle>());
                }
                
                for(int i = 0; i < branchHandles.Count; i++) {
                    if (!branchHandles[i].IsTrackingController()) {
                        branchHandles[i].TrackController(e.other.transform);
                        branchHandles[i].SetSourceCord(this);
                        branchHandles[i].gameObject.SetActive(allowBranching);
                    }
                }
            }                
        }

        private void OnControllerLeaveArea(object sender, SimpleTriggerEventArgs e) {
            int index = nearbyControllers.IndexOf(e.other.transform);
            if (index != -1) {
                nearbyControllers.RemoveAt(index);
                branchHandles[index].TrackController(null);
            }
        }

        public LinkedList<BranchNode> GetBranches() {
            return branches;
        }

        /// <summary>
        /// This method searches the network of cords connected to this cord in the direction determined by searchDownstream.
        /// 
        /// For a given cord, flow is positive if the cord flows from A -> B (i.e. A is the output and B is the input)
        /// Flow is negative if the cords flows from B -> A
        /// 
        /// Searching for output jacks means looking for endpoints which are 'downstream' from the start point. This means we only take paths with positive flow during our search. 
        /// This is reversed when searching for input jacks. 
        /// 
        /// </summary>
        /// <param name="getOutputJacks"></param>
        /// <param name="cordPos"></param>
        /// <returns></returns>
        public HashSet<Jack> GetConnectedJacks(bool searchDownstream, LinkedListNode<BranchNode> start) {
            HashSet<Jack> results = new HashSet<Jack>();

            float workingFlow; 
            if (searchDownstream) {
                workingFlow = Flow;
            }
            else {
                workingFlow = -Flow;
            }

            //Debug.Log("Node position in cord: " + start.Value.cordPosition);
            //Debug.Log("Working flow: " + workingFlow);

            if (workingFlow > 0) {

                if (B.GetComponent<BranchHandle>()) {
                    BranchHandle handle = B.GetComponent<BranchHandle>();
                    results.UnionWith(handle.GetSourceCord().GetConnectedJacks(searchDownstream, handle.GetNodeInSourceCord()));

                }else if (B.GetComponent<OwnedObject>()) {
                    Transform owner = B.GetComponent<OwnedObject>().Owner;

                    if (owner.GetComponent<Plug>()) {
                        Plug plug = owner.GetComponent<Plug>();

                        if(plug.DestinationJack != null) {
                            results.Add(plug.DestinationJack);
                        }
                    }
                }

                if(start != null) {
                    LinkedListNode<BranchNode> current = start;
                    while(current.Next != null) {
                        current = current.Next;
                        results.UnionWith(SearchBranch(current.Value, searchDownstream));
                    }
                }
            }
            else if(workingFlow < 0){

                if (A.GetComponent<BranchHandle>()) {
                    //Debug.Log("Getting handle in A");
                    BranchHandle handle = A.GetComponent<BranchHandle>();
                    results.UnionWith(handle.GetSourceCord().GetConnectedJacks(searchDownstream, handle.GetNodeInSourceCord()));

                }else if (A.GetComponent<OwnedObject>()) {
                    Transform owner = A.GetComponent<OwnedObject>().Owner;

                    if (owner.GetComponent<Plug>()) {
                        Plug plug = owner.GetComponent<Plug>();
                        //Debug.Log("Getting plug in A");

                        if(plug.DestinationJack != null) {
                            results.Add(plug.DestinationJack);
                        }
                    }
                }

                if(start != null) {
                    LinkedListNode<BranchNode> current = start;

                    while(current.Previous != null) {
                        current = current.Previous;
                        results.UnionWith(SearchBranch(current.Value, searchDownstream));
                    }
                }
            }

            return results;
        }

        private HashSet<Jack> SearchBranch(BranchNode n, bool searchDownstream) {
            if(n.handle == null) {
                return new HashSet<Jack>();
            }

            BranchHandle branchToSearch = n.handle;
            LinkedListNode<BranchNode> nextStart;

            if (branchToSearch.IsCordStartPoint()) {
                nextStart = branchToSearch.GetBranchCord().GetBranches().First;
            }
            else {
                nextStart = branchToSearch.GetBranchCord().GetBranches().Last;
            }

            return branchToSearch.GetBranchCord().GetConnectedJacks(searchDownstream, nextStart);
        }

        public LinkedListNode<BranchNode> InsertBranchNode(BranchHandle handle, int cordPosition) {

            //When inserting a new branch node, the handle becomes a persistent fixture on the wire and we need a new branch handle to follow the user's controller
            for(int i = 0; i < branchHandles.Count; i++) {
                if (branchHandles[i].Equals(handle)) {
                    BranchHandle newHandle = Instantiate(BranchHandlePrefab).GetComponent<BranchHandle>();
                    newHandle.TrackController(branchHandles[i].GetTrackedController());
                    newHandle.SetSourceCord(this);
                    branchHandles[i] = newHandle;
                    branchHandles[i].gameObject.SetActive(allowBranching);
                }
            }

            LinkedListNode<BranchNode> current = branches.First;

            while(current.Value.cordPosition < cordPosition) {
                current = current.Next;
            }

            BranchNode n = new BranchNode(handle, cordPosition);
            return branches.AddBefore(current, n);
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
    }
}
