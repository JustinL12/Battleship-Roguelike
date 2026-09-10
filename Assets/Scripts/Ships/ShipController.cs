using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using BattleshipRoguelike.Grid;
using BattleshipRoguelike.Inventory;

namespace BattleshipRoguelike.Ships
{
    public class ShipController : MonoBehaviour
    {
        [SerializeField] private ShipSegment[] segments;
        [SerializeField] private Camera cam;

        private bool isHorizontal = true;
        private bool isDragging;
        private bool isPlaced;
        private bool wasPlacedBeforeDrag;
        private bool isInInventory;
        private int placedX;
        private int placedY;
        private GridManager placedBoard;
        private Vector3 originalPosition;
        private bool originalIsHorizontal;
        private bool interactable = true;
        private ShipController sourcePrefab;

        public int SegmentCount => segments.Length;
        public bool IsInInventory => isInInventory;
        public bool IsPlaced => isPlaced;
        public ShipController SourcePrefab => sourcePrefab;
        public Sprite Icon => segments.Length > 0 ? segments[0].Sprite : null;

        public Sprite[] SegmentSprites
        {
            get
            {
                var sprites = new Sprite[segments.Length];
                for (int i = 0; i < segments.Length; i++)
                {
                    sprites[i] = segments[i].Sprite;
                }

                return sprites;
            }
        }

