using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ComposeVR {
    public class MIDINoteHaptics : MonoBehaviour {
        public int BufferSize = 256;
        public float HapticGain = 1.0f;
        public int Channel = 0;

        private bool active = false;
        private double frequency = 20;
        private double phase;
        private double increment;

        private OVRHapticsClip hapticsClip;
        private byte[] buffer;
        private int bufferIndex = 0;

        private int audioRate;
        private int hapticsRate;
        private int stepSize;

        private int maxFreq;

        // Use this for initialization
        void Awake () {
            audioRate = AudioSettings.outputSampleRate;
            hapticsRate = OVRHaptics.Config.SampleRateHz;
            if(hapticsRate == 0) {
                hapticsRate = 320;
            }

            maxFreq = hapticsRate / 2;

            stepSize = audioRate / hapticsRate;

            buffer = new byte[BufferSize];
            GetComponent<AudioSource>().Play();
        }

        public void StartHapticFeedback(int note) {
            active = true;
            frequency = MIDINoteToHapticFrequency(note);
            increment = frequency * 2 * Math.PI / hapticsRate;
            phase = 0;
            GetComponent<AudioSource>().Play();
        }

        private double MIDINoteToHapticFrequency(int note) {
            double noteFrequency = Math.Pow(2, (note - 69) / 12) * 440;

            while(noteFrequency > maxFreq) {
                noteFrequency = noteFrequency / 2;
            }

            return noteFrequency;
        }

        public void StopHapticFeedback() {
            active = false;
        }

        private void OnAudioFilterRead(float[] data, int channels) {
            if (!active) {
                return;
            }

            for(int i = 0; i < data.Length / channels; i += stepSize) {
                if(bufferIndex >= BufferSize) {
                    hapticsClip = new OVRHapticsClip(buffer, BufferSize);
                    OVRHaptics.Channels[Channel].Preempt(hapticsClip);
                    bufferIndex = 0;
                }

                phase += increment;
                byte sample = (byte)(HapticGain * Math.Sin(phase) * 255.0);
                buffer[bufferIndex] = sample;
                bufferIndex += 1;
            }
        }
    }
}
