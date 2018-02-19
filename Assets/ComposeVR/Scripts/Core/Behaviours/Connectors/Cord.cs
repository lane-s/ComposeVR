using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using VRTK;

namespace ComposeVR {
    [RequireComponent(typeof(LineRenderer))]
    public sealed class Cord : MonoBehaviour {

        public Transform branchHandlePrefab;

        public float SegmentLength = 0.005f;
        public float RelaxAmount = 0.05f;
        public float RelaxTime = 10.0f;
        public float PruneDistance = 0.01f;
        public float BranchHandleShowDistance = 0.8f;

        public int RelaxIterationsPerFrame = 2;

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
        private List<int> closestPoints;
        private List<float> smallestDistances;
        private List<Transform> branchHandles;

        void Awake() {
            lineRenderer = GetComponent<LineRenderer>();
            path = new List<Vector3>();

            cordColor = new Color(UnityEngine.Random.value, UnityEngine.Random.value, UnityEngine.Random.value);

            if (lineRenderer) {
                lineRenderer.material.SetColor("_TintColor", cordColor);
            }

            timeRelaxed = RelaxTime;

            SimpleTrigger boundingBoxTrigger = GetComponentInChildren<SimpleTrigger>();
            boundingBoxTrigger.TriggerEnter += OnControllerEnterArea;
            boundingBoxTrigger.TriggerExit += OnControllerLeaveArea;

            nearbyControllers = new List<Transform>();
            closestPoints = new List<int>();
            smallestDistances = new List<float>();
            branchHandles = new List<Transform>();
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
               if(A.gameObject.activeSelf && path[0] != A.position) {
                    if(Vector3.Distance(path[0], A.position) > SegmentLength) {
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

                if(nearbyControllers.Count > 0) {
                    UpdateClosestPoints();
                }

               for(int i = 0; i < nearbyControllers.Count; i++) {
                    if(smallestDistances[i] < BranchHandleShowDistance) {
                        branchHandles[i].gameObject.SetActive(true);
                        branchHandles[i].position = GetPointAtIndex(closestPoints[i]);
                    }
                    else {
                        branchHandles[i].gameObject.SetActive(false);
                    }
                }

                lastPos = B.position;
            }
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
                minX = Mathf.Min(path[i].x, minX);
                minY = Mathf.Min(path[i].y, minY);
                minZ = Mathf.Min(path[i].z, minZ);

                maxX = Mathf.Max(path[i].x, maxX);
                maxY = Mathf.Max(path[i].y, maxY);
                maxZ = Mathf.Max(path[i].z, maxZ);
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
        private void UpdateClosestPoints() {
            for(int j = 0; j < nearbyControllers.Count; j++) {
                Vector3 diff = nearbyControllers[j].transform.position - GetPointAtIndex(closestPoints[j]);
                smallestDistances[j] = diff.sqrMagnitude;
            }

            for(int i = 0; i < path.Count; i++) {
                for(int j = 0; j < nearbyControllers.Count; j++) {
                    Vector3 diff = nearbyControllers[j].transform.position - path[i];
                    if(diff.sqrMagnitude <= smallestDistances[j]) {
                        smallestDistances[j] = diff.sqrMagnitude;
                        closestPoints[j] = i;
                    }
                }
            }

        }

        public Vector3 GetPointAtIndex(int i) {
            if(i >= path.Count) {
                i = path.Count - 1;
            }

            if(i < 0) {
                return Vector3.positiveInfinity;
            }

            return path[i];
        }

        public void SetCordEnds(Transform start, Transform end) {
            A = start;
            B = end;
        }

        private void OnControllerEnterArea(object sender, SimpleTriggerEventArgs e) {
            if (!nearbyControllers.Contains(e.other.transform)) {
                nearbyControllers.Add(e.other.transform);
                closestPoints.Add(-1);
                smallestDistances.Add(float.PositiveInfinity);
                branchHandles.Add(Instantiate(branchHandlePrefab) as Transform);
            }                
        }

        private void OnControllerLeaveArea(object sender, SimpleTriggerEventArgs e) {
            int index = nearbyControllers.IndexOf(e.other.transform);
            if (index != -1) {
                nearbyControllers.RemoveAt(index);
                closestPoints.RemoveAt(index);
                smallestDistances.RemoveAt(index);

                Destroy(branchHandles[index].gameObject);
                branchHandles.RemoveAt(index);
            }
        }
    }
}
