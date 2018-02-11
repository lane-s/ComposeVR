using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using VRTK;
using VRTK.Examples;
using ComposeVR;

namespace ComposeVR {

    public class ModuleMenu : MonoBehaviour {

        public VRTK_ControllerEvents offhandControllerEvents;
        public VRTK_ControllerEvents mainControllerEvents;

        public VRTK_Pointer pointer;

        public float instrumentPlaceholderRotationSpeed = 1.0f;

        private Transform menuObject;
        private bool menuActive = true;

        private List<Transform> contextObjects;

        //Instrument menu object
        private Transform instrumentBank;

        //Objects needed to place instruments
        private Transform modulePlaceholder;
        private Transform module;

        private bool placingModule = false;

        private TCPClientObject client;

        // Use this for initialization
        void Start() {
            offhandControllerEvents.StartMenuPressed += new ControllerInteractionEventHandler(OnMenuButtonPressed);
            mainControllerEvents.TriggerReleased += new ControllerInteractionEventHandler(OnTriggerReleased);
            mainControllerEvents.TouchpadAxisChanged += new ControllerInteractionEventHandler(OnTouchpadAxisChanged);

            menuObject = transform.Find("MenuObject");
            instrumentBank = menuObject.Find("ModuleBank");

            client = GameObject.FindGameObjectWithTag("TCPClient").GetComponent<TCPClientObject>();

            contextObjects = new List<Transform>();
        }

        // Update is called once per frame
        void Update() {
            if (placingModule) {
                pointer.Toggle(true);

                if (pointer.IsStateValid()) {
                    modulePlaceholder.gameObject.SetActive(true);
                    GameObject cursor = pointer.GetComponent<VRTK_StraightPointerRenderer>().GetPointerObjects()[1];
                    modulePlaceholder.position = cursor.transform.position + new Vector3(0, modulePlaceholder.localScale.y / 2, 0);
                }
                else {
                    modulePlaceholder.gameObject.SetActive(false);
                }
            }
        }

        private void OnMenuButtonPressed(object sender, ControllerInteractionEventArgs e) {
            menuActive = !menuActive;
            menuObject.gameObject.SetActive(menuActive);

            foreach (Transform t in contextObjects) {
                t.gameObject.SetActive(!menuActive);
            }
        }

        public void OnNewModuleButtonPressed(Button b) {
            NewModuleButton n = b.GetComponent<NewModuleButton>();
            modulePlaceholder = Instantiate(n.modulePlaceholderPrefab) as Transform;
            module = n.modulePrefab;

            placingModule = true;
        }

        private void OnTriggerReleased(object sender, ControllerInteractionEventArgs e) {
            if (placingModule) {
                if (!pointer.GetComponent<VRTK_ControllerUIPointerEvents_ListenerExample>().UIHover) {
                    pointer.Toggle(false);
                }

                //If placeholder position is valid, replace with module instance
                if (modulePlaceholder.gameObject.activeSelf && !modulePlaceholder.GetComponent<ModulePlaceholderObject>().IsBlocked()) {
                    Transform newModule = Instantiate(module) as Transform;
                    newModule.transform.position = modulePlaceholder.position;
                    newModule.rotation = modulePlaceholder.rotation;
                }

                Destroy(modulePlaceholder.gameObject);

            }

            placingModule = false;
        }

        private void OnTouchpadAxisChanged(object sender, ControllerInteractionEventArgs e) {
            if (placingModule) {
                Vector3 angularVelocity = new Vector3(0, -e.touchpadAxis.x * instrumentPlaceholderRotationSpeed, 0);
                modulePlaceholder.GetComponent<ModulePlaceholderObject>().SetAngularVelocity(angularVelocity);
            }
        }

        public void AddContextObjects(List<Transform> objects) {
            contextObjects.AddRange(objects);

            if (!menuActive) {
                foreach (Transform t in contextObjects) {
                    t.gameObject.SetActive(true);
                }
            }
        }

        public void ClearContextObjects() {
            foreach (Transform t in contextObjects) {
                t.gameObject.SetActive(false);
            }
            contextObjects.Clear();
        }


        public void toggleInstrumentBank() {
            bool state = instrumentBank.gameObject.activeInHierarchy;
            instrumentBank.gameObject.SetActive(!state);
        }

        public void toggleInstrumentBank(bool state) {
            instrumentBank.gameObject.SetActive(state);
        }
    }
}