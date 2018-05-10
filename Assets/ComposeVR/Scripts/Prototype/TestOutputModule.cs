using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRTK;

namespace ComposeVR
{
    public class TestOutputModule : MonoBehaviour, IJackOutput
    {

        public PhysicalDataOutput OutputJack;

        private IJackOutput output;

        private void Awake()
        {

        }

        public void SendData(PhysicalDataPacket data)
        {
            OutputJack.SendData(data);
        }
    }

    public class TestData : PhysicalDataPacket
    {
        public Control3DEventArgs controlData;
    }
}