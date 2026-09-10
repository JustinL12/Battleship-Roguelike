using UnityEngine;
using TMPro;
using BattleshipRoguelike.Ships;

namespace BattleshipRoguelike.Grid
{
    public class GridCell : MonoBehaviour
    {
        public int x;
        public int y;
        public bool occupied;
        public bool fired;
        public ShipSegment occupant;

        private TextMeshPro markText;

        public void SetCoordinates(int gx, int gy)
        {
            x = gx;
            y = gy;
        }

        public void ShowMark(string symbol, Color color)
        {
            GameObject markObject = new GameObject("Mark");
            markObject.transform.SetParent(transform, false);
            markObject.transform.localPosition = Vector3.zero;

            markText = markObject.AddComponent<TextMeshPro>();
            markText.text = symbol;
            markText.color = color;
            markText.alignment = TextAlignmentOptions.Center;
            markText.fontSize = 3f;

            MeshRenderer meshRenderer = markObject.GetComponent<MeshRenderer>();
            if (meshRenderer != null)
            {
                meshRenderer.sortingOrder = 15;
            }
        }

        public void SetMarkColor(Color color)
        {
            if (markText != null)
            {
                markText.color = color;
            }
        }
    }
}
