using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using VRTK;

namespace ComposeVR {
    [RequireComponent(typeof(VRTK_InteractableObject))]
    public sealed class Plug : MonoBehaviour {

        public Jack SourceJack;
        public Jack DestinationJack;


        public float AutoPlugDistance = 0.5f;
        public float MaxHandSeparationBeforeUnsnap = 1.0f;
        public float MaxJackDistanceBeforeUnsnap = 1.3f;
        public float SmallestAllowedInitialDistanceFromJack = 0.16f;

        public float SnapToHandSpeed = 20.0f;
        public float SnapCooldownTime = 1.0f;
        public float JackSnapSpeed = 0.5f;
        public float RotationSnapSpeed = 900f;
        public Transform PlugTransform;
        public Transform CordAttachPoint;

        private Jack targetJack;
        private List<Jack> nearbyJacks;

        private Plug connectedPlug;
        
        private VRTK_InteractableObject interactable;
        private SnapToTargetPosition positionSnap;
        private SnapToTargetRotation rotationSnap;
        private float normalSnapSpeed;
        private bool snapCooldown;
        private Vector3 controllerPositionOnJackAxis;

        private IEnumerator snapToJackRoutine;

        void Awake() {
            interactable = GetComponent<VRTK_InteractableObject>();
            nearbyJacks = new List<Jack>();

            interactable.InteractableObjectGrabbed += OnGrabbed;
            interactable.InteractableObjectUngrabbed += OnUngrabbed;

            positionSnap = PlugTransform.GetComponent<SnapToTargetPosition>();
            rotationSnap = PlugTransform.GetComponent<SnapToTargetRotation>();
            snapCooldown = false;
        }

        /// <summary>
        /// Try to snap to nearby jacks
        /// </summary>
        void Update() {
            if (targetJack == null && !snapCooldown) {
                foreach (Jack j in nearbyJacks) {

                    if (j.GetState() == Jack.State.Free) {
                        targetJack = j;
                        if (TrySnapToJack()) {
                            break;
                        }
                    }
                }
            }
        }

        private void OnGrabbed(object sender, InteractableObjectEventArgs e) {
            if (DestinationJack != null) {
                //The plug was plugged in 
                targetJack = DestinationJack;
                DestinationJack.SetState(Jack.State.Free);

                if (SourceJack != null) {
                    DestinationJack.DisconnectJack(SourceJack);
                    SourceJack.DisconnectJack(DestinationJack);
                }

                DestinationJack = null;
                connectedPlug.SourceJack = null;
            }
            else {
                PlugTransform.SetParent(null);
                StartCoroutine(SnapBackToController());
            }

            TrySnapToJack();
        }

        private void OnUngrabbed(object sender, InteractableObjectEventArgs e) {
            if (targetJack != null) {
                DestinationJack = targetJack;
                connectedPlug.SourceJack = targetJack;

                ResetPlugTransform();

                if (GetDistanceToConnectionPointOnJackAixs() < AutoPlugDistance) {
                    StartCoroutine(AutoPlugIntoTarget());
                }
                else {
                    UnSnapFromJack();
                }
            }

            if(DestinationJack == null && SourceJack == null) {
                //Self destruct this cord
            }
        }

        //We have a small buffer distance so that the plug doesn't begin snapping to a jack when it's likely to immediately snap back to the controller
        private const float BUFFER_DISTANCE = 0.025f;

        /// <summary>
        /// Starts the SnapToJack coroutine if all the required conditions are met.
        /// 
        /// Checks are made to ensure that the plug won't immediately after snapping
        /// If the plug is coming from a source jack, we make sure the target jack is allowed to connect to the source jack
        /// </summary>
        /// <returns>Whether the plug will start snapping to the target jack</returns>
        private bool TrySnapToJack() {
            if (interactable.IsGrabbed() && targetJack != null) {

                bool correctJackType = true;

                if(SourceJack != null) {
                    if(SourceJack.GetComponent<InputJack>() != null && targetJack.GetComponent<OutputJack>() == null) {
                        correctJackType = false;
                    }else if(SourceJack.GetComponent<OutputJack>() != null && targetJack.GetComponent<InputJack>() == null) {
                        correctJackType = false;
                    }
                }

                if (correctJackType && GetDistanceToConnectionPointOnJackAixs() < MaxJackDistanceBeforeUnsnap && GetDistanceToInitialSnapPoint() < MaxHandSeparationBeforeUnsnap) {
                    snapToJackRoutine = SnapToJack();
                    StartCoroutine(snapToJackRoutine);
                    return true;
                }

                CancelSnap();
            }

            return false;
        }

