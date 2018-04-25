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
    [RequireComponent(typeof(VRTK_InteractableObject))]
    public sealed class SoundModuleObject : MonoBehaviour, IModule {

        public Transform FaderSystemPrefab;
        public SoundModuleController Module;
        private PhysicalDataInput input;

        private const float FADER_MIN = -252f;
        private const float FADER_MAX = 6.021f;
        private const float INITIAL_FADER_POS = 0.540f;

        private bool isPlaced = false;
        private Transform faderSystem;
        private Text gainDisplay;

        void Awake() {
            input = GetComponentInChildren<PhysicalDataInput>();

            Module.SetController(this);

            GetComponent<VRTK_InteractableObject>().InteractableObjectUngrabbed += OnUngrabbed;
        }

        void OnUngrabbed(object sender, InteractableObjectEventArgs e) {
            if (!isPlaced) {
                Debug.Log("Ungrabbed!");
                InitializeFaderSystem();
                GetComponentInChildren<CordDispenser>().enabled = true;
                Module.Initialize();
                isPlaced = true;
            } 
        }
        
        void Update() {
            if (Input.GetKeyDown(KeyCode.F)) {
                Debug.Log("Notes playing on module " + Module.GetID());
                int[] playingNotes = Module.GetPlayingNotes();
                for(int i = 0; i < playingNotes.Length; i++) {
                    if(playingNotes[i] > 0) {
                        Debug.Log(i + " is playing on " + playingNotes[i] + " orbs");
                    }
                }
            }
        }

        private void InitializeFaderSystem() {
            Transform faderAttachPoint = transform.Find("FaderAttachPoint");
            faderSystem = Instantiate(FaderSystemPrefab, faderAttachPoint.position, faderAttachPoint.rotation);
            faderSystem.GetComponent<VRTK_TransformFollow>().gameObjectToFollow = faderAttachPoint.gameObject;
            //Throwaway code for temporary volume faders
            faderSystem.GetComponentInChildren<Fader>().FaderValueChanged += OnFaderValueChanged;
            gainDisplay = faderSystem.GetComponentInChildren<Text>();
            SetInitialFaderPosition();
        }

        private void SetInitialFaderPosition() {
            faderSystem.GetComponentInChildren<Fader>().SetNormalizedValue(INITIAL_FADER_POS);
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

            gainDisplay.text = gainText;
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

        PhysicalDataInput IModule.GetInputJack() {
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
        PhysicalDataInput GetInputJack();
        SoundModuleMenu GetModuleMenu();
    }
}