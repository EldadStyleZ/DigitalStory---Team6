using System;
using System.Collections.Generic;
using DigitalStory.Logic;
using UnityEngine;

namespace DigitalStory.View
{
    /// <summary>
    /// The trees, bushes, ruins and shacks around the arena's edge (plan section S). The scene builder makes the props and
    /// measures each one's footprint; when a match starts the composition root draws a map with those footprints and calls
    /// <see cref="Place"/> once, which moves each prop to its place and turns it, or hides it for the match.
    /// </summary>
    /// <remarks>
    /// The props are not static: a statically batched mesh would keep drawing where it was built, whatever its transform.
    /// </remarks>
    public sealed class SceneryView : MonoBehaviour
    {
        [SerializeField, Tooltip("Every prop of the scenery, in the order the map places them. The scene builder fills it.")]
        private Transform[] _props = new Transform[0];

        [SerializeField, Tooltip("Each prop's footprint, in the same order: metres on the ground from its pivot to the farthest corner of its box, so it holds whichever way the prop turns. The scene builder measures it.")]
        private float[] _footprints = new float[0];

        [SerializeField, Min(0f), Tooltip("Props at least this tall block the chase camera's view of the giant; the bushes below it are looked over.")]
        private float _blockerLeastHeight = 2f;

        private bool _isPlaced;

        /// <summary>Each prop's footprint, in the order <see cref="Place"/> takes their placements.</summary>
        public IReadOnlyList<float> Footprints => _footprints;

        /// <summary>How many props there are.</summary>
        public int PropCount => _props.Length;

        /// <summary>
        /// Moves each prop to its placement's spot on the ground, keeping its height, turns it by the placement's yaw about its
        /// own pivot, and hides each prop the map left out. Call once, when the match starts.
        /// </summary>
        public void Place(IReadOnlyList<SceneryPlacement> placements)
        {
            if (placements == null)
            {
                throw new ArgumentNullException(nameof(placements));
            }

            if (placements.Count != _props.Length)
            {
                throw new ArgumentException($"The map places {placements.Count} props, but the scenery has {_props.Length}.", nameof(placements));
            }

            if (_isPlaced)
            {
                throw new InvalidOperationException("SceneryView is already placed for this match.");
            }

            _isPlaced = true;
            for (int index = 0; index < _props.Length; index++)
            {
                Transform prop = _props[index];
                SceneryPlacement placement = placements[index];
                prop.gameObject.SetActive(placement.IsPlaced);
                if (!placement.IsPlaced)
                {
                    continue;
                }

                var spot = new Vector3(placement.Position.x, prop.position.y, placement.Position.z);
                prop.SetPositionAndRotation(spot, Quaternion.Euler(0f, placement.YawDegrees, 0f) * prop.rotation);
            }
        }

        /// <summary>Adds the box of every shown prop at least as tall as the blocker height, for the chase camera.</summary>
        public void AddBlockers(List<Bounds> blockers)
        {
            for (int index = 0; index < _props.Length; index++)
            {
                if (_props[index].gameObject.activeSelf)
                {
                    AddBlocker(blockers, _props[index]);
                }
            }
        }

        /// <summary>Adds the box of <paramref name="prop"/> when it is at least as tall as the blocker height.</summary>
        public void AddBlocker(List<Bounds> blockers, Transform prop)
        {
            if (MeshBounds.TryMeasure(prop, out Bounds bounds) && bounds.size.y >= _blockerLeastHeight)
            {
                blockers.Add(bounds);
            }
        }

        private void Awake()
        {
            if (_footprints.Length != _props.Length)
            {
                Debug.LogError($"SceneryView has {_props.Length} props but {_footprints.Length} footprints. Rebuild the scene.", this);
            }
        }
    }
}
