using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ComposeVR
{
    [RequireComponent(typeof(SnapToTargetPosition))]
    [RequireComponent(typeof(Scalable))]
    public class NoteCore : MonoBehaviour
    {
        public int Note;

        private Color _color;
        public Color Color
        {
            get
            {
                return _color;
            }
            set
            {
                _color = value;
                GetComponent<MeshRenderer>().material.SetColor("_EmissionColor", _color);
            }
        }

        private SnapToTargetPosition positionSnap;
        private Scalable scalable;
        private const float CORE_SNAP_SPEED = 0.65f;

        private void Awake()
        {
            positionSnap = GetComponent<SnapToTargetPosition>();
            positionSnap.UseLocalPosition = true;

            scalable = GetComponent<Scalable>();
            scalable.TargetScale = transform.localScale;
        }

        public void SetPosition(Vector3 pos)
        {
            positionSnap.SnapToTarget(pos, CORE_SNAP_SPEED, InterpolationType.Exponential);
        }

        public void SetScale(Vector3 scale)
        {
            scalable.TargetScale = scale;
        }
    }
}
