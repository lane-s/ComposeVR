using UnityEngine;

namespace ComposeVR {
    public static class Utility {

        /// <summary>
        /// 
        /// </summary>
        /// <param name="layer"></param>
        /// <param name="layerMask"></param>
        /// <returns> true if layer is in layerMask, false otherwise </returns>
        public static bool IsInLayerMask(int layer, LayerMask layerMask) {
            return layerMask == (layerMask | (1 << layer));
        }

        /// <summary>
        /// Project the vector U onto v
        /// </summary>
        /// <param name="U"></param>
        /// <param name="V"></param>
        /// <returns></returns>
        public static Vector3 ProjectVector(Vector3 U, Vector3 V) {
            return (Vector3.Dot(U, V) * V / V.sqrMagnitude);
        }

        public static Vector3 ProjectVectorOntoPlane(Vector3 vec, Vector3 planeOrigin, Vector3 planeNormal) {
            Vector3 originToPoint = vec - planeOrigin;
            float perpendicularDistance = Vector3.Dot(originToPoint, planeNormal.normalized);
            return vec - planeNormal.normalized * perpendicularDistance;
        }

        public static Vector3 ProjectPointOnSegment(Vector3 point, Vector3 start, Vector3 end) {
            Vector3 segmentAxis = (end - start);
            Vector3 projectionOnAxis = Vector3.Project(point - start, segmentAxis) + start;

            return ClampProjection(projectionOnAxis, start, end);
        }
        /// <summary>
        /// Clamp proj so that it does not go past start or end
        /// </summary>
        /// <param name="proj"></param>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <returns> The clamped vector </returns>
        public static Vector3 ClampProjection(Vector3 proj, Vector3 start, Vector3 end) {
            float startDistance = (proj - start).sqrMagnitude;
            float endDistance = (proj - end).sqrMagnitude;

            float totalDistance = (start - end).sqrMagnitude;

            if(startDistance >= totalDistance) {
                return end;
            }else if(endDistance >= totalDistance) {
                return start;
            }

            return proj;
        }

        public static float LinearToLog(float val, float min, float max) {
            float b = Mathf.Log10(max / min) / (max - min);
            float a = max / Mathf.Pow(10.0f, b * max);

            return a * Mathf.Pow(10.0f, b * val);
        }
    }
}