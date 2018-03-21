using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using VRTK;
using OSCsharp.Data;

namespace ComposeVR {
    /// <summary>
    /// Sound Modules contain samples or virtual instruments
    /// </summary>
    public sealed class SoundModuleObject : MonoBehaviour, IModule {

        public Text GainDisplay;
        public SoundModuleController Module;
        private InputJack input;

        private const float FADER_MIN = -252f;
        private const float FADER_MAX = 6.021f;
        private const float INITIAL_FADER_POS = 0.540f;

        void Awake() {
            input = GetComponentInChildren<InputJack>();

            Module.SetController(this);
            Module.Initialize();

            //Throwaway code for temporary volume faders
            GetComponentInChildren<Fader>().FaderValueChanged += OnFaderValueChanged;
            SetInitialFaderPosition();
        }

        private void SetInitialFaderPosition() {
            GetComponentInChildren<Fader>().SetNormalizedValue(INITIAL_FADER_POS);
            SetGainDisplayText(FaderPercentageToGain(INITIAL_FADER_POS));
        }

        private float FaderPercentageToGain(float faderPercentage) {
            float logPercentage = Utility.LinearToLog(1.0f-faderPercentage+0.1f, 0.1f, 1.1f) - 0.1f;
            logPercentage = Utility.LinearToLog(logPercentage + 0.1f, 0.1f, 1.1f) - 0.1f;

            return logPercentage * (FADER_MIN - FADER_MAX) + FADER_MAX;
        }

        private void OnFaderValueChanged(object sender, Control3DEventArgs e) {
            float newGain = FaderPercentageToGain(e.normalizedValue);
            SetGainDisplayText(newGain);

            string address = "/" + Module.GetID() + "/trackParam/volume";
            OscMessage volumeChange = new OscMessage(address, e.normalizedValue);

            ComposeVRManager.Instance.OSCEventDispatcher.SendOSCPacket(address, volumeChange);
        }

        private void SetGainDisplayText(float gain) {
            string gainText = string.Format("{0:0.0}", Mathf.Abs(gain)) + "dB";
            if(gain >= 0) {
                gainText = "+ " + gainText;
            }
            else {
                gainText = "- " + gainText;
            }

            GainDisplay.text = gainText;
        }

        void IModule.PositionBrowser() {
            DeviceBrowserObject browser = ComposeVRManager.Instance.DeviceBrowserObject;

            //Position browser above module
            browser.transform.position = transform.position + Vector3.up * Module.Config.browserYOffset;

            //Rotate browser towards user's headset
            Quaternion lookAtPlayer = Quaternion.LookRotation(browser.transform.position - GameObject.FindGameObjectWithTag("Headset").transform.position);
            browser.transform.rotation = Quaternion.Euler(browser.transform.rotation.eulerAngles.x, lookAtPlayer.eulerAngles.y, browser.transform.rotation.eulerAngles.z);
        }

        void IModule.PositionModuleMenu() {
            SoundModuleMenu menu = ComposeVRManager.Instance.SoundModuleMenu;

            menu.transform.position = transform.position + Vector3.up * Module.Config.moduleMenuYOffset;

            Quaternion lookAtPlayer = Quaternion.LookRotation(menu.transform.position - GameObject.FindGameObjectWithTag("Headset").transform.position);
            menu.transform.rotation = Quaternion.Euler(menu.transform.rotation.eulerAngles.x, lookAtPlayer.eulerAngles.y, menu.transform.rotation.eulerAngles.z);
        }

        DeviceBrowserController IModule.GetBrowserController() {
            return ComposeVRManager.Instance.DeviceBrowserObject.Controller;
        }

        ComposeVROSCEventDispatcher IModule.GetOSCEventDispatcher() {
            return ComposeVRManager.Instance.OSCEventDispatcher;
        }

        InputJack IModule.GetInputJack() {
            return input;
        }

        SoundModuleMenu IModule.GetModuleMenu() {
            return ComposeVRManager.Instance.SoundModuleMenu;
        }
    }

    public interface IModule {
        void PositionBrowser();
        void PositionModuleMenu();
        DeviceBrowserController GetBrowserController();
        ComposeVROSCEventDispatcher GetOSCEventDispatcher();
        InputJack GetInputJack();
        SoundModuleMenu GetModuleMenu();
    }
}