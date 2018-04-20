using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ComposeVR;

namespace ComposeVR {

    public class SimpleTriggerEventArgs : EventArgs {
        public SimpleTriggerEventArgs(Collider other) {
            this.other = other;
        }

        public Collider other { get; private set; }
    }

    public class SimpleTrigger : MonoBehaviour {

        public event EventHandler<SimpleTriggerEventArgs> TriggerEnter;
        public event EventHandler<SimpleTriggerEventArgs> TriggerExit;

        void OnTriggerEnter(Collider other) {
            if (TriggerEnter != null) {
                SimpleTriggerEventArgs args = new SimpleTriggerEventArgs(other);
                TriggerEnter(this, args);
            }
        }

        void OnTriggerExit(Collider other) {
            if (TriggerExit != null) {
                SimpleTriggerEventArgs args = new SimpleTriggerEventArgs(other);
                TriggerExit(this, args);
            }
        }
    }
}