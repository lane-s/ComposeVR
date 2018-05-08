using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRTK;
using MathNet.Numerics;
using System;

namespace ComposeVR {
    public class NoteTriggerEventArgs : EventArgs {
        public int Velocity;
        public NoteTriggerEventArgs(int noteVelocity) {
            Velocity = noteVelocity;
        }
    }

    [RequireComponent(typeof(VRTK_ControllerEvents))]
    public class ControllerNoteTrigger : MonoBehaviour {

        public event EventHandler<NoteTriggerEventArgs> NoteTriggered;
        public event EventHandler<EventArgs> NoteReleased;
        
        public OVRInput.RawAxis1D VelocityAxis;
        public int FramesToAverage = 5;
        [Tooltip("How many frames of controller data should be recorded to determine the average trigger change over time")]

        public float SteadyTimeBeforeNoteOn = 0.001f;
        public float IgnoreTriggerThreshold = 0.01f;

        private Queue<float> lastXTriggerPos;

        private bool triggerHeld = false;
        private bool triggerStarted = false;

        private EventArgs defaultArgs;
        private NoteTriggerEventArgs noteTriggerArgs;

        private float onTime;
        private float offTime;

        private float localMaxTriggerPos = Mathf.NegativeInfinity;
        private float lastLocalIncreaseTime;

        private void Awake() {
            lastXTriggerPos = new Queue<float>();
            defaultArgs = new EventArgs();
            noteTriggerArgs = new NoteTriggerEventArgs(60);
            lastLocalIncreaseTime = Mathf.Infinity;
        }

        private void Update() {
            float triggerPos = OVRInput.Get(VelocityAxis);
            AddPoint(triggerPos);

            float averageTriggerPos = GetAverageTriggerPosOverLastXFrames();

            if (averageTriggerPos < IgnoreTriggerThreshold) {
                bool triggerWasHeld = triggerHeld;
                triggerHeld = false;
                if (triggerWasHeld) {
                    if (NoteReleased != null) {
                        NoteReleased(this, defaultArgs);
                    }
                    //Debug.Log("Note off");
                    lastLocalIncreaseTime = Mathf.Infinity;
                    localMaxTriggerPos = Mathf.NegativeInfinity;
                }
            }
            else if (!triggerHeld) {
                if(averageTriggerPos > localMaxTriggerPos + 0.05f) {
                    localMaxTriggerPos = averageTriggerPos;
                    lastLocalIncreaseTime = Time.time;
                }

                if(Time.time - lastLocalIncreaseTime > SteadyTimeBeforeNoteOn) {
                    noteTriggerArgs.Velocity = GetNoteVelocityFromTriggerPos(localMaxTriggerPos);
                    if (NoteTriggered != null) {
                        NoteTriggered(this, noteTriggerArgs);
                    }
                    onTime = Time.time;
                    //Debug.Log("Note on. Trigger pos: " + localMaxTriggerPos);
                    triggerHeld = true;
                }
            }
        }

        private void AddPoint(float triggerPos) {
            lastXTriggerPos.Enqueue(triggerPos);

            if(lastXTriggerPos.Count > FramesToAverage) {
                lastXTriggerPos.Dequeue();
            }
        }

        private float GetAverageTriggerPosOverLastXFrames() {
            float[] triggerPosArr = lastXTriggerPos.ToArray();

            float sumPos = 0;

            for(int i = 0; i < triggerPosArr.Length; i++) {
                sumPos += triggerPosArr[i];
            }

            return sumPos / triggerPosArr.Length;
        }

        private int GetNoteVelocityFromTriggerPos(float triggerPos) {
            return (int)triggerPos.Remap(IgnoreTriggerThreshold, 1.0f, 45, 120);
        }

        public int GetNoteVelocity() {
            float avgTrigger = GetAverageTriggerPosOverLastXFrames();
            return (int)avgTrigger.Remap(IgnoreTriggerThreshold, 1.0f, 50, 127);
        }

        public bool TriggerIsPressed() {
            return GetAverageTriggerPosOverLastXFrames() > IgnoreTriggerThreshold;
        }
    }
}
