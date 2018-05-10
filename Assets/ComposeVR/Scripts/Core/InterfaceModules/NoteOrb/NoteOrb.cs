using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using VRTK;

namespace ComposeVR
{

    using ControllerHand = SDK_BaseController.ControllerHand;

    public class NoteData : PhysicalDataPacket
    {
        public enum Status { On, Off };
        public Status NoteStatus;
        public int Note;
        public int Velocity;

        public NoteData(Status status, int Note, int Velocity)
        {
            this.NoteStatus = status;
            this.Note = Note;
            this.Velocity = Velocity;
        }
    }

    [RequireComponent(typeof(Scalable))]
    [RequireComponent(typeof(VRTK_InteractableObject))]
    public class NoteOrb : MonoBehaviour
    {

        public event EventHandler<EventArgs> SelfDestruct;
        private EventArgs defaultSelfDestructArgs;


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
        private PhysicalDataOutput output;

        private List<NoteCore> noteCores;//TODO: Optimal data structure would be something like Java's LinkedHashMap, but the core number will remain small enough for it to not matter
        public List<NoteCore> NoteCores
        {
            get
            {
                return noteCores;
            }
            set
            {
                noteCores = value;
            }
        }

        private Dictionary<NoteOrb, List<NoteCore>> foreignCores;

        private List<int> selectedNotes;
        public List<int> SelectedNotes
        {
            get
            {
                return selectedNotes;
            }
            set
            {
                selectedNotes = value;
                if (selectedNotes.Count > 0)
                {
                    hapticNote = selectedNotes[0];
                }
            }
        }

        private int hapticNote;

        private SimpleTrigger shellTrigger;
        private HashSet<VRTK_ControllerReference> collidingControllers;
        private HashSet<VRTK_ControllerReference> controllersPlayingOrb;

        private bool onCooldown = false;
        private const float cooldownTime = 0.3f;

        private float baseShellEmissionGain;
        private Color baseShellColor;

        const float FULL_ORB_CORE_RADIUS = 0.1147263f;
        private Vector3 fullCoreScale;
        private Vector3 initialCoreScale;

        private Transform coreContainer;
        private Transform coreContainerLowerBound;

        void Awake()
        {
            output = GetComponentInChildren<PhysicalDataOutput>();

            shellTrigger = transform.Find("Shell").GetComponent<SimpleTrigger>();
            shellTrigger.TriggerEnter += OnShellTriggerEnter;
            shellTrigger.TriggerExit += OnShellTriggerExit;

            Material shellMat = shellTrigger.transform.GetComponent<MeshRenderer>().material;
            baseShellEmissionGain = shellMat.GetFloat("_EmissionGain");
            baseShellColor = shellMat.GetColor("_TintColor");

            foreignCores = new Dictionary<NoteOrb, List<NoteCore>>();

            coreContainer = transform.Find("Cores");
            coreContainerLowerBound = coreContainer.Find("LowerBound");

            NoteCore initialCore = coreContainer.Find("InitialCore").GetComponent<NoteCore>();
            initialCoreScale = initialCore.transform.localScale;
            fullCoreScale = new Vector3(FULL_ORB_CORE_RADIUS, FULL_ORB_CORE_RADIUS, FULL_ORB_CORE_RADIUS);

            noteCores = new List<NoteCore>();
            noteCores.Add(initialCore);

            collidingControllers = new HashSet<VRTK_ControllerReference>();
            controllersPlayingOrb = new HashSet<VRTK_ControllerReference>();

            selectedNotes = new List<int>();

            interactable = GetComponent<VRTK_InteractableObject>();
            interactable.InteractableObjectGrabbed += OnGrabbed;
            interactable.InteractableObjectUngrabbed += OnUngrabbed;

            miniature = GetComponent<Miniature>();

            HMD = GameObject.FindGameObjectWithTag("Headset").transform;

            noteSelector = ComposeVRManager.Instance.NoteSelectorObject;
            defaultSelfDestructArgs = new EventArgs();
        }

