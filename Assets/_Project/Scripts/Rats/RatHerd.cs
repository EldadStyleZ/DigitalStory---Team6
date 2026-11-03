using System.Collections.Generic;
using UnityEngine;

namespace PiperToy
{
    /// <summary>
    /// The parent of every rat. When the game starts it creates the rats from the Rat prefab, scattered over the
    /// field, and tells each one which flute to listen to. When a song plays, it finds the rats close enough to
    /// hear it and gives each one its part, such as its spot in the heart or its place in the conga line.
    /// </summary>
    public sealed class RatHerd : MonoBehaviour
    {
        [Header("Rats")]
        [SerializeField, Tooltip("The prefab every rat is made from. Change a rat's speed or colors there.")]
        private Rat _ratPrefab;

        [SerializeField, Min(0), Tooltip("How many rats to create when the game starts.")]
        private int _ratCount = 20;

        [SerializeField, Tooltip("The size of the area the rats start in, centered on this object, in units.")]
        private Vector2 _startArea = new Vector2(24f, 14f);

        [SerializeField, Tooltip("The flute the rats listen to.")]
        private Flute _flute;

        [Header("Songs")]
        [SerializeField, Min(0f), Tooltip("Floating and fainting rats stop up to this many seconds apart, so they do not all pop or wake at once.")]
        private float _staggerSeconds = 2f;

        [SerializeField, Min(0f), Tooltip("How much the heart grows for each rat in it, in units.")]
        private float _heartSizePerRat = 0.25f;

        [SerializeField, Min(0f), Tooltip("The smallest heart, in units.")]
        private float _smallestHeart = 2.2f;

        [SerializeField, Min(0f), Tooltip("The biggest heart, in units.")]
        private float _biggestHeart = 4.5f;

        private readonly List<Rat> _rats = new List<Rat>();
        private readonly List<Rat> _listeners = new List<Rat>();

        public IReadOnlyList<Rat> Rats => _rats;

        private void Awake()
        {
            for (int index = 0; index < _ratCount; index++)
            {
                Vector2 spot = (Vector2)transform.position + new Vector2(
                    Random.Range(-0.5f, 0.5f) * _startArea.x,
                    Random.Range(-0.5f, 0.5f) * _startArea.y);

                // Instantiate copies the prefab into the scene, as a child of this object so the Hierarchy stays tidy.
                Rat rat = Instantiate(_ratPrefab, spot, Quaternion.identity, transform);
                rat.name = $"Rat {index + 1}";
                rat.Listen(_flute);
                _rats.Add(rat);
            }
        }

        private void OnEnable()
        {
            _flute.SongPlayed += HandleSongPlayed;
        }

        private void OnDisable()
        {
            _flute.SongPlayed -= HandleSongPlayed;
        }

        private void HandleSongPlayed(Song song)
        {
            FindListeners(song.Radius);

            switch (song.Effect)
            {
                case SongEffect.Dance:
                case SongEffect.CrowdSurf:
                    foreach (Rat rat in _listeners)
                    {
                        rat.Charm(song.Seconds);
                    }

                    break;
                case SongEffect.Conga:
                    LineUp(song);
                    break;
                case SongEffect.Heart:
                    FormAHeart(song);
                    break;
                case SongEffect.Disco:
                    foreach (Rat rat in _listeners)
                    {
                        rat.StartDisco(song.Seconds, song.Color);
                    }

                    break;
                case SongEffect.Balloons:
                    foreach (Rat rat in _listeners)
                    {
                        rat.StartFloating(song.Seconds + Random.Range(0f, _staggerSeconds), song.Color);
                    }

                    break;
                case SongEffect.Choir:
                    foreach (Rat rat in _listeners)
                    {
                        rat.StartSinging(song.Seconds, song.Color);
                    }

                    break;
                case SongEffect.Faint:
                    foreach (Rat rat in _listeners)
                    {
                        rat.Faint(song.Seconds + Random.Range(0f, _staggerSeconds), song.Color);
                    }

                    break;
            }
        }

        // The rats close enough to hear the song, nearest first, so the conga line starts with the nearest.
        private void FindListeners(float radius)
        {
            Vector2 piper = _flute.transform.position;
            _listeners.Clear();

            foreach (Rat rat in _rats)
            {
                if (Vector2.Distance(rat.transform.position, piper) <= radius)
                {
                    _listeners.Add(rat);
                }
            }

            _listeners.Sort((first, second) => Vector2.Distance(first.transform.position, piper)
                .CompareTo(Vector2.Distance(second.transform.position, piper)));
        }

        // Each rat follows the one before it, and the first follows the Piper.
        private void LineUp(Song song)
        {
            Transform leader = _flute.transform;

            foreach (Rat rat in _listeners)
            {
                rat.JoinConga(leader, song.Seconds, song.Color);
                leader = rat.transform;
            }
        }

        private void FormAHeart(Song song)
        {
            float size = Mathf.Clamp(_listeners.Count * _heartSizePerRat, _smallestHeart, _biggestHeart);

            for (int index = 0; index < _listeners.Count; index++)
            {
                float angle = 2f * Mathf.PI * index / _listeners.Count;
                _listeners[index].JoinHeart(HeartPoint(angle) * size, song.Seconds, song.Color);
            }
        }

        // The classic heart curve. It measures about 2 across, so multiplying by the size gives the heart's width.
        private static Vector2 HeartPoint(float angle)
        {
            float sine = Mathf.Sin(angle);
            float x = 16f * sine * sine * sine;
            float y = (13f * Mathf.Cos(angle)) - (5f * Mathf.Cos(2f * angle)) - (2f * Mathf.Cos(3f * angle)) - Mathf.Cos(4f * angle);
            return new Vector2(x, y) / 16f;
        }
    }
}
