﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ComposeVR;

namespace ComposeVR {
    public class ModulePlaceholderObject : MonoBehaviour {

        public LayerMask ignoreLayers;

        private int blockingCollisions = 0;
        private Color initialColor;

        private Vector3 angularVelocity = Vector3.zero;
        private float scaleVelocity = 0;

        private void Awake() {
            //TODO: Create robust placeholder system that generates a placeholder based on the given prefab
            if (GetComponent<MeshRenderer>()) {
                Material mat = GetComponent<MeshRenderer>().material;

                if (mat.HasProperty("_Color")) {
                    initialColor = mat.color;
                }
            }
            else {
                foreach (Transform child in transform) {
                    foreach (Transform nestedChild in child) {
                        if (nestedChild.GetComponent<MeshRenderer>()) {
                            Material mat = nestedChild.GetComponent<MeshRenderer>().material;
                            if (mat.HasProperty("_Color")) {
                                initialColor = mat.color;
                                break;
                            }
                        }
                    }
                }
            }
        }

        private void OnEnable() {
            blockingCollisions = 0;
            setColorRecursive(transform, initialColor);
        }

        private void Update() {
            transform.Rotate(angularVelocity * Time.deltaTime);
        }

        public void OnTriggerEnter(Collider other) {

            if (!Utility.isInLayerMask(other.gameObject.layer, ignoreLayers)) {
                blockingCollisions += 1;
            }

            if (blockingCollisions > 0) {
                setColorRecursive(transform, new Color(1, 0, 0, initialColor.a));
            }

            //Orient towards player when placed on floor
            if (other.transform.CompareTag("Floor")) {
                GameObject mainCamera = GameObject.FindGameObjectWithTag("MainCamera");
                Vector3 toCamera = transform.position - mainCamera.transform.position;
                Quaternion lookRotation = Quaternion.LookRotation(toCamera);
                transform.rotation = Quaternion.Euler(new Vector3(transform.rotation.eulerAngles.x, lookRotation.eulerAngles.y, transform.rotation.eulerAngles.z));
            }
        }

        public void OnTriggerExit(Collider other) {
            if (!Utility.isInLayerMask(other.gameObject.layer, ignoreLayers)) {
                blockingCollisions -= 1;
            }

            if (blockingCollisions == 0) {
                setColorRecursive(transform, initialColor);
            }
        }

        public bool IsBlocked() {
            return blockingCollisions > 0;
        }

        public void SetAngularVelocity(Vector3 v) {
            angularVelocity = v;
        }

        private void setColorRecursive(Transform t, Color col) {
            if (t.GetComponent<MeshRenderer>()) {
                Material mat = t.GetComponent<MeshRenderer>().material;

                if (mat.HasProperty("_Color")) {
                    t.GetComponent<MeshRenderer>().material.color = col;
                }
            }

            foreach (Transform child in t) {
                setColorRecursive(child, col);
            }
        }

    }
}