using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using BattleshipRoguelike.Inventory;
using BattleshipRoguelike.Map;

namespace BattleshipRoguelike.Upgrades
{
    public class UpgradeInventoryPanel : MonoBehaviour
    {
        [SerializeField] private Canvas canvas;
        [SerializeField] private Camera worldCamera;
        [SerializeField] private int columns = 5;
        [SerializeField] private int visibleRows = 3;
        [SerializeField] private Vector2 cardSize = new Vector2(48f, 64f);
        [SerializeField] private Vector2 cardSpacing = new Vector2(4f, 4f);
        [SerializeField] private Vector2 buttonSize = new Vector2(160f, 50f);
        [SerializeField] private float screenMargin = 5f;

        private RectTransform panelRoot;
        private RectTransform content;
        private Button toggleButton;
        private readonly List<UpgradeCardUI> cards = new List<UpgradeCardUI>();

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

            BuildButton();
            BuildPanel();
            panelRoot.gameObject.SetActive(false);
        }

        private void LateUpdate()
        {
            PositionButton();
        }

        private void PositionButton()
        {
            float screenX = screenMargin;
            if (ShipInventory.Instance != null && worldCamera != null)
            {
                Vector3 screenPoint = worldCamera.WorldToScreenPoint(
                    new Vector3(ShipInventory.Instance.StripMaxX, 0f, -worldCamera.transform.position.z));
                screenX = screenPoint.x + screenMargin;
            }

            RectTransform buttonRect = (RectTransform)toggleButton.transform;
            buttonRect.anchoredPosition = new Vector2(screenX, screenMargin);

            panelRoot.anchoredPosition = new Vector2(screenX, screenMargin + buttonSize.y + screenMargin);
        }

