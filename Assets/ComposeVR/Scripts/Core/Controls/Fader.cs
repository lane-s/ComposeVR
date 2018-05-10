using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRTK;

namespace ComposeVR
{
    public class Fader : MonoBehaviour
    {
        public float FaderSnapSpeed = 0.5f;
        [Tooltip("How fast should the grip snap to the grabbing controller's position on the axis of the fader rail")]

        public float ChangeEventDistance = 0.005f;
        [Tooltip("How far does the grip need to move before the fader emits a FaderValueChanged event")]

        public Control3DEventHandler FaderValueChanged;

        private VRTK_InteractableObject gripHandle;

        private Transform gripTransform;
        private SnapToTargetPosition gripTransformSnap;
        private Vector3 lastGripTransformPosition;

        private Transform rail;
        private Transform railStart;
        private Transform railEnd;
        private float railLength;

        private bool gripIsGrabbed;

        // Use this for initialization
        void Awake()
        {
            InitializeFaderParts();

            gripHandle.InteractableObjectGrabbed += OnGripGrabbed;
            gripHandle.InteractableObjectUngrabbed += OnGripUngrabbed;

            gripTransformSnap = gripTransform.GetComponent<SnapToTargetPosition>();
            gripTransformSnap.enabled = false;
        }

        private void OnGripGrabbed(object sender, InteractableObjectEventArgs e)
        {
            gripTransform.SetParent(null);
            gripIsGrabbed = true;
            gripTransformSnap.enabled = true;
            lastGripTransformPosition = gripTransform.position;
        }

        private void OnGripUngrabbed(object sender, InteractableObjectEventArgs e)
        {
            ResetGripTransform();
            gripIsGrabbed = false;
            gripTransformSnap.enabled = false;
        }

        private void ResetGripTransform()
        {
            gripHandle.transform.position = gripTransform.position;
            gripHandle.transform.rotation = gripTransform.rotation;
            gripTransform.SetParent(gripHandle.transform);
        }

        // Update is called once per frame
        void Update()
        {
            if (gripIsGrabbed)
            {
                Vector3 targetPosition = Utility.ProjectPointOnSegment(gripHandle.transform.position, railStart.position, railEnd.position);
                gripTransform.position = Vector3.Lerp(gripTransform.position, targetPosition, Time.deltaTime * FaderSnapSpeed);

                if (Vector3.Distance(gripTransform.position, lastGripTransformPosition) > ChangeEventDistance)
                {
                    lastGripTransformPosition = gripTransform.position;

                    if (FaderValueChanged != null)
                    {
                        Control3DEventArgs e = new Control3DEventArgs();

                        float startDistance = Vector3.Distance(gripTransform.position, railStart.position);
                        e.value = startDistance;
                        e.normalizedValue = startDistance / railLength;

                        FaderValueChanged(this, e);
                    }
                }
            }
        }

        public void SetNormalizedValue(float val)
        {
            InitializeFaderParts();

            Vector3 faderAxis = (railEnd.position - railStart.position).normalized;
            gripHandle.transform.position = railStart.position + faderAxis * val * railLength;
            gripTransform.position = gripHandle.transform.position;
        }

        private void InitializeFaderParts()
        {
            gripHandle = transform.Find("Grip").GetComponent<VRTK_InteractableObject>();
            gripTransform = gripHandle.transform.Find("GripTransform");

            rail = transform.Find("Rail");
            railStart = rail.Find("Start");
            railEnd = rail.Find("End");
            railLength = Vector3.Distance(railStart.position, railEnd.position);

        }
    }
}
