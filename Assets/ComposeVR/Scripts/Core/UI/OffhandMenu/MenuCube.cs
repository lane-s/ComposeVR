﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRTK;

namespace ComposeVR {
    public class MenuCube : MonoBehaviour {

        public VRTK_ControllerEvents MenuControllerEvents;
        public float SelectionStep = 2f;
        public float RotationSpeed = 30f;

        public float AxisChangeBeforeRotate = 0.1f;

        private bool rotating;
        private Quaternion targetRotation;
        private const float epsilonAngle = 1f;
            
        public bool PlayMode = false;

        private void Awake() {
            MenuControllerEvents.TouchpadAxisChanged += OnTouchpadAxisChanged;
            MenuControllerEvents.StartMenuPressed += OnModeChange;
        }

        // Update is called once per frame
        void Update () {
            if (rotating) {
                transform.localRotation = Quaternion.Slerp(transform.localRotation, targetRotation, Time.deltaTime * RotationSpeed);
                if(Quaternion.Angle(transform.localRotation, targetRotation) < epsilonAngle) {
                    transform.localRotation = targetRotation;
                    rotating = false;
                }
            }	
        }

        private void OnTouchpadAxisChanged(object sender, ControllerInteractionEventArgs e) {
            if (rotating) {
                return;
            }

            if(e.touchpadAxis.x > AxisChangeBeforeRotate) {
                rotating = true;
                targetRotation = transform.localRotation * Quaternion.AngleAxis(SelectionStep * 90f, Vector3.up);
            }else if(e.touchpadAxis.x < -AxisChangeBeforeRotate) {
                targetRotation = transform.localRotation * Quaternion.AngleAxis(-SelectionStep * 90f, Vector3.up); 
                rotating = true;
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

