using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRTK;

namespace ComposeVR {

    [RequireComponent(typeof(Scalable))]
    [RequireComponent(typeof(VRTK_InteractableObject))]
    public class Miniature : MonoBehaviour {

        [Tooltip("How big should the object be at it's full size?")]
        public Vector3 FullScale = new Vector3(1, 1, 1);

        private bool released = false;
        
        void Awake() {
            GetComponent<VRTK_InteractableObject>().InteractableObjectUngrabbed += OnMiniatureUngrabbed;
        }

        public void Release() {
            GetComponent<Scalable>().enabled = true;
            GetComponent<Scalable>().TargetScale = FullScale;
            released = true;
        }

        private void OnMiniatureUngrabbed(object sender, InteractableObjectEventArgs e) {
            if (released) {
                transform.SetParent(null);
                GetComponent<VRTK_InteractableObject>().InteractableObjectUngrabbed -= OnMiniatureUngrabbed;
            }
        }
    }

}
