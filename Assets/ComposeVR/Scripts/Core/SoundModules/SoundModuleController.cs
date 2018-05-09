using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using OSCsharp.Data;

namespace ComposeVR {
    public class SoundModuleEventArgs : EventArgs {
        public string ModuleName;
    }

    [Serializable]
    public class SoundModuleController : RemoteEventHandler, IPhysicalDataInput {

        public event EventHandler<SoundModuleEventArgs> ModuleNameChanged;

        [Serializable]
        public class SoundModuleConfiguration {
            public float browserYOffset;
        }

        [Serializable]
        public class SoundModuleState {
            public string ModuleName = "New Module";
            public bool ModuleLoaded;
            public bool Browsing;
        }

        public SoundModuleConfiguration Config;
        private ISoundModule Module;

        private SoundModuleState State = new SoundModuleState();
        private SoundModuleEventArgs soundModuleEventArgs;

        public void Initialize() {
            soundModuleEventArgs = new SoundModuleEventArgs();

            //Register with the router
            Register();

            //Command DAW to create a new sound module
            RemoteEventEmitter.Instance.CreateSoundModule(GetID());

            Module.GetInputJack().AddInput(this);

            playingNotes = new int[127];
        } 

        public void OnChangeInstrumentButtonClicked() {
            Debug.Log("Change instruments");
            RequestBrowser("Instrument", "Devices", 0, true, false);
        }

        public void OnLoadPresetButtonClicked() {
            RequestBrowser("Instrument", "", 0, true, true);
        }

        public void SetController(ISoundModule controller) {
            this.Module = controller;
        }

        /// <summary>
        /// Event triggered after the DAW has successfully created a new sound module
        /// </summary>
        /// 
        public void OnSoundModuleCreated(Protocol.Event e) {
            Debug.Log("Track created with id " + GetID());
            RequestBrowser("Instrument", "Devices", 0, false, false);
        }

        private void RequestBrowser(string deviceType, string contentType, int deviceIndex, bool replaceDevice, bool displayTagColumn) {
            Module.PositionBrowser();

            DeviceBrowserController browser = Module.GetBrowserController();

            browser.OpenBrowser(GetID(), deviceType, contentType, deviceIndex, replaceDevice, displayTagColumn);
            browser.BrowserClosed += OnBrowserClosed;
            browser.BrowserSelectionChanged += OnBrowserSelectionChanged;
            Module.AllowPointerSelection(false);
            State.Browsing = true;
        }

        private void OnBrowserClosed(object sender, BrowserEventArgs e) {
            DeviceBrowserController browser = Module.GetBrowserController();
            browser.BrowserClosed -= OnBrowserClosed;
            browser.BrowserSelectionChanged -= OnBrowserSelectionChanged;
            Module.AllowPointerSelection(true);
            State.Browsing = false;
        }

        private void OnBrowserSelectionChanged(object sender, BrowserEventArgs e) {
            State.ModuleName = e.SelectedResult;
            soundModuleEventArgs.ModuleName = State.ModuleName;

            if (!State.ModuleLoaded) {
                State.ModuleLoaded = true;
                //TODO: Make module glow brighter indicate some sound has been loaded
            }

            if(ModuleNameChanged != null) {
                ModuleNameChanged(this, soundModuleEventArgs);
            }
        }

        void IPhysicalDataInput.ReceiveData(PhysicalDataPacket data) {
            NoteData incomingMIDI = data as NoteData;

            if(incomingMIDI != null) {
                PlayMIDINote(incomingMIDI);
            }
        }

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

        public int[] GetPlayingNotes() {
            return playingNotes;
        }

        public bool IsBrowsing() {
            return State.Browsing;
        }

        public string GetName() {
            return State.ModuleName;
        }
    }
}
