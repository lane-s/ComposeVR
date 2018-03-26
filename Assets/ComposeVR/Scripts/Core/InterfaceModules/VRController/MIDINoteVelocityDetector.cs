using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRTK;
using MathNet.Numerics;
using System;

namespace ComposeVR {
    [RequireComponent(typeof(VRTK_ControllerEvents))]
    public class MIDINoteVelocityDetector : MonoBehaviour {

        public OVRInput.RawAxis1D VelocityAxis;
        public int VelocityFrames = 5;
        [Tooltip("How many frames of controller data should be recorded to determine the note velocity")]

        private Queue<double> time;
        private Queue<double> triggerPos;

        private int currentFrame = 0;
        private float lastTriggerPos = 1.0f;

        private void Awake() {
            time = new Queue<double>();
            triggerPos = new Queue<double>();
        }

        private void Update() {
            float axisPos = GetTriggerAxis();
            if(axisPos == 0.0f) {
                time.Clear();
                triggerPos.Clear();
            }

            addPoint(axisPos);
        }

        private float GetTriggerAxis() {
            return OVRInput.Get(VelocityAxis);
        }

        private void addPoint(float axisPos) {
            time.Enqueue(Time.time);
            triggerPos.Enqueue(axisPos);

            if(time.Count > VelocityFrames) {
                time.Dequeue();
                triggerPos.Dequeue();
            }
        }

        public int GetNoteVelocity() {
            addPoint(GetTriggerAxis());

            Tuple<double, double> linearFit = Fit.Line(time.ToArray(), triggerPos.ToArray());
            double yIntercept = linearFit.Item1;
            double slope = linearFit.Item2;

            double[] timeArr = time.ToArray();
            double[] posArr = triggerPos.ToArray();

            double velocity = slope / 18.0;

            int MIDIVelocity = (int)(Math.Abs(velocity * 127));
            if(MIDIVelocity > 127) {
                MIDIVelocity = 127;
            }

            return MIDIVelocity;
        }
    }
}
