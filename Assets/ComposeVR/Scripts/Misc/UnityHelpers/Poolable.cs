using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ComposeVR {

    public class Poolable : MonoBehaviour {
        public event EventHandler<EventArgs> Initialized;
        public bool InPool {
            get { return inPool; }
        }

        private bool inPool = true;

        private ObjectPool pool;
        private EventArgs defaultArgs;

        private void Awake() {
            defaultArgs = new EventArgs();
        }

        public void Initialize(ObjectPool pool) {
            inPool = false;
            this.pool = pool;
            gameObject.SetActive(true);
            if(Initialized != null) {
                Initialized(this, defaultArgs);
            }
        }

        public void ReturnToPool() {
            inPool = true;
            gameObject.SetActive(false);
            pool.ReturnObject(this);
        }
    }
}
