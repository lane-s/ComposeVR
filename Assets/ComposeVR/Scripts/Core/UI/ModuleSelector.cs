using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ComposeVR {
    public class ModuleSelector : MonoBehaviour {

        public MiniatureInstantiator SoundModuleInstantiator;
        public SoundModuleMenu SoundModuleMenu;
        public PointerSelector PointerSelector;

        private SelectableModule selectedModule;

        // Use this for initialization
        void Awake () {
            SoundModuleInstantiator.MiniatureReleased += OnSoundModuleCreated;
            PointerSelector.ModuleSelected += OnPointerSelection;
        }
        
        private void OnSoundModuleCreated(object sender, MiniatureEventArgs e) {
            if (e.Miniature.GetComponent<SelectableModule>()) {
                OnModuleSelected(e.Miniature.GetComponent<SelectableModule>());
            }
        }

        private void OnPointerSelection(object sender, ModuleSelectionEventArgs e) {
            OnModuleSelected(e.SelectedModule);
        }

        private void OnModuleSelected(SelectableModule selected) {
            OnModuleDeselected();

            selectedModule = selected;
            SoundModuleMenu.OnModuleSelected(selectedModule);
        }

        private void OnModuleDeselected() {
            if(selectedModule != null) {
                SoundModuleMenu.OnModuleDeselected();
                selectedModule = null;
            }
        }
    }
}
