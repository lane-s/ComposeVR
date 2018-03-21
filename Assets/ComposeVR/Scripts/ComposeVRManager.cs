using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ComposeVR {
    public class ComposeVRManager : SingletonObject<ComposeVRManager> {

        private DeviceBrowserObject browserObject;
        private NoteChooser noteChooser;
        private SoundModuleMenu soundModuleMenu;

        private ComposeVROSCEventDispatcher oscEventDispatcher;
        private int handlerCount = 0;

        public DeviceBrowserObject DeviceBrowserObject {
            get { return browserObject; }
        }

        public NoteChooser NoteChooserObject {
            get { return noteChooser; }
        }

        public SoundModuleMenu SoundModuleMenu {
            get { return soundModuleMenu; }
        }

        public ComposeVROSCEventDispatcher OSCEventDispatcher {
            get { return oscEventDispatcher; }
        }

        // Use this for initialization
        void Awake() {
            browserObject = transform.Find("DeviceBrowser").GetComponent<DeviceBrowserObject>();
            noteChooser = transform.Find("NoteChooser").GetChild(0).GetComponent<NoteChooser>();
            soundModuleMenu = transform.Find("SoundModuleMenu").GetComponent<SoundModuleMenu>();

            oscEventDispatcher = transform.Find("OSCEventDispatcher").GetComponent<ComposeVROSCEventDispatcher>();
        }

        public string GetNewHandlerID() {
            string result = "ComposeVRObject-"+handlerCount;
            handlerCount += 1;

            return result;
        }
    }
}