        void Update()
        {
            bool scaledUp = (miniature.FullScale - transform.localScale).magnitude < DISPLAY_SIZE_DIFFERENCE;

            if (scaledUp && interactable.IsGrabbed() && !displayingNoteSelector && selectedNotes.Count > 0)
            {
                RequestNoteSelector();
            }

            if (displayingNoteSelector)
            {
                PositionNoteSelector();
            }
        }

        private void OnGrabbed(object sender, InteractableObjectEventArgs e)
        {
            e.interactingObject.GetComponent<VRTK_ControllerEvents>().ButtonOnePressed += OnDuplicateButtonPressed;
            e.interactingObject.GetComponentInChildren<VRTK_ControllerTooltips>().ToggleTips(true, VRTK_ControllerTooltips.TooltipButtons.ButtonOneTooltip);
        }

        private void OnUngrabbed(object sender, InteractableObjectEventArgs e)
        {
            e.interactingObject.GetComponentInChildren<VRTK_ControllerTooltips>().ToggleTips(false, VRTK_ControllerTooltips.TooltipButtons.ButtonOneTooltip);

            if (displayingNoteSelector)
            {
                StopDisplayingNoteSelector();
            }

            //Destroy the orb if there are no selected notes/cores remaining

            if (noteCores.Count == 0)
            {
                if (SelfDestruct != null)
                {
                    SelfDestruct(this, defaultSelfDestructArgs);
                }

                DestroyNoteOrb();
            }

            e.interactingObject.GetComponent<VRTK_ControllerEvents>().ButtonOnePressed -= OnDuplicateButtonPressed;
        }

        private void DestroyNoteOrb()
        {
            Plug childPlug = GetComponentInChildren<Plug>();
            if(childPlug != null)
            {
                //If a plug was plugged in, detach it and see if it should be collapsed back into a cord
                childPlug.transform.SetParent(null);
                childPlug.TryCollapseCord();
            }

            Destroy(gameObject);
        }

        private void OnDuplicateButtonPressed(object sender, ControllerInteractionEventArgs e)
        {
            //Bypass the normal Request/Release in order to seamlessly transfer the note selector to the duplicate orb
            displayingNoteSelector = false;
            noteSelector.NoteSelected -= OnNoteSelectionChanged;

            VRTK_InteractGrab grabber = e.controllerReference.scriptAlias.GetComponent<VRTK_InteractGrab>();
            grabber.ForceRelease();

            interactable.isGrabbable = false;

            NoteOrb orbCopy = NoteOrbFactory.DuplicateNoteOrb(this);
            orbCopy.displayingNoteSelector = true;
            orbCopy.SetNoteSelectorHand(e.controllerReference);
            noteSelector.NoteSelected += orbCopy.OnNoteSelectionChanged;

            grabber.ForceGrab(orbCopy.GetComponent<VRTK_InteractableObject>(), () =>
            {
                interactable.isGrabbable = true;
            }, false);
        }

        private void RequestNoteSelector()
        {

            SetNoteSelectorHand(VRTK_ControllerReference.GetControllerReference(interactable.GetGrabbingObject()));

            int selectedNote = selectedNotes.Count > 0 ? selectedNotes[0] : -1;

            displayingNoteSelector = noteSelector.Request(noteSelectorHand, selectedNote);
            if (displayingNoteSelector)
            {
                noteSelector.NoteSelected += OnNoteSelectionChanged;
                SetRootNote(noteSelector.GetSelectedNote());
                ComposeVRManager.Instance.ModuleMenu.Hide();
            }
        }

        private void SetNoteSelectorHand(VRTK_ControllerReference grabbingController)
        {
            ControllerHand grabbingHand = grabbingController.hand;
            noteSelectorHand = grabbingHand == ControllerHand.Left ? ControllerHand.Right : ControllerHand.Left;
        }

