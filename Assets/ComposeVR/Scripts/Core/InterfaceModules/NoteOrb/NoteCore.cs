using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ComposeVR {
    [RequireComponent(typeof(SnapToTargetPosition))]
    [RequireComponent(typeof(Scalable))]
    public class NoteCore : MonoBehaviour {
        public int Note;
        private SnapToTargetPosition positionSnap;
        private Scalable scalable;
        private const float CORE_SNAP_SPEED = 0.65f;

        private void Awake() {
            positionSnap = GetComponent<SnapToTargetPosition>();
            positionSnap.UseLocalPosition = true;

            scalable = GetComponent<Scalable>();
            scalable.TargetScale = transform.localScale;
        }

        public void SetColor(Color c) {
            GetComponent<MeshRenderer>().material.SetColor("_EmissionColor", c);
        }

        public void SetPosition(Vector3 pos) {
            positionSnap.SnapToTarget(pos, CORE_SNAP_SPEED, InterpolationType.Exponential);
        }

        public void SetScale(Vector3 scale) {
            scalable.TargetScale = scale; 
        }
    }
}
