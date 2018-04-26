using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRTK;

namespace ComposeVR {
    public class ModuleMenu : MonoBehaviour {
        public VRTK_ControllerEvents offHandController;
        bool playMode = false;

        private void Awake() {
            offHandController.StartMenuPressed += OnModeChange;
        }

        private void Start() {
            Display();
        }

        public void Display() {
            if (playMode) {
                return;
            }

            gameObject.SetActive(true);
            NoteOrb noteMini = GetComponentInChildren<NoteOrb>();
            if(noteMini != null) {
                noteMini.SetRootNote(ComposeVRManager.Instance.LastNoteSelected);
            }
        }

        public void Hide() {
            gameObject.SetActive(false);
        }

        void OnModeChange(object sender, ControllerInteractionEventArgs e) {
            playMode = !playMode;

            if (playMode) {
                Hide();
            }
            else {
                Display();
            }
        }
    }
}