        private void BuildButton()
        {
            GameObject buttonObject = new GameObject("UpgradesButton", typeof(RectTransform), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(canvas.transform, false);

            RectTransform rect = (RectTransform)buttonObject.transform;
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(0f, 0f);
            rect.pivot = new Vector2(0f, 0f);
            rect.sizeDelta = buttonSize;

            Image image = buttonObject.GetComponent<Image>();
            image.color = new Color(0.15f, 0.15f, 0.2f, 0.9f);

            toggleButton = buttonObject.GetComponent<Button>();
            toggleButton.onClick.AddListener(TogglePanel);

            GameObject labelObject = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            labelObject.transform.SetParent(buttonObject.transform, false);
            RectTransform labelRect = (RectTransform)labelObject.transform;
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;

            TextMeshProUGUI label = labelObject.GetComponent<TextMeshProUGUI>();
            label.text = "Upgrades";
            label.alignment = TextAlignmentOptions.Center;
            label.fontSize = 24f;
            label.color = Color.white;
        }

        private void BuildPanel()
        {
            float viewportWidth = columns * cardSize.x + (columns - 1) * cardSpacing.x;
            float viewportHeight = visibleRows * cardSize.y + (visibleRows - 1) * cardSpacing.y;

            GameObject panelObject = new GameObject("UpgradesPanel", typeof(RectTransform), typeof(Image), typeof(ScrollRect));
            panelObject.transform.SetParent(canvas.transform, false);
            panelRoot = (RectTransform)panelObject.transform;
            panelRoot.anchorMin = new Vector2(0f, 0f);
            panelRoot.anchorMax = new Vector2(0f, 0f);
            panelRoot.pivot = new Vector2(0f, 0f);
            panelRoot.sizeDelta = new Vector2(viewportWidth + 8f, viewportHeight + 8f);

            Image panelImage = panelObject.GetComponent<Image>();
            panelImage.color = new Color(0.05f, 0.05f, 0.08f, 0.9f);

            GameObject viewportObject = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
            viewportObject.transform.SetParent(panelRoot, false);
            RectTransform viewportRect = (RectTransform)viewportObject.transform;
            viewportRect.anchorMin = new Vector2(0f, 0f);
            viewportRect.anchorMax = new Vector2(1f, 1f);
            viewportRect.offsetMin = new Vector2(4f, 4f);
            viewportRect.offsetMax = new Vector2(-4f, -4f);

            Image viewportImage = viewportObject.GetComponent<Image>();
            viewportImage.color = Color.white;
            Mask mask = viewportObject.GetComponent<Mask>();
            mask.showMaskGraphic = false;

            GameObject contentObject = new GameObject("Content", typeof(RectTransform), typeof(GridLayoutGroup), typeof(ContentSizeFitter));
            contentObject.transform.SetParent(viewportRect, false);
            content = (RectTransform)contentObject.transform;
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(0f, 1f);
            content.pivot = new Vector2(0f, 1f);

            GridLayoutGroup grid = contentObject.GetComponent<GridLayoutGroup>();
            grid.cellSize = cardSize;
            grid.spacing = cardSpacing;
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = columns;

            ContentSizeFitter fitter = contentObject.GetComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

            ScrollRect scrollRect = panelObject.GetComponent<ScrollRect>();
            scrollRect.viewport = viewportRect;
            scrollRect.content = content;
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
        }

        private void TogglePanel()
        {
            bool willOpen = !panelRoot.gameObject.activeSelf;
            panelRoot.gameObject.SetActive(willOpen);
            if (willOpen)
            {
                RefreshCards();
            }
        }

        private void RefreshCards()
        {
            foreach (UpgradeCardUI card in cards)
            {
                Destroy(card.gameObject);
            }

            cards.Clear();

            if (RunManager.Instance == null)
            {
                return;
            }

            foreach (UpgradeStack stack in RunManager.Instance.Upgrades)
            {
                UpgradeCardUI card = CreateCard();
                card.Bind(stack);
                cards.Add(card);
            }
        }

        private UpgradeCardUI CreateCard()
        {
            GameObject cardObject = new GameObject("UpgradeCard", typeof(RectTransform), typeof(Image), typeof(UpgradeCardUI));
            cardObject.transform.SetParent(content, false);
            RectTransform cardRect = (RectTransform)cardObject.transform;
            cardRect.sizeDelta = cardSize;

            GameObject nameObject = new GameObject("Name", typeof(RectTransform), typeof(TextMeshProUGUI));
            nameObject.transform.SetParent(cardRect, false);
            RectTransform nameRect = (RectTransform)nameObject.transform;
            nameRect.anchorMin = Vector2.zero;
            nameRect.anchorMax = Vector2.one;
            nameRect.offsetMin = new Vector2(2f, 2f);
            nameRect.offsetMax = new Vector2(-2f, -2f);
            TextMeshProUGUI nameLabel = nameObject.GetComponent<TextMeshProUGUI>();
            nameLabel.alignment = TextAlignmentOptions.Center;
            nameLabel.fontSize = 10f;
            nameLabel.textWrappingMode = TextWrappingModes.Normal;
            nameLabel.color = Color.white;

            GameObject countObject = new GameObject("Count", typeof(RectTransform), typeof(TextMeshProUGUI));
            countObject.transform.SetParent(cardRect, false);
            RectTransform countRect = (RectTransform)countObject.transform;
            countRect.anchorMin = new Vector2(1f, 0f);
            countRect.anchorMax = new Vector2(1f, 0f);
            countRect.pivot = new Vector2(1f, 0f);
            countRect.sizeDelta = new Vector2(20f, 12f);
            countRect.anchoredPosition = new Vector2(-2f, 2f);
            TextMeshProUGUI countLabel = countObject.GetComponent<TextMeshProUGUI>();
            countLabel.alignment = TextAlignmentOptions.BottomRight;
            countLabel.fontSize = 9f;
            countLabel.color = Color.white;

            UpgradeCardUI card = cardObject.GetComponent<UpgradeCardUI>();
            card.Initialize(cardObject.GetComponent<Image>(), nameLabel, countLabel);
            return card;
        }
    }
}
