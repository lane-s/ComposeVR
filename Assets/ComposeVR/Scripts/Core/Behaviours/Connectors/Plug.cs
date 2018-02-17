using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using VRTK;

namespace ComposeVR {
    [RequireComponent(typeof(VRTK_InteractableObject))]
    public sealed class Plug : MonoBehaviour {

        public Jack OriginJack;
        public Jack DestinationJack;


        public float AutoPlugDistance = 0.5f;
        public float MaxHandSeparationBeforeUnsnap = 1.0f;
        public float MaxJackDistanceBeforeUnsnap = 1.3f;

        public float SnapToHandSpeed = 20.0f;
        public float SnapCooldownTime = 1.0f;
        public float JackSnapSpeed = 0.5f;
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

        private IEnumerator snapToJackRoutine;

        // Use this for initialization
        void Awake() {
            interactable = GetComponent<VRTK_InteractableObject>();
            nearbyJacks = new List<Jack>();

            interactable.InteractableObjectGrabbed += OnGrabbed;
            interactable.InteractableObjectUngrabbed += OnUngrabbed;

            positionSnap = PlugTransform.GetComponent<SnapToTargetPosition>();
            rotationSnap = PlugTransform.GetComponent<SnapToTargetRotation>();
            snapCooldown = false;
        }

        // Update is called once per frame
        void Update() {
            if (targetJack == null && !snapCooldown) {
                foreach (Jack j in nearbyJacks) {

                    if (j.GetState() == Jack.State.Free) {
                        targetJack = j;
                        TrySnapToJack();
                        break;
                    }
                }
            }
            else if(targetJack != null){
                if (snapToJackRoutine == null && targetJack.GetState() != Jack.State.Free) {
                    targetJack = null;
                }
            }
        }


        private void OnGrabbed(object sender, InteractableObjectEventArgs e) {
            if (DestinationJack != null) {
                //The plug was plugged in 
                targetJack = DestinationJack;
                DestinationJack.SetState(Jack.State.Free);

                if (OriginJack != null) {
                    DestinationJack.DisconnectJack(OriginJack);
                    OriginJack.DisconnectJack(DestinationJack);
                }

                DestinationJack = null;
                connectedPlug.OriginJack = null;
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
                connectedPlug.OriginJack = targetJack;

                ResetPlugTransform();

                if (GetTargetJackDistance() < AutoPlugDistance) {
                    StartCoroutine(AutoPlugIntoTarget());
                }
                else {
                    UnSnapFromJack();
                }
            }

            if(DestinationJack == null && OriginJack == null) {
                //Self destruct this cord
            }
        }

        //We have a small buffer distance so that the plug doesn't begin snapping to a jack when it's likely to immediately snap back to the controller
        private const float BUFFER_DISTANCE = 0.025f;

        private void TrySnapToJack() {
            if (interactable.IsGrabbed() && targetJack != null) {
                if (GetTargetJackDistance() < MaxJackDistanceBeforeUnsnap - BUFFER_DISTANCE && GetDistanceToJackAxis() < MaxHandSeparationBeforeUnsnap - BUFFER_DISTANCE) {
                    snapToJackRoutine = SnapToJack();
                    StartCoroutine(snapToJackRoutine);
                }
                else {
                    targetJack = null;
                }
            }
        }

        private void ResetPlugTransform() {
            transform.position = PlugTransform.position;
            transform.rotation = PlugTransform.rotation;
            PlugTransform.parent = transform;
        }

        private Vector3 projectionOnJackAxis;

        private float GetTargetJackDistance() {
            if (targetJack != null) {
                Vector3 plugToSnapPoint = targetJack.PlugSnapPoint.position - PlugTransform.position;
                projectionOnJackAxis = PlugTransform.position + plugToSnapPoint - Vector3.Dot(plugToSnapPoint, targetJack.PlugSnapPoint.forward) * targetJack.PlugSnapPoint.forward;

                return Vector3.Distance(targetJack.PlugSnapPoint.position, projectionOnJackAxis);
            }
            else {
                return float.PositiveInfinity;
            }
        }

