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
    /// PlugReceptacles represent a generic endpoint for physical connections (which always end with a Plug)
    /// 
    /// They detect nearby Plugs and pass them on to a sibling PlugAttach component if one is present
    /// </summary>
    public class PlugReceptacle : MonoBehaviour {

        public EventHandler<PhysicalConnectionEventArgs> PlugConnected;
        public EventHandler<PhysicalConnectionEventArgs> PlugDisconnected;

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
