﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRTK;

namespace ComposeVR {

    using ControllerHand = SDK_BaseController.ControllerHand;

    public class NoteData : PhysicalDataPacket {
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

    [RequireComponent(typeof(Scalable))]
    [RequireComponent(typeof(VRTK_InteractableObject))]
    public class NoteOrb : MonoBehaviour {
        

        public float HitEmissionGain;
        public float TouchHapticsStrength;
        public float TouchHapticsDuration;
        public float TouchHapticsInterval;

        private ControllerHand noteSelectorHand;
        private NoteSelector noteSelector;
        private bool displayingNoteSelector;
        private const float NOTE_SELECTOR_RIGHT_OFFSET = 0.2f;
        private const float NOTE_SELECTOR_LEFT_OFFSET = 0.325f;
        private const float DISPLAY_SIZE_DIFFERENCE = 0.45f; //How close to full scale does the orb need to be before the selector is displayed

        private VRTK_InteractableObject interactable;
        private Miniature miniature;

        private Transform HMD;
        private Transform core;
        private PhysicalDataOutput output;

        private List<int> selectedNotes;
        private int hapticNote;

        private SimpleTrigger shellTrigger;
        private HashSet<VRTK_ControllerReference> collidingControllers;
        private HashSet<VRTK_ControllerReference> controllersPlayingOrb;
            
        private bool onCooldown = false;
        private const float cooldownTime = 0.3f;
        private float baseShellEmissionGain;

        void Awake() {

            output = GetComponentInChildren<PhysicalDataOutput>();

            shellTrigger = transform.Find("Shell").GetComponent<SimpleTrigger>();
            shellTrigger.TriggerEnter += OnShellTriggerEnter;
            shellTrigger.TriggerExit += OnShellTriggerExit;
                    
            Material shellMat = shellTrigger.transform.GetComponent<MeshRenderer>().material;
            baseShellEmissionGain = shellMat.GetFloat("_EmissionGain");

            core = transform.Find("Core");

            collidingControllers = new HashSet<VRTK_ControllerReference>();
            controllersPlayingOrb = new HashSet<VRTK_ControllerReference>();

            selectedNotes = new List<int>();

            interactable = GetComponent<VRTK_InteractableObject>();
            interactable.InteractableObjectUngrabbed += OnUngrabbed;

            miniature = GetComponent<Miniature>();

            HMD = GameObject.FindGameObjectWithTag("Headset").transform;

            noteSelector = ComposeVRManager.Instance.NoteSelectorObject;
        }

        void Update() {
            if((miniature.FullScale - transform.localScale).magnitude < DISPLAY_SIZE_DIFFERENCE && interactable.IsGrabbed() && !displayingNoteSelector) {
                ControllerHand grabbingHand = VRTK_ControllerReference.GetControllerReference(interactable.GetGrabbingObject()).hand;
                noteSelectorHand = grabbingHand == ControllerHand.Left ? ControllerHand.Right : ControllerHand.Left;

                int selectedNote = selectedNotes.Count > 0 ? selectedNotes[0] : -1;

                displayingNoteSelector = noteSelector.Request(noteSelectorHand, selectedNote);
                if (displayingNoteSelector) {
                    noteSelector.NoteSelected += OnNoteSelectionChanged;
                    SelectNote(noteSelector.GetSelectedNote());
                    ComposeVRManager.Instance.ModuleMenu.Hide();
                }
            }

            if (displayingNoteSelector && interactable.GetGrabbingObject() != null) {
                Vector3 selectorToHMD = HMD.position - noteSelector.transform.position;
                noteSelector.transform.rotation = Quaternion.LookRotation(selectorToHMD, HMD.up);

                Vector3 upVec = noteSelectorHand == ControllerHand.Left ? -HMD.up : HMD.up;
                Vector3 orbToHMD = (HMD.position - transform.position).normalized;

                Vector3 selectorOffset = Vector3.Cross(orbToHMD, upVec);
                selectorOffset = noteSelectorHand == ControllerHand.Left ? selectorOffset * NOTE_SELECTOR_LEFT_OFFSET : selectorOffset * NOTE_SELECTOR_RIGHT_OFFSET;

                noteSelector.transform.position = transform.position + selectorOffset + selectorToHMD * 0.15f;
                
            }
        }

