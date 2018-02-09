using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using VRTK;
using Google.Protobuf.Collections;

namespace ComposeVR {

    public class BrowserColumnEventArgs : EventArgs {
        public BrowserColumnEventArgs(BrowserColumn b, int selectionIndex, int pageChange) {
            col = b;
            selIndex = selectionIndex;
            pgChange = pageChange;
        }

        private BrowserColumn col;
        private int selIndex;
        private int pgChange;

        public BrowserColumn browserColumn {
            get { return col; }
        }

        public int selectionIndex {
            get { return selIndex; }
        }

        public int pageChange {
            get { return pgChange; }
        }
    }

    public class BrowserColumn : RemoteEventHandler {

        public Transform resultButtonPrefab;
        public float resultSpacing = 0;
        public float resultStartOffset = 80;
        public bool isFilterColumn = false;

        public event EventHandler<BrowserColumnEventArgs> ItemSelected;
        public event EventHandler<BrowserColumnEventArgs> PageChange;

        private List<Button> resultButtons;

        private UnityAction buttonPressed;

        private Button upArrow;
        private Button downArrow;
        private Color normalColor;

        private int numPages = 1;

        private string selectedItemName;
        private int selectedItemIndex;


        //TODO Call event PageChange when PageScrollBar changes page
        //TODO Call event PageChange when up/down button is pushed
        //TODO Display list of results when UpdateColumn is called
        //TODO Highlight result that is pointed at
        //TODO When result is clicked, call ItemSelected event with scrollPosition + resultIndex

        private void Awake() {
            resultButtons = new List<Button>();
            Register("browser/" + gameObject.name);

            upArrow = transform.Find("UpArrow").GetComponent<Button>();
            upArrow.onClick.AddListener(UpArrowClicked);

            downArrow = transform.Find("DownArrow").GetComponent<Button>();
            downArrow.onClick.AddListener(DownArrowClicked);

            arrowVisibilityChanged(true, false);
            arrowVisibilityChanged(false, false);

            selectDefault(0, "Any " + gameObject.name);
        }

        // Use this for initialization
        void Start() {

        }

        // Update is called once per frame
        void Update() {

        }

        public void OnBrowserColumnChanged(Protocol.Event e) {

            int totalResults = e.BrowserEvent.OnBrowserColumnChangedEvent.TotalResults;
            int resultsPerPage = e.BrowserEvent.OnBrowserColumnChangedEvent.ResultsPerPage;

            RepeatedField<string> results = e.BrowserEvent.OnBrowserColumnChangedEvent.Results;

            numPages = (totalResults + resultsPerPage - 1) / resultsPerPage;


            float buttonHeight = resultButtonPrefab.GetComponent<RectTransform>().localScale.y;

            //Resize canvas
            //Vector3 newScale = GetComponent<RectTransform>().localScale;
            //newScale.y = (buttonHeight + resultSpacing) * results.Length - resultSpacing;
            //GetComponent<RectTransform>().localScale = newScale;

            //Remove extra buttons
            while (resultButtons.Count > resultsPerPage) {
                Destroy(resultButtons[resultButtons.Count - 1].gameObject);
                resultButtons.RemoveAt(resultButtons.Count - 1);
            }

            //Add buttons as needed
            Vector3 buttonPosition = new Vector3(0, (buttonHeight + resultSpacing) * resultButtons.Count - resultStartOffset, 0);

            while (resultButtons.Count < resultsPerPage) {
                Transform newButton = Instantiate(resultButtonPrefab) as Transform;
                newButton.SetParent(transform);
                newButton.localPosition = Vector3.zero;
                newButton.localRotation = Quaternion.Euler(0, 0, 0);

                Button nb = newButton.GetComponent<Button>();
                normalColor = nb.colors.normalColor;

                int buttonIndex = resultButtons.Count;

                //Set up pressed callback with index
                nb.onClick.AddListener(
                    () => { OnItemSelect(buttonIndex); }
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

                buttonPosition += Vector3.down * t.rect.height + Vector3.down * resultSpacing;

                resultButtons.Add(nb);
            }


            //Label each button with result name
            for (int i = 0; i < results.Count; i++) {
                resultButtons[i].gameObject.SetActive(true);
                Text buttonText = resultButtons[i].GetComponentInChildren<Text>();
                buttonText.text = results[i];

                colorButtonIfSelected(i);
            }

            //Deactivate buttons not needed to display the results
            for (int i = results.Count; i < resultButtons.Count; i++) {
                resultButtons[i].gameObject.SetActive(false);
            }

        }

        public void selectDefault(int index, string name) {
            if (isFilterColumn) {
                selectedItemIndex = index;
                selectedItemName = name;
                colorButtonIfSelected(index);
            }
        }

        private void colorButtonIfSelected(int buttonIndex) {
            if (buttonIndex < 0 || buttonIndex >= resultButtons.Count) {
                return;
            }

            if(selectedItemIndex < 0 || selectedItemIndex >= resultButtons.Count) {
                return;
            }

            //Color if selected
            if (resultButtons[selectedItemIndex].GetComponentInChildren<Text>().text.Equals(selectedItemName) && buttonIndex == selectedItemIndex) {

                ColorBlock cb = resultButtons[buttonIndex].colors;
                cb.normalColor = cb.pressedColor;
                resultButtons[buttonIndex].colors = cb;

            }
        }

        private void deselectItem() {
            if (selectedItemIndex < resultButtons.Count && selectedItemIndex >= 0) {
                if (resultButtons[selectedItemIndex].GetComponentInChildren<Text>().text.Equals(selectedItemName)) {
                    Debug.Log("Reverting color");
                    ColorBlock cb = resultButtons[selectedItemIndex].colors;
                    cb.normalColor = normalColor;
                    resultButtons[selectedItemIndex].colors = cb;
                }
            }
            selectedItemIndex = -1;
            selectedItemName = "";
        }

        private void OnItemSelect(int itemIndex) {
            Debug.Log("Item " + itemIndex + " selected!");
            deselectItem();

            selectedItemIndex = itemIndex;
            selectedItemName = resultButtons[itemIndex].GetComponentInChildren<Text>().text;
            colorButtonIfSelected(itemIndex);

            if (ItemSelected != null) {
                BrowserColumnEventArgs e = new BrowserColumnEventArgs(this, itemIndex, 0);
                ItemSelected(this, e);
            }
        }

        private void UpArrowClicked() {
            OnPageChange(-1);
        }

        private void DownArrowClicked() {
            OnPageChange(1);
        }

        public void OnArrowVisibilityChanged(Protocol.Event e) {
            arrowVisibilityChanged(e.BrowserEvent.OnArrowVisibilityChangedEvent.UpArrow, e.BrowserEvent.OnArrowVisibilityChangedEvent.Visible);
        }

        private void arrowVisibilityChanged(bool upArrow, bool visible) {
            if (upArrow) {
                this.upArrow.gameObject.SetActive(visible);
            }
            else {
                this.downArrow.gameObject.SetActive(visible);
            }
        }

        private void OnPageChange(int pageChange) {

            if (PageChange != null) {
                BrowserColumnEventArgs e = new BrowserColumnEventArgs(this, -1, pageChange);
                PageChange(this, e);
            }
        }

        public void resetColumn() {
            selectDefault(0, "Any " + gameObject.name);
            arrowVisibilityChanged(true, false);
            arrowVisibilityChanged(false, false);
            deselectItem();
        }
    }
}