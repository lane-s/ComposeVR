using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ComposeVR {
    public static class ExtensionMethods {
        public static float Remap(this float value, float from1, float to1, float from2, float to2) {
            return (value - from1) / (to1 - from1) * (to2 - from2) + from2;
        }

        public static T GetComponentInOwner<T>(this Transform transform) {
            OwnedObject ownedObject = transform.GetComponent<OwnedObject>();

            if(ownedObject != null) {
                Transform owner = ownedObject.Owner;
                T component = owner.GetComponent<T>();

                return component;
            }

            return default(T);
        }
    }
}

