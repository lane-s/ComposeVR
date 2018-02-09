using System.Collections;
using System.Collections.Generic;

namespace ComposeVR {
    /// <summary>
    /// A collection of commands which can be sent to the connected DAW
    /// </summary>
    public static class RemoteEventEmitter {

        /// <summary>
        /// Creates a new sound module (corresponding to a new track) on the DAW
        /// </summary>
        /// <param name="client"></param>
        /// <param name="senderID"> The id to give the sound module</param>
        public static void CreateSoundModule(TCPClient client, string senderID) {

            Protocol.Module.CreateSoundModule createEvent = new Protocol.Module.CreateSoundModule {
                SenderId = senderID
            };

            Protocol.ModuleEvent moduleEvent = new Protocol.ModuleEvent {
                HandlerId = "app",
                CreateSoundModuleEvent = createEvent
            };

            Protocol.Event remoteEvent = new Protocol.Event {
                ModuleEvent = moduleEvent,
                MethodName = "CreateSoundModule"
            };
            
            client.send(remoteEvent);
        }

        /// <summary>
        /// Opens a browser on a module
        /// </summary>
        /// <param name="client"></param>
        /// <param name="moduleID">The module to browse on</param>
        public static void OpenBrowser(TCPClient client, string moduleID, string deviceType) {
            Protocol.Module.OpenBrowser openEvent = new Protocol.Module.OpenBrowser {
                DeviceType = deviceType
            };

            Protocol.ModuleEvent moduleEvent = new Protocol.ModuleEvent {
                HandlerId = moduleID,
                OpenBrowserEvent = openEvent
            };

            Protocol.Event remoteEvent = new Protocol.Event {
                ModuleEvent = moduleEvent,
                MethodName = "OpenBrowser"
            };

            client.send(remoteEvent);
        }

        public static void CloseBrowser(TCPClient client) {
            Protocol.Browser.CloseBrowser closeEvent = new Protocol.Browser.CloseBrowser {
                Commit = true
            };

            Protocol.BrowserEvent browserEvent = new Protocol.BrowserEvent {
                Path = "",
                CloseBrowserEvent = closeEvent
            };

            Protocol.Event remoteEvent = new Protocol.Event {
                BrowserEvent = browserEvent,
                MethodName = "CloseBrowser"
            };

            client.send(remoteEvent);
        }

        public static void ChangeResultsPage(TCPClient client, int pageChange) {
            Protocol.Browser.ChangeResultsPage changeResultsEvent = new Protocol.Browser.ChangeResultsPage {
                PageChange = pageChange
            };

            Protocol.BrowserEvent browserEvent = new Protocol.BrowserEvent {
                Path = "",
                ChangeResultsPageEvent = changeResultsEvent
            };

            Protocol.Event remoteEvent = new Protocol.Event {
                BrowserEvent = browserEvent,
                MethodName = "ChangeResultsPage"
            };

            client.send(remoteEvent);
        }

        public static void LoadDeviceAtIndex(TCPClient client, int selectionIndex) {
            Protocol.Browser.LoadDeviceAtIndex loadEvent = new Protocol.Browser.LoadDeviceAtIndex {
                Index = selectionIndex
            };

            Protocol.BrowserEvent browserEvent = new Protocol.BrowserEvent {
                Path = "",
                LoadDeviceAtIndexEvent = loadEvent
            };

            Protocol.Event remoteEvent = new Protocol.Event {
                BrowserEvent = browserEvent,
                MethodName = "LoadDeviceAtIndex"
            };

            client.send(remoteEvent);
        }

        public static void ChangeFilterPage(TCPClient client, string columnName, int pageChange) {
            Protocol.Browser.ChangeFilterPage changeFilterEvent = new Protocol.Browser.ChangeFilterPage {
                ColumnName = columnName,
                PageChange = pageChange
            };

            Protocol.BrowserEvent browserEvent = new Protocol.BrowserEvent {
                Path = "/filter",
                ChangeFilterPageEvent = changeFilterEvent
            };

            Protocol.Event remoteEvent = new Protocol.Event {
                BrowserEvent = browserEvent,
                MethodName = "ChangeFilterPage"
            };

            client.send(remoteEvent);
        }

        public static void SelectFilterItem(TCPClient client, string columnName, int selectionIndex) {
            Protocol.Browser.SelectFilterItem selectFilterEvent = new Protocol.Browser.SelectFilterItem {
                ColumnName = columnName,
                ItemIndex = selectionIndex
            };

            Protocol.BrowserEvent browserEvent = new Protocol.BrowserEvent {
                Path = "/filter",
                SelectFilterItemEvent = selectFilterEvent
            };

            Protocol.Event remoteEvent = new Protocol.Event {
                BrowserEvent = browserEvent,
                MethodName = "SelectFilterItem"
            };

            client.send(remoteEvent);
        }

    }
}