        private void PositionNoteSelector()
        {
            Vector3 selectorToHMD = HMD.position - noteSelector.transform.position;
            noteSelector.transform.rotation = Quaternion.LookRotation(selectorToHMD, HMD.up);

            Vector3 upVec = noteSelectorHand == ControllerHand.Left ? -HMD.up : HMD.up;
            Vector3 orbToHMD = (HMD.position - transform.position).normalized;

            Vector3 selectorOffset = Vector3.Cross(orbToHMD, upVec);
            selectorOffset = noteSelectorHand == ControllerHand.Left ? selectorOffset * NOTE_SELECTOR_LEFT_OFFSET : selectorOffset * NOTE_SELECTOR_RIGHT_OFFSET;

            noteSelector.transform.position = transform.position + selectorOffset + selectorToHMD * 0.15f;
        }

        private void StopDisplayingNoteSelector()
        {
            noteSelector.NoteSelected -= OnNoteSelectionChanged;
            noteSelector.Release();
            displayingNoteSelector = false;
            ComposeVRManager.Instance.ModuleMenu.Display();
        }

        void OnShellTriggerEnter(object sender, SimpleTriggerEventArgs args)
        {
            Collider other = args.other;

            HandleNoteOrbCollisionEnter(other);
            HandleBatonCollisionEnter(other);
        }

        void OnShellTriggerExit(object sender, SimpleTriggerEventArgs args)
        {
            Collider other = args.other;

            HandleNoteOrbCollisionExit(other);
            HandleBatonCollisionExit(other);
        }

        private void HandleNoteOrbCollisionEnter(Collider other)
        {
            NoteOrb otherOrb = other.gameObject.GetComponentInActor<NoteOrb>();

            if (!otherOrb)
                return;

            if (!IsAbsorbedOrb() || IsCopy(otherOrb))
                return;

            otherOrb.GiveCores(this, noteCores);

            noteCores.Clear();
            selectedNotes.Clear();
            StopDisplayingNoteSelector();
        }

        private void HandleNoteOrbCollisionExit(Collider other)
        {
            NoteOrb otherOrb = other.gameObject.GetComponentInActor<NoteOrb>();

            if (!otherOrb)
                return;

            if (IsAbsorbedOrb() || IsCopy(otherOrb))
                return;

            if (foreignCores.ContainsKey(otherOrb))
            {
                otherOrb.GiveCores(this, foreignCores[otherOrb]);
                otherOrb.MergeCores(this);

                for (int i = 0; i < foreignCores[otherOrb].Count; i++)
                {
                    selectedNotes.Remove(foreignCores[otherOrb][i].Note);
                    noteCores.Remove(foreignCores[otherOrb][i]);
                }
                UpdateCorePositions();

                foreignCores.Remove(otherOrb);
            }
        }

        /// <summary>
        /// Determine if this orb should be absorbed or if it is the absorbing orb when two orbs intersect to create a chord
        /// </summary>
        /// <returns></returns>
        private bool IsAbsorbedOrb()
        {
            if (noteCores.Count == 0)
            {
                return true;
            }
            else if (!interactable.IsGrabbed())
            {
                return false;
            }

            return true;
        }

        private bool IsCopy(NoteOrb other)
        {
            if (selectedNotes.Count != other.SelectedNotes.Count)
            {
                return false;
            }

            for (int i = 0; i < selectedNotes.Count; i++)
            {
                if (other.SelectedNotes[i] != selectedNotes[i])
                {
                    return false;
                }
            }

            return true;
        }

        private void HandleBatonCollisionEnter(Collider other)
        {
            Baton baton = other.GetComponent<Baton>();
            if (baton)
            {
                SetShellColor(ComposeVRManager.Instance.NoteColors.GetNoteColor(selectedNotes[0]));

                if (other.GetComponent<ActorSubObject>())
                {
                    Transform owner = other.GetComponent<ActorSubObject>().Actor;

                    VRTK_ControllerReference controllerReference = VRTK_ControllerReference.GetControllerReference(owner.gameObject);
                    if (!collidingControllers.Contains(controllerReference))
                    {
                        if (owner.GetComponent<ControllerNoteTrigger>().TriggerIsPressed())
                        {
                            OrbOnFromControllerEnter(owner.GetComponent<ControllerNoteTrigger>().GetNoteVelocity(), controllerReference);
                        }

                        owner.GetComponent<ControllerNoteTrigger>().NoteTriggered += OnControllerTriggerPressed;
                        owner.GetComponent<ControllerNoteTrigger>().NoteReleased += OnControllerTriggerReleased;

                        collidingControllers.Add(controllerReference);
                    }
                }
            }
        }