        public bool IsSunk
        {
            get
            {
                foreach (ShipSegment segment in segments)
                {
                    if (!segment.hit)
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        private void Awake()
        {
            if (cam == null)
            {
                cam = Camera.main;
            }
        }

        private void Update()
        {
            if (!interactable || Mouse.current == null)
            {
                return;
            }

            if (!isDragging)
            {
                if (Mouse.current.leftButton.wasPressedThisFrame)
                {
                    TryBeginDrag();
                }
                return;
            }

            Vector2 mouseWorld = GetMouseWorldPosition();
            transform.position = new Vector3(mouseWorld.x, mouseWorld.y, transform.position.z);

            if (ShipInventory.Instance != null)
            {
                bool overStrip = ShipInventory.Instance.IsPointOverStrip(mouseWorld);
                ShipInventory.Instance.SetHoverInsertIndex(overStrip ? ShipInventory.Instance.GetInsertIndexForWorldY(mouseWorld.y) : -1);
            }

            if (Mouse.current.rightButton.wasPressedThisFrame)
            {
                ToggleOrientation();
            }

            if (Mouse.current.leftButton.wasReleasedThisFrame)
            {
                isDragging = false;
                TryResolveDrop();
            }
        }

        private void TryBeginDrag()
        {
            Vector2 mouseWorld = GetMouseWorldPosition();
            Collider2D hitCollider = Physics2D.OverlapPoint(mouseWorld);
            if (hitCollider == null || hitCollider.GetComponentInParent<ShipController>() != this)
            {
                return;
            }

            isDragging = true;
            originalPosition = transform.position;
            originalIsHorizontal = isHorizontal;
            wasPlacedBeforeDrag = isPlaced;

            if (isPlaced && GridManager.Instance != null)
            {
                GridManager.Instance.ReleaseFootprint(placedX, placedY, isHorizontal, segments.Length);
                isPlaced = false;
            }

            if (isInInventory && ShipInventory.Instance != null)
            {
                ShipInventory.Instance.RemoveFromInventory(this);
                SetInventoryState(false);
            }
        }

        private void ToggleOrientation()
        {
            isHorizontal = !isHorizontal;
            ApplyOrientationOffsets();
        }

        private void ApplyOrientationOffsets()
        {
            float cellSize = GridManager.Instance != null ? GridManager.Instance.CellSize : 1f;

            for (int i = 0; i < segments.Length; i++)
            {
                float offset = (i - (segments.Length - 1) * 0.5f) * cellSize;
                segments[i].transform.localPosition = isHorizontal
                    ? new Vector3(offset, 0f, 0f)
                    : new Vector3(0f, offset, 0f);
            }
        }

        private bool TrySnap()
        {
            GridManager grid = GridManager.Instance;
            if (grid == null)
            {
                return false;
            }

            var cells = new List<Vector2Int>(segments.Length);
            for (int i = 0; i < segments.Length; i++)
            {
                Vector2 worldPos = transform.TransformPoint(segments[i].transform.localPosition);
                if (!grid.TryGetNearestCell(worldPos, out int cx, out int cy))
                {
                    return false;
                }

                cells.Add(new Vector2Int(cx, cy));
            }

            int gx = cells[0].x;
            int gy = cells[0].y;

            for (int i = 1; i < cells.Count; i++)
            {
                int expectedX = isHorizontal ? gx + i : gx;
                int expectedY = isHorizontal ? gy : gy + i;
                if (cells[i].x != expectedX || cells[i].y != expectedY)
                {
                    return false;
                }
            }

            if (!grid.IsFootprintFree(gx, gy, isHorizontal, segments.Length))
            {
                return false;
            }

            Vector2 snappedRoot = grid.CellToWorld(gx, gy) - (Vector2)segments[0].transform.localPosition;
            transform.position = new Vector3(snappedRoot.x, snappedRoot.y, transform.position.z);

            grid.TryOccupyFootprint(gx, gy, isHorizontal, segments);
            isPlaced = true;
            placedX = gx;
            placedY = gy;
            placedBoard = grid;
            return true;
        }

        public void PlaceAt(GridManager board, int gx, int gy, bool horizontal)
        {
            isHorizontal = horizontal;
            ApplyOrientationOffsets();

            Vector2 rootPos = board.CellToWorld(gx, gy) - (Vector2)segments[0].transform.localPosition;
            transform.position = new Vector3(rootPos.x, rootPos.y, transform.position.z);

            board.TryOccupyFootprint(gx, gy, horizontal, segments);
            isPlaced = true;
            placedX = gx;
            placedY = gy;
            placedBoard = board;
            wasPlacedBeforeDrag = true;
        }

        private void TryResolveDrop()
        {
            if (TrySnap())
            {
                ShipInventory.Instance?.SetHoverInsertIndex(-1);
                return;
            }

            Vector2 pivotWorld = transform.TransformPoint(segments[0].transform.localPosition);
            bool onBoard = GridManager.Instance != null && GridManager.Instance.TryGetNearestCell(pivotWorld, out _, out _);

            if (onBoard && wasPlacedBeforeDrag)
            {
                RevertToOriginal();
                ShipInventory.Instance?.SetHoverInsertIndex(-1);
                return;
            }

            if (ShipInventory.Instance != null)
            {
                Vector2 mouseWorld = GetMouseWorldPosition();
                bool overStrip = ShipInventory.Instance.IsPointOverStrip(mouseWorld);
                int index = overStrip ? ShipInventory.Instance.GetInsertIndexForWorldY(mouseWorld.y) : -1;
                ShipInventory.Instance.AddToInventory(this, index);
                ShipInventory.Instance.SetHoverInsertIndex(-1);
            }
            else
            {
                RevertToOriginal();
            }
        }

        private void RevertToOriginal()
        {
            transform.position = originalPosition;
            if (isHorizontal != originalIsHorizontal)
            {
                isHorizontal = originalIsHorizontal;
                ApplyOrientationOffsets();
            }

            if (wasPlacedBeforeDrag && GridManager.Instance != null)
            {
                GridManager.Instance.TryOccupyFootprint(placedX, placedY, isHorizontal, segments);
                isPlaced = true;
                placedBoard = GridManager.Instance;
            }
            else
            {
                isPlaced = false;
            }
        }

        private Vector2 GetMouseWorldPosition()
        {
            Vector2 screenPos = Mouse.current.position.ReadValue();
            Vector3 worldPos = cam.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, -cam.transform.position.z));
            return worldPos;
        }

        public void SetInteractable(bool value)
        {
            interactable = value;
        }

        public void SetSourcePrefab(ShipController prefab)
        {
            sourcePrefab = prefab;
        }

        public void SetVisible(bool visible)
        {
            foreach (ShipSegment segment in segments)
            {
                segment.SetVisible(visible);
            }
        }

        public void LockForRoundStart()
        {
            interactable = false;
            if (!isInInventory)
            {
                SetVisible(false);
            }
        }

        public void SetInventoryState(bool inInventory)
        {
            isInInventory = inInventory;
            float scale = inInventory && ShipInventory.Instance != null ? ShipInventory.Instance.IconScale : 1f;

            isHorizontal = false;
            ApplyOrientationOffsets();
            transform.localScale = Vector3.one * scale;
        }

        public Vector2Int[] TryGetOccupiedCells()
        {
            if (!isPlaced)
            {
                return null;
            }

            var result = new Vector2Int[segments.Length];
            for (int i = 0; i < segments.Length; i++)
            {
                result[i] = isHorizontal ? new Vector2Int(placedX + i, placedY) : new Vector2Int(placedX, placedY + i);
            }

            return result;
        }

        public void NotifyHitAndCheckSunk(ShipSegment segment, Color sunkColor)
        {
            segment.hit = true;

            if (!IsSunk)
            {
                return;
            }

            SetVisible(true);

            Vector2Int[] cells = TryGetOccupiedCells();
            if (cells == null)
            {
                return;
            }

            GridManager board = placedBoard != null ? placedBoard : GridManager.Instance;
            if (board == null)
            {
                return;
            }

            foreach (Vector2Int cell in cells)
            {
                board.GetCell(cell.x, cell.y)?.SetMarkColor(sunkColor);
            }
        }
    }
}
