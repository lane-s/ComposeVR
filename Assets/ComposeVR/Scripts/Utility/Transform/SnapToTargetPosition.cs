using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;

namespace ComposeVR {
    public class SnapToTargetPosition : MonoBehaviour {

        private Vector3 targetPosition;
        public Vector3 TargetPosition {
            get { return targetPosition;  }
            set {
                targetPosition = value;
                HasReachedTarget = false;
                fireEvent = true;
            }
        }

        public float CloseEnoughDistance;
        public float Speed;
        public event EventHandler<EventArgs> TargetReached; 
        public bool HasReachedTarget = false;

        private bool fireEvent = false;

        void Awake() {
            targetPosition = transform.position;
        }

        // Update is called once per frame
        void Update() {
            HasReachedTarget = false;

            if(Vector3.Distance(TargetPosition, transform.position) > CloseEnoughDistance) {

                if (GetComponent<Rigidbody>()) {
                    GetComponent<Rigidbody>().MovePosition(Vector3.Lerp(GetComponent<Rigidbody>().position, TargetPosition, Time.deltaTime * Speed));
                }
                else {
                    transform.position = Vector3.Lerp(transform.position, TargetPosition, Time.deltaTime * Speed);
                }

            }
            else {
                HasReachedTarget = true;
            }

            if (HasReachedTarget && fireEvent) {
                fireEvent = false;
                if(TargetReached != null) {
                    TargetReached(this, new EventArgs());
                }
            }
        }

    }

}