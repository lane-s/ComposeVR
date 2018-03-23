namespace VRTK.Examples
{
    using UnityEngine;

    public class VRTK_ControllerUIPointerEvents_ListenerExample : MonoBehaviour
    {
        public bool togglePointerOnHit = true;
		public bool UIHover = false;

        private bool validPointerState = false;

        private void Start()
        {
            pointer = GetComponent<VRTK_Pointer>();
            GetComponent<VRTK_Pointer>().PointerStateValid += OnPointerStateValid;
            GetComponent<VRTK_Pointer>().PointerStateInvalid += OnPointerStateInvalid;
        }

        VRTK_Pointer pointer;

        private void Update() {
        }

        private void OnPointerStateValid(object sender, DestinationMarkerEventArgs e) {
            pointer.pointerRenderer.tracerVisibility = VRTK_BasePointerRenderer.VisibilityStates.AlwaysOn;
            pointer.pointerRenderer.cursorVisibility = VRTK_BasePointerRenderer.VisibilityStates.AlwaysOn;
        }

        private void OnPointerStateInvalid(object sender, DestinationMarkerEventArgs e) {
            pointer.pointerRenderer.tracerVisibility = VRTK_BasePointerRenderer.VisibilityStates.AlwaysOff;
            pointer.pointerRenderer.cursorVisibility = VRTK_BasePointerRenderer.VisibilityStates.AlwaysOff;
        }
    }
}