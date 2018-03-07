using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;

namespace ComposeVR {
    [Serializable]
    public class SoundModuleController : RemoteEventHandler, IJackInput {

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

            Module.GetInputJack().AddInput(this);
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

        void IJackInput.ReceiveData(WireData data) {
            MIDIData incomingMIDI = data as MIDIData;

            if(incomingMIDI != null) {
                PlayMIDINote(incomingMIDI);
            }
        }

        private void PlayMIDINote(MIDIData data) {

            Protocol.Module.MIDINote noteEvent = new Protocol.Module.MIDINote {
                MIDI = Google.Protobuf.ByteString.CopyFrom(data.GetPackedMessage())
            };

            Protocol.ModuleEvent moduleEvent = new Protocol.ModuleEvent {
                HandlerId = GetID(),
                MidiNoteEvent = noteEvent
            };

            Protocol.Event remoteEvent = new Protocol.Event {
                ModuleEvent = moduleEvent,
                MethodName = "PlayMIDINote"
            };

            Module.GetUDPClient().send(remoteEvent);
        }

    }
}
