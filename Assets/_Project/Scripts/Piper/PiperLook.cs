using UnityEngine;

namespace PiperToy
{
    /// <summary>
    /// How the Piper looks: its body turns the way it walks, and during Crowd Surf the rats lift it up and it
    /// bobs on top of them. It only moves the child Body, so the Piper's physics stay where they are.
    /// </summary>
    public sealed class PiperLook : MonoBehaviour
    {
        [SerializeField, Tooltip("The child that holds the Piper's picture.")]
        private Transform _body;

        [SerializeField]
        private PiperMovement _movement;

        [SerializeField]
        private Flute _flute;

        [SerializeField, Min(0f), Tooltip("How high the rats lift the Piper, in units.")]
        private float _liftHeight = 0.45f;

        [SerializeField, Min(0f), Tooltip("How far the Piper bobs while lifted, in units.")]
        private float _bobHeight = 0.08f;

        [SerializeField, Min(0f), Tooltip("Bobs per second while lifted.")]
        private float _bobsPerSecond = 1.5f;

        private float _liftedUntil;

        public bool IsLifted => Time.time < _liftedUntil;

        // OnEnable and OnDisable pair up, so the Piper stops listening when it is switched off or destroyed.
        private void OnEnable()
        {
            _flute.SongPlayed += HandleSongPlayed;
        }

        private void OnDisable()
        {
            _flute.SongPlayed -= HandleSongPlayed;
        }

        private void Update()
        {
            float angle = Mathf.Atan2(_movement.Facing.y, _movement.Facing.x) * Mathf.Rad2Deg;
            _body.localRotation = Quaternion.Euler(0f, 0f, angle);

            float bob = Mathf.Sin(Time.time * _bobsPerSecond * 2f * Mathf.PI) * _bobHeight;
            _body.localPosition = IsLifted ? new Vector3(0f, _liftHeight + bob, 0f) : Vector3.zero;
        }

        private void HandleSongPlayed(Song song)
        {
            if (song.Effect == SongEffect.CrowdSurf)
            {
                _liftedUntil = Time.time + song.Seconds;
            }
        }
    }
}
