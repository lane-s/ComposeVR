using System.Collections;
using System.Collections.Generic;
using System;
using System.Text;
using UnityEngine;
using ComposeVR.Protocol;

namespace ComposeVR
{

    /// <summary>
    /// Singleton responsible for delivering events coming in to the system to the intended handler
    /// </summary>
    [Serializable]
    public class RemoteEventRouter
    {

        private static RemoteEventRouter instance;
        private RemoteEventRouter() { }

        public static RemoteEventRouter Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new RemoteEventRouter();
                }
                return instance;
            }
        }


        private Dictionary<string, RemoteEventHandler> handlerDictionary;

        private Dictionary<string, RemoteEventHandler> getHandlerDictionary()
        {
            if (handlerDictionary == null)
            {
                handlerDictionary = new Dictionary<string, RemoteEventHandler>();
            }
            return handlerDictionary;
        }

        public void RouteEvent(Protocol.Event e)
        {
            string receiverID = "";

            switch (e.EventCase)
            {
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

            if (getHandlerDictionary().ContainsKey(receiverID))
            {
                RemoteEventHandler receiver = getHandlerDictionary()[receiverID];
                receiver.HandleEvent(e);
            }
        }

        public void AddReceiver(string receiverID, RemoteEventHandler receiver)
        {
            getHandlerDictionary().Add(receiverID, receiver);
        }

        public void RemoveReceiver(string receiverID)
        {
            getHandlerDictionary().Remove(receiverID);
        }

    }
}