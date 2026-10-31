using DigitalStory.Data;
using DigitalStory.Logic;
using UnityEngine;

namespace DigitalStory.View
{
    /// <summary>Shows every rat where the rules put it.</summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("DigitalStory/Rat Swarm View")]
    public sealed class RatSwarmView : MonoBehaviour
    {
        private const int RatOrder = 3;

        private RatSwarm _swarm;
        private TownArtConfig _art;
        private SpriteRenderer[] _renderers;
        private Transform[] _transforms;

        /// <summary>Supplies the rats to show. Call once, before this object starts.</summary>
        public void Initialize(RatSwarm swarm, TownArtConfig art)
        {
            _swarm = swarm;
            _art = art;
        }

        private void Start()
        {
            // GameRoot has already reported the problem if nothing was supplied.
            if (_swarm == null)
            {
                return;
            }

            _renderers = new SpriteRenderer[_swarm.Count];
            _transforms = new Transform[_swarm.Count];

            for (int index = 0; index < _swarm.Count; index++)
            {
                AddRat(index);
            }
        }

        private void LateUpdate()
        {
            if (_renderers == null)
            {
                return;
            }

            for (int index = 0; index < _renderers.Length; index++)
            {
                ShowRat(index);
            }
        }

        private void AddRat(int index)
        {
            var rat = new GameObject("Rat");
            rat.transform.SetParent(transform, false);

            var ratRenderer = rat.AddComponent<SpriteRenderer>();
            ratRenderer.sprite = _art.RatSprite;
            ratRenderer.sortingOrder = RatOrder;

            _renderers[index] = ratRenderer;
            _transforms[index] = rat.transform;
        }

        private void ShowRat(int index)
        {
            ref readonly Rat rat = ref _swarm[index];
            SpriteRenderer ratRenderer = _renderers[index];
            ratRenderer.enabled = rat.IsAlive;

            if (!rat.IsAlive)
            {
                return;
            }

            ratRenderer.color = _swarm.IsCharmed(index) ? _art.CharmedRatColor : _art.CalmRatColor;
            _transforms[index].position = TownSpace.ToScene(rat.Position);

            if (rat.Velocity != Vector2.zero)
            {
                _transforms[index].rotation = TownSpace.ToSceneRotation(Mathf.Atan2(rat.Velocity.y, rat.Velocity.x));
            }
        }
    }
}
