using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ComposeVR {
    public class ComposeVRManager : SingletonObject<ComposeVRManager> {

        private DeviceBrowserObject browserObject;
        private UDPClient udpClient;

        // Use this for initialization
        void Awake() {
            browserObject = GameObject.FindGameObjectWithTag("DeviceBrowser").GetComponent<DeviceBrowserObject>();
            udpClient = GameObject.FindGameObjectWithTag("UDPClient").GetComponent<UDPClient>();
        }

        public DeviceBrowserObject GetBrowserObject() {
            return browserObject;
        }

        public UDPClient GetUDPClient() {
            return udpClient;
        }
    }
}
