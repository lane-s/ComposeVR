using System;
using UnityEngine;

namespace ComposeVR {
    public class CordFollowerEventArgs : EventArgs{
        public Vector3 NextMoveVector;
        
        public CordFollowerEventArgs(Vector3 nextMoveVector) {
            this.NextMoveVector = nextMoveVector;
        }
    }

    public class CordFollower : MonoBehaviour {
        public event EventHandler<CordFollowerEventArgs> NextPointReached;
        public float Speed;

        private Cord cord;
        private int targetPointIndex;
        private int currentPointIndex;

        private const float SNAP_TO_TARGET_SPEED = 12f;

        private void Update() {

            if (currentPointIndex != targetPointIndex && cord != null) {
                int nextPointIndex = NextPointIndex();

                Vector3 toNextPoint = (cord.GetPathPointAtIndex(nextPointIndex) - cord.GetPathPointAtIndex(currentPointIndex)).normalized;

                Vector3 nextPos = transform.position + toNextPoint * Speed * Time.deltaTime;

                Vector3 nextDiff = cord.GetPathPointAtIndex(nextPointIndex) - nextPos;

                if (!nextPos.Equals(cord.GetPathPointAtIndex(nextPointIndex))) {

                    while (Vector3.Dot(toNextPoint, nextDiff) < 0) {
                        Vector3 nextMove = Vector3.zero;

                        currentPointIndex = nextPointIndex;
                        nextPointIndex = NextPointIndex();

                        if (currentPointIndex == targetPointIndex) {
                            nextPos = cord.GetPathPointAtIndex(targetPointIndex);
                            break;
                        }
                        else {
                            nextMove = (cord.GetPathPointAtIndex(nextPointIndex) - cord.GetPathPointAtIndex(currentPointIndex)).normalized;
                            nextPos = cord.GetPathPointAtIndex(currentPointIndex) + nextMove * nextDiff.magnitude;
                        }

                        nextDiff = cord.GetPathPointAtIndex(nextPointIndex) - nextPos;
                        OnNextPointReached(nextMove);
                    }

                }

                transform.position = nextPos;
            }
            else {
                transform.position = Vector3.Lerp(transform.position, cord.GetPathPointAtIndex(targetPointIndex), Time.deltaTime * SNAP_TO_TARGET_SPEED);
            }
        }

        private int NextPointIndex() {
            if(currentPointIndex < targetPointIndex) {
                return currentPointIndex + 1;
            }
            else if(currentPointIndex > targetPointIndex) {
                return currentPointIndex - 1;
            }
            else {
                return currentPointIndex;
            }
        }

        private void OnNextPointReached(Vector3 nextMove) {
            if(NextPointReached != null) {
                NextPointReached(this, new CordFollowerEventArgs(nextMove));
            }
        }

        public void SetCord(Cord c) {
            cord = c;
        }

        public void SetTargetPoint(int targetPointIndex) {
            this.targetPointIndex = targetPointIndex;
        }

        public void TeleportToPoint(int pointIndex) {
            currentPointIndex = pointIndex;
            transform.position = cord.GetPathPointAtIndex(pointIndex);
        }
    }

}