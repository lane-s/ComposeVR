using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ComposeVR
{
    public class BrowserEventArgs : EventArgs
    {
        public string SelectedResult;
        public bool SelectionConfirmed;
    }

    [Serializable]
    public class DeviceBrowserController : RemoteEventHandler
    {

        public event EventHandler<BrowserEventArgs> BrowserClosed;
        public event EventHandler<BrowserEventArgs> BrowserSelectionChanged;

        private IDeviceBrowser deviceBrowser;

        [Serializable]
        public class DeviceBrowserState
        {
            public BrowserColumnController ResultColumn;
            public List<BrowserColumnController> FilterColumns;
            public string TargetDeviceType;
            public string CurrentDeviceType;
            public string SelectedResult;

            public bool displayTagColumn;
        }

        private DeviceBrowserState State = new DeviceBrowserState();
        private BrowserEventArgs browserEventArgs;

        public void SetDeviceBrowser(IDeviceBrowser b)
        {
            deviceBrowser = b;
        }

        public void Initialize()
        {
            RegisterRemoteID("browser");
            InitializeColumns();
            SetVisible(false);

            browserEventArgs = new BrowserEventArgs();
            State.SelectedResult = "";
        }

        private void InitializeColumns()
        {
            State.ResultColumn = deviceBrowser.GetResultColumn();
            State.FilterColumns = deviceBrowser.GetFilterColumns();

            State.ResultColumn.PageChanged += OnPageChanged;
            State.ResultColumn.ItemSelected += OnItemSelected;
            State.ResultColumn.DeviceTypeChanged += OnDeviceTypeChanged;

            foreach (BrowserColumnController c in State.FilterColumns)
            {
                c.PageChanged += OnPageChanged;
                c.ItemSelected += OnItemSelected;
                c.DeviceTypeChanged += OnDeviceTypeChanged;
            }
        }

        public void OpenBrowser(string moduleID, string deviceType, string contentType, int deviceIndex, bool replaceDevice, bool displayTagColumn)
        {
            if (BrowserClosed != null)
            {
                browserEventArgs.SelectedResult = State.SelectedResult;
                BrowserClosed(this, browserEventArgs);
            }

            RemoteEventEmitter.Instance.CloseBrowser();
            RemoteEventEmitter.Instance.OpenBrowser(moduleID, deviceType, contentType, deviceIndex, replaceDevice);

            State.TargetDeviceType = deviceType;
            ResetBrowser();

            State.displayTagColumn = displayTagColumn;
            SetVisible(true);
        }

        public void CloseBrowser(bool selectionConfirmed)
        {
            if (BrowserClosed != null)
            {
                browserEventArgs.SelectedResult = State.SelectedResult;
                browserEventArgs.SelectionConfirmed = selectionConfirmed;
                BrowserClosed(this, browserEventArgs);
            }

            SetVisible(false);
            deviceBrowser.Hide();
        }

        private void SetVisible(bool visible)
        {
            State.ResultColumn.SetVisible(visible);

            foreach (BrowserColumnController c in State.FilterColumns)
            {
                if (c.Config.Name.Equals("Tags"))
                {
                    c.SetVisible(visible && State.displayTagColumn);
                }
                else
                {
                    c.SetVisible(visible);
                }
            }
        }

        private void ResetBrowser()
        {
            foreach (BrowserColumnController c in State.FilterColumns)
            {
                c.ResetColumn();
            }

            State.ResultColumn.ResetColumn();

            State.CurrentDeviceType = "";
            State.displayTagColumn = false;
        }

        private void OnPageChanged(object sender, PageChangedEventArgs e)
        {
            if (e.ColumnType == BrowserColumnController.ColumnType.RESULTS)
            {
                RemoteEventEmitter.Instance.ChangeResultsPage(e.PageChange);
            }
            else if (e.ColumnType == BrowserColumnController.ColumnType.FILTER)
            {
                RemoteEventEmitter.Instance.ChangeFilterPage(e.ColumnName, e.PageChange);
            }
        }

        private void OnItemSelected(object sender, ItemSelectedEventArgs e)
        {
            if (e.ColumnType == BrowserColumnController.ColumnType.RESULTS)
            {
                RemoteEventEmitter.Instance.SelectResult(e.SelectionIndex);
                State.SelectedResult = e.SelectionName;

                browserEventArgs.SelectedResult = e.SelectionName;
                browserEventArgs.SelectionConfirmed = false;
                if (BrowserSelectionChanged != null)
                {
                    BrowserSelectionChanged(this, browserEventArgs);
                }
            }
            else if (e.ColumnType == BrowserColumnController.ColumnType.FILTER)
            {
                RemoteEventEmitter.Instance.SelectFilterItem(e.ColumnName, e.SelectionIndex);
            }
        }

        private void OnDeviceTypeChanged(object sender, DeviceTypeChangedEventArgs e)
        {
            State.CurrentDeviceType = e.DeviceType;

            /*
            if (State.CurrentDeviceType.Equals(State.TargetDeviceType)) {
                SetVisible(true);
            }
            else {
                SetVisible(false);
            }*/
        }

        public void OnConfirmButtonClicked()
        {
            CloseBrowser(true);
            RemoteEventEmitter.Instance.CommitSelection(true);
        }

        public void OnCancelButtonClicked()
        {
            CloseBrowser(false);
            RemoteEventEmitter.Instance.CommitSelection(false);
        }

    }
}
