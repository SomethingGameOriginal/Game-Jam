using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target;
    public Vector3 often = new Vector3(0, 0, -10);
    public float smoothTime;
    Vector3 velocity;
    void Start()
    {
        
    }
    void LateUpdate()
    {
        Vector3 desired = target.position + often;
        transform.position = Vector3.SmoothDamp(transform.position, desired, ref velocity, smoothTime);
    }
}
