using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRTK;

namespace ComposeVR {
    public class TestOutputModule : MonoBehaviour, IJackOutput {

        public OutputJack OutputJack;

        private IJackOutput output;

        private void Awake() {
        
        }

        public void SendData(WireData data) {
            OutputJack.SendData(data);
        }
    }

    public class TestData : WireData {
        public Control3DEventArgs controlData;
    }
}