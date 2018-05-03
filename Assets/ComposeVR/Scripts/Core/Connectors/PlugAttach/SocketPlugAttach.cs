using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRTK;

namespace ComposeVR {
    public sealed class SocketPlugAttach : PlugAttach {

        [Tooltip("The positon and rotation of the plug when it is plugged all the way into this socket")]
        public Transform PlugConnectionPoint;

        [Tooltip("If the plug is closer to the socket than this distance, it will automatically be plugged all the way in")]
        public float AutoPlugDistance = 0.16f;

        [Tooltip("If the plug is closer to the socket than this distance and the controller is separated, then the plug will be forceably ungrabbed")]
        public float LockedIntoSocketDistance = 0.02f;

        [Tooltip("If a Plug is behind the socket when it is locked, it will snap in front of the socket with this value being its initial distance from the socket")]
        public float MinInitialSocketDistance = 0.12f;

        private Vector3 controllerPosOnSocketAxis;
        private SnapToTargetPosition positionSnap;
        private SnapToTargetRotation rotationSnap;
        private IEnumerator snapToSocketRoutine;

        protected override void OnPlugLocked() {
            if (GetComponent<CordDispenser>()) {
                GetComponent<CordDispenser>().Block();
            }

            positionSnap = LockedPlug.PlugTransform.GetComponent<SnapToTargetPosition>();
            rotationSnap = LockedPlug.PlugTransform.GetComponent<SnapToTargetRotation>();

            snapToSocketRoutine = SnapToSocket();
            StartCoroutine(snapToSocketRoutine);
            Debug.Log("Plug locked");
        }

        protected override void UnlockPlug() {
            base.UnlockPlug();
            StopSnapping();
        }

        protected override void OnReceptacleAvailable() {
            if (GetComponent<CordDispenser>()) {
                GetComponent<CordDispenser>().Unblock();
            }
        }

        private void StopSnapping() {
            if(snapToSocketRoutine != null) {
                StopCoroutine(snapToSocketRoutine);
                snapToSocketRoutine = null;
            }
        }

        protected override void OnMaxControllerSeparationExceeded() {
            if (PlugIsLockedIntoSocket()) {
                lockedPlugGrabber.ForceRelease();
            }
            else {
                base.OnMaxControllerSeparationExceeded();
            }
        }

        private bool PlugIsLockedIntoSocket() {
            return Vector3.Distance(LockedPlug.PlugTransform.position, PlugConnectionPoint.position) < LockedIntoSocketDistance;
        }

        protected override void OnLockedPlugGrabbed(object sender, InteractableObjectEventArgs args) {
            base.OnLockedPlugGrabbed(sender, args);
            LockedPlug.DisconnectFromReceptacle();

            snapToSocketRoutine = SnapToSocket();
            StartCoroutine(snapToSocketRoutine);
        }

        protected override void OnLockedPlugUngrabbed(object sender, InteractableObjectEventArgs args) {
            if (InAutoPlugRange()) {
                StartCoroutine(AutoPlugIntoTarget());
            }
            else {
                base.OnLockedPlugUngrabbed(sender, args);
            }
        }

        private bool InAutoPlugRange() {
            return Vector3.Distance(LockedPlug.PlugTransform.position, PlugConnectionPoint.position) < AutoPlugDistance;
        }
    
        /// <summary>
        /// Checks if the given point is closer to the jack connection point than the closest allowed point
        /// </summary>
        /// <param name="point"></param>
        /// <param name="closestAllowedPoint"></param>
        /// <returns></returns>
        private bool IsCloserThan(Vector3 point, Vector3 closestAllowedPoint) {
            return Vector3.Dot(point - closestAllowedPoint, PlugConnectionPoint.forward) > 0;
        }

        /// <summary>
        /// Gets the point on the jack axis to snap to.
        /// 
        /// The point is calculated by getting the component of the grabbing controller position along the jack axis
        /// </summary>
        /// <param name="closestAllowedPoint">The snap point will not be closer to the jack than this point</param>
        /// <returns>The snap point</returns>
        private Vector3 GetSnapPoint(Vector3 closestAllowedPoint) {

            Vector3 snapPoint = GetSnapPoint();

            if(IsCloserThan(snapPoint, closestAllowedPoint)) {
                snapPoint = closestAllowedPoint;
            }

            return snapPoint;
        }

        private Vector3 GetSnapPoint() {
            Vector3 controllerPos = lockedPlugGrabber.transform.position;
            Vector3 controllerToConnectionPoint = PlugConnectionPoint.position - controllerPos;

            return controllerPosOnSocketAxis = controllerPos + controllerToConnectionPoint - Vector3.Dot(controllerToConnectionPoint, PlugConnectionPoint.forward) * PlugConnectionPoint.forward;
        }

        /// <summary>
        /// </summary>
        /// <returns>The distance from the plug model to the point that it will initially try to snap to</returns>
        private bool IsPlugOnSocketAxis() {
            Vector3 snapPoint = GetSnapPoint();
            Vector3 plugToSnapPoint = snapPoint - LockedPlug.PlugTransform.position;
            Vector3 projectOnJackAxis = Vector3.Dot(plugToSnapPoint, PlugConnectionPoint.forward) * PlugConnectionPoint.forward;

            float distance = (plugToSnapPoint - projectOnJackAxis).magnitude;

            if(distance < 0.005f) {
                //The distance to the jack axis is small, so we don't need to snap the plug in front of the jack
                return true;
            }

            return false;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="closestAllowedPoint"></param>
        /// <returns>The point that the plug will move to</returns>
        private Vector3 PositionPlugOnJackAxis(Vector3 closestAllowedPoint) {
            Vector3 snapPoint = GetSnapPoint(closestAllowedPoint);
            positionSnap.SnapToTarget(snapPoint, PositionSnapSpeed);
            return snapPoint;
        }

        /// <summary>
        /// Positions the plug along the jack axis until the unsnap conditions are met or the plug is let go by the user
        /// </summary>
        /// <returns></returns>
        private IEnumerator SnapToSocket() {
            yield return null;

            LockedPlug.PlugTransform.SetParent(null);

            rotationSnap.SnapToTarget(PlugConnectionPoint.rotation, RotationSnapSpeed);

            bool aligned = IsPlugOnSocketAxis();

            Vector3 closestAllowedStartPosition = PlugConnectionPoint.position - PlugConnectionPoint.forward * MinInitialSocketDistance;

            while (LockedPlug != null) {
                if (!aligned) {
                    PositionPlugOnJackAxis(closestAllowedStartPosition);
                    if (positionSnap.HasReachedTarget) {
                        aligned = true;
                    }
                }else{
                    PositionPlugOnJackAxis(PlugConnectionPoint.position);
                }

                yield return new WaitForEndOfFrame();
            }

            yield return null;
        }

        /// <summary>
        /// Moves the plug to the jack's connection point and connects the plug to the jack
        /// </summary>
        /// <returns></returns>
        private IEnumerator AutoPlugIntoTarget() {
            LockedPlug.DestinationReceptacle = plugReceptacle;
            StopSnapping();

            positionSnap.SnapToTarget(PlugConnectionPoint.position, 1f);

            while (!positionSnap.HasReachedTarget) {
                yield return new WaitForEndOfFrame();
            }

            LockedPlug.ConnectToReceptacle(plugReceptacle);
            LockedPlug.transform.SetParent(PlugConnectionPoint);
            yield return null;
        }
    }
}