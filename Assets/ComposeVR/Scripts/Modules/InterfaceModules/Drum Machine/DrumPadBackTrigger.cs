using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ComposeVR;

namespace ComposeVR {

    public class DrumPadBackTrigger : MonoBehaviour {

        // Use this for initialization
        void OnTriggerEnter(Collider other) {
            if (other.GetComponent<MalletHead>()) {
                other.GetComponent<MalletHead>().enteringFromBack = true;
            }
        }

        void OnTriggerExit(Collider other) {
            if (other.GetComponent<MalletHead>()) {
                other.GetComponent<MalletHead>().enteringFromBack = false;
            }
        }
    }
}