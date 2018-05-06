﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRTK;
using ComposeVR;

namespace ComposeVR {

    [RequireComponent(typeof(MIDINoteHaptics))]
    public class Baton : MonoBehaviour {

        public float minVelocity = 1f;
        public float maxVelocity = 25.0f;

        public bool enteringFromBack;
        public bool intersectingOrb;
        public Transform controller;

        private VRTK_ControllerReference controllerReference;
        private Vector3 controllerVelocity;
        private Vector3 angularVelocity;

        private bool onCooldown = false;
        private const float cooldownTime = 0.05f;
        private int hapticNote;

        void Update() {
            VRTK_ControllerReference controllerReference = VRTK_ControllerReference.GetControllerReference(controller.gameObject);
            controllerVelocity = VRTK_DeviceFinder.GetControllerVelocity(controllerReference);
            angularVelocity = VRTK_DeviceFinder.GetControllerAngularVelocity(controllerReference);
        }

        public int GetVelocity() {
            float vel = controllerVelocity.magnitude + angularVelocity.magnitude;
            if (vel < minVelocity)
                return 0;

            Debug.Log(vel);
            return (int)Mathf.Clamp(vel.Remap(minVelocity, maxVelocity, 20, 127), 20, 127);
        }

        IEnumerator cooldown() {
            onCooldown = true;
            yield return new WaitForSeconds(cooldownTime);
            onCooldown = false;
        }

        public void StartCooldown() {
            StartCoroutine(cooldown());
        }

        public bool IsOnCooldown() {
            return onCooldown;
        }

        public void StartHapticFeedback(int hapticNote) {
            this.hapticNote = hapticNote;
            GetComponent<MIDINoteHaptics>().StartHapticFeedback(hapticNote); 
        }

        public void StopHapticFeedback(int hapticNote) {
            if(this.hapticNote == hapticNote) {
                GetComponent<MIDINoteHaptics>().StopHapticFeedback();
            }
        }
    }
}