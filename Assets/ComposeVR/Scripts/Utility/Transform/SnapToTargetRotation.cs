using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ComposeVR {

    public class SnapToTargetRotation : MonoBehaviour {

        public bool HasReachedTarget = false;

        private Quaternion targetRotation;

        private float speed;
        private InterpolationType interpolationType;
        private float t;
        private float startTime;
        private float totalAngularDistance;
        private float totalMoveTime;
        private Quaternion startRotation;
        private Rigidbody rb;


        // Use this for initialization
        void Awake() {
            rb = GetComponent<Rigidbody>();
            targetRotation = transform.rotation;
            t = Mathf.Infinity;
            interpolationType = InterpolationType.Exponential;
        }

        // Update is called once per frame
        void Update() {
            if(rb == null) {
                RotateToTarget();
            }
        }

        private void FixedUpdate() {
            if(rb != null) {
                RotateToTarget();
            }
        }

        private void RotateToTarget() {
            if (t <= 1) {
                Rotate();
            }
            else{
                HasReachedTarget = true;
            }
        }

        private void Rotate() {
            float elapsedTime = Time.time - startTime;
            t = elapsedTime / totalMoveTime;

            if(interpolationType == InterpolationType.Exponential) {
                t = Mathf.Pow(t, 0.5f);
            }

            Quaternion currentRotation = Quaternion.Slerp(startRotation, targetRotation, t);

            if (rb != null) {
                rb.MoveRotation(currentRotation);
            }
            else {
               transform.rotation = currentRotation; 
            }
        }

        public void SnapToTarget(Quaternion target, float rotationSpeed, InterpolationType interpolationType) {
            targetRotation = target;
            speed = rotationSpeed;
            this.interpolationType = interpolationType;

            startRotation = transform.rotation;
            totalAngularDistance = Quaternion.Angle(startRotation, targetRotation);

            if(totalAngularDistance < 0.005f) {
                t = 1;
                HasReachedTarget = true;
                return;
            }

            if(t <= 1) {
                RotateToTarget();
            }

            t = 0;
            startTime = Time.time;
            totalMoveTime = totalAngularDistance / speed;
            HasReachedTarget = false;
        }

        public void SnapToTarget(Quaternion target, float rotationSpeed) {
            SnapToTarget(target, rotationSpeed, this.interpolationType);
        }

        public void SnapToTarget(Quaternion target) {
            SnapToTarget(target, this.speed, this.interpolationType);
        }
    }

}