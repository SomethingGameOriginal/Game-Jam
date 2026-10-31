using UnityEngine;

namespace _Project.VideoGameFeelShooting.Develop.CameraFeature
{
    public class CameraFollow : MonoBehaviour
    {
        public Transform target;
        public Transform aimPoint;
        public Vector3 often = new Vector3(0, 0, -10);
        public float smoothTime;
        Vector3 velocity;
        public float lookAheadStrength;
        public float lookAheadMaxDistance;
        void Start()
        {

        }
        void LateUpdate()
        {
            Vector3 desired = target.position + often;

            Vector3 mousePosition = Input.mousePosition;
            mousePosition.z = -Camera.main.transform.position.z;

            Vector3 aimPoint = Camera.main.ScreenToWorldPoint(mousePosition);

            Vector3 aimOffset = aimPoint - target.position;
            aimOffset *= lookAheadStrength;
            aimOffset = Vector3.ClampMagnitude(aimOffset, lookAheadMaxDistance);

            desired += aimOffset;

            transform.position = Vector3.SmoothDamp(transform.position, desired, ref velocity, smoothTime);
        }
    }
}
