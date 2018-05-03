using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ComposeVR {
    /// <summary>
    /// This component is required for any game object that needs to receive data from other, physically connected game objects
    /// </summary>
    public class PhysicalDataInput : MonoBehaviour {

        private HashSet<IPhysicalDataInput> connectedInputs;

        private void Awake() {
            GetComponent<PlugReceptacle>().PlugConnected += OnPlugConnected;
            GetComponent<PlugReceptacle>().PlugDisconnected += OnPlugDisconnected;
        }

        private void OnPlugConnected(object sender, PhysicalConnectionEventArgs e) {
            HashSet<PlugReceptacle> connectedOutputJacks = e.ConnectedCord.GetConnectedReceptacles(true, e.PlugNodeInCord);
            foreach(PlugReceptacle j in connectedOutputJacks) {
                if (j.GetComponent<PhysicalDataOutput>()) {
                    j.GetComponent<PhysicalDataOutput>().ConnectInputs(connectedInputs);
                }
            }
        }

        private void OnPlugDisconnected(object sender, PhysicalConnectionEventArgs e) {
            HashSet<PlugReceptacle> connectedOutputJacks = e.ConnectedCord.GetConnectedReceptacles(true, e.PlugNodeInCord);
            foreach(PlugReceptacle j in connectedOutputJacks) {
                if (j.GetComponent<PhysicalDataOutput>()) {
                    j.GetComponent<PhysicalDataOutput>().DisconnectInputs(connectedInputs);
                }
            }
        }

        public void AddInput(IPhysicalDataInput input) {

            GetConnectedInputs().Add(input);
        }

        public HashSet<IPhysicalDataInput> GetConnectedInputs() {
            if(connectedInputs == null) {
                connectedInputs = new HashSet<IPhysicalDataInput>();
            }
            return connectedInputs;
        }
    }

    public interface IPhysicalDataInput {
        void ReceiveData(PhysicalDataPacket data);
    }
}