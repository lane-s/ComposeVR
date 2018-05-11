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
        public SimpleTrigger PlayTrigger;

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

        private HashSet<Triggerable> collidingTriggers;
        private HashSet<Triggerable> triggeredObjects;

        private TriggerEventArgs lastTriggerEventArgs;

        private void Awake()
        {
            VRTK_ControllerReference controllerReference = VRTK_ControllerReference.GetControllerReference(Controller.gameObject);
            triggeredObjects = new HashSet<Triggerable>();

            Controller.NoteTriggered += OnNoteTriggered;
            Controller.NoteReleased += OnNoteReleased;

            collidingTriggers = new HashSet<Triggerable>();
            triggeredObjects = new HashSet<Triggerable>();

            lastTriggerEventArgs = new TriggerEventArgs();

            PlayTrigger.TriggerEnter += OnPlayTriggerEnter;
            PlayTrigger.TriggerExit += OnPlayTriggerExit;
        }

        void Update()
        {
            controllerVelocity = VRTK_DeviceFinder.GetControllerVelocity(controllerReference);
            angularVelocity = VRTK_DeviceFinder.GetControllerAngularVelocity(controllerReference);
        }

        private void OnPlayTriggerEnter(object sender, SimpleTriggerEventArgs e)
        {
            Triggerable triggerable = e.other.transform.GetComponentInActor<Triggerable>();
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

        private void OnPlayTriggerExit(object sender, SimpleTriggerEventArgs e)
        {
            Triggerable triggerable = e.other.transform.GetComponentInActor<Triggerable>();
            if (triggerable != null)
            {
                collidingTriggers.Remove(triggerable);
                StopTriggering(triggerable, lastTriggerEventArgs);
            }
        }

        private void StartTriggering(Triggerable triggerable, TriggerEventArgs args)
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

        private void StopTriggering(Triggerable triggerable, TriggerEventArgs args)
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
            foreach(Triggerable triggerable in collidingTriggers)
            {
                StartTriggering(triggerable, e);
            }
        }

        private void OnNoteReleased(object sender, EventArgs e)
        {
            StopTriggeringAll();
        }

        private void StopTriggeringAll()
        {
            HashSet<Triggerable> triggeredObjectsCopy = new HashSet<Triggerable>(triggeredObjects);
            foreach(Triggerable triggerable in triggeredObjectsCopy)
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

        public void OnPlayModeEntered()
        {
            transform.GetChild(0).gameObject.SetActive(true);
        }

        public void OnPlayModeExited()
        {
            StopTriggeringAll();
            transform.GetChild(0).gameObject.SetActive(false);
        }
    }
}