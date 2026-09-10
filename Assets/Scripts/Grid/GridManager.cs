using System.Collections.Generic;
using UnityEngine;
using BattleshipRoguelike.Ships;

namespace BattleshipRoguelike.Grid
{
    public class GridManager : MonoBehaviour
    {
        public static GridManager Instance { get; private set; }

        [SerializeField] private bool isPlayerBoard = true;
        [SerializeField] private GridCell cellPrefab;
        [SerializeField] private int width = 5;
        [SerializeField] private int height = 5;
        [SerializeField] private float cellSize = 1f;
        [SerializeField] private Vector2 origin = new Vector2(0.5f, 0.5f);
        [SerializeField] private Color panelColor = new Color(0.5f, 0.5f, 0.5f, 0.25f);
        [SerializeField] private int panelSortingOrder = -5;

        private GridCell[,] cells;

        public int Width => width;
        public int Height => height;
        public float CellSize => cellSize;
        public Vector2 Origin => origin;

        private void Awake()
        {
            if (isPlayerBoard)
            {
                Instance = this;
            }

            BuildGrid();
            BuildLabels();
            CreatePanel();
        }

        private void BuildGrid()
        {
            cells = new GridCell[width, height];

            if (cellPrefab == null)
            {
                Debug.LogError("GridManager: cellPrefab is not assigned.");
                return;
            }

            for (int gx = 0; gx < width; gx++)
            {
                for (int gy = 0; gy < height; gy++)
                {
                    GridCell cell = Instantiate(cellPrefab, transform);
                    cell.name = $"Cell_{gx}_{gy}";
                    cell.transform.localPosition = origin + new Vector2(gx * cellSize, gy * cellSize);
                    cell.SetCoordinates(gx, gy);
                    cells[gx, gy] = cell;
                }
            }
        }

        private void BuildLabels()
        {
            for (int gx = 0; gx < width; gx++)
            {
                CreateLabel($"Label_Col_{gx + 1}", (gx + 1).ToString(), origin + new Vector2(gx * cellSize, height * cellSize));
            }

            for (int gy = 0; gy < height; gy++)
            {
                char letter = (char)('A' + (height - 1 - gy));
                CreateLabel($"Label_Row_{letter}", letter.ToString(), origin + new Vector2(-1 * cellSize, gy * cellSize));
            }
        }

        private void CreateLabel(string labelName, string text, Vector2 localPosition)
        {
            GameObject labelObject = new GameObject(labelName);
            labelObject.transform.SetParent(transform, false);
            labelObject.transform.localPosition = localPosition;

            TextMesh textMesh = labelObject.AddComponent<TextMesh>();
            textMesh.text = text;
            textMesh.anchor = TextAnchor.MiddleCenter;
            textMesh.alignment = TextAlignment.Center;
            textMesh.characterSize = 0.15f;
            textMesh.fontSize = 48;
            textMesh.color = Color.white;

            MeshRenderer meshRenderer = labelObject.GetComponent<MeshRenderer>();
            if (meshRenderer != null)
            {
                meshRenderer.sortingOrder = 20;
            }
        }

        private void CreatePanel()
        {
            float gridLeft = origin.x - cellSize * 0.5f;
            float gridRight = origin.x + (width - 1) * cellSize + cellSize * 0.5f;
            float gridBottom = origin.y - cellSize * 0.5f;
            float gridTop = origin.y + (height - 1) * cellSize + cellSize * 0.5f;

            float panelLeft = gridLeft - cellSize;
            float panelRight = gridRight;
            float panelBottom = gridBottom;
            float panelTop = gridTop + cellSize;

            GameObject panelObject = new GameObject("BoardPanel");
            panelObject.transform.SetParent(transform, false);
            panelObject.transform.localPosition = new Vector3((panelLeft + panelRight) * 0.5f, (panelBottom + panelTop) * 0.5f, 0f);
            panelObject.transform.localScale = new Vector3(panelRight - panelLeft, panelTop - panelBottom, 1f);

            SpriteRenderer panelRenderer = panelObject.AddComponent<SpriteRenderer>();
            Texture2D texture = Texture2D.whiteTexture;
            panelRenderer.sprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, texture.width, texture.height),
                new Vector2(0.5f, 0.5f),
                texture.width);
            panelRenderer.material = new Material(Shader.Find("Sprites/Default"));
            panelRenderer.sortingOrder = panelSortingOrder;
            panelRenderer.color = panelColor;
        }

        public Vector2 CellToWorld(int gx, int gy)
        {
            return (Vector2)transform.position + origin + new Vector2(gx * cellSize, gy * cellSize);
        }

        public bool InBounds(int gx, int gy)
        {
            return gx >= 0 && gx < width && gy >= 0 && gy < height;
        }

        public GridCell GetCell(int gx, int gy)
        {
            return InBounds(gx, gy) ? cells[gx, gy] : null;
        }

        public bool TryGetNearestCell(Vector2 worldPos, out int gx, out int gy)
        {
            Vector2 localPos = worldPos - (Vector2)transform.position;
            gx = Mathf.RoundToInt((localPos.x - origin.x) / cellSize);
            gy = Mathf.RoundToInt((localPos.y - origin.y) / cellSize);
            return InBounds(gx, gy);
        }

        private static void GetFootprintCell(int gx, int gy, bool horizontal, int i, out int cx, out int cy)
        {
            cx = horizontal ? gx + i : gx;
            cy = horizontal ? gy : gy + i;
        }

        public bool IsFootprintFree(int gx, int gy, bool horizontal, int length)
        {
            for (int i = 0; i < length; i++)
            {
                GetFootprintCell(gx, gy, horizontal, i, out int cx, out int cy);
                if (!InBounds(cx, cy) || cells[cx, cy].occupied)
                {
                    return false;
                }
            }

            return true;
        }

        public bool TryOccupyFootprint(int gx, int gy, bool horizontal, IReadOnlyList<ShipSegment> segmentsInOrder)
        {
            if (!IsFootprintFree(gx, gy, horizontal, segmentsInOrder.Count))
            {
                return false;
            }

            for (int i = 0; i < segmentsInOrder.Count; i++)
            {
                GetFootprintCell(gx, gy, horizontal, i, out int cx, out int cy);
                cells[cx, cy].occupied = true;
                cells[cx, cy].occupant = segmentsInOrder[i];
            }

            return true;
        }

        public void ReleaseFootprint(int gx, int gy, bool horizontal, int length)
        {
            for (int i = 0; i < length; i++)
            {
                GetFootprintCell(gx, gy, horizontal, i, out int cx, out int cy);
                if (InBounds(cx, cy))
                {
                    cells[cx, cy].occupied = false;
                    cells[cx, cy].occupant = null;
                }
            }
        }

        public bool TryGetRandomFreeFootprint(int length, out int gx, out int gy, out bool horizontal)
        {
            var candidates = new List<(int x, int y, bool h)>();
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    candidates.Add((x, y, true));
                    candidates.Add((x, y, false));
                }
            }

            for (int i = candidates.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (candidates[i], candidates[j]) = (candidates[j], candidates[i]);
            }

            foreach (var candidate in candidates)
            {
                if (IsFootprintFree(candidate.x, candidate.y, candidate.h, length))
                {
                    gx = candidate.x;
                    gy = candidate.y;
                    horizontal = candidate.h;
                    return true;
                }
            }

            gx = 0;
            gy = 0;
            horizontal = true;
            return false;
        }
    }
}