        private void CancelSnap() {
            targetJack = null;
        }

        /// <summary>
        /// Gets the point on the jack axis to snap to.
        /// 
        /// The point is calculated by getting the component of the grabbing controller position along the jack axis
        /// </summary>
        /// <param name="closestAllowedPoint">The snap point will not be closer to the jack than this point</param>
        /// <returns>The snap point</returns>
        private Vector3 GetSnapPoint(Vector3 closestAllowedPoint) {
            Vector3 controllerPos = interactable.GetGrabbingObject().transform.position;
            Vector3 snapPoint = targetJack.PlugConnectionPoint.position + Vector3.Dot(controllerPos, targetJack.PlugConnectionPoint.forward) * targetJack.PlugConnectionPoint.forward;

            if(Vector3.Dot(snapPoint - closestAllowedPoint, targetJack.PlugConnectionPoint.forward) > 0) {
                snapPoint = closestAllowedPoint;
            }

            return snapPoint;
        }

        /// <summary>
        /// Gets the distance along only the jack axis from the plug model to where connected plugs end up 
        /// </summary>
        /// <returns></returns>
        private float GetDistanceToConnectionPointOnJackAixs() {
            if (targetJack != null) {
                Vector3 plugToConnectionPoint = targetJack.PlugConnectionPoint.position - PlugTransform.position;
                controllerPositionOnJackAxis = PlugTransform.position + plugToConnectionPoint - Vector3.Dot(plugToConnectionPoint, targetJack.PlugConnectionPoint.forward) * targetJack.PlugConnectionPoint.forward;

                return Vector3.Distance(targetJack.PlugConnectionPoint.position, controllerPositionOnJackAxis);
            }
            else {
                return float.PositiveInfinity;
            }
        }

        /// <summary>
        /// </summary>
        /// <returns>The distance from the plug model to the point that it will initially try to snap to</returns>
        private float GetDistanceToInitialSnapPoint() {
            if (targetJack != null) {
                Vector3 plugToSnapPoint = targetJack.PlugConnectionPoint.position - PlugTransform.position;
                controllerPositionOnJackAxis = PlugTransform.position + plugToSnapPoint - Vector3.Dot(plugToSnapPoint, targetJack.PlugConnectionPoint.forward) * targetJack.PlugConnectionPoint.forward;

                float distance = Vector3.Distance(PlugTransform.position, controllerPositionOnJackAxis);

                if(distance < 0.005f) {
                    //The distance to the jack axis is small, so we don't need to snap the plug in front of the jack
                    return 0f;
                }
                else {
                    //The plug will need to snap in front of the jack so that it doesn't intersect the jack
                    Vector3 closestAllowedStartPosition = targetJack.PlugConnectionPoint.position - targetJack.PlugConnectionPoint.forward * SmallestAllowedInitialDistanceFromJack;
                    Vector3 initialSnapPoint = GetSnapPoint(closestAllowedStartPosition);

                    return Vector3.Distance(initialSnapPoint, PlugTransform.position);
                }
            }
            else {
                return float.PositiveInfinity;
            }
        }

        private void PositionPlugOnJackAxis(Vector3 closestAllowedPoint) {
            if (!interactable.IsGrabbed()) {
                return;
            }

            positionSnap.SnapToTarget(GetSnapPoint(closestAllowedPoint), JackSnapSpeed);
        }

