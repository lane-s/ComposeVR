using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ComposeVR {
    [RequireComponent(typeof(Jack))]
    public class OutputJack : MonoBehaviour {

        private HashSet<IJackInput> connectedInputs;

        void Awake() {
            connectedInputs = new HashSet<IJackInput>();
            GetComponent<Jack>().PlugConnected += OnPlugConnected;
            GetComponent<Jack>().PlugDisconnected += OnPlugDisconnected;
        }

        private void OnPlugConnected(object sender, JackEventArgs e) {

            HashSet<Jack> connectedInputJacks = e.ConnectedCord.GetConnectedJacks(true, e.PlugNodeInCord);
            foreach(Jack j in connectedInputJacks) {
                connectedInputs.UnionWith(j.GetComponent<InputJack>().GetConnectedInputs());
            }
        }

        private void OnPlugDisconnected(object sender, JackEventArgs e) {
            connectedInputs.Clear();
        }

        public void SendData(WireData data) {
            foreach(IJackInput connectedInput in connectedInputs) {
                connectedInput.ReceiveData(data);
            }
        }

        public void DisconnectInputs(HashSet<IJackInput> toDisconnect) {
            connectedInputs.ExceptWith(toDisconnect);
        }

        public void ConnectInputs(HashSet<IJackInput> toConnect) {
            connectedInputs.UnionWith(toConnect);
        }
    }

    public class WireData{

    }

    public interface IJackOutput {
        void SendData(WireData data);
    }

}
