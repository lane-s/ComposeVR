using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;

namespace ComposeVR {
    public class SnapToTargetPosition : MonoBehaviour {

        private Vector3 targetPosition;

        private float Speed;
        public event EventHandler<EventArgs> TargetReached;
        public event EventHandler<EventArgs> MoveCancelled;

        public bool HasReachedTarget = false;

        public enum InterpolationType {Linear, Exponential};
        public InterpolationType interpolationType;

        private float t;
        private float startTime;
        private float totalDistanceToTarget;
        private float totalMoveTime;
        
        private Vector3 startPosition;

        private bool fireEvent = false;

        void Awake() {
            targetPosition = transform.position;
            t = Mathf.Infinity;
        }

        // Update is called once per frame
        void Update() {
            if(t <= 1) {
                Move();
            }

            if(t >= 1 && !HasReachedTarget) {
                HasReachedTarget = true;
                Debug.Log("Target Reached");
                if(TargetReached != null) {
                    TargetReached(this, new EventArgs());
                }
            }
        }

        private void Move() {
            float elapsedTime = Time.time - startTime;

            t = elapsedTime / totalMoveTime;

            if(interpolationType == InterpolationType.Exponential) {
                t = Mathf.Pow(t, 0.5f);
            }

            if (GetComponent<Rigidbody>()) {
                GetComponent<Rigidbody>().MovePosition(Vector3.Lerp(startPosition, targetPosition, t));
            }
            else {
                transform.position = Vector3.Lerp(startPosition, targetPosition, t);
            }
        }

        public void SnapToTarget(Vector3 targetPosition, float speed, InterpolationType interpolationType) {
            this.targetPosition = targetPosition;
            Speed = speed;
            this.interpolationType = interpolationType;

            startPosition = transform.position;
            totalDistanceToTarget = Vector3.Distance(startPosition, targetPosition);

            if(totalDistanceToTarget < 0.005f) {
                t = 1;
                HasReachedTarget = true;
                return;
            }

            if (t <= 1) {
                Move();
            }


            t = 0;
            startTime = Time.time;
            totalMoveTime = totalDistanceToTarget / Speed;
            HasReachedTarget = false;
        }

        public void SnapToTarget(Vector3 targetPosition, float speed) {
            SnapToTarget(targetPosition, speed, this.interpolationType);
        }

        public void SnapToTarget(Vector3 targetPosition) {
            SnapToTarget(targetPosition, this.Speed, this.interpolationType);
        }

    }

}