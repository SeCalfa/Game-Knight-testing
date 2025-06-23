using UnityEngine;

namespace Game.Code.Character
{
    public class FollowCamera : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private Vector3 cameraOffset;
        [SerializeField] private float cameraSmooth;
        
        private void LateUpdate()
        {
            FollowTarget();
        }

        private void FollowTarget()
        {
            transform.position = Vector3.Lerp(
                transform.position,
                target.position + cameraOffset,
                Time.deltaTime * 1 / cameraSmooth);
        }
    }
}