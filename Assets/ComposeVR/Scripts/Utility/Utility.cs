using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ComposeVR;

namespace ComposeVR {
    public class Utility : MonoBehaviour {

        public static bool isInLayerMask(int layer, LayerMask layermask) {
            return layermask == (layermask | (1 << layer));
        }
    }
}