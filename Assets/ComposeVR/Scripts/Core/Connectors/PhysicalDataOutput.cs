using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ComposeVR {
    /// <summary>
    /// This component is required for any game object that needs to send data to other, physically connected game objects
    /// </summary>
    public class PhysicalDataOutput : MonoBehaviour {

        private HashSet<IPhysicalDataInput> connectedInputs;

        void Awake() {
            connectedInputs = new HashSet<IPhysicalDataInput>();
            GetComponent<PhysicalDataEndpoint>().PlugConnected += OnPlugConnected;
            GetComponent<PhysicalDataEndpoint>().PlugDisconnected += OnPlugDisconnected;
        }

        private void OnPlugConnected(object sender, PhysicalConnectionEventArgs e) {

            HashSet<PhysicalDataEndpoint> connectedInputJacks = e.ConnectedCord.GetConnectedEndpoints(false, e.PlugNodeInCord);
            foreach(PhysicalDataEndpoint j in connectedInputJacks) {
                connectedInputs.UnionWith(j.GetComponent<PhysicalDataInput>().GetConnectedInputs());
            }
        }

        private void OnPlugDisconnected(object sender, PhysicalConnectionEventArgs e) {
            connectedInputs.Clear();
        }

        public void SendData(PhysicalDataPacket data) {
            foreach(IPhysicalDataInput connectedInput in connectedInputs) {
                connectedInput.ReceiveData(data);
            }
        }

        public void DisconnectInputs(HashSet<IPhysicalDataInput> toDisconnect) {
            connectedInputs.ExceptWith(toDisconnect);
        }

        public void ConnectInputs(HashSet<IPhysicalDataInput> toConnect) {
            connectedInputs.UnionWith(toConnect);
        }
    }

    public class PhysicalDataPacket{

    }

    public interface IJackOutput {
        void SendData(PhysicalDataPacket data);
    }

}
