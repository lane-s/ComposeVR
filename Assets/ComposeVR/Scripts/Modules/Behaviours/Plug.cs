using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ComposeVR {
    [RequireComponent(typeof(LineRenderer))]
    public class Plug : MonoBehaviour {

        public Transform OriginJack;
        public float SegmentLength = 0.005f;
        public float RelaxAmount = 0.05f;
        public float PruneDistance = 0.01f;

        private Color cordColor;

        private LineRenderer lineRenderer;
        private List<Vector3> path;

        private bool pathUpdated;

        // Use this for initialization
        void Awake() {
            path = new List<Vector3>();

            lineRenderer = GetComponent<LineRenderer>();
            cordColor = new Color(Random.value, Random.value, Random.value);

            lineRenderer.material.SetColor("_TintColor", cordColor);
        }

        // Update is called once per frame
        void Update() {

            //Start a path from the origin
            if(path.Count == 0 && OriginJack != null) {
                path.Add(OriginJack.position);
            }

            pathUpdated = false;

            //Extend path if the plug moves
            if(path.Count > 0) {
                if(Vector3.Distance(path.Last(), transform.position) > SegmentLength) {
                    path.Add(transform.position);
                    pathUpdated = true;
                }
            }

            RelaxPath();
            UpdateLine();
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
    }

}