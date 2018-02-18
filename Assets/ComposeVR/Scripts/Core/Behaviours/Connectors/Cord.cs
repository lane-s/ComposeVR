using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ComposeVR {
    [RequireComponent(typeof(LineRenderer))]
    public sealed class Cord : MonoBehaviour {

        public float SegmentLength = 0.005f;
        public float RelaxAmount = 0.05f;
        public float RelaxTime = 10.0f;
        public float PruneDistance = 0.01f;

        public int RelaxIterationsPerFrame = 2;

        private Transform A;
        private Transform B;

        private LineRenderer lineRenderer;
        private List<Vector3> path;
        private Vector3 lastPos;
        private float timeRelaxed;
        private Color cordColor;

        private bool updateLine;


        void Awake() {
            lineRenderer = GetComponent<LineRenderer>();
            path = new List<Vector3>();

            cordColor = new Color(UnityEngine.Random.value, UnityEngine.Random.value, UnityEngine.Random.value);

            if (lineRenderer) {
                lineRenderer.material.SetColor("_TintColor", cordColor);
            }

            timeRelaxed = RelaxTime;
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
                }

                lastPos = B.position;

            }
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

        public void SetCordEnds(Transform start, Transform end) {
            A = start;
            B = end;
        }
    }
}
