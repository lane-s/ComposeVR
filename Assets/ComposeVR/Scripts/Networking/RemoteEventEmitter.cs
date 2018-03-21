using System;
using System.Collections;
using System.Collections.Generic;

namespace ComposeVR {
    /// <summary>
    /// A collection of commands which can be sent to the connected DAW
    /// </summary>
    [Serializable]
    public class RemoteEventEmitter {
        private static RemoteEventEmitter instance;
        private RemoteEventEmitter() { }

        public static RemoteEventEmitter Instance {
            get {
                if(instance == null) {
                    instance = new RemoteEventEmitter();
                }
                return instance;
            }
        }
        
        private IEventEmitter Emitter;

        public void SetEmitter(IEventEmitter e) {
            Emitter = e;
        }

        /// <summary>
        /// Creates a new sound module (corresponding to a new track) on the DAW
        /// </summary>
        /// <param name="client"></param>
        /// <param name="senderID"> The id to give the sound module</param>
        public void CreateSoundModule(string senderID) {

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
            
            Emitter.EmitEvent(remoteEvent);
        }

        /// <summary>
        /// Opens a browser on a module
        /// </summary>
        /// <param name="client"></param>
        /// <param name="moduleID">The module to browse on</param>
        public void OpenBrowser(string moduleID, string deviceType, string contentType, int deviceIndex, bool replaceDevice) {
            Protocol.Module.OpenBrowser openEvent = new Protocol.Module.OpenBrowser {
                DeviceType = deviceType,
                ContentType = contentType,
                DeviceIndex = deviceIndex,
                ReplaceDevice = replaceDevice
            };

            Protocol.ModuleEvent moduleEvent = new Protocol.ModuleEvent {
                HandlerId = moduleID,
                OpenBrowserEvent = openEvent
            };

            Protocol.Event remoteEvent = new Protocol.Event {
                ModuleEvent = moduleEvent,
                MethodName = "OpenBrowser"
            };

            Emitter.EmitEvent(remoteEvent);
        }

        public void CloseBrowser() {
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

            Emitter.EmitEvent(remoteEvent);
        }

        public void ChangeResultsPage(int pageChange) {
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

            Emitter.EmitEvent(remoteEvent);
        }

        public void LoadDeviceAtIndex(int selectionIndex) {
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

            Emitter.EmitEvent(remoteEvent);
        }

        public void ChangeFilterPage(string columnName, int pageChange) {
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

            Emitter.EmitEvent(remoteEvent);
        }

        public void SelectFilterItem(string columnName, int selectionIndex) {
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

            Emitter.EmitEvent(remoteEvent);
        }

    }

}