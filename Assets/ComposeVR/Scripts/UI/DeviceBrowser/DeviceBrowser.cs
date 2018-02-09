using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using ComposeVR;

namespace ComposeVR {

    public class DeviceBrowser : RemoteEventHandler {

        private BrowserColumn resultsColumn;
        private List<BrowserColumn> filterColumns;

        void Awake() {
            Register("browser");
        }

        // Use this for initialization
        void Start() {
            filterColumns = new List<BrowserColumn>();

            foreach (BrowserColumn c in GetComponentsInChildren<BrowserColumn>()) {
                c.ItemSelected += OnItemSelected;
                c.PageChange += OnPageChanged;

                if (c.name.Equals("Results")) {
                    resultsColumn = c;
                }
                else {
                    filterColumns.Add(c);
                }

                c.gameObject.SetActive(false);
            }
        }

        // Update is called once per frame
        void Update() {

        }

        /// <summary>
        /// Opens the browser on supplied module
        /// </summary>
        /// <param name="moduleID"> The module that the browser needs to browse for</param>
        /// <param name="browserAnchor"> The transform where the browser should display</param>
        public void openBrowser(string moduleID, string deviceType) {
            resetAllColumns();
            RemoteEventEmitter.CloseBrowser(getClient());

            RemoteEventEmitter.OpenBrowser(getClient(), moduleID, deviceType);
            resultsColumn.gameObject.SetActive(true);
            resultsColumn.setTargetDeviceType(deviceType);

            foreach (BrowserColumn c in filterColumns) {
                if (c.name.Equals("Tags") && deviceType != "Presets") {
                    continue;
                }
                c.setTargetDeviceType(deviceType);
                c.gameObject.SetActive(true);
            }

            //Show canvas and set size based on total number of columns
        }

        public void closeBrowser() {
            resultsColumn.gameObject.SetActive(false);
            resultsColumn.resetColumn();

            foreach (BrowserColumn c in filterColumns) {
                c.gameObject.SetActive(false);
                c.resetColumn();
            }
        }

        private void resetAllColumns() {
            resultsColumn.resetColumn();

            foreach(BrowserColumn c in filterColumns) {
                c.resetColumn();
            }
        }


        public void OnPageChanged(object sender, BrowserColumnEventArgs e) {
            if (e.BrowserColumn.name.Equals("Results") && e.PageChange != 0) {
                RemoteEventEmitter.ChangeResultsPage(getClient(), e.PageChange);
            }
            else {
                Debug.Log(e.BrowserColumn.name);
                RemoteEventEmitter.ChangeFilterPage(getClient(), e.BrowserColumn.name, e.PageChange);
            }
        }

        public void OnItemSelected(object sender, BrowserColumnEventArgs e) {
            if (e.BrowserColumn.name.Equals("Results")) {
                RemoteEventEmitter.LoadDeviceAtIndex(getClient(), e.SelectionIndex);
                closeBrowser();
            }
            else {
                RemoteEventEmitter.SelectFilterItem(getClient(), e.BrowserColumn.name, e.SelectionIndex);
            }
        }

    }
}