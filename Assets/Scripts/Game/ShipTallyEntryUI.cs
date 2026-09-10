using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BattleshipRoguelike.Game
{
    public class ShipTallyEntryUI : MonoBehaviour
    {
        private RectTransform segmentContainer;
        private TMP_Text countLabel;

        public void Initialize(RectTransform segmentContainer, TMP_Text count)
        {
            this.segmentContainer = segmentContainer;
            countLabel = count;
        }

        public void Bind(Sprite[] segmentSprites, Vector2 segmentSize, int count)
        {
            foreach (Sprite segmentSprite in segmentSprites)
            {
                GameObject segmentObject = new GameObject("Segment", typeof(RectTransform), typeof(Image));
                segmentObject.transform.SetParent(segmentContainer, false);

                Image segmentImage = segmentObject.GetComponent<Image>();
                segmentImage.sprite = segmentSprite;
                segmentImage.preserveAspect = true;

                LayoutElement layoutElement = segmentObject.AddComponent<LayoutElement>();
                layoutElement.preferredWidth = segmentSize.x;
                layoutElement.preferredHeight = segmentSize.y;
            }

            countLabel.gameObject.SetActive(count > 1);
            countLabel.text = $"x{count}";
        }
    }
}
