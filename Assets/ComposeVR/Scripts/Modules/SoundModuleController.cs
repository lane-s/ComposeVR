using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;

namespace ComposeVR {
    [Serializable]
    public class SoundModuleController : RemoteEventHandler {

        [Serializable]
        public class SoundModuleConfiguration {
            public float browserYOffset;
        }

        public SoundModuleConfiguration Config;
        private IModule Module;

        public void Initialize() {
            //Register with the router
            Register();

            //Command DAW to create a new sound module
            RemoteEventEmitter.Instance.CreateSoundModule(GetID());
        } 

        public void SetController(IModule controller) {
            this.Module = controller;
        }


        /// <summary>
        /// Event triggered after the DAW has successfully created a new sound module
        /// </summary>
        /// 
        public void OnSoundModuleCreated(Protocol.Event e) {
            Module.PositionBrowserAtModule();

            Debug.Log("Track created with id " + GetID());
            Module.GetBrowserController().OpenBrowser(GetID(), "Instrument");
        }
    }
}
