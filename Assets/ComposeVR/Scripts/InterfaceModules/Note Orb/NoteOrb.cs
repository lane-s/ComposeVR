using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRTK;

namespace ComposeVR {

    public class MIDIData : WireData {
        public byte Status;
        public byte Note;
        public byte Velocity;

        public MIDIData(byte Status, byte Note, byte Velocity) {
            this.Status = Status;
            this.Note = Note;
            this.Velocity = Velocity;
        }

        public byte[] GetPackedMessage() {
            byte[] msg = { Status, Note, Velocity };
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

        private const byte NOTE_ON_STATUS = 0x90;
        private const byte NOTE_OFF_STATUS = 0x80;

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
            Wand head = other.GetComponent<Wand>();
            if (head) {
                Material mat = GetComponentInChildren<MeshRenderer>().material;
                mat.SetColor("_TintColor", Color.green);
                //Debug.Log("Playing note: " + noteByte);

                if (other.GetComponent<OwnedObject>()) {
                    Transform owner = other.GetComponent<OwnedObject>().Owner;

                    controllerReference = VRTK_ControllerReference.GetControllerReference(owner.gameObject);
                    if (!nearbyControllers.Contains(controllerReference)) {
                        if (owner.GetComponent<VRTK_ControllerEvents>().triggerPressed) {
                            NoteOn(head.GetMalletVelocity());
                        }

                        owner.GetComponent<VRTK_ControllerEvents>().TriggerPressed += OnControllerTriggerPressed;
                        owner.GetComponent<VRTK_ControllerEvents>().TriggerReleased += OnControllerTriggerReleased;

                        nearbyControllers.Add(controllerReference);
                    }
                }
            }
        }

        void OnTriggerExit(Collider other) {
            Wand head = other.GetComponent<Wand>();
            if (head) {
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
            MIDIData data = new MIDIData(NOTE_ON_STATUS, noteByte, velocityByte);
            output.SendData(data);
            VRTK_ControllerHaptics.TriggerHapticPulse(controllerReference, TouchHapticsStrength, TouchHapticsDuration, TouchHapticsInterval);

            Material mat = GetComponentInChildren<MeshRenderer>().material;
            mat.SetFloat("_EmissionGain", HitEmissionGain);
        }

        void NoteOff() {
            int noteVelocity = 110;
            byte velocityByte = (byte)noteVelocity;
            MIDIData data = new MIDIData(NOTE_OFF_STATUS, noteByte, velocityByte);
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