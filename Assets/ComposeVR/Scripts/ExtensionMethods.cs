using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ComposeVR {
    public static class ExtensionMethods {
        public static float Remap(this float value, float from1, float to1, float from2, float to2) {
            return (value - from1) / (to1 - from1) * (to2 - from2) + from2;
        }

        public static T GetComponentInActor<T>(this Transform transform) {
            ActorSubObject ownedObject = transform.GetComponent<ActorSubObject>();
            T component;

            if(ownedObject != null) {
                Transform actor = ownedObject.Actor;
                component = actor.GetComponent<T>();
                return component;
            }

            component = transform.GetComponent<T>();
            return component;
        }

        public static T GetComponentInActor<T>(this GameObject gameObject) {
            return gameObject.transform.GetComponentInActor<T>();
        }
    }
}

