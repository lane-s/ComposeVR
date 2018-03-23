using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRTK;

namespace ComposeVR {
    public class MenuToggle : MonoBehaviour {

        bool active = false;
        public VRTK_ControllerEvents ControllerEvents;
        public VRTK_Pointer Pointer;

        // Use this for initialization
        void Awake() {
            ControllerEvents.StartMenuPressed += OnMenuButtonPressed;
        }

        private void OnMenuButtonPressed(object sender, ControllerInteractionEventArgs e) {
            active = !active;

            if(Pointer != null) {
                bool showPointer = !active;
                Pointer.enabled = showPointer;
                Pointer.GetComponent<VRTK_StraightPointerRenderer>().enabled = showPointer;
                Pointer.GetComponent<VRTK_UIPointer>().enabled = showPointer;
            }

            for(int i = 0; i < transform.childCount; i++) {
                transform.GetChild(i).gameObject.SetActive(active);
            }
        }
    }
}