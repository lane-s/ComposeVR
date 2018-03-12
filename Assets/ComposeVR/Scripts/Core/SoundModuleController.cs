using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using OSCsharp.Data;

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

            playingNotes = new int[127];
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
            NoteData incomingMIDI = data as NoteData;

            if(incomingMIDI != null) {
                PlayMIDINote(incomingMIDI);
            }
        }

        private OscTimeTag now;
        private OscBundle bundle;
        private string OSCNoteAddress;
        private int[] playingNotes;

        private void PlayMIDINote(NoteData data) {

            string noteStatus;
            if(data.NoteStatus == NoteData.Status.On) {
                noteStatus = "on";
                playingNotes[data.Note] += 1;
            }
            else {
                noteStatus = "off";
                playingNotes[data.Note] -= 1;
                if(playingNotes[data.Note] < 0) {
                    playingNotes[data.Note] = 0;
                }

                //Only send a note off message if none of the inputs are playing the note
                if(playingNotes[data.Note] != 0) {
                    return;
                }
            }

            string noteAddress = "/" + GetID() + "/note/" + noteStatus;

            int MIDI = data.Note;
            MIDI |= ((int)(data.Velocity) << 16);

            OscMessage noteMessage = new OscMessage(noteAddress, MIDI);
            Module.GetOSCEventDispatcher().SendOSCPacket(noteAddress, noteMessage);
        }

    }
}
