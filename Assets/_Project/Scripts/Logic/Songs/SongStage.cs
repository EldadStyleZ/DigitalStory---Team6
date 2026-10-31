using UnityEngine;

namespace DigitalStory.Logic
{
    /// <summary>What songs are doing to the whole night, rather than to single rats.</summary>
    public sealed class SongStage
    {
        private float _surfRadius;

        public float SurfLeft { get; private set; }
        public float ChoirLeft { get; private set; }
        public float DiscoLeft { get; private set; }
        public float CongaLeft { get; private set; }

        public bool IsSurfing => SurfLeft > 0f;
        public bool IsSinging => ChoirLeft > 0f;
        public bool IsDiscoing => DiscoLeft > 0f;
        public bool IsDancingConga => CongaLeft > 0f;

        /// <summary>How close to the Piper a rat stays fully charmed, or 0 when no song does this.</summary>
        public float CharmZoneRadius => IsSurfing ? _surfRadius : 0f;

        public void StartSurf(float seconds, float charmZoneRadius)
        {
            SurfLeft = seconds;
            _surfRadius = charmZoneRadius;
        }

        public void StartChoir(float seconds) => ChoirLeft = seconds;

        public void StartDisco(float seconds) => DiscoLeft = seconds;

        public void StartConga(float seconds) => CongaLeft = seconds;

        /// <summary>Ends every song at once.</summary>
        public void Stop()
        {
            SurfLeft = 0f;
            ChoirLeft = 0f;
            DiscoLeft = 0f;
            CongaLeft = 0f;
        }

        public void Tick(float deltaTime)
        {
            SurfLeft = Mathf.Max(0f, SurfLeft - deltaTime);
            ChoirLeft = Mathf.Max(0f, ChoirLeft - deltaTime);
            DiscoLeft = Mathf.Max(0f, DiscoLeft - deltaTime);
            CongaLeft = Mathf.Max(0f, CongaLeft - deltaTime);
        }
    }
}
