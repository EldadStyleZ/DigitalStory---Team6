using DigitalStory.Data;
using DigitalStory.Logic;
using UnityEngine;

namespace DigitalStory.View
{
    /// <summary>Shows every cat where the rules put it.</summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("DigitalStory/Cat View")]
    public sealed class CatView : MonoBehaviour
    {
        private const int CatOrder = 3;

        // Above the darkness, so a black cat still shows at night.
        private const int EyesOrder = 11;

        private CatPack _cats;
        private TownArtConfig _art;
        private SpriteRenderer[] _renderers;
        private SpriteRenderer[] _eyes;
        private SpriteRenderer[] _snoozes;

        /// <summary>Supplies the cats to show. Call once, before this object starts.</summary>
        public void Initialize(CatPack cats, TownArtConfig art)
        {
            _cats = cats;
            _art = art;
        }

        private void Start()
        {
            // GameRoot has already reported the problem if nothing was supplied.
            if (_cats == null)
            {
                return;
            }

            _renderers = new SpriteRenderer[_cats.Count];
            _eyes = new SpriteRenderer[_cats.Count];
            _snoozes = new SpriteRenderer[_cats.Count];

            for (int index = 0; index < _cats.Count; index++)
            {
                _renderers[index] = AddCat();
                _eyes[index] = AddEyes(_renderers[index].transform);
                _snoozes[index] = AddSnooze(_renderers[index].transform);
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
                ShowCat(in _cats[index], _renderers[index]);
                ShowEyes(in _cats[index], _eyes[index]);
                ShowSnooze(in _cats[index], _snoozes[index]);
            }
        }

        private SpriteRenderer AddCat()
        {
            var cat = new GameObject("Cat");
            cat.transform.SetParent(transform, false);

            var catRenderer = cat.AddComponent<SpriteRenderer>();
            catRenderer.sprite = _art.CatSprite;
            catRenderer.sortingOrder = CatOrder;
            return catRenderer;
        }

        private SpriteRenderer AddEyes(Transform cat)
        {
            var eyes = new GameObject("Eyes");
            eyes.transform.SetParent(cat, false);

            var eyesRenderer = eyes.AddComponent<SpriteRenderer>();
            eyesRenderer.sprite = _art.CatEyesSprite;
            eyesRenderer.sortingOrder = EyesOrder;
            return eyesRenderer;
        }

        private SpriteRenderer AddSnooze(Transform cat)
        {
            var snooze = new GameObject("Snooze");
            snooze.transform.SetParent(cat, false);
            snooze.transform.localScale = Vector3.one * TownSpace.ToSceneLength(_art.SnoozeSize);

            var snoozeRenderer = snooze.AddComponent<SpriteRenderer>();
            snoozeRenderer.sprite = _art.SnoozeSprite;
            snoozeRenderer.color = _art.SnoozeColor;
            snoozeRenderer.sortingOrder = EyesOrder;
            snoozeRenderer.enabled = false;
            return snoozeRenderer;
        }

        private void ShowSnooze(in Cat cat, SpriteRenderer snooze)
        {
            snooze.enabled = cat.IsAsleep;

            if (cat.IsAsleep)
            {
                float rise = Mathf.Repeat(Time.time * _art.SnoozeRiseSpeed, _art.SnoozeRise);
                Vector2 above = Vector2.down * (_art.SnoozeHeight + rise);
                snooze.transform.SetPositionAndRotation(TownSpace.ToScene(cat.Position + above), Quaternion.identity);
            }
        }

        private void ShowEyes(in Cat cat, SpriteRenderer eyes)
        {
            eyes.enabled = !cat.IsAsleep;
            eyes.color = cat.FleeLeft > 0f ? _art.FleeingCatEyeColor : _art.CatEyeColor;
        }

        private void ShowCat(in Cat cat, SpriteRenderer catRenderer)
        {
            catRenderer.color = cat.IsAsleep ? _art.SleepingCatColor : _art.CatColor;
            catRenderer.transform.position = TownSpace.ToScene(cat.Position);

            if (cat.Velocity != Vector2.zero)
            {
                catRenderer.transform.rotation = TownSpace.ToSceneRotation(Mathf.Atan2(cat.Velocity.y, cat.Velocity.x));
            }
        }
    }
}
