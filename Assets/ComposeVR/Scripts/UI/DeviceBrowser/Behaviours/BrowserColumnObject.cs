using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using VRTK;
using Google.Protobuf.Collections;

namespace ComposeVR {

    using Arrow = Protocol.Browser.OnArrowVisibilityChanged.Types.Arrow;
        
    public sealed class BrowserColumnObject : MonoBehaviour, IBrowserColumn {

        public Transform resultButtonPrefab;
        public BrowserColumnController Controller;

        private List<SelectableButton> resultButtons;
        private Button upArrow;
        private Button downArrow;

        private Color originalNormalColor;


        private void Awake() {
            resultButtons = new List<SelectableButton>();
            CreateArrows();

            Controller.SetBrowserColumn(this);
            Controller.Initialize(gameObject.name);
        }

        private void CreateArrows() {
            upArrow = transform.Find("UpArrow").GetComponent<Button>();
            upArrow.onClick.AddListener(
                () => { Controller.OnArrowClicked(Arrow.Up); }
            );

            downArrow = transform.Find("DownArrow").GetComponent<Button>();
            downArrow.onClick.AddListener(
                () => { Controller.OnArrowClicked(Arrow.Down);  }
            );
        }



        void IBrowserColumn.UpdateItem(int itemIndex, string itemName) {
            resultButtons[itemIndex].SetText(itemName);
        }

        void IBrowserColumn.SelectItem(int itemIndex) {
            //Select button
            resultButtons[itemIndex].Select();
        }

        void IBrowserColumn.DeselectItem(int itemIndex) {
            resultButtons[itemIndex].Deselect();
        }

        void IBrowserColumn.SetItemVisibility(int itemIndex, bool visible) {
            resultButtons[itemIndex].gameObject.SetActive(visible);
        }

        string IBrowserColumn.GetItemText(int itemIndex) {
            return resultButtons[itemIndex].GetText();
        }

        void IBrowserColumn.SetArrowVisibility(Arrow arrow, bool visible) {
            if (arrow == Arrow.Up) {
                this.upArrow.gameObject.SetActive(visible);
            }
            else {
                this.downArrow.gameObject.SetActive(visible);
            }
        }

        void IBrowserColumn.ExpandToSize(int size) {

            float buttonHeight = resultButtonPrefab.GetComponent<RectTransform>().localScale.y;

            //Add buttons as needed
            Vector3 buttonPosition = new Vector3(0, (buttonHeight + Controller.Config.ResultSpacing) * resultButtons.Count - Controller.Config.ResultStartOffset, 0);

            while (resultButtons.Count < size) {
                Transform newButton = Instantiate(resultButtonPrefab) as Transform;
                newButton.SetParent(transform);
                newButton.localPosition = Vector3.zero;
                newButton.localRotation = Quaternion.Euler(0, 0, 0);

                SelectableButton nb = newButton.GetComponent<SelectableButton>();

                int buttonIndex = resultButtons.Count;

                //Set up pressed callback with index
                nb.GetButton().onClick.AddListener(
                    () => { Controller.OnItemSelected(buttonIndex); }
                );

                //Position button
                RectTransform t = nb.GetComponent<RectTransform>();
                t.position = Vector3.zero;
                t.rotation = Quaternion.Euler(0, 0, 0);

                t.localScale = Vector3.one;
                t.localEulerAngles = Vector3.zero;
                t.localPosition = Vector3.zero;
                t.localRotation = Quaternion.Euler(0, 0, 0);
                t.anchoredPosition = buttonPosition;

                buttonPosition += Vector3.down * t.rect.height + Vector3.down * Controller.Config.ResultSpacing;

                nb.gameObject.SetActive(false);

                resultButtons.Add(nb);
            }
        }

    }

    public interface IBrowserColumn {
        void UpdateItem(int itemIndex, string itemName);
        void SelectItem(int itemIndex);
        void DeselectItem(int itemIndex);
        string GetItemText(int itemIndex);
        void SetItemVisibility(int itemIndex, bool visible);
        void SetArrowVisibility(Arrow arrow, bool visible);
        void ExpandToSize(int size);
    }
}