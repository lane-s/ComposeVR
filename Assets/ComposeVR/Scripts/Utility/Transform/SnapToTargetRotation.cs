using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ComposeVR {

    public class SnapToTargetRotation : MonoBehaviour {

        private Quaternion targetRotation;
        public Quaternion TargetRotation {
            get { return targetRotation; }
            set {
                targetRotation = value;
                HasReachedTarget = false;
            }
        }


        public float Speed;
        public float CloseEnoughAngle;
        public bool HasReachedTarget;

        // Use this for initialization
        void Awake() {
            targetRotation = transform.rotation;
        }

        // Update is called once per frame
        void Update() {
            if(Quaternion.Angle(transform.rotation, TargetRotation) > CloseEnoughAngle) {
                transform.rotation = Quaternion.Slerp(transform.rotation, TargetRotation, Time.deltaTime * Speed);
                HasReachedTarget = false;
            }
            else {
                HasReachedTarget = true;
            }
        }
    }

}