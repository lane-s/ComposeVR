using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ComposeVR {

    using Arrow = Protocol.Browser.OnArrowVisibilityChanged.Types.Arrow;

    public class PageChangedEventArgs : EventArgs {
        public PageChangedEventArgs(string columnName, BrowserColumnController.ColumnType columnType, int pageChange) {
            this.columnName = columnName;
            this.columnType = columnType;
            this.pageChange = pageChange;
        }

        private string columnName;
        private BrowserColumnController.ColumnType columnType;
        private int pageChange;

        public string ColumnName {
            get { return columnName;  }
        }

        public BrowserColumnController.ColumnType ColumnType {
            get { return columnType; }
        }

        public int PageChange {
            get { return pageChange; }
        }
    }

    public class ItemSelectedEventArgs : EventArgs {
        public ItemSelectedEventArgs(string columnName, BrowserColumnController.ColumnType columnType, int selectionIndex) {
            this.columnName = columnName;
            this.columnType = columnType;
            this.selectionIndex = selectionIndex;
        }

        private string columnName;
        private BrowserColumnController.ColumnType columnType;
        private int selectionIndex;

        public string ColumnName {
            get { return columnName;  }
        }

        public BrowserColumnController.ColumnType ColumnType {
            get { return columnType; }
        }

        public int SelectionIndex {
            get { return selectionIndex; }
        }
    }

    public class DeviceTypeChangedEventArgs : EventArgs {
        public DeviceTypeChangedEventArgs(string deviceType) {
            this.deviceType = deviceType;
        }

        private string deviceType;

        public string DeviceType {
            get { return deviceType; }
        }
    }

    [Serializable]
    public class BrowserColumnController : RemoteEventHandler {

        private IBrowserColumn browserColumn;
        public enum ColumnType { RESULTS, FILTER};

        public event EventHandler<ItemSelectedEventArgs> ItemSelected;
        public event EventHandler<PageChangedEventArgs> PageChanged;
        public event EventHandler<DeviceTypeChangedEventArgs> DeviceTypeChanged;

        [Serializable]
        public class BrowserColumnConfiguration {
            public string Name;
            public ColumnType Type;
            public string DefaultSelection;
            public float ResultSpacing = 0;
            public float ResultStartOffset = 80;
            public int InitialSize = 15;
        }

        [Serializable]
        public class BrowserColumnState {
            public string SelectedItemName;
            public int SelectedItemIndex;
            public int ColumnSize;
            public bool UpArrowVisible;
            public bool DownArrowVisible;
        }

        public BrowserColumnConfiguration Config;
        private BrowserColumnState State = new BrowserColumnState();

        public void SetBrowserColumn(IBrowserColumn c) {
            this.browserColumn = c;
        }

        public void Initialize() {
            RegisterRemoteID("browser/" + Config.Name);

            State.ColumnSize = Config.InitialSize;
            State.SelectedItemIndex = -1;

            browserColumn.ExpandToSize(Config.InitialSize);
            browserColumn.SetArrowVisibility(Arrow.Up, false);
            browserColumn.SetArrowVisibility(Arrow.Down, false);
        }

        public void SetVisible(bool visible) {
            for(int i = 0; i < State.ColumnSize; i++) {
                if (visible) {
                    bool itemHasText = browserColumn.GetItemText(i).Length > 0;
                    browserColumn.SetItemVisibility(i, itemHasText);
                    browserColumn.SetArrowVisibility(Arrow.Up, State.UpArrowVisible);
                    browserColumn.SetArrowVisibility(Arrow.Down, State.DownArrowVisible);
                }
                else {
                    browserColumn.SetItemVisibility(i, false);
                    browserColumn.SetArrowVisibility(Arrow.Up, false);
                    browserColumn.SetArrowVisibility(Arrow.Down, false);
                }
            }
        }

        public void ResetColumn() {
            State.SelectedItemIndex = -1;
            State.SelectedItemName = "";
            SetVisible(false);
        }

        public void OnArrowClicked(Arrow arrow) {
            if(PageChanged != null) {
                int pageChange = arrow == Arrow.Down ? 1 : -1;
                var args = new PageChangedEventArgs(Config.Name, Config.Type, pageChange);
                PageChanged(this, args);
            }
        }

        public void OnItemSelected(int itemIndex) {

            //Deselect previously selected item
            if(State.SelectedItemIndex != -1 && browserColumn.GetItemText(State.SelectedItemIndex).Equals(State.SelectedItemName)) {
                browserColumn.DeselectItem(State.SelectedItemIndex);
            }

            State.SelectedItemName = browserColumn.GetItemText(itemIndex);
            State.SelectedItemIndex = itemIndex;

            browserColumn.SelectItem(itemIndex);

            if(ItemSelected != null) {
                var args = new ItemSelectedEventArgs(Config.Name, Config.Type, itemIndex);
                ItemSelected(this, args);
            }
        }

        public void OnBrowserItemChanged(Protocol.Event e) {
            int itemIndex = e.BrowserEvent.OnBrowserItemChangedEvent.ItemIndex;
            string itemName = e.BrowserEvent.OnBrowserItemChangedEvent.ItemName;
            browserColumn.UpdateItem(itemIndex, itemName);

            //Make default selection
            if(State.SelectedItemIndex == -1 && itemName.Equals(Config.DefaultSelection)) {
                OnItemSelected(itemIndex);
            }

            bool itemIsSelected = State.SelectedItemIndex == itemIndex && itemName.Equals(itemName);

            if(itemIsSelected) {
                browserColumn.SelectItem(itemIndex);
            }
            else {
                browserColumn.DeselectItem(itemIndex);
            }
        }

        public void OnBrowserColumnChanged(Protocol.Event e) {

            int totalResults = e.BrowserEvent.OnBrowserColumnChangedEvent.TotalResults;
            int resultsPerPage = e.BrowserEvent.OnBrowserColumnChangedEvent.ResultsPerPage;

            browserColumn.ExpandToSize(resultsPerPage);
            State.ColumnSize = Math.Max(resultsPerPage, State.ColumnSize);

            if(DeviceTypeChanged != null) {
                var args = new DeviceTypeChangedEventArgs(e.BrowserEvent.OnBrowserColumnChangedEvent.DeviceType);
                DeviceTypeChanged(this, args);
            }
        }

        public void OnArrowVisibilityChanged(Protocol.Event e) {
            Protocol.Browser.OnArrowVisibilityChanged visChanged = e.BrowserEvent.OnArrowVisibilityChangedEvent;

            if(visChanged.Arrow == Arrow.Up) {
                State.UpArrowVisible = visChanged.Visible;
            }
            else {
                State.DownArrowVisible = visChanged.Visible;
            }

            browserColumn.SetArrowVisibility(visChanged.Arrow, visChanged.Visible);
        }
    }
}
