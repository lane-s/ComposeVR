using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ComposeVR {
    public class InputJack : MonoBehaviour {

        private HashSet<IJackInput> connectedInputs;

        private void Awake() {
            GetComponent<Jack>().PlugConnected += OnPlugConnected;
            GetComponent<Jack>().PlugDisconnected += OnPlugDisconnected;
        }

        private void OnPlugConnected(object sender, JackEventArgs e) {
            HashSet<Jack> connectedOutputJacks = e.ConnectedCord.GetConnectedJacks(true, e.PlugNodeInCord);
            foreach(Jack j in connectedOutputJacks) {
                if (j.GetComponent<OutputJack>()) {
                    j.GetComponent<OutputJack>().ConnectInputs(connectedInputs);
                }
            }
        }

        private void OnPlugDisconnected(object sender, JackEventArgs e) {
            HashSet<Jack> connectedOutputJacks = e.ConnectedCord.GetConnectedJacks(true, e.PlugNodeInCord);
            foreach(Jack j in connectedOutputJacks) {
                if (j.GetComponent<OutputJack>()) {
                    j.GetComponent<OutputJack>().DisconnectInputs(connectedInputs);
                }
            }
        }

        public void AddInput(IJackInput input) {

            GetConnectedInputs().Add(input);
        }

        public HashSet<IJackInput> GetConnectedInputs() {
            if(connectedInputs == null) {
                connectedInputs = new HashSet<IJackInput>();
            }
            return connectedInputs;
        }
    }

    public interface IJackInput {
        void ReceiveData(WireData data);
    }
}