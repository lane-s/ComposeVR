using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRTK;

namespace ComposeVR {
    public class ModuleSelectionEventArgs : EventArgs {
        public SelectableModule SelectedModule;

        public ModuleSelectionEventArgs(SelectableModule selectedModule) {
            SelectedModule = selectedModule;
        }
    }

    public class PointerSelector : MonoBehaviour {

        public event EventHandler<ModuleSelectionEventArgs> ModuleSelected;

        public bool togglePointerOnHit = true;
		public bool UIHover = false;

        private bool validPointerState = false;
        private SelectableModule hoverModule;
        private ModuleSelectionEventArgs selectionEventArgs;

        private void Awake()
        {
            pointer = GetComponent<VRTK_Pointer>();
            GetComponent<VRTK_Pointer>().PointerStateValid += OnPointerStateValid;
            GetComponent<VRTK_Pointer>().PointerStateInvalid += OnPointerStateInvalid;
            selectionEventArgs = new ModuleSelectionEventArgs(null);
        }

        VRTK_Pointer pointer;

        private void Update() {
        }

        private void OnPointerStateValid(object sender, DestinationMarkerEventArgs e) {
            PointerBlocker blocker = e.raycastHit.collider.GetComponent<PointerBlocker>();
            if(blocker != null && blocker.Blocking) {
                OnModuleHoverEnd();
                return;
            }

            pointer.pointerRenderer.tracerVisibility = VRTK_BasePointerRenderer.VisibilityStates.AlwaysOn;
            pointer.pointerRenderer.cursorVisibility = VRTK_BasePointerRenderer.VisibilityStates.AlwaysOn;

            SelectableModule module = e.raycastHit.collider.transform.GetComponentInActor<SelectableModule>();
            Debug.Log(e.raycastHit.collider.name);
            OnModuleHoverBegin(module);
        }

        private void OnPointerStateInvalid(object sender, DestinationMarkerEventArgs e) {
            pointer.pointerRenderer.tracerVisibility = VRTK_BasePointerRenderer.VisibilityStates.AlwaysOff;
            pointer.pointerRenderer.cursorVisibility = VRTK_BasePointerRenderer.VisibilityStates.AlwaysOff;
            OnModuleHoverEnd();
        }

        private void OnModuleHoverBegin(SelectableModule module) {
            if(module != null) {
                hoverModule = module;
                GetComponent<VRTK_ControllerEvents>().TriggerPressed += OnTriggerPressedOverModule;
            }
            else {
                OnModuleHoverEnd();
            }
        }
        
        private void OnModuleHoverEnd() {
            if(hoverModule != null) {
                hoverModule = null;
                GetComponent<VRTK_ControllerEvents>().TriggerPressed -= OnTriggerPressedOverModule;
            }
        }

        private void OnTriggerPressedOverModule(object sender, ControllerInteractionEventArgs e) {
            if(ModuleSelected != null) {
                selectionEventArgs.SelectedModule = hoverModule;
                ModuleSelected(this, selectionEventArgs);
            }
        }
    }
}
