using UnityEngine;

namespace PiperToy
{
    /// <summary>Keeps the camera over its target, catching up smoothly instead of jumping.</summary>
    public sealed class CameraFollow : MonoBehaviour
    {
        [SerializeField, Tooltip("What the camera follows: the Piper.")]
        private Transform _target;

        [SerializeField, Min(0f), Tooltip("Roughly how long the camera takes to catch up, in seconds.")]
        private float _smoothSeconds = 0.25f;

        private Vector3 _velocity;

        // LateUpdate runs after every Update, so the camera moves after the Piper has moved.
        private void LateUpdate()
        {
            var goal = new Vector3(_target.position.x, _target.position.y, transform.position.z);
            transform.position = Vector3.SmoothDamp(transform.position, goal, ref _velocity, _smoothSeconds);
        }
    }
}