        private void OnUngrabbed(object sender, InteractableObjectEventArgs e) {
            if (displayingNoteSelector) {
                noteSelector.NoteSelected -= OnNoteSelectionChanged;
                noteSelector.Release();
                displayingNoteSelector = false;
                ComposeVRManager.Instance.ModuleMenu.Display();
            }
        }

        void OnShellTriggerEnter(object sender, SimpleTriggerEventArgs args) {
            Collider other = args.other;

            Baton baton = other.GetComponent<Baton>();
            if (baton) {
                SetShellColor(ComposeVRManager.Instance.NoteColors.GetNoteColor(selectedNotes[0]));

                if (other.GetComponent<OwnedObject>()) {
                    Transform owner = other.GetComponent<OwnedObject>().Owner;

                    VRTK_ControllerReference controllerReference = VRTK_ControllerReference.GetControllerReference(owner.gameObject);
                    if (!collidingControllers.Contains(controllerReference)) {
                        if (owner.GetComponent<VRTK_ControllerEvents>().triggerPressed) {
                            OrbOnFromControllerEnter(baton.GetVelocity(), controllerReference);
                        }

                        owner.GetComponent<VRTK_ControllerEvents>().TriggerPressed += OnControllerTriggerPressed;
                        owner.GetComponent<VRTK_ControllerEvents>().TriggerReleased += OnControllerTriggerReleased;

                        collidingControllers.Add(controllerReference);
                    }
                }
            }
        }

        void OnShellTriggerExit(object sender, SimpleTriggerEventArgs args) {
            Collider other = args.other;

            Baton baton = other.GetComponent<Baton>();
            if (baton) {
                SetShellColor(Color.white);
                if (other.GetComponent<OwnedObject>()) {
                    Transform owner = other.GetComponent<OwnedObject>().Owner;

                    VRTK_ControllerReference controllerReference = VRTK_ControllerReference.GetControllerReference(owner.gameObject);

                    if (collidingControllers.Contains(controllerReference)) {
                        collidingControllers.Remove(controllerReference);

                        owner.GetComponent<VRTK_ControllerEvents>().TriggerPressed -= OnControllerTriggerPressed; 
                        owner.GetComponent<VRTK_ControllerEvents>().TriggerReleased -= OnControllerTriggerReleased; 

                        OrbOffFromControllerExit(controllerReference);
                        if(selectedNotes.Count > 0) {
                            baton.StopHapticFeedback(hapticNote);
                        }
                    }
                }
            }
        }

        private void OrbOnFromControllerEnter(int velocity, VRTK_ControllerReference controller) {
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

            SetShellEmissionGain(HitEmissionGain);
        }

        private void OrbOffFromControllerExit(VRTK_ControllerReference controller) {
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

            SetShellEmissionGain(baseShellEmissionGain);
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
            OrbOnFromControllerEnter(velocity, e.controllerReference);
        }

        private void OnControllerTriggerReleased(object sender, ControllerInteractionEventArgs e) {
            OrbOffFromControllerExit(e.controllerReference);
        }

        private const float NOTE_CHOOSER_OFFSET = 0.1f;

        private void OnNoteSelectionChanged(object sender, NoteSelectionEventArgs args) {
            SelectNote(args.Note);
            StartCoroutine(PreviewSelection());
        }

        public void SelectNote(int note) {
            hapticNote = note;
            selectedNotes.Clear();
            selectedNotes.Add(note);
            SetCoreColor(ComposeVRManager.Instance.NoteColors.GetNoteColor(note));
            ComposeVRManager.Instance.LastNoteSelected = note;
        }

        private IEnumerator PreviewSelection() {
            SetShellColor(ComposeVRManager.Instance.NoteColors.GetNoteColor(selectedNotes[0]));
            OrbOn(95);
            yield return new WaitForSecondsRealtime(0.1f);
            SetShellColor(Color.white);
            OrbOff();
        }

        private void SetShellColor(Color c) {
            Material mat = shellTrigger.GetComponent<MeshRenderer>().material;
            mat.SetColor("_TintColor", c);
        }

        private void SetShellEmissionGain(float gain) {
            Material shellMat = shellTrigger.GetComponent<MeshRenderer>().material;
            shellMat.SetFloat("_EmissionGain", gain);
        }

        private void SetCoreColor(Color c) {
            core.GetComponent<MeshRenderer>().material.SetColor("_EmissionColor", c);
        }
    }
}