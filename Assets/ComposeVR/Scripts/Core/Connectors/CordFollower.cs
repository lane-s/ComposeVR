using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ComposeVR {
    public class CordFollower : MonoBehaviour {
        public float Speed;

        private Cord cord;
        private int targetPointIndex;
        private int currentPointIndex;

        private const float SNAP_TO_TARGET_SPEED = 12f;

        private void Update() {

            if (currentPointIndex != targetPointIndex && cord != null) {
                int nextPointIndex = NextPointIndex();

                Vector3 toNextPoint = (cord.GetPointAtIndex(nextPointIndex) - cord.GetPointAtIndex(currentPointIndex)).normalized;

                Vector3 nextPos = transform.position + toNextPoint * Speed * Time.deltaTime;

                Vector3 nextDiff = cord.GetPointAtIndex(nextPointIndex) - nextPos;

                if (!nextPos.Equals(cord.GetPointAtIndex(nextPointIndex))) {

                    while (Vector3.Dot(toNextPoint, nextDiff) < 0) {
                        currentPointIndex = nextPointIndex;
                        nextPointIndex = NextPointIndex();

                        if (currentPointIndex == targetPointIndex) {
                            nextPos = cord.GetPointAtIndex(targetPointIndex);
                            break;
                        }
                        else {
                            Vector3 nextMove = (cord.GetPointAtIndex(nextPointIndex) - cord.GetPointAtIndex(currentPointIndex)).normalized;
                            nextPos = cord.GetPointAtIndex(currentPointIndex) + nextMove * nextDiff.magnitude;
                        }

                        nextDiff = cord.GetPointAtIndex(nextPointIndex) - nextPos;
                    }

                }

                transform.position = nextPos;
            }
            else {
                transform.position = Vector3.Lerp(transform.position, cord.GetPointAtIndex(targetPointIndex), Time.deltaTime * SNAP_TO_TARGET_SPEED);
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

        public void SetCord(Cord c) {
            cord = c;
        }

        public void SetTargetPoint(int targetPointIndex) {
            this.targetPointIndex = targetPointIndex;
        }

        public void TeleportToPoint(int pointIndex) {
            currentPointIndex = pointIndex;
            transform.position = cord.GetPointAtIndex(pointIndex);
        }
    }

}