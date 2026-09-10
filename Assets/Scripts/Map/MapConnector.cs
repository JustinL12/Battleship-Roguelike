using UnityEngine;

namespace BattleshipRoguelike.Map
{
    public class MapConnector : MonoBehaviour
    {
        [SerializeField] private float dashLength = 0.12f;
        [SerializeField] private float gapLength = 0.1f;
        [SerializeField] private float thickness = 0.04f;
        [SerializeField] private Color color = new Color(1f, 1f, 1f, 0.5f);
        [SerializeField] private int sortingOrder = 5;

        public void Draw(Vector2 from, Vector2 to)
        {
            Vector2 delta = to - from;
            float distance = delta.magnitude;
            if (distance <= 0f)
            {
                return;
            }

            Vector2 direction = delta / distance;
            float step = dashLength + gapLength;
            int dashCount = Mathf.Max(1, Mathf.FloorToInt(distance / step));

            for (int i = 0; i < dashCount; i++)
            {
                Vector2 dashCenter = from + direction * (i * step + dashLength * 0.5f);
                CreateDash(dashCenter, direction);
            }
        }

        private void CreateDash(Vector2 center, Vector2 direction)
        {
            GameObject dash = new GameObject("Dash");
            dash.transform.SetParent(transform, false);
            dash.transform.position = center;
            dash.transform.up = direction;
            dash.transform.localScale = new Vector3(thickness, dashLength, 1f);

            SpriteRenderer renderer = dash.AddComponent<SpriteRenderer>();
            Texture2D texture = Texture2D.whiteTexture;
            renderer.sprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, texture.width, texture.height),
                new Vector2(0.5f, 0.5f),
                texture.width);
            renderer.material = new Material(Shader.Find("Sprites/Default"));
            renderer.color = color;
            renderer.sortingOrder = sortingOrder;
        }
    }
}
