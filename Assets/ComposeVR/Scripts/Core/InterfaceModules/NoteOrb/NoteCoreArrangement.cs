using System.Collections;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace ComposeVR {
    [Serializable]
    public class NoteCoreArrangement {
        public List<Vector3> corePositions;
        public Vector3 coreScale;
    }

    [CreateAssetMenu(fileName = "CoreArrangementScheme", menuName = "ComposeVR/NoteCoreArrangementScheme", order = 1)]
    public class NoteCoreArrangementScheme : ScriptableObject {
        public List<NoteCoreArrangement> Arrangements = new List<NoteCoreArrangement>();

        public NoteCoreArrangement GetArrangement(int numNotes) {
            if(numNotes <= 0)
                return null;

            if(numNotes - 1 < Arrangements.Count)
                return Arrangements[numNotes - 1];

            Debug.LogError("Chord size not supported"); //TODO: Switch from custom chord arrangements to procedurally generated arrangements when the chord size is large
            NoteCoreArrangement defaultArrangement = new NoteCoreArrangement();

            defaultArrangement.corePositions = new List<Vector3>(numNotes);
            for(int i = 0; i < defaultArrangement.corePositions.Count; i++) {
                defaultArrangement.corePositions[i] = new Vector3(0.4f * UnityEngine.Random.value, 0.4f * UnityEngine.Random.value, 0.4f * UnityEngine.Random.value);
            }
            defaultArrangement.coreScale = Vector3.one * 0.1f / numNotes;

            return defaultArrangement;
        }
    }
}
