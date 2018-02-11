using System;
using System.Collections.Generic;
using ComposeVR.Protocol;
using UnityEngine;

namespace ComposeVR {

    public sealed class TCPClientObject : SingletonObject<TCPClientObject>, IEventQueue {

        public TCPClientController Controller;

        private float maxUpdateTime = 1.0f;

        private Queue<Protocol.Event> EventQueue;
        private System.Object eventQueueLock = new System.Object();

        void Awake() {
            Controller.SetEventQueue(this);
            EventQueue = new Queue<Protocol.Event>();
            DontDestroyOnLoad(this.gameObject);
        }

        // Update is called once per frame
        void Update() {
            int eventCount = 0;

            lock (eventQueueLock) {
                if (EventQueue != null) {
                    eventCount = EventQueue.Count;
                }
            }

            //Set the time to stop processing incoming events, otherwise the frame may hang
            float timeout = Time.realtimeSinceStartup + maxUpdateTime;

            while (eventCount > 0) {

                if (Time.realtimeSinceStartup > timeout) {
                    break;
                }

                //Try to get an event from the queue
                Protocol.Event currentEvent = null;
                lock (eventQueueLock) {
                    currentEvent = EventQueue.Dequeue();
                }

                //Send the event on to its target object
                if (currentEvent != null) {
                    GetComponent<RemoteEventRouter>().RouteEvent(currentEvent);
                }

                eventCount--;
            }
        }

        void OnApplicationQuit() {
            Controller.Close();
        }

        void IEventQueue.QueueEvent(Protocol.Event e) {
            lock (eventQueueLock) {
                EventQueue.Enqueue(e);
            }
        }
    }

    public interface IEventQueue {
        void QueueEvent(Protocol.Event e);
    }
}