using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ComposeVR
{
    [RequireComponent(typeof(Button))]
    public sealed class SelectableButton : MonoBehaviour
    {

        private Button button;
        private Text buttonText;
        private Color normalColor;
        private bool selected = false;

        private const int MAX_TEXT_LENGTH = 25;

        public string Text
        {
            get
            {
                if (buttonText == null)
                {
                    buttonText = GetComponentInChildren<Text>();
                }

                return buttonText.text;
            }

            set
            {
                if (value.Length > MAX_TEXT_LENGTH)
                {
                    buttonText.text = value.Substring(0, MAX_TEXT_LENGTH);
                }
                else
                {
                    buttonText.text = value;
                }
            }
        }

        // Use this for initialization
        void Awake()
        {
            button = GetComponent<Button>();
            buttonText = GetComponentInChildren<Text>();
            normalColor = button.colors.normalColor;
        }

        public void Select()
        {
            ColorBlock cb = button.colors;
            cb.normalColor = cb.pressedColor;
            button.colors = cb;

            selected = true;
        }

        public void Deselect()
        {
            ColorBlock cb = button.colors;
            cb.normalColor = normalColor;
            button.colors = cb;

            selected = false;
        }

        public Button GetButton()
        {
            if (button == null)
            {
                button = GetComponent<Button>();
            }

            return button;
        }

        public bool IsSelected()
        {
            return selected;
        }
    }
}
