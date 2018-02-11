using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ComposeVR {
    [RequireComponent(typeof(Button))]
    [RequireComponent(typeof(Text))]
    public sealed class SelectableButton : MonoBehaviour {

        private Button button;
        private Text buttonText;
        private Color normalColor;

        // Use this for initialization
        void Start() {
            button = GetComponent<Button>();
            buttonText = GetComponentInChildren<Text>();
            normalColor = button.colors.normalColor;
        }

        public void Select() {
            ColorBlock cb = button.colors;
            normalColor = cb.normalColor;
            cb.normalColor = cb.pressedColor;
            button.colors = cb;
        }

        public void Deselect() {
            ColorBlock cb = button.colors;
            cb.normalColor = normalColor;
            button.colors = cb;
        }

        public Button GetButton() {
            return button;
        }

        public void SetText(string text) {
            buttonText.text = text;
        }

        public string GetText() {
            return buttonText.text;
        }
    }
}
