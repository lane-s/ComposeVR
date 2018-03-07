using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ComposeVR;

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

    public class DrumPad : MonoBehaviour {

        private byte noteByte;

        public int midiNoteNumber {
            get { return (int)noteByte; }
            set { noteByte = (byte)value; }
        }

        private UDPClient client;
        private Color ogColor;
        private OutputJack output;

        private bool onCooldown = false;
        private const float cooldownTime = 0.3f;

        void Awake() {
            ogColor = GetComponentInChildren<MeshRenderer>().material.color;
            output = transform.parent.GetComponentInChildren<OutputJack>();
            float randNote = Random.value * 126;
            midiNoteNumber = (int)randNote;
        }

        // Update is called once per frame
        void Update() {

        }

        void OnTriggerEnter(Collider other) {
            MalletHead head = other.GetComponent<MalletHead>();
            if (head) {
                int noteVelocity = head.GetMalletVelocity();
                if (!head.enteringFromBack && !head.IsOnCooldown() && noteVelocity > 0) {
                    //Send note on message
                    byte velocityByte = (byte)noteVelocity;
                    MIDIData data = new MIDIData(0x90, noteByte, velocityByte);
                    output.SendData(data);

                    head.struckPad = true;
                    GetComponentInChildren<MeshRenderer>().material.color = new Color(0, 1, 0);
                }
            }
        }

        void OnTriggerExit(Collider other) {
            MalletHead head = other.GetComponent<MalletHead>();
            if (head) {
                if (head.struckPad) {
                    //Send note off message
                    int noteVelocity = 110;
                    byte velocityByte = (byte)noteVelocity;
                    MIDIData data = new MIDIData(0x80, noteByte, velocityByte);
                    output.SendData(data);

                    head.struckPad = false;
                    head.StartCooldown();
                }
                GetComponentInChildren<MeshRenderer>().material.color = ogColor;
            }
        }

    }
}