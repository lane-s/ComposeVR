using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRTK;
using ComposeVR;

namespace ComposeVR
{

    [RequireComponent(typeof(MIDINoteHaptics))]
    public class Baton : MonoBehaviour
    {
        public ControllerNoteTrigger Controller;

        private VRTK_ControllerReference controllerReference;
        private Vector3 controllerVelocity;
        private Vector3 angularVelocity;

        private int _hapticNote = 60;
        public int HapticNote
        {
            get
            {
                return _hapticNote;
            }
            set
            {
                _hapticNote = value;
            }
        }

        private HashSet<TriggerableObject> collidingTriggers;
        private HashSet<TriggerableObject> triggeredObjects;

        private TriggerEventArgs lastTriggerEventArgs;

        private void Awake()
        {
            VRTK_ControllerReference controllerReference = VRTK_ControllerReference.GetControllerReference(Controller.gameObject);
            triggeredObjects = new HashSet<TriggerableObject>();

            Controller.NoteTriggered += OnNoteTriggered;
            Controller.NoteReleased += OnNoteReleased;

            collidingTriggers = new HashSet<TriggerableObject>();
            triggeredObjects = new HashSet<TriggerableObject>();
        }

        void Update()
        {
            controllerVelocity = VRTK_DeviceFinder.GetControllerVelocity(controllerReference);
            angularVelocity = VRTK_DeviceFinder.GetControllerAngularVelocity(controllerReference);
        }

        private void OnTriggerEnter(Collider other)
        {
            TriggerableObject triggerable = other.transform.GetComponentInActor<TriggerableObject>();
            if (triggerable != null)
            {
                collidingTriggers.Add(triggerable);
                if (Controller.IsTriggerPressed())
                {
                    lastTriggerEventArgs.Velocity = Controller.GetNoteVelocity();
                    StartTriggering(triggerable, lastTriggerEventArgs);
                }
            }
        }

        private void OnTriggerExit(Collider other)
        {
            TriggerableObject triggerable = other.transform.GetComponentInActor<TriggerableObject>();
            if (triggerable != null)
            {
                collidingTriggers.Remove(triggerable);
                StopTriggering(triggerable, lastTriggerEventArgs);
            }
        }

        private void StartTriggering(TriggerableObject triggerable, TriggerEventArgs args)
        {
            if (triggeredObjects.Add(triggerable))
            {
                if(triggeredObjects.Count == 1)
                {
                    StartHapticFeedback();
                }

                triggerable.TriggerStart(this, args);
            }
        }

        private void StopTriggering(TriggerableObject triggerable, TriggerEventArgs args)
        {
            if (triggeredObjects.Remove(triggerable))
            {
                if(triggeredObjects.Count == 0)
                {
                    StopHapticFeedback();
                }

                triggerable.TriggerEnd(this, args);
            }
        }

        private void OnNoteTriggered(object sender, TriggerEventArgs e)
        {
            lastTriggerEventArgs = e;
            foreach(TriggerableObject triggerable in collidingTriggers)
            {
                StartTriggering(triggerable, e);
            }
        }

        private void OnNoteReleased(object sender, EventArgs e)
        {
            foreach(TriggerableObject triggerable in collidingTriggers)
            {
                StopTriggering(triggerable, lastTriggerEventArgs);
            }
        }

        private void StartHapticFeedback()
        {
            GetComponent<MIDINoteHaptics>().StartHapticFeedback(HapticNote);
        }

        private void StopHapticFeedback()
        {
            GetComponent<MIDINoteHaptics>().StopHapticFeedback();
        }
    }
}