        private void HandleBatonCollisionExit(Collider other)
        {
            Baton baton = other.GetComponent<Baton>();
            if (baton)
            {
                SetShellColor(baseShellColor);
                if (other.GetComponent<ActorSubObject>())
                {
                    Transform actor = other.GetComponent<ActorSubObject>().Actor;

                    VRTK_ControllerReference controllerReference = VRTK_ControllerReference.GetControllerReference(actor.gameObject);

                    if (collidingControllers.Contains(controllerReference))
                    {
                        collidingControllers.Remove(controllerReference);

                        actor.GetComponent<ControllerNoteTrigger>().NoteTriggered -= OnControllerTriggerPressed;
                        actor.GetComponent<ControllerNoteTrigger>().NoteReleased -= OnControllerTriggerReleased;

                        OrbOffFromControllerExit(controllerReference);
                        if (selectedNotes.Count > 0)
                        {
                            baton.StopHapticFeedback(hapticNote);
                        }
                    }
                }
            }
        }

        private void OrbOnFromControllerEnter(int velocity, VRTK_ControllerReference controller)
        {
            if (controllersPlayingOrb.Contains(controller))
            {
                return;
            }

            controllersPlayingOrb.Add(controller);

            if (selectedNotes.Count > 0)
            {
                Baton baton = controller.scriptAlias.GetComponent<BatonHolder>().baton;
                if (baton != null)
                {
                    baton.StartHapticFeedback(hapticNote);
                }
            }

            OrbOn(velocity);
        }

        private void OrbOffFromControllerExit(VRTK_ControllerReference controller)
        {
            if (!controllersPlayingOrb.Contains(controller))
            {
                return;
            }

            controllersPlayingOrb.Remove(controller);

            Baton baton = controller.scriptAlias.GetComponent<BatonHolder>().baton;
            if (baton != null)
            {
                baton.StopHapticFeedback(hapticNote);
            }

            OrbOff();
        }

        private void OrbOn(int velocity)
        {
            foreach (int note in selectedNotes)
            {
                NoteOn(note, velocity);
            }

            SetShellEmissionGain(HitEmissionGain);
        }

        private void OrbOff()
        {
            foreach (int note in selectedNotes)
            {
                NoteOff(note);
            }

            SetShellEmissionGain(baseShellEmissionGain);
        }

        private void NoteOn(int note, int velocity)
        {
            NoteData data = new NoteData(NoteData.Status.On, note, velocity);
            output.SendData(data);
        }

        private void NoteOff(int note)
        {
            int velocity = 110;
            NoteData data = new NoteData(NoteData.Status.Off, note, velocity);
            output.SendData(data);
        }

        private void OnControllerTriggerPressed(object sender, NoteTriggerEventArgs e)
        {
            ControllerNoteTrigger noteTrigger = sender as ControllerNoteTrigger;
            VRTK_ControllerReference controllerReference = VRTK_ControllerReference.GetControllerReference(noteTrigger.gameObject);

            OrbOnFromControllerEnter(e.Velocity, controllerReference);
        }

        private void OnControllerTriggerReleased(object sender, EventArgs e)
        {
            ControllerNoteTrigger noteTrigger = sender as ControllerNoteTrigger;
            VRTK_ControllerReference controllerReference = VRTK_ControllerReference.GetControllerReference(noteTrigger.gameObject);

            OrbOffFromControllerExit(controllerReference);
        }

        private const float NOTE_CHOOSER_OFFSET = 0.1f;

        private void OnNoteSelectionChanged(object sender, NoteSelectionEventArgs args)
        {
            SetRootNote(args.Note);
            StartCoroutine(PreviewSelection());
        }

