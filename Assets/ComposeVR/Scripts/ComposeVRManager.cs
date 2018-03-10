using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ComposeVR {
    public class ComposeVRManager : SingletonObject<ComposeVRManager> {

        private DeviceBrowserObject browserObject;
        private ComposeVROSCEventDispatcher oscEventDispatcher;
        private int handlerCount = 0;

        // Use this for initialization
        void Awake() {
            browserObject = GameObject.FindGameObjectWithTag("DeviceBrowser").GetComponent<DeviceBrowserObject>();
            oscEventDispatcher = GameObject.FindGameObjectWithTag("OSCEventDispatcher").GetComponent<ComposeVROSCEventDispatcher>();
        }

        public DeviceBrowserObject GetBrowserObject() {
            return browserObject;
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
