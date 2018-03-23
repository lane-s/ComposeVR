﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ComposeVR {

    [Serializable]
    public class DeviceBrowserController : RemoteEventHandler {

        public event EventHandler<EventArgs> BrowserClosed;

        private IDeviceBrowser deviceBrowser;

        [Serializable]
        public class DeviceBrowserState {
            public BrowserColumnController ResultColumn;
            public List<BrowserColumnController> FilterColumns;
            public string TargetDeviceType;
            public string CurrentDeviceType;
        }

        private DeviceBrowserState State = new DeviceBrowserState();

        public void SetDeviceBrowser(IDeviceBrowser b) {
            deviceBrowser = b;
        }

        public void Initialize() {
            RegisterRemoteID("browser");
            InitializeColumns();
            SetVisible(false);
        }

        private void InitializeColumns() {
            State.ResultColumn = deviceBrowser.GetResultColumn();
            State.FilterColumns = deviceBrowser.GetFilterColumns();

            State.ResultColumn.PageChanged += OnPageChanged;
            State.ResultColumn.ItemSelected += OnItemSelected;
            State.ResultColumn.DeviceTypeChanged += OnDeviceTypeChanged;

            foreach(BrowserColumnController c in State.FilterColumns) {
                c.PageChanged += OnPageChanged;
                c.ItemSelected += OnItemSelected;
                c.DeviceTypeChanged += OnDeviceTypeChanged;
            }
        }

        public void OpenBrowser(string moduleID, string deviceType, string contentType, int deviceIndex, bool replaceDevice) {
            if(BrowserClosed != null) {
                BrowserClosed(this, new EventArgs());
            }

            RemoteEventEmitter.Instance.CloseBrowser();
            RemoteEventEmitter.Instance.OpenBrowser(moduleID, deviceType, contentType, deviceIndex, replaceDevice);

            State.TargetDeviceType = deviceType;
            ResetBrowser();
            SetVisible(true);
        }

        public void CloseBrowser() {
            if(BrowserClosed != null) {
                BrowserClosed(this, new EventArgs());
            }

            SetVisible(false);
            deviceBrowser.Hide();
        }

        private void SetVisible(bool visible) {
            State.ResultColumn.SetVisible(visible);

            foreach(BrowserColumnController c in State.FilterColumns) {
                c.SetVisible(visible);
            }
        }

        private void ResetBrowser() {
            foreach(BrowserColumnController c in State.FilterColumns){
                c.ResetColumn();
            }

            State.ResultColumn.ResetColumn();

            State.CurrentDeviceType = "";
        }

        private void OnPageChanged(object sender, PageChangedEventArgs e) {
            if (e.ColumnType == BrowserColumnController.ColumnType.RESULTS) {
                RemoteEventEmitter.Instance.ChangeResultsPage(e.PageChange);
            }
            else if(e.ColumnType == BrowserColumnController.ColumnType.FILTER) {
                RemoteEventEmitter.Instance.ChangeFilterPage(e.ColumnName, e.PageChange);
            }
        }

        private void OnItemSelected(object sender, ItemSelectedEventArgs e) {
            if (e.ColumnType == BrowserColumnController.ColumnType.RESULTS) {
                RemoteEventEmitter.Instance.SelectResult(e.SelectionIndex);
            }
            else if(e.ColumnType == BrowserColumnController.ColumnType.FILTER) {
                RemoteEventEmitter.Instance.SelectFilterItem(e.ColumnName, e.SelectionIndex);
            }
        }

        private void OnDeviceTypeChanged(object sender, DeviceTypeChangedEventArgs e) {
            State.CurrentDeviceType = e.DeviceType;
            SetVisible(true);
            /*
            if (State.CurrentDeviceType.Equals(State.TargetDeviceType)) {
                SetVisible(true);
            }
            else {
                SetVisible(false);
            }*/
        }

        public void OnConfirmButtonClicked() {
            CloseBrowser();
            RemoteEventEmitter.Instance.CommitSelection(true);
        }

        public void OnCancelButtonClicked() {
            CloseBrowser();
            RemoteEventEmitter.Instance.CommitSelection(false);
        }

    }
}
