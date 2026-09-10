using UnityEngine;

namespace BattleshipRoguelike.Map
{
    public class ZoneNode : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Color idleColor = Color.white;
        [SerializeField] private Color battleIdleColor = new Color(0.65f, 0.1f, 0.2f, 1f);
        [SerializeField] private Color bossIdleColor = new Color(0.85f, 0.65f, 0.05f, 1f);
        [SerializeField] private Color selectedColor = Color.yellow;
        [SerializeField] private float dimmedSaturationFactor = 0.45f;
        [SerializeField] private float dimmedValueFactor = 0.55f;
        [SerializeField] private Color currentMarkerColor = Color.green;
        [SerializeField] private float currentMarkerScale = 0.35f;

        private static Sprite sharedDotSprite;

        private SpriteRenderer currentMarkerRenderer;

        public ZoneNodeId Id { get; private set; }
        public ZoneType Type { get; private set; }
        public bool IsSelected { get; private set; }
        public bool IsDimmed { get; private set; }

        private void Awake()
        {
            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponent<SpriteRenderer>();
            }

            CreateCurrentMarker();
        }

        public void Initialize(ZoneNodeId id, ZoneType type)
        {
            Id = id;
            Type = type;
            IsDimmed = false;
            SetSelected(false);
            SetCurrent(false);
        }

        public void SetSelected(bool selected)
        {
            IsSelected = selected;
            RefreshColor();
        }

        public void SetDimmed(bool dimmed)
        {
            IsDimmed = dimmed;
            RefreshColor();
        }

        public void SetCurrent(bool current)
        {
            if (currentMarkerRenderer != null)
            {
                currentMarkerRenderer.gameObject.SetActive(current);
            }
        }

        private void RefreshColor()
        {
            if (spriteRenderer == null)
            {
                return;
            }

            Color baseColor = Type switch
            {
                ZoneType.Battle => battleIdleColor,
                ZoneType.Boss => bossIdleColor,
                _ => idleColor
            };

            if (IsDimmed)
            {
                spriteRenderer.color = DimmedVariant(baseColor);
                return;
            }

            spriteRenderer.color = IsSelected ? selectedColor : baseColor;
        }

        private Color DimmedVariant(Color baseColor)
        {
            Color.RGBToHSV(baseColor, out float h, out float s, out float v);
            Color dimmed = Color.HSVToRGB(h, s * dimmedSaturationFactor, v * dimmedValueFactor);
            dimmed.a = baseColor.a;
            return dimmed;
        }

        private void CreateCurrentMarker()
        {
            GameObject markerObject = new GameObject("CurrentMarker");
            markerObject.transform.SetParent(transform, false);
            markerObject.transform.localPosition = new Vector3(0f, 0f, -0.05f);
            markerObject.transform.localScale = Vector3.one * currentMarkerScale;

            currentMarkerRenderer = markerObject.AddComponent<SpriteRenderer>();
            currentMarkerRenderer.sprite = GetDotSprite();
            currentMarkerRenderer.color = currentMarkerColor;
            currentMarkerRenderer.sortingOrder = (spriteRenderer != null ? spriteRenderer.sortingOrder : 0) + 1;
            markerObject.SetActive(false);
        }

        private static Sprite GetDotSprite()
        {
            if (sharedDotSprite != null)
            {
                return sharedDotSprite;
            }

            const int size = 32;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
            float radius = size * 0.5f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), center);
                    texture.SetPixel(x, y, dist <= radius ? Color.white : new Color(1f, 1f, 1f, 0f));
                }
            }

            texture.Apply();
            sharedDotSprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
            return sharedDotSprite;
        }
    }
}
