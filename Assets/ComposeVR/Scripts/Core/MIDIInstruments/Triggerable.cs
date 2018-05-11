using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ComposeVR
{
    public class Triggerable : MonoBehaviour {

        public event EventHandler<TriggerEventArgs> TriggerStarted;
        public event EventHandler<TriggerEventArgs> TriggerEnded;

        private HashSet<object> triggerers;

        private void Awake()
        {
            triggerers = new HashSet<object>();
        }

        public void TriggerStart(object sender, TriggerEventArgs triggerEventArgs)
        {
            if (triggerers.Add(sender))
            {
                if(triggerers.Count == 1 && TriggerStarted != null)
                {
                    TriggerStarted(sender, triggerEventArgs);
                }
            }
        }

        public void TriggerEnd(object sender, TriggerEventArgs triggerEventArgs)
        {
            if (triggerers.Remove(sender))
            {
                if(triggerers.Count == 0 && TriggerEnded != null)
                {
                    TriggerEnded(sender, triggerEventArgs);
                }
            };
        }
    }
}
