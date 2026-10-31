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

        private CatPack _cats;
        private TownArtConfig _art;
        private SpriteRenderer[] _renderers;

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

            for (int index = 0; index < _cats.Count; index++)
            {
                _renderers[index] = AddCat();
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
