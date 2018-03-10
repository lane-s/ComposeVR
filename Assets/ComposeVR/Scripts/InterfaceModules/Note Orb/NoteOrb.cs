using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRTK;

namespace ComposeVR {

    public class MIDIData : WireData {
        byte Type;
        byte Note;
        byte Velocity;

        public MIDIData(byte Type, byte Note, byte Velocity) {
            this.Type = Type;
            this.Note = Note;
            this.Velocity = Velocity;
        }

        public byte[] GetPackedMessage() {
            byte[] msg = { Type, Note, Velocity };
            return msg;
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

        void Awake() {
            Material mat = GetComponentInChildren<MeshRenderer>().material;
            originalEmissionGain = mat.GetFloat("_EmissionGain");

            output = transform.parent.GetComponentInChildren<OutputJack>();
            float randNote = Random.value * 126;
            midiNoteNumber = (int)randNote;
        }

        // Update is called once per frame
        void Update() {

        }

        void OnTriggerEnter(Collider other) {
            Wand head = other.GetComponent<Wand>();
            if (head) {
                Material mat = GetComponentInChildren<MeshRenderer>().material;
                mat.SetFloat("_EmissionGain", HitEmissionGain);
                //Debug.Log("Playing note: " + noteByte);

                if (other.GetComponent<OwnedObject>()) {
                    Transform owner = other.GetComponent<OwnedObject>().Owner;
                    owner.GetComponent<VRTK_ControllerEvents>().TriggerPressed += OnControllerTriggerPressed; 
                    owner.GetComponent<VRTK_ControllerEvents>().TriggerReleased += OnControllerTriggerReleased;

                    controllerReference = VRTK_ControllerReference.GetControllerReference(owner.gameObject);
                    if (owner.GetComponent<VRTK_ControllerEvents>().triggerPressed) {
                        NoteOn(head.GetMalletVelocity());
                    }

                }
            }
        }

        void OnTriggerExit(Collider other) {
            Wand head = other.GetComponent<Wand>();
            if (head) {
                Material mat = GetComponentInChildren<MeshRenderer>().material;
                mat.SetFloat("_EmissionGain", originalEmissionGain);

                if (other.GetComponent<OwnedObject>()) {
                    Transform owner = other.GetComponent<OwnedObject>().Owner;
                    owner.GetComponent<VRTK_ControllerEvents>().TriggerPressed -= OnControllerTriggerPressed; 
                    owner.GetComponent<VRTK_ControllerEvents>().TriggerReleased -= OnControllerTriggerReleased; 
                }

                NoteOff();
            }
        }

        void NoteOn(int noteVelocity) {
            byte velocityByte = (byte)noteVelocity;
            MIDIData data = new MIDIData(144, noteByte, velocityByte);
            output.SendData(data);
            VRTK_ControllerHaptics.TriggerHapticPulse(controllerReference, TouchHapticsStrength, TouchHapticsDuration, TouchHapticsInterval);
        }

        void NoteOff() {
            int noteVelocity = 110;
            byte velocityByte = (byte)noteVelocity;
            MIDIData data = new MIDIData(128, noteByte, velocityByte);
            output.SendData(data);
        }

        void OnControllerTriggerPressed(object sender, ControllerInteractionEventArgs e) {
            NoteOn((int)e.buttonPressure * 127);
        }

        void OnControllerTriggerReleased(object sender, ControllerInteractionEventArgs e) {
            NoteOff();
        }

    }
}