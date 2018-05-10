using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ComposeVR
{
    public class SelectableModule : MonoBehaviour
    {
        public MeshRenderer OutlineRenderer;
        public PointerDetector PointerDetector;

        public float SelectedOutlineWidth = 0.08f;

        private bool _selected;
        public bool Selected
        {
            set
            {
                _selected = value;
                UpdateOutline();
            }
            get
            {
                return _selected;
            }
        }

        private bool pointerHover;

        private void Awake()
        {
            PointerDetector.PointerEnter += OnPointerEnter;
            PointerDetector.PointerExit += OnPointerExit;
        }

        private void OnPointerEnter(object sender, PointerDetectorEventArgs e)
        {
            pointerHover = true;
            UpdateOutline();
        }

        private void OnPointerExit(object sender, PointerDetectorEventArgs e)
        {
            pointerHover = false;
            UpdateOutline();
        }

        private void UpdateOutline()
        {
            if (_selected || pointerHover)
            {
                ShowOutline();
            }
            else
            {
                HideOutline();
            }
        }

        private void ShowOutline()
        {
            OutlineRenderer.material.SetFloat("_ASEOutlineWidth", SelectedOutlineWidth);
        }

        private void HideOutline()
        {
            OutlineRenderer.material.SetFloat("_ASEOutlineWidth", 0);
        }

    }
}
