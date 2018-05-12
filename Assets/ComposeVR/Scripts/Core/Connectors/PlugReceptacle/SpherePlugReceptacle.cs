using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRTK;

namespace ComposeVR
{
    public class SpherePlugReceptacle : PlugReceptacle
    {
        public Transform Sphere;
        public float Radius;

        private SnapToTargetPosition positionSnap;
        private SnapToTargetRotation rotationSnap;
        private IEnumerator snapToSphereRoutine;

        private GameObject plugAttach;

        private Vector3 targetPosition;
        private Quaternion targetRotation;

        protected override void Awake()
        {
            plugAttach = new GameObject("SpherePlugAttach");
            plugAttach.transform.SetParent(transform);
            plugAttach.transform.position = transform.position;
            base.Awake();
        }

        protected override void OnPlugLocked()
        {
            positionSnap = LockedPlug.PlugTransform.GetComponent<SnapToTargetPosition>();
            rotationSnap = LockedPlug.PlugTransform.GetComponent<SnapToTargetRotation>();

            if(lockedPlugGrabber != null)
            {
                snapToSphereRoutine = SnapToSphere();
                StartCoroutine(snapToSphereRoutine);
            }
        }

        protected override void UnlockPlug()
        {
            base.UnlockPlug();
            StopSnapping();
        }

        private void StopSnapping()
        {
            if(snapToSphereRoutine != null)
            {
                StopCoroutine(snapToSphereRoutine);
                snapToSphereRoutine = null;
            }
        }

        protected override void OnLockedPlugGrabbed(object sender, InteractableObjectEventArgs args)
        {
            base.OnLockedPlugGrabbed(sender, args);
            LockedPlug.DisconnectFromDataEndpoint();

            snapToSphereRoutine = SnapToSphere();
            StartCoroutine(snapToSphereRoutine);
        }

        protected override void OnLockedPlugUngrabbed(object sender, InteractableObjectEventArgs args)
        {
            StopSnapping();
            LockedPlug.ConnectToDataEndpoint(plugReceptacle);

            plugAttach.transform.position = targetPosition;
            plugAttach.transform.rotation = targetRotation;
            LockedPlug.GetComponent<VRTK_TransformFollow>().gameObjectToFollow = plugAttach;
        }

        private IEnumerator SnapToSphere()
        {
            yield return null;

            LockedPlug.PlugTransform.SetParent(null);

            while (LockedPlug != null)
            {
                Vector3 toGrabber = lockedPlugGrabber.controllerAttachPoint.position - Sphere.position;
                targetPosition = Sphere.position + toGrabber.normalized * Sphere.lossyScale.z * Radius;
                targetRotation = Quaternion.LookRotation(-toGrabber);

                rotationSnap.SnapToTarget(targetRotation, RotationSnapSpeed);
                positionSnap.SnapToTarget(targetPosition, PositionSnapSpeed);

                yield return new WaitForEndOfFrame();
            }
        }
    }
}
