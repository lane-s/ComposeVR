using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ComposeVR {
    public class InputJack : MonoBehaviour {

        private List<IJackInput> connectedInputs;
        public void AddInput(IJackInput input) {

            GetConnectedInputs().Add(input);
        }

        public List<IJackInput> GetConnectedInputs() {
            if(connectedInputs == null) {
                connectedInputs = new List<IJackInput>();
            }
            return connectedInputs;
        }
    }

    public interface IJackInput {
        void ReceiveData(WireData data);
    }
}