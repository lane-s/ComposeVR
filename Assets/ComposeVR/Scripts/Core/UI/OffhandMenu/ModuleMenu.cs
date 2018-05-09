using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRTK;

namespace ComposeVR {
    public class ModuleMenu : MonoBehaviour, IDisplayable{

        private void Awake() {
        }

        private void Start() {
            Display();
        }

        public void Display() {
            if (transform.parent.GetComponent<MenuCube>().PlayMode) {
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

    }
}
