using UnityEngine;

namespace BattleshipRoguelike.Inventory
{
    public class InventorySlotDivider : MonoBehaviour
    {
        private SpriteRenderer spriteRenderer;
        private Color idleColor;
        private Color highlightColor;

        public void Initialize(Color idle, Color highlight, int sortingOrder)
        {
            idleColor = idle;
            highlightColor = highlight;

            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
            Texture2D texture = Texture2D.whiteTexture;
            spriteRenderer.sprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, texture.width, texture.height),
                new Vector2(0.5f, 0.5f),
                texture.width);
            spriteRenderer.material = new Material(Shader.Find("Sprites/Default"));
            spriteRenderer.sortingOrder = sortingOrder;
            spriteRenderer.color = idleColor;
        }

        public void SetHighlighted(bool highlighted)
        {
            if (spriteRenderer != null)
            {
                spriteRenderer.color = highlighted ? highlightColor : idleColor;
            }
        }

        public void SetTransform(Vector3 worldPosition, float width, float thickness)
        {
            transform.position = worldPosition;
            transform.localScale = new Vector3(width, thickness, 1f);
        }
    }
}
