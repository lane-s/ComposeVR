using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ComposeVR {
    public class ComposeVRManager : SingletonObject<ComposeVRManager> {
        public NoteColorScheme NoteColors;

        private DeviceBrowserObject browserObject;
        private NoteSelector noteSelector;
        private SoundModuleMenu soundModuleMenu;

        private ComposeVROSCEventDispatcher oscEventDispatcher;
        private int handlerCount = 0;

        public DeviceBrowserObject DeviceBrowserObject {
            get {
                if(browserObject == null) {
                    browserObject = transform.Find("DeviceBrowser").GetComponent<DeviceBrowserObject>();
                }
                return browserObject;
            }
        }

        public NoteSelector NoteSelectorObject {
            get {
                if(noteSelector == null) {
                    noteSelector = transform.Find("NoteSelector").GetComponent<NoteSelector>();
                }
                return noteSelector;
            }
        }

        public SoundModuleMenu SoundModuleMenu {
            get {
                if(soundModuleMenu == null) {
                    soundModuleMenu = transform.Find("SoundModuleMenu").GetComponent<SoundModuleMenu>();
                }
                return soundModuleMenu;
            }
        }

        public ComposeVROSCEventDispatcher OSCEventDispatcher {
            get {
                if(oscEventDispatcher == null) {
                    oscEventDispatcher = transform.Find("OSCEventDispatcher").GetComponent<ComposeVROSCEventDispatcher>();
                }
                return oscEventDispatcher;
            }
        }

        // Use this for initialization
        void Awake() {
        }

        public string GetNewHandlerID() {
            string result = "ComposeVRObject-"+handlerCount;
            handlerCount += 1;

            return result;
        }
    }
}
