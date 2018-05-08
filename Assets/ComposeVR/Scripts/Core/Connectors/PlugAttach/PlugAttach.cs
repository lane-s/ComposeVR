using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRTK;

namespace ComposeVR {
    /// <summary>
    /// Subclasses of PlugAttach should define some mechanism for snapping a Plug to a PlugReceptacle 
    /// </summary>
    [RequireComponent(typeof(PlugReceptacle))]
    public abstract class PlugAttach : MonoBehaviour {
        private Plug _lockedPlug;
        public Plug LockedPlug {
            get {
                return _lockedPlug;
            }
            set {
                _lockedPlug = value;
                if (_lockedPlug != null) {
                    OnPlugLocked();
                }
                else {
                    OnReceptacleAvailable();
                }
            }
        }

        [Tooltip("How far away can the controller grabbing the Plug be before it is unlocked")]
        public float MaxControllerSeparation = 0.14f;

        [Tooltip("The speed at which the Plug will snap when locked by this attach mechanism")]
        public float PositionSnapSpeed = 0.2f;

        [Tooltip("The speed at which the Plug will rotate to the correct orientation when locked by this attach mechanism")]
        public float RotationSnapSpeed = 900f;

        [Tooltip("A Plug entering this volume will be snapped to the PlugAttach")]
        public SimpleTrigger PlugDetector;

        protected VRTK_InteractGrab lockedPlugGrabber;
        protected PlugReceptacle plugReceptacle;

        private List<Plug> nearbyPlugs;

        protected virtual void Awake() {
            plugReceptacle = GetComponent<PlugReceptacle>();
            nearbyPlugs = new List<Plug>();
                
            if (PlugDetector) {
                PlugDetector.TriggerEnter += OnPlugDetectorTriggerEntered;
                PlugDetector.TriggerExit += OnPlugDetectorTriggerExit;
            }
            else {
                Debug.LogError("PlugAttach requires a reference to a SimpleTrigger in the PlugDetector field in the inspector");
            }
        }

        protected virtual void Update() {
            //If the PlugAttach has no lock on a plug, loop through the nearby plugs and try to get one
            if(LockedPlug == null) {
                for(int i = 0; i < nearbyPlugs.Count; i++) {
                    TryLockPlug(nearbyPlugs[i]);
                }
            }else if(Vector3.Distance(LockedPlug.transform.position, LockedPlug.PlugTransform.position) > MaxControllerSeparation) {
                OnMaxControllerSeparationExceeded();
            }
        }

        protected virtual void OnMaxControllerSeparationExceeded() {
            UnlockPlug();
        }

        protected virtual void OnLockedPlugGrabbed(object sender, InteractableObjectEventArgs args) {
            lockedPlugGrabber = args.interactingObject.GetComponentInActor<VRTK_InteractGrab>();
        }

        protected virtual void OnLockedPlugUngrabbed(object sender, InteractableObjectEventArgs args) {
            UnlockPlug();
        }

        private void OnPlugDetectorTriggerEntered(object sender, SimpleTriggerEventArgs args) {
            Plug p = args.other.gameObject.GetComponentInActor<Plug>();
            if(p != null) {
                nearbyPlugs.Add(p);
            }
        }

        private void OnPlugDetectorTriggerExit(object sender, SimpleTriggerEventArgs args) {
            Plug p = args.other.gameObject.GetComponentInActor<Plug>();
            if(p != null) {
                nearbyPlugs.Remove(p);

                //If the plug attachLock was held by the PlugAttach, release it
                if (p.Equals(LockedPlug) && !p.IsPluggedIn()) {
                    UnlockPlug();
                }
            }
        }

        private void TryLockPlug(Plug p) {
            if (p.GetComponent<VRTK_InteractableObject>().IsGrabbed()) {
                float flow = p.ConnectedCord.Flow;
                if (p.CordAttachPoint.Equals(p.ConnectedCord.StartNode)) {
                    flow = -flow;
                }

                bool validReceptacleType = (flow > 0 && GetComponent<PhysicalDataInput>() != null) || (flow < 0 && GetComponent<PhysicalDataOutput>() != null) || flow == 0;

                if (validReceptacleType) {
                    if (p.AttachLock(this)) {
                        LockedPlug = p;
                        LockedPlug.GetComponent<VRTK_InteractableObject>().InteractableObjectUngrabbed += OnLockedPlugUngrabbed;
                        LockedPlug.GetComponent<VRTK_InteractableObject>().InteractableObjectGrabbed += OnLockedPlugGrabbed;
                        lockedPlugGrabber = LockedPlug.GetComponent<VRTK_InteractableObject>().GetGrabbingObject().GetComponentInActor<VRTK_InteractGrab>();
                    }
                }
            }
        }

        protected virtual void UnlockPlug() {
            if (nearbyPlugs.Contains(LockedPlug)) {
                //Move Plug to the back of the nearby list
                nearbyPlugs.Remove(LockedPlug);
                nearbyPlugs.Add(LockedPlug);
            }

            LockedPlug.GetComponent<VRTK_InteractableObject>().InteractableObjectUngrabbed -= OnLockedPlugUngrabbed;
            LockedPlug.GetComponent<VRTK_InteractableObject>().InteractableObjectGrabbed -= OnLockedPlugGrabbed;

            LockedPlug.AttachUnlock(this);
            LockedPlug = null;
            lockedPlugGrabber = null;
        }

        protected abstract void OnPlugLocked();
        protected abstract void OnReceptacleAvailable();
    }
}
