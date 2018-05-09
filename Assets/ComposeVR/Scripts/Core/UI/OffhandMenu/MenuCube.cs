using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRTK;

namespace ComposeVR {
    [RequireComponent(typeof(SnapToTargetRotation))]
    public class MenuCube : MonoBehaviour {

        public VRTK_ControllerEvents MenuControllerEvents;
        public float SelectionStep = 2f;
        public float RotationSpeed = 30f;

        public float AxisChangeBeforeRotate = 0.1f;

        private SnapToTargetRotation rotationSnap;
            
        public bool PlayMode = false;

        private void Awake() {
            MenuControllerEvents.TouchpadAxisChanged += OnTouchpadAxisChanged;
            MenuControllerEvents.StartMenuPressed += OnModeChange;
            rotationSnap = GetComponent<SnapToTargetRotation>();
        }

        private void OnTouchpadAxisChanged(object sender, ControllerInteractionEventArgs e) {
            if (!rotationSnap.HasReachedTarget) {
                return;
            }

            if(e.touchpadAxis.x > AxisChangeBeforeRotate) {
                Quaternion targetRotation = transform.localRotation * Quaternion.AngleAxis(SelectionStep * 90f, Vector3.up);
                rotationSnap.SnapToTarget(targetRotation, RotationSpeed, InterpolationType.Linear);
            }else if(e.touchpadAxis.x < -AxisChangeBeforeRotate) {
                Quaternion targetRotation = transform.localRotation * Quaternion.AngleAxis(-SelectionStep * 90f, Vector3.up); 
                rotationSnap.SnapToTarget(targetRotation, RotationSpeed, InterpolationType.Linear);
            }
        }

        void OnModeChange(object sender, ControllerInteractionEventArgs e) {
            PlayMode = !PlayMode;

            if (PlayMode) {
                for(int i = 0; i < transform.childCount; i++) {
                    transform.GetChild(i).GetComponent<IDisplayable>().Hide();
                }
            }
            else {
                for(int i = 0; i < transform.childCount; i++) {
                    transform.GetChild(i).GetComponent<IDisplayable>().Display();
                }
            }
        }
    }
}

