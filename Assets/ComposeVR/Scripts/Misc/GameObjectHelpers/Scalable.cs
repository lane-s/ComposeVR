using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ComposeVR {
    public class Scalable : MonoBehaviour {

        public Vector3 TargetScale;
        public float Smooth;
        public float CloseEnoughScale = 0.0f;

        public bool TargetReached {
            get { return transform.localScale == TargetScale; }
        }

        private const float epsilon = 0.05f;

        private void Awake() {
            if (TargetScale == Vector3.zero) {
                TargetScale = transform.localScale;
            }
        }

        // Update is called once per frame
        void Update() {

            if (transform.localScale != TargetScale) {
                transform.localScale = Vector3.Lerp(transform.localScale, TargetScale, Time.deltaTime * Smooth);

                float workingEpsilon = CloseEnoughScale == 0.0f ? epsilon : CloseEnoughScale;
                if((transform.localScale - TargetScale).magnitude < workingEpsilon) {
                    transform.localScale = TargetScale;
                }
            }
        }
    }

}