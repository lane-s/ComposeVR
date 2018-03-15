using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ComposeVR {

    public class TestInputModule : MonoBehaviour, IJackInput {

        public InputJack input;
        private Color originalColor;

        // Use this for initialization
        void Awake() {
            input.AddInput(this);
            originalColor = GetComponent<MeshRenderer>().material.color;
        }

        void IJackInput.ReceiveData(WireData data) {
            TestData d = data as TestData;

            if (d != null) {
                float dialVal = d.controlData.normalizedValue / 100f;

                float H, S, V;
                Color.RGBToHSV(originalColor, out H, out S, out V);


                GetComponent<MeshRenderer>().material.color = Color.HSVToRGB(dialVal, S, V);
            }
        }
    }
}