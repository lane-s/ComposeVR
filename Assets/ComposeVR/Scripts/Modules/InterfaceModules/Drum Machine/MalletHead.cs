using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRTK;
using ComposeVR;

namespace ComposeVR {

    public class MalletHead : MonoBehaviour {

        public float minVelocity = 1f;
        public float maxVelocity = 25.0f;

        public bool enteringFromBack;
        public bool struckPad;
        public Transform controller;

        private VRTK_ControllerReference controllerReference;
        private Vector3 controllerVelocity;
        private Vector3 angularVelocity;

        private bool onCooldown = false;
        private const float cooldownTime = 0.05f;

        void Update() {
            VRTK_ControllerReference controllerReference = VRTK_ControllerReference.GetControllerReference(controller.gameObject);
            controllerVelocity = VRTK_DeviceFinder.GetControllerVelocity(controllerReference);
            angularVelocity = VRTK_DeviceFinder.GetControllerAngularVelocity(controllerReference);
        }

        public int GetMalletVelocity() {
            float vel = controllerVelocity.magnitude + angularVelocity.magnitude;
            if (vel < minVelocity)
                return 0;

            return (int)Mathf.Clamp(vel.Remap(minVelocity, maxVelocity, 1, 127), 0, 127);
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
    }
}