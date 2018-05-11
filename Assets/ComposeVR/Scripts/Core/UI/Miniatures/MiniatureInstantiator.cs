using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRTK;

namespace ComposeVR
{

    public class MiniatureEventArgs : EventArgs
    {
        public Miniature Miniature;
        public MiniatureEventArgs(Miniature miniature)
        {
            Miniature = miniature;
        }
    }

    public class MiniatureInstantiator : MonoBehaviour
    {
        public event EventHandler<MiniatureEventArgs> MiniatureReleased;

        [Tooltip("The miniature object to instantiate")]
        public Miniature Prefab;

        [Tooltip("How fast to rotate the miniature when displaying it")]
        public float SpinSpeed = 3.0f;

        [Tooltip("How far away from the instantiator can a minature get before it is released")]
        public float ReleaseDistance = 0.075f;

        private Miniature currentMiniature;
        private VRTK_InteractableObject currentMiniInteractable;

        private bool miniatureInPlace = true;
        private const float EPSILON = 0.005f;
        private const float SNAP_SPEED = 20f;
        private const float SNAP_ROT_SPEED = 20f;

        private MiniatureEventArgs miniatureEventArgs;

        void Awake()
        {
            miniatureEventArgs = new MiniatureEventArgs(null);
            NewMiniature();
        }

        void Update()
        {
            if (currentMiniInteractable.IsGrabbed())
            {
                miniatureInPlace = false;
                if (Vector3.Distance(currentMiniature.transform.position, transform.position) > ReleaseDistance)
                {
                    ReleaseMiniature();
                }
            }
            else
            {
                if (miniatureInPlace)
                {
                    SpinMiniature(Time.deltaTime);
                }
                else
                {
                    SnapMiniatureInPlace(Time.deltaTime);
                }
            }
        }

        private void ReleaseMiniature()
        {

            currentMiniInteractable.InteractableObjectUngrabbed -= OnMiniatureUnGrabbed;
            currentMiniature.Release();
            if (MiniatureReleased != null)
            {
                miniatureEventArgs.Miniature = currentMiniature;
                MiniatureReleased(this, miniatureEventArgs);
            }
            NewMiniature();
        }

        private void SpinMiniature(float delta)
        {
            currentMiniature.transform.rotation = Quaternion.AngleAxis(SpinSpeed * delta, transform.up) * currentMiniature.transform.rotation;
            transform.rotation = Quaternion.AngleAxis(SpinSpeed * delta, transform.up) * transform.rotation;
        }

        private void SnapMiniatureInPlace(float delta)
        {
            miniatureInPlace = true;

            if (Vector3.Distance(transform.position, currentMiniature.transform.position) > EPSILON)
            {
                miniatureInPlace = false;
                currentMiniature.transform.position = Vector3.Lerp(currentMiniature.transform.position, transform.position, delta * SNAP_SPEED);
            }

            if (Quaternion.Angle(transform.rotation, currentMiniature.transform.rotation) > EPSILON)
            {
                miniatureInPlace = false;
                currentMiniature.transform.rotation = Quaternion.Slerp(currentMiniature.transform.rotation, transform.rotation, SNAP_ROT_SPEED);
            }
        }

        private void NewMiniature()
        {
            currentMiniature = Instantiate(Prefab, transform.position, transform.rotation, transform);
            miniatureInPlace = true;

            currentMiniInteractable = currentMiniature.GetComponent<VRTK_InteractableObject>();
            currentMiniInteractable.InteractableObjectUngrabbed += OnMiniatureUnGrabbed;
        }

        private void OnMiniatureUnGrabbed(object sender, InteractableObjectEventArgs e)
        {
            currentMiniature.transform.parent = transform;
        }
    }
}
