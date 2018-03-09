using System;
using System.Collections.Generic;
using UnityEngine;

namespace ComposeVR {
    public class JackEventArgs : EventArgs {
        private Cord connectedCord;
        private LinkedListNode<BranchNode> plugEnd;

        public JackEventArgs(Cord connectedCord, LinkedListNode<BranchNode> plugEnd) {
            this.connectedCord = connectedCord;
            this.plugEnd = plugEnd;
        }

        public Cord ConnectedCord {
            get { return connectedCord;  }
            set { connectedCord = value;  }
        }

        public LinkedListNode<BranchNode> PlugNodeInCord {
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
    public sealed class Jack : MonoBehaviour {

        public Transform PlugStart;
        public Transform PlugConnectionPoint;

        public SimpleTrigger PlugDetector;

        public EventHandler<JackEventArgs> PlugConnected;
        public EventHandler<JackEventArgs> PlugDisconnected;

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
            OwnedObject o = e.other.GetComponent<OwnedObject>();
            if(o != null) {
                Plug p = o.Owner.GetComponent<Plug>();
                if(p != null) {
                    p.AddNearbyJack(this);
                }
            }
        }

        void OnPlugLeaveArea(object sender, SimpleTriggerEventArgs e) {
            OwnedObject o = e.other.GetComponent<OwnedObject>();
            if(o != null) {
                Plug p = o.Owner.GetComponent<Plug>();
                if(p != null) {
                    p.RemoveNearbyJack(this);
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

        public void Connect(Cord connectedCord, LinkedListNode<BranchNode> plugNodeInCord) {
            if(PlugConnected != null) {
                PlugConnected(this, new JackEventArgs(connectedCord, plugNodeInCord));
            }
        }

        public void Disconnect(Cord connectedCord, LinkedListNode<BranchNode> plugNodeInCord) {
            if(PlugDisconnected != null) {
                PlugDisconnected(this, new JackEventArgs(connectedCord, plugNodeInCord));
            }
        }
    }
}