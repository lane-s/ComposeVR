﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRTK;

namespace ComposeVR {

    public class NoteData : WireData {
        public enum Status { On, Off };
        public Status NoteStatus;
        public int Note;
        public int Velocity;

        public NoteData(Status status, int Note, int Velocity) {
            this.NoteStatus = status;
            this.Note = Note;
            this.Velocity = Velocity;
        }
    }

    public class NoteOrb : MonoBehaviour {

        public float HitEmissionGain;
        public float TouchHapticsStrength;
        public float TouchHapticsDuration;
        public float TouchHapticsInterval;

        private byte noteByte;

        public int midiNoteNumber {
            get { return (int)noteByte; }
            set { noteByte = (byte)value; }
        }

        private UDPClient client;
        private OutputJack output;
        private float originalEmissionGain;

        private bool onCooldown = false;
        private const float cooldownTime = 0.3f;
        private VRTK_ControllerReference controllerReference;

        private HashSet<VRTK_ControllerReference> nearbyControllers;

        void Awake() {
            Material mat = GetComponentInChildren<MeshRenderer>().material;
            originalEmissionGain = mat.GetFloat("_EmissionGain");

            output = transform.parent.GetComponentInChildren<OutputJack>();
            float randNote = Random.value * 126;
            midiNoteNumber = (int)randNote;

            nearbyControllers = new HashSet<VRTK_ControllerReference>();
        }

        // Update is called once per frame
        void Update() {

        }

        void OnTriggerEnter(Collider other) {
            Wand wand = other.GetComponent<Wand>();
            if (wand) {
                Material mat = GetComponentInChildren<MeshRenderer>().material;
                mat.SetColor("_TintColor", Color.green);
                //Debug.Log("Playing note: " + noteByte);

                if (other.GetComponent<OwnedObject>()) {
                    Transform owner = other.GetComponent<OwnedObject>().Owner;

                    controllerReference = VRTK_ControllerReference.GetControllerReference(owner.gameObject);
                    if (!nearbyControllers.Contains(controllerReference)) {
                        if (owner.GetComponent<VRTK_ControllerEvents>().triggerPressed) {
                            NoteOn(wand.GetVelocity());
                        }

                        owner.GetComponent<VRTK_ControllerEvents>().TriggerPressed += OnControllerTriggerPressed;
                        owner.GetComponent<VRTK_ControllerEvents>().TriggerReleased += OnControllerTriggerReleased;

                        nearbyControllers.Add(controllerReference);
                    }
                }
            }
        }

        void OnTriggerExit(Collider other) {
            Wand wand = other.GetComponent<Wand>();
            if (wand) {
                Material mat = GetComponentInChildren<MeshRenderer>().material;
                mat.SetColor("_TintColor", Color.white);

                if (other.GetComponent<OwnedObject>()) {
                    Transform owner = other.GetComponent<OwnedObject>().Owner;

                    controllerReference = VRTK_ControllerReference.GetControllerReference(owner.gameObject);

                    if (nearbyControllers.Contains(controllerReference)) {
                        nearbyControllers.Remove(controllerReference);

                        owner.GetComponent<VRTK_ControllerEvents>().TriggerPressed -= OnControllerTriggerPressed; 
                        owner.GetComponent<VRTK_ControllerEvents>().TriggerReleased -= OnControllerTriggerReleased; 
                    }
                }

                NoteOff();
            }
        }

        void NoteOn(int noteVelocity) {
            byte velocityByte = (byte)noteVelocity;
            NoteData data = new NoteData(NoteData.Status.On, noteByte, velocityByte);
            output.SendData(data);
            VRTK_ControllerHaptics.TriggerHapticPulse(controllerReference, TouchHapticsStrength, TouchHapticsDuration, TouchHapticsInterval);

            Material mat = GetComponentInChildren<MeshRenderer>().material;
            mat.SetFloat("_EmissionGain", HitEmissionGain);
        }

        void NoteOff() {
            int noteVelocity = 110;
            byte velocityByte = (byte)noteVelocity;
            NoteData data = new NoteData(NoteData.Status.Off, noteByte, velocityByte);
            output.SendData(data);

            Material mat = GetComponentInChildren<MeshRenderer>().material;
            mat.SetFloat("_EmissionGain", originalEmissionGain);
        }

        void OnControllerTriggerPressed(object sender, ControllerInteractionEventArgs e) {
            NoteOn((int)(e.buttonPressure * 127));
        }

        void OnControllerTriggerReleased(object sender, ControllerInteractionEventArgs e) {
            NoteOff();
        }

    }
}