        private float GetDistanceToJackAxis() {
            if (targetJack != null) {
                Vector3 plugToSnapPoint = targetJack.PlugSnapPoint.position - PlugTransform.position;
                projectionOnJackAxis = PlugTransform.position + plugToSnapPoint - Vector3.Dot(plugToSnapPoint, targetJack.PlugSnapPoint.forward) * targetJack.PlugSnapPoint.forward;

                return Vector3.Distance(PlugTransform.position, projectionOnJackAxis);
            }
            else {
                return float.PositiveInfinity;
            }
        }

        public void SetConnectedPlug(Plug plug) {
            connectedPlug = plug;
        }

        private void PositionPlugOnJackAxis() {
            if (!interactable.IsGrabbed()) {
                return;
            }

            //Move the plug to the projection of the hand position along the jack axis
            var controllerPos = interactable.GetGrabbingObject().transform.position;
            var targetPosition = targetJack.PlugSnapPoint.position + Vector3.Dot(controllerPos, targetJack.PlugSnapPoint.forward) * targetJack.PlugSnapPoint.forward;

            //Don't let the plug go behind the jack's snap point
            if(Vector3.Dot(targetPosition - targetJack.PlugSnapPoint.position, targetJack.PlugSnapPoint.forward) > 0) {
                targetPosition = targetJack.PlugSnapPoint.position;
            }

            positionSnap.SnapToTarget(targetPosition, JackSnapSpeed);
        }

        private const float UnSnapConstant = 0.05f;

        private IEnumerator SnapToJack() {
            targetJack.SetState(Jack.State.Blocked);

            PlugTransform.SetParent(null);

            rotationSnap.enabled = true;
            rotationSnap.TargetRotation = targetJack.PlugSnapPoint.rotation;

            float distanceFromJackAxis = GetDistanceToJackAxis();
            Vector3 startPosition = projectionOnJackAxis;

            bool aligned = distanceFromJackAxis < 0.005f;

            if (!aligned) {
                Vector3 closestAllowedStartPosition = targetJack.PlugSnapPoint.position - targetJack.PlugSnapPoint.forward * 0.16f;

                //Plug snap point points towards the jack, so if startPosition is closer to the jack than closestAllowed then it will be pointing the same way as the snap point
               if(Vector3.Dot(startPosition - closestAllowedStartPosition, targetJack.PlugSnapPoint.forward) > 0) {
                    startPosition = closestAllowedStartPosition;
                }
            }

            while (targetJack != null) {
                if (!aligned) {
                    positionSnap.SnapToTarget(startPosition, JackSnapSpeed);
                    if (positionSnap.HasReachedTarget) {
                        aligned = true;
                    }
                }else{
                    PositionPlugOnJackAxis();

                    if (!interactable.IsGrabbed()) {
                        break;
                    }

                    float handDistance = Vector3.Distance(interactable.GetGrabbingObject().transform.position, PlugTransform.position);
                    float jackDistance = GetTargetJackDistance();

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

        private IEnumerator AutoPlugIntoTarget() {
            StopCoroutine(snapToJackRoutine);
            snapToJackRoutine = null;

            positionSnap.SnapToTarget(DestinationJack.PlugSnapPoint.position, 1f);

            while (!positionSnap.HasReachedTarget) {
                yield return new WaitForEndOfFrame();
            }

            if(DestinationJack != null && OriginJack != null) {
                DestinationJack.ConnecToJack(OriginJack);
                OriginJack.ConnecToJack(DestinationJack);
            }

            yield return null;
        }

        private EventHandler<EventArgs> ReturnedToHand;

        private void UnSnapFromJack() {
            StopCoroutine(snapToJackRoutine);
            snapToJackRoutine = null;

            rotationSnap.enabled = false;

            targetJack.SetState(Jack.State.Free);
            targetJack = null;
        }

        private IEnumerator SnapBackToController() {
            yield return null;

            snapCooldown = true;

            rotationSnap.enabled = true;

            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;

            positionSnap.SnapToTarget(transform.position, SnapToHandSpeed);
            rotationSnap.TargetRotation = transform.rotation;

            while (true) {
                positionSnap.SnapToTarget(transform.position, SnapToHandSpeed);
                if (positionSnap.HasReachedTarget) {
                    break;
                }

                rotationSnap.TargetRotation = transform.rotation;
                yield return new WaitForEndOfFrame();
            }

            ResetPlugTransform();

            rotationSnap.enabled = false;

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

    }

}