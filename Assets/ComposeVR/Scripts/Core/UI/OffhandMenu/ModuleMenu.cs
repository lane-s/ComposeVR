using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRTK;

namespace ComposeVR
{
    public class ModuleMenu : MonoBehaviour, IDisplayable
    {
        public SharedInt LastSelectedNote;

        private void Awake()
        {
        }

        private void Start()
        {
            Display();
        }

        public void Display()
        {
            if (transform.parent.GetComponent<MenuCube>().PlayMode)
            {
                return;
            }

            gameObject.SetActive(true);

            NoteOrb noteMini = GetComponentInChildren<NoteOrb>();
            if (noteMini != null)
            {
                noteMini.SetRootNote(LastSelectedNote.Value);
            }
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

    }
}
