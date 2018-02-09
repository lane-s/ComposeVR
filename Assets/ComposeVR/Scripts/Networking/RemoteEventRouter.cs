using System.Collections;
using System.Collections.Generic;
using System;
using System.Text;
using UnityEngine;
using ComposeVR.Protocol;

namespace ComposeVR {

    public class RemoteEventRouter : MonoBehaviour {

        private Dictionary<string, RemoteEventHandler> handlerDictionary;

        // Use this for initialization
        void Awake() {
        }

        // Update is called once per frame
        void Update() {

        }

        private Dictionary<string, RemoteEventHandler> getHandlerDictionary() {
            if (handlerDictionary == null) {
                handlerDictionary = new Dictionary<string, RemoteEventHandler>();
            }
            return handlerDictionary;
        }

        public void routeEvent(Protocol.Event e) {
            string receiverID = "";

            switch (e.EventCase) {
                case Protocol.Event.EventOneofCase.ModuleEvent:
                    receiverID = e.ModuleEvent.HandlerId;
                    break;
                case Protocol.Event.EventOneofCase.BrowserEvent:
                    receiverID = "browser" + e.BrowserEvent.Path;
                    break;
                default:
                    Debug.LogError("Event not recognized");
                    break;
            }

            if (getHandlerDictionary().ContainsKey(receiverID)) {
                RemoteEventHandler receiver = getHandlerDictionary()[receiverID];
                receiver.handleEvent(e);
            }
        }

        public void addReceiver(string receiverID, RemoteEventHandler receiver) {
            getHandlerDictionary().Add(receiverID, receiver);
        }

        public void removeReceiverIfPresent(string receiverID) {
            getHandlerDictionary().Remove(receiverID);
        }

    }
}