using UnityEngine;

namespace BattleshipRoguelike.Ships
{
    public abstract class ShipSegment : MonoBehaviour
    {
        public bool hit;

        private SpriteRenderer spriteRenderer;

        public Sprite Sprite => spriteRenderer.sprite;

        protected virtual void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        public void SetVisible(bool visible)
        {
            spriteRenderer.enabled = visible;
        }
    }
}