        public void SetRootNote(int note)
        {
            hapticNote = note;

            if (selectedNotes.Count > 0)
            {
                //Transpose the chord based on the difference between the new root note and the previous one
                int previousRootNote = selectedNotes[0];
                for (int i = 0; i < selectedNotes.Count; i++)
                {
                    int interval = selectedNotes[i] - previousRootNote;
                    selectedNotes[i] = note + interval;
                    SetCoreNote(noteCores[i], selectedNotes[i]);
                }
            }
            else
            {
                selectedNotes.Add(note);
                SetCoreNote(noteCores[0], note);
            }

            ComposeVRManager.Instance.LastNoteSelected = note;
        }

        private void SetCoreNote(NoteCore core, int note)
        {
            core.Note = note;
            core.Color = ComposeVRManager.Instance.NoteColors.GetNoteColor(note);
        }

        private IEnumerator PreviewSelection()
        {
            //Copy current selection so that the correct notes are turned off
            List<int> currentSelection = new List<int>(selectedNotes);

            SetShellColor(ComposeVRManager.Instance.NoteColors.GetNoteColor(selectedNotes[0]));
            OrbOn(95);
            yield return new WaitForSecondsRealtime(0.1f);
            SetShellColor(baseShellColor);

            foreach (int note in currentSelection)
            {
                NoteOff(note);
            }

            SetShellEmissionGain(baseShellEmissionGain);
        }

        private void SetShellColor(Color c)
        {
            //Don't change the color for chords
            if (selectedNotes.Count > 1)
            {
                return;
            }

            Material mat = shellTrigger.GetComponent<MeshRenderer>().material;
            mat.SetColor("_TintColor", c);
        }

        private void SetShellEmissionGain(float gain)
        {
            Material shellMat = shellTrigger.GetComponent<MeshRenderer>().material;
            shellMat.SetFloat("_EmissionGain", gain);
        }

        /// <summary>
        /// The NoteOrb receives cores from a foreign orb. It keeps track of which cores belong to which foreign orb in case it needs to return them.
        /// </summary>
        /// <param name="foreignOrb"></param>
        /// <param name="foreignOrbCores"></param>
        public void GiveCores(NoteOrb foreignOrb, List<NoteCore> foreignOrbCores)
        {
            List<NoteCore> coresCopy = new List<NoteCore>(foreignOrbCores);
            foreignCores.Add(foreignOrb, coresCopy);

            for (int i = 0; i < foreignOrbCores.Count; i++)
            {
                foreignOrbCores[i].transform.SetParent(coreContainer);
                noteCores.Add(foreignOrbCores[i]);
                selectedNotes.Add(foreignOrbCores[i].Note);
            }

            UpdateCorePositions();
            foreignOrb.SelfDestruct += OnForeignOrbSelfDestruct;
        }

        public void MergeCores(NoteOrb foreignOrb)
        {
            foreignOrb.SelfDestruct -= OnForeignOrbSelfDestruct;
            foreignCores.Remove(foreignOrb);
        }

        private void UpdateCorePositions()
        {
            if (noteCores.Count <= 0)
            {
                return;
            }

            if (noteCores.Count == 1)
            {
                noteCores[0].SetPosition(new Vector3(coreContainerLowerBound.localPosition.x, 0, coreContainerLowerBound.localPosition.z));
                noteCores[0].SetScale(initialCoreScale);
                return;
            }

            Vector3 coreScale = fullCoreScale / noteCores.Count;

            List<NoteCore> noteOrder = noteCores.OrderBy(core => core.Note).ToList();

            for (int i = 0; i < noteCores.Count; i++)
            {
                noteOrder[i].SetPosition(GetCorePosition(i, coreScale));
                noteOrder[i].SetScale(coreScale);
            }
        }

        private Vector3 GetCorePosition(int coreIndex, Vector3 coreScale)
        {
            return coreContainerLowerBound.localPosition + new Vector3(0, coreScale.y / 2 + coreIndex * coreScale.y, 0);
        }

        private void OnForeignOrbSelfDestruct(object sender, EventArgs args)
        {
            NoteOrb foreignOrb = sender as NoteOrb;
            MergeCores(foreignOrb);
        }
    }
}