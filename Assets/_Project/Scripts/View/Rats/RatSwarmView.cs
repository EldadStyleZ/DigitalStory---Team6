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
        private SongStage _stage;
        private TownArtConfig _art;
        private SpriteRenderer[] _renderers;
        private Transform[] _transforms;

        /// <summary>Supplies the rats to show. Call once, before this object starts.</summary>
        public void Initialize(RatSwarm swarm, SongStage stage, TownArtConfig art)
        {
            _swarm = swarm;
            _stage = stage;
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

            bool isCharmed = _swarm.IsCharmed(index);
            RatLook look = rat.Trance != null ? rat.Trance.Look : RatLook.Normal;
            RatLookStyle style = _art.StyleOf(look);
            Color normalColor = isCharmed ? _art.CharmedRatColor : _art.CalmRatColor;

            ratRenderer.color = look == RatLook.Normal ? normalColor : style.Tint;
            _transforms[index].localScale = Vector3.one * style.Scale;
            _transforms[index].position = TownSpace.ToScene(rat.Position + (Vector2.down * HopOf(in rat, isCharmed)));

            if (rat.Velocity != Vector2.zero)
            {
                _transforms[index].rotation = TownSpace.ToSceneRotation(Mathf.Atan2(rat.Velocity.y, rat.Velocity.x));
            }
        }

        private float HopOf(in Rat rat, bool isCharmed)
        {
            if (!_stage.IsSinging || !isCharmed || rat.IsOutOfReach)
            {
                return 0f;
            }

            return Mathf.Abs(Mathf.Sin((Time.time * _art.ChoirHopRate) + rat.Phase)) * _art.ChoirHopHeight;
        }
    }
}
