using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using VRTK;
using UnityEngine.UI;
using ComposeVR;

namespace ComposeVR {

    public class ModulePlacedEventArgs : EventArgs {
        public ModulePlacedEventArgs(Transform obj) {
            this.obj = obj;
        }
        public Transform obj { get; private set; }
    }


    public class NewModuleButton : MonoBehaviour {

        public string anchorTag;

        private ModuleMenu moduleMenu;

        public Transform modulePrefab;
        public Transform modulePlaceholderPrefab;

        public event EventHandler<ModulePlacedEventArgs> ModulePlaced;

        private Transform modulePlaceholder;
        private Transform controllerAnchor;

        private bool placingObject = false;

        // Use this for initialization
        void Start() {
            moduleMenu = GameObject.FindGameObjectWithTag("ModuleMenu").GetComponent<ModuleMenu>();
            moduleMenu.mainControllerEvents.TriggerReleased += OnTriggerReleased;

            controllerAnchor = GameObject.FindGameObjectWithTag(anchorTag).transform;
        }

        // Update is called once per frame
        void Update() {
            if (placingObject) {
                modulePlaceholder.position = controllerAnchor.position;
                modulePlaceholder.rotation = controllerAnchor.rotation;
            }
        }

        public void OnNewModuleButtonPressed(Button b) {
            modulePlaceholder = Instantiate(modulePlaceholderPrefab) as Transform;
            placingObject = true;
        }

        void OnTriggerReleased(object sender, ControllerInteractionEventArgs e) {

            if (placingObject) {
                placingObject = false;

                if (modulePlaceholder.gameObject.activeInHierarchy && !modulePlaceholder.GetComponent<ModulePlaceholder>().IsBlocked()) {
                    Transform newModule = Instantiate(modulePrefab) as Transform;
                    newModule.position = modulePlaceholder.position;
                    newModule.rotation = modulePlaceholder.rotation;

                    if (ModulePlaced != null) {
                        ModulePlacedEventArgs args = new ModulePlacedEventArgs(newModule);
                        ModulePlaced(this, args);
                    }
                }
                else {
                    Debug.Log("Placement blocked");
                }

                if (modulePlaceholder.gameObject.activeInHierarchy) {
                    Destroy(modulePlaceholder.gameObject);
                }
            }
        }
    }
}