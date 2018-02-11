using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ComposeVR {
    public class ComposeVRManager : SingletonObject<ComposeVRManager> {

        private DeviceBrowserObject BrowserObject;

        // Use this for initialization
        void Awake() {
            BrowserObject = GameObject.FindGameObjectWithTag("DeviceBrowser").GetComponent<DeviceBrowserObject>();
        }

        public DeviceBrowserObject GetBrowserObject() {
            return BrowserObject;
        }
    }
}
