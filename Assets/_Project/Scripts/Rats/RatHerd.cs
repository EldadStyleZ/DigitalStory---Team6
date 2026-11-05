using System.Collections.Generic;
using UnityEngine;

namespace PiperToy
{
    /// <summary>
    /// The parent of every rat. When the game starts it creates the rats from the Rat prefab, scattered over the
    /// field, and tells each one which flute to listen to. When a song plays, it finds the rats close enough to
    /// hear it and gives each one its part, such as its spot in the heart or its place in the conga line.
    /// </summary>
    /// <remarks>
    /// The conga line and the heart are formations: their song's seconds only start counting once every rat in
    /// them has reached its place, or got as close as an obstacle lets it, so the rats dance the whole song together.
    /// </remarks>
    public sealed class RatHerd : MonoBehaviour
    {
        // How much room a rat needs beside the conga line's path, a little more than a rat's radius.
        private const float LineRoom = 0.3f;

        // How far the conga line may swing away from straight behind the Piper, and how big each try is, in degrees.
        private const float WidestSwing = 180f;
        private const float SwingStep = 30f;
        private static readonly float[] BothSides = { 1f, -1f };

        // How far past the crowd's outermost spot a rat may stand and still count as under the Piper: about a rat's
        // width, because the crowd's spots are packed closer together than the rats can stand.
        private const float CrowdSlack = 0.5f;

        [Header("Rats")]
        [SerializeField, Tooltip("The prefab every rat is made from. Change a rat's speed or colors there.")]
        private Rat _ratPrefab;

        [SerializeField, Min(0), Tooltip("How many rats to create when the game starts.")]
        private int _ratCount = 20;

        [SerializeField, Tooltip("The size of the area the rats start in, centered on this object, in units.")]
        private Vector2 _startArea = new Vector2(24f, 14f);

        [SerializeField, Min(0f), Tooltip("How much free room a rat needs around the spot it starts on, so none starts inside an obstacle.")]
        private float _startRoom = 0.5f;

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

        [SerializeField, Min(0f), Tooltip("How far apart the rats stand in the Crowd Surf crowd, in units.")]
        private float _crowdSpacing = 0.22f;

        [SerializeField, Min(0f), Tooltip("How far apart the rats walk in the conga line, in units.")]
        private float _congaGap = 0.7f;

        private readonly List<Rat> _rats = new List<Rat>();
        private readonly List<Rat> _listeners = new List<Rat>();
        private readonly List<Rat> _forming = new List<Rat>();
        private readonly List<Rat> _crowd = new List<Rat>();
        private readonly CongaLine _congaLine = new CongaLine();
        private PiperMovement _piper;
        private float _crowdRadius;

        public IReadOnlyList<Rat> Rats => _rats;

        private void Awake()
        {
            _piper = _flute.GetComponent<PiperMovement>();

            for (int index = 0; index < _ratCount; index++)
            {
                Vector2 spot = FreeSpot();

                // Instantiate copies the prefab into the scene, as a child of this object so the Hierarchy stays tidy.
                Rat rat = Instantiate(_ratPrefab, spot, Quaternion.identity, transform);
                rat.name = $"Rat {index + 1}";
                rat.Listen(_flute);
                _rats.Add(rat);
            }
        }

        /// <summary>
        /// A random spot in the start area with nothing on it, such as an obstacle or another rat. After many tries
        /// it settles for the last spot, and physics pushes the rat out of whatever it landed on.
        /// </summary>
        public Vector2 FreeSpot()
        {
            const int Tries = 30;
            Vector2 spot = transform.position;

            for (int attempt = 0; attempt < Tries; attempt++)
            {
                spot = (Vector2)transform.position + new Vector2(
                    Random.Range(-0.5f, 0.5f) * _startArea.x,
                    Random.Range(-0.5f, 0.5f) * _startArea.y);

                if (Physics2D.OverlapCircle(spot, _startRoom) == null)
                {
                    break;
                }
            }

            return spot;
        }

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
            if (_forming.Count > 0)
            {
                WatchTheFormation();
            }

            if (_crowd.Count > 0)
            {
                WatchTheCrowd();
            }
        }

        // Once the Crowd Surf crowd is under the Piper, the crowd carries the Piper faster, until the last rat of the
        // crowd lets go.
        private void WatchTheCrowd()
        {
            _crowd.RemoveAll(rat => rat.Mood != RatMood.Carry);
            _piper.IsCarried = _crowd.Count > 0 && (_piper.IsCarried || IsTheCrowdUnderThePiper());
        }

        // Every rat stands inside the crowd's circle round the Piper, or is as close as an obstacle lets it get.
        private bool IsTheCrowdUnderThePiper()
        {
            Vector2 piper = _flute.transform.position;

            foreach (Rat rat in _crowd)
            {
                bool isUnder = Vector2.Distance(rat.transform.position, piper) <= _crowdRadius;

                if (!isUnder && !rat.IsStuck)
                {
                    return false;
                }
            }

            return true;
        }

        // The conga line follows the Piper's footsteps, so they are recorded as the Piper moves, which is in physics.
        private void FixedUpdate()
        {
            _congaLine.Record(_flute.transform.position);
        }

