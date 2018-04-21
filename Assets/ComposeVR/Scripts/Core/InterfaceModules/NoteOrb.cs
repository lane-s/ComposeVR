﻿using System.Collections;
using System.Collections.Generic;
using System.Linq;
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

        private List<int> selectedNotes;

        private UDPClient client;
        private OutputJack output;
        private float originalEmissionGain;

        private bool onCooldown = false;
        private const float cooldownTime = 0.3f;

        private VRTK_ControllerReference controllerReference;
        private NoteChooser noteChooser;
        private int hapticNote;

        private HashSet<VRTK_ControllerReference> nearbyControllers;
        private HashSet<VRTK_ControllerReference> controllersPlayingOrb;

        void Awake() {
            Material mat = GetComponentInChildren<MeshRenderer>().material;
            originalEmissionGain = mat.GetFloat("_EmissionGain");

            output = transform.parent.GetComponentInChildren<OutputJack>();
            nearbyControllers = new HashSet<VRTK_ControllerReference>();
            controllersPlayingOrb = new HashSet<VRTK_ControllerReference>();

            selectedNotes = new List<int>();
            output.GetComponent<Jack>().PlugConnected += OnPlugConnected;
        }

        void OnTriggerEnter(Collider other) {
            Baton baton = other.GetComponent<Baton>();
            if (baton) {
                SetShellColor(Color.green);

                if (other.GetComponent<OwnedObject>()) {
                    Transform owner = other.GetComponent<OwnedObject>().Owner;

                    controllerReference = VRTK_ControllerReference.GetControllerReference(owner.gameObject);
                    if (!nearbyControllers.Contains(controllerReference)) {
                        if (owner.GetComponent<VRTK_ControllerEvents>().triggerPressed) {
                            OrbOnFromController(baton.GetVelocity(), controllerReference);
                        }

                        owner.GetComponent<VRTK_ControllerEvents>().TriggerPressed += OnControllerTriggerPressed;
                        owner.GetComponent<VRTK_ControllerEvents>().TriggerReleased += OnControllerTriggerReleased;

                        nearbyControllers.Add(controllerReference);
                    }
                }
            }
        }

        void OnTriggerExit(Collider other) {
            Baton baton = other.GetComponent<Baton>();
            if (baton) {
                SetShellColor(Color.white);
                if (other.GetComponent<OwnedObject>()) {
                    Transform owner = other.GetComponent<OwnedObject>().Owner;

                    controllerReference = VRTK_ControllerReference.GetControllerReference(owner.gameObject);

                    if (nearbyControllers.Contains(controllerReference)) {
                        nearbyControllers.Remove(controllerReference);

                        owner.GetComponent<VRTK_ControllerEvents>().TriggerPressed -= OnControllerTriggerPressed; 
                        owner.GetComponent<VRTK_ControllerEvents>().TriggerReleased -= OnControllerTriggerReleased; 

                        OrbOffFromController(controllerReference);
                        if(selectedNotes.Count > 0) {
                            baton.StopHapticFeedback(hapticNote);
                        }
                    }
                }

            }
        }

        private void OrbOnFromController(int velocity, VRTK_ControllerReference controller) {
            if (controllersPlayingOrb.Contains(controller)) {
                return;
            }

            controllersPlayingOrb.Add(controller);

            if(selectedNotes.Count > 0) {
                Baton baton = controller.scriptAlias.GetComponent<BatonHolder>().baton;
                if(baton != null) {
                    baton.StartHapticFeedback(hapticNote);
                }
            }

            OrbOn(velocity);
        }

        private void OrbOn(int velocity) {
            foreach (int note in selectedNotes) {
                NoteOn(note, velocity);
            }

            Material mat = GetComponentInChildren<MeshRenderer>().material;
            mat.SetFloat("_EmissionGain", HitEmissionGain);
        }

        private void OrbOffFromController(VRTK_ControllerReference controller) {
            if (!controllersPlayingOrb.Contains(controller)) {
                return;
            }

            controllersPlayingOrb.Remove(controller);

            Baton baton = controller.scriptAlias.GetComponent<BatonHolder>().baton;
            if(baton != null) {
                baton.StopHapticFeedback(hapticNote);
            }

            OrbOff();
        }

        private void OrbOff() {
            foreach(int note in selectedNotes) {
                NoteOff(note);
            }

            Material mat = GetComponentInChildren<MeshRenderer>().material;
            mat.SetFloat("_EmissionGain", originalEmissionGain);
        }

        private void NoteOn(int note, int velocity) {
            NoteData data = new NoteData(NoteData.Status.On, note, velocity);
            output.SendData(data);
        }

        private void NoteOff(int note) {
            int velocity = 110;
            NoteData data = new NoteData(NoteData.Status.Off, note, velocity);
            output.SendData(data);
        }

        private void OnControllerTriggerPressed(object sender, ControllerInteractionEventArgs e) {
            int velocity = e.controllerReference.scriptAlias.GetComponent<MIDINoteVelocityDetector>().GetNoteVelocity();
            OrbOnFromController(velocity, e.controllerReference);
        }

        private void OnControllerTriggerReleased(object sender, ControllerInteractionEventArgs e) {
            OrbOffFromController(e.controllerReference);
        }

        private const float NOTE_CHOOSER_OFFSET = 0.1f;

        private void OpenNoteChooser() {
            noteChooser = ComposeVRManager.Instance.NoteChooserObject;
            noteChooser.Display(true);

            noteChooser.NoteChoiceConfirmed += OnNoteChoiceConfirmed;
            noteChooser.NoteSelectionChanged += OnNoteSelectionChanged;

            noteChooser.transform.parent.position = transform.position + Vector3.up * NOTE_CHOOSER_OFFSET;
            noteChooser.transform.parent.rotation = Quaternion.LookRotation(noteChooser.transform.parent.position - GameObject.FindGameObjectWithTag("Headset").transform.position);
            noteChooser.transform.parent.position -= noteChooser.transform.parent.forward * 0.05f;
            noteChooser.transform.parent.SetParent(this.transform.parent);

            Collider[] toIgnore = new Collider[1];
            toIgnore[0] = noteChooser.GetComponent<Collider>();

            transform.parent.GetComponent<VRTK_InteractableObject>().ignoredColliders = toIgnore;
        }

        private void OnNoteChoiceConfirmed(object sender, NoteChooserEventArgs args) {
            selectedNotes = args.SelectedNotes.ToList<int>();
            noteChooser.NoteChoiceConfirmed -= OnNoteChoiceConfirmed;
            noteChooser.NoteSelectionChanged -= OnNoteSelectionChanged;

            if(selectedNotes.Count > 0) {
                hapticNote = selectedNotes[0];
            }
        }

        private void OnNoteSelectionChanged(object sender, NoteChooserEventArgs args) {
            selectedNotes = args.SelectedNotes.ToList<int>();
            StartCoroutine(previewSelection());
        }

        private IEnumerator previewSelection() {
            SetShellColor(Color.green);
            OrbOn(95);
            yield return new WaitForSecondsRealtime(0.25f);
            SetShellColor(Color.white);
            OrbOff();
        }

        private void SetShellColor(Color c) {
            Material mat = GetComponentInChildren<MeshRenderer>().material;
            mat.SetColor("_TintColor", c);
        }

        private void OnPlugConnected(object sender, JackEventArgs args) {
            OpenNoteChooser();
        }
    }
}