using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRTK;
using System;

namespace ComposeVR {
    [RequireComponent(typeof(VRTK_InteractableObject))]
    [RequireComponent(typeof(CordFollower))]
    public sealed class Plug : MonoBehaviour {

        public PlugReceptacle DestinationReceptacle;

        public float SnapToHandSpeed = 20.0f;

        public Transform PlugTransform;
        public Transform CordAttachPoint;

        private bool _attachLocked = false;
        public bool AttachLocked {
            get { return _attachLocked; }
        } 
        private object attachLocker;

        private Cord connectedCord;

        private VRTK_InteractableObject interactable;
        private SnapToTargetPosition positionSnap;
        private SnapToTargetRotation rotationSnap;
        private float normalSnapSpeed;
        private Vector3 controllerPositionOnJackAxis;
        private bool snappingEnabled = true;

        private Vector3 plugColliderCenter;
        private float plugColliderHeight;

        private const float PLUGGED_IN_COLLIDER_HEIGHT = 0.02f;

        public Cord ConnectedCord {
            get { return connectedCord; }
            set {
                connectedCord = value;
                GetComponent<CordFollower>().SetCord(connectedCord);
            }
        }

        void Awake() {
            interactable = GetComponent<VRTK_InteractableObject>();

            interactable.InteractableObjectGrabbed += OnGrabbed;
            interactable.InteractableObjectUngrabbed += OnUngrabbed;

            positionSnap = PlugTransform.GetComponent<SnapToTargetPosition>();
            rotationSnap = PlugTransform.GetComponent<SnapToTargetRotation>();

            plugColliderCenter = PlugTransform.GetComponent<CapsuleCollider>().center;
            plugColliderHeight = PlugTransform.GetComponent<CapsuleCollider>().height;

            GetComponent<CordFollower>().enabled = false;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public bool AttachLock(object locker) {
            if (!_attachLocked && snappingEnabled) {
                //Only allow the lock if the plug model is in its original position
                if(PlugTransform.parent.Equals(transform) && PlugTransform.localPosition.sqrMagnitude <= 0.005f) {
                    _attachLocked = true;
                    attachLocker = locker;
                    return true;
                }
            }

            return false;
        }

        public bool AttachUnlock(object unlocker) {
            if(_attachLocked && attachLocker.Equals(unlocker)) {
                _attachLocked = false;
                attachLocker = null;
                if (interactable.IsGrabbed()) {
                    StartCoroutine(SnapToController());
                }
                else {
                    StartCoroutine(SnapModelToOrigin());
                }
                return true;
            }

            return false;
        }

        private void OnGrabbed(object sender, InteractableObjectEventArgs e) {
            if (DestinationReceptacle == null) {
                PlugTransform.SetParent(null);
                StartCoroutine(SnapToController());
            }

            connectedCord.AllowBranching(false);
        }

        private void OnUngrabbed(object sender, InteractableObjectEventArgs e) {
            ResetPlugTransform();

            if (connectedCord != null) {
                connectedCord.AllowBranching(true);
            }

            StartCoroutine(TryCollapse());
        }

        /// <summary>
        /// Collapse the cord if one of the following conditions are met:
        ///     1. Both ends of the cord are plugs and neither plug is plugged in
        ///     2. One end of the cord is an unplugged plug and the other is a BranchHandle.
        /// </summary>
        private IEnumerator TryCollapse() {
            yield return null;
            if (!IsPluggedIn() && connectedCord != null) {

                Transform oppositeNode = GetOppositeCordNode();
                Plug p = oppositeNode.GetComponentInActor<Plug>();

                if(p != null && !p.IsPluggedIn()) {
                    connectedCord.Collapse();
                }else if (oppositeNode.GetComponent<BranchHandle>()) {
                    connectedCord.Collapse();
                }
            }
        }

        private Transform GetOppositeCordNode() {
            if (connectedCord != null) {
                if (connectedCord.StartNode.Equals(CordAttachPoint)) {
                    return connectedCord.EndNode;
                }
                else {
                    return connectedCord.StartNode;
                }
            }

            return null;
        }

        public void DisconnectFromReceptacle() {
            DestinationReceptacle.Disconnect(connectedCord, CordAttachPoint);
            DestinationReceptacle = null;

            transform.SetParent(null);
            PlugTransform.GetComponent<CapsuleCollider>().center = plugColliderCenter;
            PlugTransform.GetComponent<CapsuleCollider>().height = plugColliderHeight;

            Transform oppositeNode = GetOppositeCordNode();
            Plug p = oppositeNode.GetComponentInActor<Plug>();

            if(p != null && !p.IsPluggedIn()) {
                connectedCord.Flow = 0;
            }
        }

        public void ConnectToReceptacle(PlugReceptacle receptacle) {
            DestinationReceptacle = receptacle;

            float flow = 1;
            if (CordAttachPoint.Equals(connectedCord.StartNode)) {
                flow = -flow;
            }

            if (DestinationReceptacle.GetComponent<PhysicalDataInput>()) {
                connectedCord.Flow = flow;
            }
            else {
                connectedCord.Flow = -flow;
            }

            connectedCord.Flowing = true;

            DestinationReceptacle.Connect(connectedCord, CordAttachPoint);
            ShrinkCollider();
        }

        public void ShrinkCollider() {
            PlugTransform.GetComponent<CapsuleCollider>().center = new Vector3(0, 0, 0);
            PlugTransform.GetComponent<CapsuleCollider>().height = PLUGGED_IN_COLLIDER_HEIGHT;
        }

        /// <summary>
        /// Reposition and reorient the root Plug object where it's physical representation is located. Reparent the model to the root object.
        /// </summary>
        private void ResetPlugTransform() {
            PlugTransform.SetParent(null);
            transform.position = PlugTransform.position;
            transform.rotation = PlugTransform.rotation;
            PlugTransform.parent = transform;
            PlugTransform.localPosition = Vector3.zero;
        }

        private IEnumerator SnapToController() {
            snappingEnabled = false;
            yield return null;

            transform.localPosition = Vector3.zero;
            transform.rotation = Quaternion.LookRotation(transform.parent.forward);

            StartCoroutine(SnapModelToOrigin());
        }

        /// <summary>
        /// Quickly moves the plug back to the controller and sets it back to its initial state
        /// </summary>
        /// <returns></returns>
        private IEnumerator SnapModelToOrigin() {
            snappingEnabled = false;

            positionSnap.SnapToTarget(transform.position, SnapToHandSpeed);
            float originalCloseEnough = positionSnap.closeEnoughDistance;
            positionSnap.closeEnoughDistance = 0.015f;

            rotationSnap.SnapToTarget(transform.rotation, 1200f);

            while (true) {
                float targetDistance = Vector3.Distance(transform.position, PlugTransform.position);
                float snapSpeed = Mathf.Clamp(SnapToHandSpeed * targetDistance, SnapToHandSpeed, SnapToHandSpeed + 5f);
                positionSnap.SnapToTarget(transform.position+GetComponent<Rigidbody>().velocity, snapSpeed);
                if (positionSnap.HasReachedTarget) {
                    break;
                }

                yield return new WaitForFixedUpdate();
            }

            positionSnap.closeEnoughDistance = originalCloseEnough;

            ResetPlugTransform();
            snappingEnabled = true;
        }

        public void EnableSnapping() {
            snappingEnabled = true;
        }

        public void DisableSnapping() {
            snappingEnabled = false;
        }

        public bool IsPluggedIn() {
            return DestinationReceptacle != null;
        }

        public void DestroyPlug() {
            ResetPlugTransform();
            Destroy(gameObject);
        }
    }
}