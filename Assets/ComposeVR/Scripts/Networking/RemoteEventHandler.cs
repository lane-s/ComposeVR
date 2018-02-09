using System.Collections;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using ComposeVR.Protocol;

namespace ComposeVR {
    public class RemoteEventHandler : MonoBehaviour {

        protected string id;
        private TCPClient client;
        private RemoteEventRouter router;

        public void handleEvent(Protocol.Event e) {

            Type thisType = this.GetType();
            MethodInfo theMethod = thisType.GetMethod(e.MethodName);

            if (theMethod != null) {
                theMethod.Invoke(this, new object[] { e });
            }
        }

        public string getID() {
            if (id == null) {
                id = Guid.NewGuid().ToString();
            }
            return id;
        }

        protected void Register() {
            getRouter().addReceiver(getID(), this);
        }

        protected void Register(string newId) {
            setID(newId);
            Register();
        }

        protected TCPClient getClient() {
            if (client == null) {
                client = GameObject.FindGameObjectWithTag("TCPClient").GetComponent<TCPClient>();
            }
            return client;
        }

        private RemoteEventRouter getRouter() {
            if (router == null) {
                router = getClient().GetComponent<RemoteEventRouter>();
            }
            return router;
        }

        private void setID(string newId) {
            if (this.id != null) {
                getRouter().removeReceiverIfPresent(this.id);
            }

            this.id = newId;
        }


    }
}