        /// <summary>
        /// Positions the plug along the jack axis until the unsnap conditions are met or the plug is let go by the user
        /// </summary>
        /// <returns></returns>
        private IEnumerator SnapToJack() {
            targetJack.SetState(Jack.State.Blocked);
            PlugTransform.SetParent(null);

            rotationSnap.SnapToTarget(targetJack.PlugConnectionPoint.rotation, RotationSnapSpeed);

            float distanceFromJackAxis = GetDistanceToInitialSnapPoint();
            bool aligned = distanceFromJackAxis == 0;

            Vector3 closestAllowedStartPosition = targetJack.PlugConnectionPoint.position - targetJack.PlugConnectionPoint.forward * SmallestAllowedInitialDistanceFromJack;

            while (targetJack != null) {
                if (!aligned) {
                    PositionPlugOnJackAxis(closestAllowedStartPosition);
                    if (positionSnap.HasReachedTarget) {
                        aligned = true;
                    }
                }else{
                    PositionPlugOnJackAxis(targetJack.PlugConnectionPoint.position);

                    if (!interactable.IsGrabbed()) {
                        break;
                    }

                    float handDistance = Vector3.Distance(interactable.GetGrabbingObject().transform.position, PlugTransform.position);
                    float jackDistance = GetDistanceToConnectionPointOnJackAixs();

                    if ((handDistance > MaxHandSeparationBeforeUnsnap + BUFFER_DISTANCE && jackDistance > AutoPlugDistance) || jackDistance > MaxJackDistanceBeforeUnsnap + BUFFER_DISTANCE) {
                        UnSnapFromJack();
                        StartCoroutine(SnapBackToController());
                        break;
                    }
                }

                yield return new WaitForEndOfFrame();
            }

            yield return null;
        }

        /// <summary>
        /// Moves the plug to the jack's connection point and makes a data connection between physically connected jacks
        /// </summary>
        /// <returns></returns>
        private IEnumerator AutoPlugIntoTarget() {
            StopCoroutine(snapToJackRoutine);
            snapToJackRoutine = null;

            positionSnap.SnapToTarget(DestinationJack.PlugConnectionPoint.position, 1f);

            while (!positionSnap.HasReachedTarget) {
                yield return new WaitForEndOfFrame();
            }

            if(DestinationJack != null && SourceJack != null) {
                DestinationJack.ConnecToJack(SourceJack);
                SourceJack.ConnecToJack(DestinationJack);
            }

            yield return null;
        }

        private void UnSnapFromJack() {
            StopCoroutine(snapToJackRoutine);
            snapToJackRoutine = null;

            targetJack.SetState(Jack.State.Free);
            targetJack = null;
        }

        /// <summary>
        /// Reposition and reorient the root Plug object where it's physical representation is located. Reparent the model to the root object.
        /// </summary>
        private void ResetPlugTransform() {
            transform.position = PlugTransform.position;
            transform.rotation = PlugTransform.rotation;
            PlugTransform.parent = transform;
        }

        /// <summary>
        /// Quickly moves the plug back to the controller and sets it back to its initial state
        /// </summary>
        /// <returns></returns>
        private IEnumerator SnapBackToController() {
            yield return null;

            snapCooldown = true;

            transform.localPosition = Vector3.zero;
            transform.rotation = Quaternion.LookRotation(transform.parent.forward);

            positionSnap.SnapToTarget(transform.position, SnapToHandSpeed);
            rotationSnap.SnapToTarget(transform.rotation, RotationSnapSpeed);

            Debug.Log("Snapping back to hand");
            while (true) {
                positionSnap.SnapToTarget(transform.position, SnapToHandSpeed);
                if (positionSnap.HasReachedTarget) {
                    break;
                }

                yield return new WaitForEndOfFrame();
            }
            Debug.Log("Snap complete");

            ResetPlugTransform();
            StartCoroutine(SnapCooldown());
        }

        private IEnumerator SnapCooldown() {
            yield return new WaitForSeconds(SnapCooldownTime);
            snapCooldown = false;
        }

        public void AddNearbyJack(Jack j) {
            if (!nearbyJacks.Contains(j)) {
                nearbyJacks.Add(j);
            }
        }

        public void RemoveNearbyJack(Jack j) {
            nearbyJacks.Remove(j);
        }

        public void SetConnectedPlug(Plug plug) {
            connectedPlug = plug;
        }

    }

}