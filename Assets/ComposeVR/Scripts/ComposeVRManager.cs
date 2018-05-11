using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ComposeVR
{
    public class ComposeVRManager : SingletonObject<ComposeVRManager>
    {
        public NoteOrb NoteOrbPrefab;
        public NoteCore NoteCorePrefab;

        private int handlerCount = 0;

        // Use this for initialization
        void Awake()
        {
        }

        public string GetNewHandlerID()
        {
            string result = "ComposeVRObject-" + handlerCount;
            handlerCount += 1;

            return result;
        }
    }
}
