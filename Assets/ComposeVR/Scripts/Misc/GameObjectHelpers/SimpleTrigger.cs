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

        public Collider other { get; set; }
    }

    public class SimpleTrigger : MonoBehaviour {

        public event EventHandler<SimpleTriggerEventArgs> TriggerEnter;
        public event EventHandler<SimpleTriggerEventArgs> TriggerExit;

        private SimpleTriggerEventArgs args;

        private void Awake() {
            args = new SimpleTriggerEventArgs(null);
        }

        void OnTriggerEnter(Collider other) {
            if (TriggerEnter != null) {
                args.other = other;
                TriggerEnter(this, args);
            }
        }

        void OnTriggerExit(Collider other) {
            if (TriggerExit != null) {
                args.other = other;
                TriggerExit(this, args);
            }
        }
    }
}