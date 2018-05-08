using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using OSCsharp.Data;

namespace ComposeVR {
    [Serializable]
    public class SoundModuleController : RemoteEventHandler, IPhysicalDataInput {

        [Serializable]
        public class SoundModuleConfiguration {
            public float browserYOffset;
            public float moduleMenuYOffset;
        }

        public SoundModuleConfiguration Config;
        private ISoundModule Module;

        public void Initialize() {
            //Register with the router
            Register();

            //Command DAW to create a new sound module
            RemoteEventEmitter.Instance.CreateSoundModule(GetID());

            Module.GetInputJack().AddInput(this);

            playingNotes = new int[127];
        } 

        private void RequestMenu() {
            Debug.Log("Sound module menu requested");
            SoundModuleMenu menu = Module.GetModuleMenu();
            menu.MenuClosed += OnMenuClosed;
            menu.ChangeInstrumentButtonClicked += OnChangeInstrumentButtonClicked;
            menu.LoadPresetButtonClicked += OnLoadPresetButtonClicked;
        }

        private void OnMenuClosed(object sender, EventArgs e) {
            SoundModuleMenu menu = Module.GetModuleMenu();
            menu.MenuClosed -= OnMenuClosed;
            menu.ChangeInstrumentButtonClicked -= OnChangeInstrumentButtonClicked;
            menu.LoadPresetButtonClicked -= OnLoadPresetButtonClicked;
        }

        private void OnChangeInstrumentButtonClicked(object sender, EventArgs e) {
            Debug.Log("Change instruments");
            Module.GetModuleMenu().Display(false);
            RequestBrowser("Instrument", "Devices", 0, true, false);
        }

        private void OnLoadPresetButtonClicked(object sender, EventArgs e) {
            Module.GetModuleMenu().Display(false);
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
        }

        private void OnBrowserClosed(object sender, EventArgs e) {
            DeviceBrowserController browser = Module.GetBrowserController();
            browser.BrowserClosed -= OnBrowserClosed;

            RequestMenu();
        }

        void IPhysicalDataInput.ReceiveData(PhysicalDataPacket data) {
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

        public int[] GetPlayingNotes() {
            return playingNotes;
        }

    }
}
