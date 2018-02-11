using System.Collections;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using ComposeVR.Protocol;

namespace ComposeVR {

    /// <summary>
    /// A RemoteEventHandler has an id which is registered with the RemoteEventRouter. Any events intended for an object with the same id as a given RemoteEventHandler will be passed on to the RemoteEventHandler
    /// </summary>
    [Serializable]
    public class RemoteEventHandler {

        protected string id;

        public void HandleEvent(Protocol.Event e) {

            Type thisType = this.GetType();
            MethodInfo theMethod = thisType.GetMethod(e.MethodName);

            if (theMethod != null) {
                theMethod.Invoke(this, new object[] { e });
            }
        }

        public string GetID() {
            if (id == null) {
                id = Guid.NewGuid().ToString();
            }
            return id;
        }

        protected void Register() {
            RemoteEventRouter.Instance.AddReceiver(GetID(), this);
        }

        protected void RegisterRemoteID(string newId) {
            SetID(newId);
            Register();
        }

        private void SetID(string newId) {
            if (this.id != null) {
                RemoteEventRouter.Instance.RemoveReceiver(this.id);
            }

            this.id = newId;
        }

    }

}