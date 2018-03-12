using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ComposeVR {

    public class NoteChooserEventArgs : EventArgs {
        private HashSet<int> selectedNotes;

        public NoteChooserEventArgs(HashSet<int> selectedNotes) {
            this.selectedNotes = selectedNotes;
        }

        public HashSet<int> SelectedNotes {
            get { return selectedNotes; }
            set { selectedNotes = value; }
        }
    }


    public class NoteChooser : MonoBehaviour {

        public event EventHandler<NoteChooserEventArgs> NoteSelectionChanged;
        public event EventHandler<NoteChooserEventArgs> NoteChoiceConfirmed;

        private int currentOctave = 3;
        private HashSet<int> selectedNotes;
        private Text octaveIndicator;
        private GameObject leftArrow;
        private GameObject rightArrow;

        private const int NOTES_PER_OCTAVE = 12;
        private const int MAX_OCTAVE = 9;
        private const int OCTAVES_PER_PAGE = 2;

        private bool visible;

        // Use this for initialization
        void Awake() {
            selectedNotes = new HashSet<int>();

            //Listen for clicks from each note button
            for(int octave = 0; octave < OCTAVES_PER_PAGE; octave++) {
                foreach (Transform note in transform.Find("Octave"+octave)) {
                    int noteIndex = note.GetSiblingIndex() + octave * NOTES_PER_OCTAVE;

                    note.GetComponent<Button>().onClick.AddListener(() => {
                        OnNoteClicked(note.GetComponent<Button>(), noteIndex);
                    });
                }
            }

            octaveIndicator = transform.Find("OctaveIndicator").GetComponent<Text>();
            leftArrow = transform.Find("LeftArrow").gameObject;
            rightArrow = transform.Find("RightArrow").gameObject;
        }

        public void Display(bool display) {
            if (visible) {
                ConfirmNoteChoice();
            }

            foreach(Transform child in transform) {
                child.gameObject.SetActive(display);
            }

            if (display) {
                leftArrow.SetActive(currentOctave > 0);
                rightArrow.SetActive(currentOctave < MAX_OCTAVE);
            }

            visible = display;

            if (display) {
                transform.parent.position = Vector3.down * 1000;
            }
        }

        public void OnLeftArrowClicked() {
            currentOctave -= 1;
            if (currentOctave < 0) {
                currentOctave = 0;
            }

            leftArrow.SetActive(currentOctave > 0);
            UpdateOctaveIndicator();
        }

        public void OnRightArrowClicked() {
            currentOctave += 1;
            if (currentOctave > MAX_OCTAVE) {
                currentOctave = MAX_OCTAVE;
            }

            rightArrow.SetActive(currentOctave < MAX_OCTAVE);
            UpdateOctaveIndicator();
        }

        private void UpdateOctaveIndicator() {
            octaveIndicator.text = "Octave: " + currentOctave;
        }

        public void OnConfirmButtonClicked() {
            ConfirmNoteChoice();
            Display(false);
        }

        private void ConfirmNoteChoice() {
            if(NoteChoiceConfirmed != null) {
                NoteChooserEventArgs args = new NoteChooserEventArgs(selectedNotes);
                NoteChoiceConfirmed(this, args);
            }
            ResetSelection();
        }

        private void ResetSelection() {
            selectedNotes.Clear();

            for(int octave = 0; octave < OCTAVES_PER_PAGE; octave++) {
                foreach(Transform note in transform.Find("Octave"+octave)) {
                    note.GetComponent<SelectableButton>().Deselect();
                }
            }
        }

        private void OnNoteClicked(Button b, int index) {
            int noteNumber = getNoteNumberFromIndex(index);
            
            if (b.GetComponent<SelectableButton>().IsSelected()) {
                b.GetComponent<SelectableButton>().Deselect();
                if (!selectedNotes.Contains(noteNumber)) {
                    Debug.LogError("The note represented by a selected note button is not in set of notes selected by NoteChooser");
                }
                else {
                    selectedNotes.Remove(noteNumber);
                }
            }
            else {
                b.GetComponent<SelectableButton>().Select();
                selectedNotes.Add(noteNumber);
            }

            if(NoteSelectionChanged != null) {
                NoteChooserEventArgs args = new NoteChooserEventArgs(selectedNotes);
                NoteSelectionChanged(this, args);
            }
        }

        private int getNoteNumberFromIndex(int index) {
            return index + currentOctave * NOTES_PER_OCTAVE;
        }
    }

}