        private void HandleSongPlayed(Song song)
        {
            FindListeners(song.Radius);

            switch (song.Effect)
            {
                case SongEffect.Dance:
                    foreach (Rat rat in _listeners)
                    {
                        rat.Charm(song.Seconds);
                    }

                    break;
                case SongEffect.CrowdSurf:
                    GatherACrowd(song);
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

        // The rats close enough to hear the song, nearest first, so the nearest gets the middle of the crowd.
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

        // The line starts straight behind the Piper, or as close to that as obstacles allow, and the rats nearest the
        // Piper's back take the first places, so few of them have to cross the line to reach theirs.
        private void LineUp(Song song)
        {
            // Rats still dancing an earlier conga join the new line, because the path now belongs to it.
            foreach (Rat rat in _rats)
            {
                if (rat.Mood == RatMood.Conga && !_listeners.Contains(rat))
                {
                    _listeners.Add(rat);
                }
            }

            Vector2 piper = _flute.transform.position;
            float length = _congaGap * (_listeners.Count + 1);
            Vector2 back = OpenWayBehind(piper, -_piper.Facing, length);
            _congaLine.Begin(piper, back, length);
            _listeners.Sort((first, second) => HowFarBack(first, piper, back).CompareTo(HowFarBack(second, piper, back)));

            StartAFormation();

            for (int place = 0; place < _listeners.Count; place++)
            {
                _listeners[place].JoinConga(_congaLine, _congaGap * (place + 1), song.Seconds, song.Color);
                _forming.Add(_listeners[place]);
            }
        }

        private static float HowFarBack(Rat rat, Vector2 piper, Vector2 back) =>
            Vector2.Dot((Vector2)rat.transform.position - piper, back);

        // Tries straight behind the Piper first, then swings out to either side, and takes the first way with room
        // for the whole line. If none has, it takes the way with the most room.
        private static Vector2 OpenWayBehind(Vector2 piper, Vector2 back, float length)
        {
            Vector2 best = back;
            float bestRoom = -1f;

            for (float swing = 0f; swing <= WidestSwing; swing += SwingStep)
            {
                foreach (float side in BothSides)
                {
                    Vector2 way = Quaternion.Euler(0f, 0f, swing * side) * back;
                    float room = RoomAlong(piper, way, length);

                    if (room >= length)
                    {
                        return way;
                    }

                    if (room > bestRoom)
                    {
                        best = way;
                        bestRoom = room;
                    }
                }
            }

            return best;
        }

        // How far a rat could walk that way before it meets something that never moves: an obstacle or the edge.
        private static float RoomAlong(Vector2 start, Vector2 way, float length)
        {
            foreach (RaycastHit2D hit in Physics2D.CircleCastAll(start, LineRoom, way, length))
            {
                if (hit.collider.attachedRigidbody == null)
                {
                    return hit.distance;
                }
            }

            return length;
        }

        // A new formation lets the rats of the last one start dancing, so none of them waits for ever for a
        // formation that has gone.
        private void StartAFormation()
        {
            StartTheClocks();
        }

        // Rats another song has taken over leave the formation. When every rat left is in its place, or as close as
        // it can get, the song's seconds start counting.
        private void WatchTheFormation()
        {
            _forming.RemoveAll(rat => !rat.IsWaitingForTheOthers);
            bool isEveryoneReady = _forming.TrueForAll(rat => rat.IsInPlace || rat.IsStuck);

            if (isEveryoneReady)
            {
                StartTheClocks();
            }
        }

        private void StartTheClocks()
        {
            foreach (Rat rat in _forming)
            {
                rat.StartTheClock();
            }

            _forming.Clear();
        }

        // Each rat gets a spot under the Piper, spiralling out from the middle, so the crowd is a tight round bunch.
        private void GatherACrowd(Song song)
        {
            const float GoldenAngle = 2.39996f;
            _crowd.Clear();
            _piper.IsCarried = false;
            _crowdRadius = (Mathf.Sqrt(Mathf.Max(0, _listeners.Count - 1)) * _crowdSpacing) + CrowdSlack;

            for (int index = 0; index < _listeners.Count; index++)
            {
                float angle = index * GoldenAngle;
                float distance = Mathf.Sqrt(index) * _crowdSpacing;
                var spot = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * distance;
                _listeners[index].JoinCrowd(spot, song.Seconds, song.Color);
                _crowd.Add(_listeners[index]);
            }
        }

        // Each spot round the heart, in turn, takes the nearest rat that has no spot yet, so the rats have short walks
        // and get in each other's way less.
        private void FormAHeart(Song song)
        {
            float size = Mathf.Clamp(_listeners.Count * _heartSizePerRat, _smallestHeart, _biggestHeart);
            Vector2 piper = _flute.transform.position;
            int count = _listeners.Count;

            StartAFormation();

            for (int index = 0; index < count; index++)
            {
                Vector2 spot = HeartPoint(2f * Mathf.PI * index / count) * size;
                Rat rat = NearestListener(piper + spot);
                _listeners.Remove(rat);
                rat.JoinHeart(spot, song.Seconds, song.Color);
                _forming.Add(rat);
            }
        }

        private Rat NearestListener(Vector2 spot)
        {
            Rat nearest = _listeners[0];

            foreach (Rat rat in _listeners)
            {
                if (Vector2.Distance(rat.transform.position, spot) < Vector2.Distance(nearest.transform.position, spot))
                {
                    nearest = rat;
                }
            }

            return nearest;
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
