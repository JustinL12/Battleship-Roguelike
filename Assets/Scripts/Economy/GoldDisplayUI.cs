using TMPro;
using UnityEngine;
using UnityEngine.UI;
using BattleshipRoguelike.Inventory;
using BattleshipRoguelike.Map;

namespace BattleshipRoguelike.Economy
{
    public class GoldDisplayUI : MonoBehaviour
    {
        [SerializeField] private Canvas canvas;
        [SerializeField] private Camera worldCamera;
        [SerializeField] private Sprite iconSprite;
        [SerializeField] private Vector2 iconSize = new Vector2(24f, 24f);
        [SerializeField] private float screenMargin = 5f;
        [SerializeField] private float spacing = 4f;

        private RectTransform rootRect;
        private TMP_Text goldLabel;

        private void Awake()
        {
            if (canvas == null)
            {
                canvas = FindAnyObjectByType<Canvas>();
            }

            if (worldCamera == null)
            {
                worldCamera = Camera.main;
            }

            BuildUI();
        }

        private void LateUpdate()
        {
            PositionRoot();

            if (RunManager.Instance != null)
            {
                goldLabel.text = RunManager.Instance.Gold.ToString();
            }
        }

        private void PositionRoot()
        {
            float screenX = screenMargin;
            if (ShipInventory.Instance != null && worldCamera != null)
            {
                Vector3 screenPoint = worldCamera.WorldToScreenPoint(
                    new Vector3(ShipInventory.Instance.StripMaxX, 0f, -worldCamera.transform.position.z));
                screenX = screenPoint.x + screenMargin;
            }

            rootRect.anchoredPosition = new Vector2(screenX, -screenMargin);
        }

        private void BuildUI()
        {
            GameObject rootObject = new GameObject("GoldDisplay", typeof(RectTransform));
            rootObject.transform.SetParent(canvas.transform, false);
            rootRect = (RectTransform)rootObject.transform;
            rootRect.anchorMin = new Vector2(0f, 1f);
            rootRect.anchorMax = new Vector2(0f, 1f);
            rootRect.pivot = new Vector2(0f, 1f);
            rootRect.sizeDelta = new Vector2(iconSize.x + spacing + 60f, iconSize.y);

            GameObject iconObject = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            iconObject.transform.SetParent(rootRect, false);
            RectTransform iconRect = (RectTransform)iconObject.transform;
            iconRect.anchorMin = new Vector2(0f, 0.5f);
            iconRect.anchorMax = new Vector2(0f, 0.5f);
            iconRect.pivot = new Vector2(0f, 0.5f);
            iconRect.sizeDelta = iconSize;
            iconRect.anchoredPosition = Vector2.zero;

            Image iconImage = iconObject.GetComponent<Image>();
            iconImage.sprite = iconSprite;
            iconImage.preserveAspect = true;

            GameObject labelObject = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            labelObject.transform.SetParent(rootRect, false);
            RectTransform labelRect = (RectTransform)labelObject.transform;
            labelRect.anchorMin = new Vector2(0f, 0.5f);
            labelRect.anchorMax = new Vector2(0f, 0.5f);
            labelRect.pivot = new Vector2(0f, 0.5f);
            labelRect.sizeDelta = new Vector2(60f, iconSize.y);
            labelRect.anchoredPosition = new Vector2(iconSize.x + spacing, 0f);

            goldLabel = labelObject.GetComponent<TextMeshProUGUI>();
            goldLabel.text = "0";
            goldLabel.alignment = TextAlignmentOptions.MidlineLeft;
            goldLabel.fontSize = 20f;
            goldLabel.color = Color.white;
        }
    }
}
