using DigitalStory.Data;
using DigitalStory.Logic;
using UnityEngine;
using UnityEngine.UIElements;

namespace DigitalStory.View
{
    /// <summary>The small map of the town, with the rats, cats, Piper and camera on it.</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(UIDocument))]
    [AddComponentMenu("DigitalStory/Minimap View")]
    public sealed class MinimapView : MonoBehaviour
    {
        private const int MapPixels = 300;
        private const float MapWidth = 150f;
        private const float EdgeMargin = 12f;
        private const float MapRadius = 6f;
        private const int RatDotSize = 2;
        private const int CatDotSize = 5;
        private const int PiperDotSize = 7;

        private NightSession _session;
        private Camera _camera;
        private TownConfig _town;
        private HudLayout _layout;
        private TownArtConfig _art;
        private HudConfig _hud;
        private MinimapPainter _painter;
        private Texture2D _texture;
        private Image _map;

        /// <summary>The map on screen, for tests and debugging.</summary>
        public VisualElement Element => _map;

        /// <summary>Supplies what to draw. Call once, before this object starts.</summary>
        public void Initialize(
            NightSession session,
            Camera sceneCamera,
            TownConfig town,
            HudLayout layout,
            TownArtConfig art,
            HudConfig hud)
        {
            _session = session;
            _camera = sceneCamera;
            _town = town;
            _layout = layout;
            _art = art;
            _hud = hud;
        }

        /// <summary>Redraws the map.</summary>
        public void Paint()
        {
            _painter.Clear();
            PaintRats();
            PaintCats();
            _painter.PaintDot(_session.Piper.Position, PiperDotSize, _hud.MinimapPiperColor);
            _painter.PaintFrame(CameraWindow(), _hud.MinimapWindowColor);
            _texture.SetPixels32(_painter.Pixels);
            _texture.Apply(false);
        }

        /// <summary>Returns the pixel drawn at a town position, for tests and debugging.</summary>
        public Color32 PixelAt(Vector2 townPosition) => _painter.PixelAt(townPosition);

        private void Start()
        {
            // GameRoot has already reported the problem if nothing was supplied.
            if (_session == null)
            {
                return;
            }

            _painter = new MinimapPainter(MapPixels, _town.Width, _town.Height);
            PaintBackground();
            BuildMap(_layout.Layer);
            Paint();
        }

        private void LateUpdate()
        {
            if (_painter != null && _layout.IsShowing)
            {
                Paint();
            }
        }

        private void OnDestroy()
        {
            // Textures made in code are not freed with the scene.
            if (_texture != null)
            {
                Destroy(_texture);
            }
        }

        private void PaintBackground()
        {
            _painter.PaintBackground(new Rect(0f, 0f, _town.Width, _town.Height), _art.GroundColor);

            for (float y = 0f; y < _town.Height; y += _painter.TownUnitsPerRow)
            {
                float edge = _session.Bank.EdgeAt(y);
                _painter.PaintBackgroundRow(y, edge, _art.ShallowsColor);
                _painter.PaintBackgroundRow(y, edge + _town.ShallowsWidth, _art.RiverColor);
            }

            foreach (House house in _session.Town.Houses)
            {
                _painter.PaintBackground(new Rect(house.X, house.Y, house.Width, house.Height), _art.RoofColor(house.RoofStyle));
            }
        }

        private void BuildMap(VisualElement parent)
        {
            _texture = new Texture2D(_painter.Width, _painter.Height, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
            };

            _map = new Image { image = _texture, scaleMode = ScaleMode.StretchToFill };
            _map.pickingMode = PickingMode.Ignore;
            _map.style.position = Position.Absolute;
            _map.style.top = EdgeMargin;
            _map.style.right = EdgeMargin;
            _map.style.width = MapWidth;
            _map.style.height = MapWidth * _painter.Height / _painter.Width;
            _map.style.borderTopLeftRadius = MapRadius;
            _map.style.borderTopRightRadius = MapRadius;
            _map.style.borderBottomLeftRadius = MapRadius;
            _map.style.borderBottomRightRadius = MapRadius;
            _map.style.overflow = Overflow.Hidden;
            parent.Add(_map);
        }

        private void PaintRats()
        {
            RatSwarm swarm = _session.Swarm;
            Color32 calm = _art.CalmRatColor;
            Color32 charmed = _art.CharmedRatColor;

            for (int index = 0; index < swarm.Count; index++)
            {
                ref readonly Rat rat = ref swarm[index];

                if (rat.IsAlive)
                {
                    _painter.PaintDot(rat.Position, RatDotSize, swarm.IsCharmed(index) ? charmed : calm);
                }
            }
        }

        private void PaintCats()
        {
            CatPack cats = _session.Cats;

            for (int index = 0; index < cats.Count; index++)
            {
                _painter.PaintDot(cats[index].Position, CatDotSize, _hud.MinimapCatColor);
            }
        }

        private Rect CameraWindow()
        {
            Vector2 centre = TownSpace.ToTown(_camera.transform.position);
            float height = _art.VisibleTownHeight;
            float width = height * _camera.aspect;
            return new Rect(centre.x - (width / 2f), centre.y - (height / 2f), width, height);
        }
    }
}
