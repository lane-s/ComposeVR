using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRTK;

namespace ComposeVR {
    public class MenuToggle : MonoBehaviour {

        bool active = false;
        public VRTK_ControllerEvents ControllerEvents;

        // Use this for initialization
        void Awake() {
            ControllerEvents.StartMenuPressed += OnMenuButtonPressed;
        }

        private void OnMenuButtonPressed(object sender, ControllerInteractionEventArgs e) {
            active = !active;

            for(int i = 0; i < transform.childCount; i++) {
                transform.GetChild(i).gameObject.SetActive(active);
            }
        }
    }
}