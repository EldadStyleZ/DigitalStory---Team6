using DigitalStory.Data;
using DigitalStory.Logic;
using UnityEngine;

namespace DigitalStory.View
{
    /// <summary>Shows the Piper where the rules put it.</summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("DigitalStory/Piper View")]
    public sealed class PiperView : MonoBehaviour
    {
        private Piper _piper;
        private SongStage _stage;
        private TownArtConfig _art;

        /// <summary>Supplies the Piper to show. Call once, before this object updates.</summary>
        public void Initialize(Piper piper, SongStage stage, TownArtConfig art)
        {
            _piper = piper;
            _stage = stage;
            _art = art;
        }

        private void LateUpdate()
        {
            if (_piper == null)
            {
                return;
            }

            transform.SetPositionAndRotation(
                TownSpace.ToScene(_piper.Position + (Vector2.down * Lift())),
                TownSpace.ToSceneRotation(_piper.Facing));
        }

        private float Lift()
        {
            return _stage.IsSurfing ? _art.SurfLift + (Mathf.Sin(Time.time * _art.SurfBobRate) * _art.SurfBob) : 0f;
        }
    }
}
