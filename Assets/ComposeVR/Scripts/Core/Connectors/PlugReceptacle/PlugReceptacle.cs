using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRTK;

namespace ComposeVR
{
    /// <summary>
    /// Subclasses of PlugAttach should define some mechanism for snapping a Plug to a PlugReceptacle 
    /// </summary>
    [RequireComponent(typeof(PhysicalDataEndpoint))]
    public abstract class PlugReceptacle : MonoBehaviour
    {
        private Plug _lockedPlug;
        public Plug LockedPlug
        {
            get
            {
                return _lockedPlug;
            }
            set
            {
                _lockedPlug = value;
                if (_lockedPlug != null)
                {
                    OnPlugLocked();
                }
                else
                {
                    OnReceptacleAvailable();
                }
            }
        }

        [Tooltip("How far away can the controller grabbing the Plug be before it is unlocked")]
        public float MaxGrabberSeparation = 0.08f;

        [Tooltip("The speed at which the Plug will snap when locked by this attach mechanism")]
        public float PositionSnapSpeed = 0.2f;

        [Tooltip("The speed at which the Plug will rotate to the correct orientation when locked by this attach mechanism")]
        public float RotationSnapSpeed = 900f;

        [Tooltip("A Plug entering this volume will be snapped to the PlugAttach")]
        public SimpleTrigger PlugLockTrigger;

        [Tooltip("A Plug leaving this volume will be unsnapped from the PlugAttach")]
        public SimpleTrigger PlugUnlockTrigger;

        protected VRTK_InteractGrab lockedPlugGrabber;
        protected PhysicalDataEndpoint plugReceptacle;

        protected List<Plug> nearbyPlugs;

        protected virtual void Awake()
        {
            plugReceptacle = GetComponent<PhysicalDataEndpoint>();
            nearbyPlugs = new List<Plug>();

            if (PlugLockTrigger != null && PlugUnlockTrigger != null)
            {
                PlugLockTrigger.TriggerEnter += OnPlugLockTriggerEnter;
                PlugLockTrigger.TriggerExit += OnPlugLockTriggerExit;
                PlugUnlockTrigger.TriggerExit += OnPlugUnlockTriggerExit;
            }
            else
            {
                Debug.LogError("PlugAttach requires a reference to a SimpleTrigger in the PlugTrigger fields in the inspector");
            }
        }

        protected virtual void Update()
        {
            //If the PlugAttach has no lock on a plug, loop through the nearby plugs and try to get one
            if (LockedPlug == null)
            {
                for (int i = 0; i < nearbyPlugs.Count; i++)
                {
                    TryLockPlug(nearbyPlugs[i]);
                }
            }
            else if (Vector3.Distance(LockedPlug.transform.position, LockedPlug.PlugTransform.position) > MaxGrabberSeparation)
            {
                OnMaxGrabberSeparationExceeded();
            }
        }

        protected virtual void OnMaxGrabberSeparationExceeded()
        {
            UnlockPlug();
        }

        public void OnLockedPlugWillBeDestroyed()
        {
            if (nearbyPlugs.Contains(LockedPlug))
            {
                nearbyPlugs.Remove(LockedPlug);
            }
            UnlockPlug();
        }

        protected virtual void OnLockedPlugGrabbed(object sender, InteractableObjectEventArgs args)
        {
            lockedPlugGrabber = args.interactingObject.GetComponentInActor<VRTK_InteractGrab>();
        }

        protected virtual void OnLockedPlugUngrabbed(object sender, InteractableObjectEventArgs args)
        {
            UnlockPlug();
        }

        private void OnPlugLockTriggerEnter(object sender, SimpleTriggerEventArgs args)
        {
            Plug p = args.other.gameObject.GetComponentInActor<Plug>();
            if (p != null && !nearbyPlugs.Contains(p))
            {
                nearbyPlugs.Add(p);
            }
        }

        private void OnPlugLockTriggerExit(object sender, SimpleTriggerEventArgs args)
        {
            Plug p = args.other.gameObject.GetComponentInActor<Plug>();
            if (p != null && nearbyPlugs.Contains(p))
            {
                nearbyPlugs.Remove(p);
            }
        }

        private void OnPlugUnlockTriggerExit(object sender, SimpleTriggerEventArgs args)
        {
            Plug p = args.other.gameObject.GetComponentInActor<Plug>();
            if (p != null)
            {
                //If the plug attachLock was held by the PlugAttach, release it
                if (p.Equals(LockedPlug) && !p.IsPluggedIn())
                {
                    UnlockPlug();
                }
            }
        }

        protected virtual void TryLockPlug(Plug p)
        {
            if (p.GetComponent<VRTK_InteractableObject>().IsGrabbed())
            {
                float flow = p.ConnectedCord.Flow;
                if (p.CordAttachPoint.Equals(p.ConnectedCord.StartNode))
                {
                    flow = -flow;
                }

                bool validReceptacleType = (flow > 0 && GetComponent<PhysicalDataInput>() != null) || (flow < 0 && GetComponent<PhysicalDataOutput>() != null) || flow == 0;

                if (validReceptacleType)
                {
                    if (p.AttachLock(this))
                    {
                        lockedPlugGrabber = p.GetComponent<VRTK_InteractableObject>().GetGrabbingObject().GetComponentInActor<VRTK_InteractGrab>();
                        LockedPlug = p;
                        LockedPlug.GetComponent<VRTK_InteractableObject>().InteractableObjectUngrabbed += OnLockedPlugUngrabbed;
                        LockedPlug.GetComponent<VRTK_InteractableObject>().InteractableObjectGrabbed += OnLockedPlugGrabbed;
                    }
                }
            }
        }


        protected virtual void UnlockPlug()
        {
            if (nearbyPlugs.Contains(LockedPlug))
            {
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
