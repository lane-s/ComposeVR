using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ComposeVR {
    public class Scalable : MonoBehaviour {

        public Vector3 TargetScale;
        public float Smooth;

        private void Awake() {
            TargetScale = transform.localScale;
        }

        // Update is called once per frame
        void Update() {

            if (transform.localScale != TargetScale) {
                transform.localScale = Vector3.Lerp(transform.localScale, TargetScale, Time.deltaTime * Smooth);
            }
        }
    }

}