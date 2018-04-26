using System;
using System.Collections.Generic;
using UnityEngine;

namespace ComposeVR {
    public class PhysicalConnectionEventArgs : EventArgs {
        private Cord connectedCord;
        private Transform plugEnd;

        public PhysicalConnectionEventArgs(Cord connectedCord, Transform plugEnd) {
            this.connectedCord = connectedCord;
            this.plugEnd = plugEnd;
        }

        public Cord ConnectedCord {
            get { return connectedCord;  }
            set { connectedCord = value;  }
        }

        public Transform PlugNodeInCord {
            get { return plugEnd; }
            set { plugEnd = value; }
        }
    }

    /// <summary>
    /// Jacks provide a way to connect different modules.
    /// 
    /// Jack gameObjects should have either an InputJack or OutputJack component depending on the type of the Jack
    /// 
    /// Jacks may also have a CordDispenser so that they give the user a cord when their hand comes near the jack
    /// 
    /// </summary>
    public sealed class PlugSocket : MonoBehaviour {

        public Transform PlugStart;
        public Transform PlugConnectionPoint;

        public SimpleTrigger PlugDetector;

        public EventHandler<PhysicalConnectionEventArgs> PlugConnected;
        public EventHandler<PhysicalConnectionEventArgs> PlugDisconnected;

        private bool blocked;

        private void Awake() {
            PlugDetector.TriggerEnter += OnPlugEnterArea;
            PlugDetector.TriggerExit += OnPlugLeaveArea;
        }

        /// <summary>
        /// Keep track of nearby plugs
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void OnPlugEnterArea(object sender, SimpleTriggerEventArgs e) {
            ActorSubObject o = e.other.GetComponent<ActorSubObject>();
            if(o != null) {
                Plug p = o.Actor.GetComponent<Plug>();
                if(p != null) {
                    p.AddNearbySocket(this);
                }
            }
        }

        void OnPlugLeaveArea(object sender, SimpleTriggerEventArgs e) {
            ActorSubObject o = e.other.GetComponent<ActorSubObject>();
            if(o != null) {
                Plug p = o.Actor.GetComponent<Plug>();
                if(p != null) {
                    p.RemoveNearbySocket(this);
                }
            }
        }

        public bool IsBlocked() {
            return blocked;
        }

        public void Block() {
            if (GetComponent<CordDispenser>()) {
                GetComponent<CordDispenser>().Block();
            }
            blocked = true;
        }

        public void Unblock() {
            if (GetComponent<CordDispenser>()) {
                GetComponent<CordDispenser>().Unblock();
            }
            blocked = false;
        }

        public void Connect(Cord connectedCord, Transform plugNodeInCord) {
            if(PlugConnected != null) {
                PlugConnected(this, new PhysicalConnectionEventArgs(connectedCord, plugNodeInCord));
            }
        }

        public void Disconnect(Cord connectedCord, Transform plugNodeInCord) {
            if(PlugDisconnected != null) {
                PlugDisconnected(this, new PhysicalConnectionEventArgs(connectedCord, plugNodeInCord));
            }
        }
    }
}