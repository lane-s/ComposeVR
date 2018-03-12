using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ComposeVR {
    public class ComposeVRManager : SingletonObject<ComposeVRManager> {

        private DeviceBrowserObject browserObject;
        private NoteChooser noteChooser;

        private ComposeVROSCEventDispatcher oscEventDispatcher;
        private int handlerCount = 0;

        // Use this for initialization
        void Awake() {
            browserObject = transform.Find("DeviceBrowser").GetComponent<DeviceBrowserObject>();
            noteChooser = transform.Find("NoteChooser").GetChild(0).GetComponent<NoteChooser>();

            oscEventDispatcher = transform.Find("OSCEventDispatcher").GetComponent<ComposeVROSCEventDispatcher>();
        }

        public DeviceBrowserObject GetBrowserObject() {
            return browserObject;
        }

        public NoteChooser GetNoteChooserObject() {
            return noteChooser;
        }

        public ComposeVROSCEventDispatcher GetOSCEventDispatcher() {
            return oscEventDispatcher;
        }

        public string GetNewHandlerID() {
            string result = "ComposeVRObject-"+handlerCount;
            handlerCount += 1;

            return result;
        }
    }
}
