﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ComposeVR;

namespace ComposeVR {
    /// <summary>
    /// Sound Modules contain samples or virtual instruments
    /// </summary>
    public sealed class SoundModuleObject : MonoBehaviour, IModule {

        public SoundModuleController Module;
        private InputJack input;

        void Awake() {
            input = GetComponentInChildren<InputJack>();

            Module.SetController(this);
            Module.Initialize();
        }

        void IModule.PositionBrowserAtModule() {
            DeviceBrowserObject browser = ComposeVRManager.Instance.DeviceBrowserObject;

            //Position browser above module
            browser.transform.position = transform.position + Vector3.up * Module.Config.browserYOffset;

            //Rotate browser towards user's headset
            Quaternion lookAtPlayer = Quaternion.LookRotation(browser.transform.position - GameObject.FindGameObjectWithTag("Headset").transform.position);
            browser.transform.rotation = Quaternion.Euler(browser.transform.rotation.eulerAngles.x, lookAtPlayer.eulerAngles.y, browser.transform.rotation.eulerAngles.z);
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
    }

    public interface IModule {
        void PositionBrowserAtModule();
        DeviceBrowserController GetBrowserController();
        ComposeVROSCEventDispatcher GetOSCEventDispatcher();
        InputJack GetInputJack();
    }
}