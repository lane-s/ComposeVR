using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ComposeVR {
    [RequireComponent(typeof(Jack))]
    public class OutputJack : MonoBehaviour {

        private List<IJackInput> connectedInputs;

        void Awake() {
            connectedInputs = new List<IJackInput>();
            GetComponent<Jack>().OtherJackConnected += OnOtherJackConnected;
            GetComponent<Jack>().OtherJackDisconnected += OnOtherJackDisconnected;
        }

        public void OnOtherJackConnected(object sender, JackEventArgs e) {
            if (e.Other.GetComponent<InputJack>()) {
                connectedInputs.AddRange(e.Other.GetComponent<InputJack>().GetConnectedInputs());
            }
            else {
                Debug.LogError("A Jack connected to an OutputJack must have an InputJack");
            }
        }

        public void OnOtherJackDisconnected(object sender, JackEventArgs e) {
            if (e.Other.GetComponent<InputJack>()) {
                connectedInputs = connectedInputs.Except(e.Other.GetComponent<InputJack>().GetConnectedInputs()).ToList();
            }
        }

        public void SendData(WireData data) {
            foreach(IJackInput connectedInput in connectedInputs) {
                connectedInput.ReceiveData(data);
            }
        }
    }

    public class WireData{

    }

    public interface IJackOutput {
        void SendData(WireData data);
    }

}
