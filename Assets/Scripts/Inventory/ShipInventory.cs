using System.Collections.Generic;
using UnityEngine;
using BattleshipRoguelike.Map;
using BattleshipRoguelike.Ships;

namespace BattleshipRoguelike.Inventory
{
    public class ShipInventory : MonoBehaviour
    {
        public static ShipInventory Instance { get; private set; }

        [SerializeField] private Camera cam;
        [SerializeField] private List<ShipController> startingShips = new List<ShipController>();
        [SerializeField] private float stripWidth = 12f / 32f;
        [SerializeField] private float iconScale = 8f / 32f;
        [SerializeField] private float topMargin = 12f / 32f;
        [SerializeField] private float slotSpacing = 2f / 32f;
        [SerializeField] private float dividerThickness = 1f / 32f;
        [SerializeField] private Color dividerIdleColor = new Color(1f, 1f, 1f, 0.35f);
        [SerializeField] private Color dividerHighlightColor = Color.green;
        [SerializeField] private Color backgroundColor = new Color(0.05f, 0.08f, 0.12f, 0.6f);
        [SerializeField] private float iconZ = -0.1f;
        [SerializeField] private int backgroundSortingOrder = 1;
        [SerializeField] private int dividerSortingOrder = 12;

        private readonly List<ShipController> ships = new List<ShipController>();
        private readonly List<InventorySlotDivider> dividers = new List<InventorySlotDivider>();
        private SpriteRenderer background;
        private int hoveredDividerIndex = -1;
        private float stripMinX;
        private float stripMaxX;

        public float IconScale => iconScale;
        public IReadOnlyList<ShipController> Ships => ships;
        public float StripMaxX => stripMaxX;

        private void Awake()
        {
            Instance = this;
            if (cam == null)
            {
                cam = Camera.main;
            }

            CreateBackground();
            SpawnStartingShips();
        }

        private void SpawnStartingShips()
        {
            if (RunManager.Instance != null)
            {
                // The scene-authored fallback ships are unused in this path but still exist as
                // active GameObjects, so they must be deactivated or GameManager's
                // FindObjectsByType<ShipController> scan would pick them up alongside the
                // freshly-spawned ones.
                foreach (ShipController ship in startingShips)
                {
                    ship.gameObject.SetActive(false);
                }

                foreach (ShipController prefab in RunManager.Instance.PlayerShipPrefabs)
                {
                    ShipController ship = Instantiate(prefab);
                    ship.SetSourcePrefab(prefab);
                    AddToInventory(ship, -1);
                }
            }
            else
            {
                foreach (ShipController ship in startingShips)
                {
                    AddToInventory(ship, -1);
                }
            }
        }

        private void LateUpdate()
        {
            RecomputeLayout();
        }

        private void CreateBackground()
        {
            GameObject backgroundObject = new GameObject("InventoryBackground");
            backgroundObject.transform.SetParent(transform, false);

            background = backgroundObject.AddComponent<SpriteRenderer>();
            Texture2D texture = Texture2D.whiteTexture;
            background.sprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, texture.width, texture.height),
                new Vector2(0.5f, 0.5f),
                texture.width);
            background.material = new Material(Shader.Find("Sprites/Default"));
            background.sortingOrder = backgroundSortingOrder;
            background.color = backgroundColor;
        }

        private void EnsureDividerCount(int count)
        {
            while (dividers.Count < count)
            {
                GameObject dividerObject = new GameObject($"Divider_{dividers.Count}");
                dividerObject.transform.SetParent(transform, false);
                InventorySlotDivider divider = dividerObject.AddComponent<InventorySlotDivider>();
                divider.Initialize(dividerIdleColor, dividerHighlightColor, dividerSortingOrder);
                dividers.Add(divider);
            }

            while (dividers.Count > count)
            {
                InventorySlotDivider last = dividers[dividers.Count - 1];
                dividers.RemoveAt(dividers.Count - 1);
                Destroy(last.gameObject);
            }
        }

        public void RecomputeLayout()
        {
            if (cam == null)
            {
                return;
            }

            float camDistance = Mathf.Abs(cam.transform.position.z);
            Vector3 topLeft = cam.ViewportToWorldPoint(new Vector3(0f, 1f, camDistance));
            Vector3 bottomLeft = cam.ViewportToWorldPoint(new Vector3(0f, 0f, camDistance));

            stripMinX = topLeft.x;
            stripMaxX = topLeft.x + stripWidth;
            float stripCenterX = topLeft.x + stripWidth * 0.5f;

            float viewHeight = topLeft.y - bottomLeft.y;
            background.transform.position = new Vector3(stripCenterX, (topLeft.y + bottomLeft.y) * 0.5f, iconZ);
            background.transform.localScale = new Vector3(stripWidth, viewHeight, 1f);

            EnsureDividerCount(ships.Count + 1);

            float y = topLeft.y - topMargin;
            for (int i = 0; i < ships.Count; i++)
            {
                dividers[i].SetTransform(new Vector3(stripCenterX, y, iconZ), stripWidth, dividerThickness);
                dividers[i].SetHighlighted(i == hoveredDividerIndex);

                y -= slotSpacing * 0.5f;
                float extent = ships[i].SegmentCount * iconScale;
                float shipCenterY = y - extent * 0.5f;
                ships[i].transform.position = new Vector3(stripCenterX, shipCenterY, iconZ);
                y -= extent;
                y -= slotSpacing * 0.5f;
            }

            dividers[ships.Count].SetTransform(new Vector3(stripCenterX, y, iconZ), stripWidth, dividerThickness);
            dividers[ships.Count].SetHighlighted(ships.Count == hoveredDividerIndex);
        }

        public void AddToInventory(ShipController ship, int index)
        {
            if (ships.Contains(ship))
            {
                return;
            }

            if (index < 0 || index > ships.Count)
            {
                index = ships.Count;
            }

            ships.Insert(index, ship);
            ship.SetInventoryState(true);
            RecomputeLayout();
        }

        public void RemoveFromInventory(ShipController ship)
        {
            ships.Remove(ship);
            RecomputeLayout();
        }

        public bool IsPointOverStrip(Vector2 worldPos)
        {
            return worldPos.x >= stripMinX && worldPos.x <= stripMaxX;
        }

        public int GetInsertIndexForWorldY(float worldY)
        {
            if (dividers.Count == 0)
            {
                return 0;
            }

            int nearest = 0;
            float bestDistance = float.MaxValue;
            for (int i = 0; i < dividers.Count; i++)
            {
                float distance = Mathf.Abs(dividers[i].transform.position.y - worldY);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    nearest = i;
                }
            }

            return nearest;
        }

        public void SetHoverInsertIndex(int index)
        {
            hoveredDividerIndex = index;
            for (int i = 0; i < dividers.Count; i++)
            {
                dividers[i].SetHighlighted(i == hoveredDividerIndex);
            }
        }
    }
}
