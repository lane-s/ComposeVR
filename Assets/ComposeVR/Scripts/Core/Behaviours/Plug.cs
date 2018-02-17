﻿using System;
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

        public float SegmentLength = 0.005f;
        public float RelaxAmount = 0.05f;
        public float RelaxTime = 10.0f;
        public float PruneDistance = 0.01f;

        public float AutoPlugDistance = 0.5f;
        public float MaxHandSeparationBeforeUnsnao = 1.0f;
        public float MaxJackDistanceBeforeUnsnap = 1.3f;

        public float SnapToHandSpeed = 20.0f;
        public float SnapCooldownTime = 1.0f;
        public float JackSnapSpeed = 0.5f;
        public Transform PlugTransform;
        public Transform CordAttachPoint;

        private Jack targetJack;

        private List<Jack> nearbyJacks;

        private Plug secondaryPlug;
        private Color cordColor;
        private float normalSnapSpeed;
        private VRTK_InteractableObject interactable;
        private SnapToTargetPosition positionSnap;
        private SnapToTargetRotation rotationSnap;
        private bool snapCooldown;

        private LineRenderer lineRenderer;
        private List<Vector3> path;
        private Vector3 lastPos;
        private float timeRelaxed;

        private bool updateLine;

        private IEnumerator snapToJackRoutine;
        private IEnumerator snapBackToHandRoutine;

        // Use this for initialization
        void Awake() {
            interactable = GetComponent<VRTK_InteractableObject>();
            path = new List<Vector3>();
            nearbyJacks = new List<Jack>();

            lineRenderer = GetComponentInChildren<LineRenderer>();
            cordColor = new Color(UnityEngine.Random.value, UnityEngine.Random.value, UnityEngine.Random.value);

            if (lineRenderer) {
                lineRenderer.material.SetColor("_TintColor", cordColor);
            }

            timeRelaxed = RelaxTime;

            interactable.InteractableObjectGrabbed += OnGrabbed;
            interactable.InteractableObjectUngrabbed += OnUngrabbed;

            positionSnap = PlugTransform.GetComponent<SnapToTargetPosition>();
            rotationSnap = PlugTransform.GetComponent<SnapToTargetRotation>();
            snapCooldown = false;
        }

        // Update is called once per frame
        void Update() {
            UpdateCord();

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

        private void UpdateCord() {

            if (secondaryPlug != null) {
                //Start a path from the origin
                if (path.Count == 0 && OriginJack != null) {
                    path.Add(OriginJack.CordOrigin.position);
                }

                if (!lastPos.Equals(CordAttachPoint.position)) {

                    updateLine = false;

                    //Extend path if the plug moves
                    if (path.Count > 0) {
                        if (Vector3.Distance(path.Last(), CordAttachPoint.position) > SegmentLength) {
                            path.Add(CordAttachPoint.position);
                            updateLine = true;
                            timeRelaxed = 0;
                        }
                    }

                    if (interactable.IsGrabbed()) {
                        updateLine = true;
                    }
                }
                else if (timeRelaxed < RelaxTime) {
                    timeRelaxed += Time.deltaTime;
                    updateLine = true;
                }

                //This plug is responsible for the cord between two plugs, so we have to check if the other plug moves too
               if(secondaryPlug.gameObject.active && path[0] != secondaryPlug.CordAttachPoint.position) {
                    if(Vector3.Distance(path[0], secondaryPlug.CordAttachPoint.position) > SegmentLength) {
                        //If it does, we add on to the beginning of the path
                        path.Insert(0, secondaryPlug.CordAttachPoint.position);
                        updateLine = true;
                        timeRelaxed = 0;
                    }
                }

                if (updateLine) {
                    RelaxPath();
                    UpdateLine();
                }

                lastPos = CordAttachPoint.position;

            }
        }

        private void RelaxPath() {
            for(int i = 0; i < path.Count; i++) {
                if(i != 0 && i != path.Count - 1) {
                    //Take the average of the current point and the adjacent points on the path
                    Vector3 targetPosition = (path[i - 1] + path[i] + path[i + 1]) / 3;

                    //Smoothly move towards this position
                    path[i] = Vector3.Lerp(path[i], targetPosition, RelaxAmount);
                }
            }

            //Prune unecessary points
            for(int i = 0; i < path.Count; i++) {
                if(i != 0 && i != path.Count - 1) {
                    if(Vector3.Distance(path[i - 1], path[i]) < PruneDistance) {
                        path.RemoveAt(i);
                    }
                }
            }

        }

        private void UpdateLine() {
            lineRenderer.positionCount = path.Count;
            lineRenderer.SetPositions(path.ToArray());
        }

        private void OnGrabbed(object sender, InteractableObjectEventArgs e) {
            if (DestinationJack != null) {
                //The jack was plugged in
                targetJack = DestinationJack;
                DestinationJack.SetState(Jack.State.Free);
                DestinationJack = null;
            }
            else {
                PlugTransform.SetParent(null);
                StartCoroutine(SnapBackToHand());
            }

            TrySnapToJack();
        }

        private void OnUngrabbed(object sender, InteractableObjectEventArgs e) {
            if (targetJack != null) {
                DestinationJack = targetJack;

                ResetPlugTransform();

                if (GetTargetJackDistance() < AutoPlugDistance) {
                    StartCoroutine(AutoPlugIntoTarget());
                }
                else {
                    UnSnapFromJack();
                }
            }
        }

        private const float BUFFER_DISTANCE = 0.025f;

        private void TrySnapToJack() {
            if (interactable.IsGrabbed() && targetJack != null) {
                if (GetTargetJackDistance() < MaxJackDistanceBeforeUnsnap - BUFFER_DISTANCE && GetDistanceToJackAxis() < MaxHandSeparationBeforeUnsnao - BUFFER_DISTANCE) {
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

        //The secondary plug is the plug which is not responsible for rendering the cord
        public void SetSecondaryPlug(Plug plug) {
            secondaryPlug = plug;
        }

        private void PositionPlugOnJackAxis() {
            if (!interactable.IsGrabbed()) {
                return;
            }

            //Move the plug to the projection of the hand position along the jack axis
            var handPos = interactable.GetGrabbingObject().transform.position;
            var targetPosition = targetJack.PlugSnapPoint.position + Vector3.Dot(handPos, targetJack.PlugSnapPoint.forward) * targetJack.PlugSnapPoint.forward;

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

                    if ((handDistance > MaxHandSeparationBeforeUnsnao && jackDistance > AutoPlugDistance) || jackDistance > MaxJackDistanceBeforeUnsnap) {
                        UnSnapFromJack();
                        StartCoroutine(SnapBackToHand());
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

            yield return null;

            //Create data connection
        }

        private EventHandler<EventArgs> ReturnedToHand;

        private void UnSnapFromJack() {
            StopCoroutine(snapToJackRoutine);
            snapToJackRoutine = null;

            rotationSnap.enabled = false;

            targetJack.SetState(Jack.State.Free);
            targetJack = null;
        }

        private IEnumerator SnapBackToHand() {
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