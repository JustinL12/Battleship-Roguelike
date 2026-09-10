using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using BattleshipRoguelike.Map;
using BattleshipRoguelike.Ships;

namespace BattleshipRoguelike.Game
{
    public class BattleSummaryPanel : MonoBehaviour
    {
        [SerializeField] private Canvas canvas;
        [SerializeField] private Vector2 panelSize = new Vector2(420f, 520f);
        [SerializeField] private Vector2 shipSegmentIconSize = new Vector2(8f, 8f);
        [SerializeField] private Vector2 continueButtonSize = new Vector2(160f, 50f);
        [SerializeField] private float sectionSpacing = 12f;
        [SerializeField] private float panelPadding = 16f;

        private GameObject backdropRoot;
        private RectTransform contentRect;
        private TMP_Text titleLabel;
        private TMP_Text profitTotalLabel;
        private RectTransform profitBreakdownContainer;
        private RectTransform enemyKillsContainer;
        private RectTransform playerLossesContainer;
        private TMP_Text shotsFiredLabel;
        private Button continueButton;
        private Action onContinue;

        private void Awake()
        {
            if (canvas == null)
            {
                canvas = FindAnyObjectByType<Canvas>();
            }

            BuildUI();
            backdropRoot.SetActive(false);
        }

        public void Show(bool won, List<ProfitLineItem> profitBreakdown, int totalProfit,
            IReadOnlyList<ShipController> enemyKills, IReadOnlyList<ShipController> playerLosses,
            int shotsFired, Action continueCallback)
        {
            onContinue = continueCallback;

            titleLabel.text = won ? "Victory" : "Defeat";

            profitTotalLabel.text = $"+{totalProfit}g";
            RebuildBreakdown(profitBreakdownContainer, profitBreakdown);

            RebuildShipTally(enemyKillsContainer, enemyKills);
            RebuildShipTally(playerLossesContainer, playerLosses);

            shotsFiredLabel.text = $"Shots Fired: {shotsFired}";

            backdropRoot.SetActive(true);
        }

        private void RebuildBreakdown(RectTransform container, List<ProfitLineItem> breakdown)
        {
            ClearChildren(container);

            foreach (ProfitLineItem item in breakdown)
            {
                TMP_Text label = CreateLabel(container, item.label, 14f, TextAlignmentOptions.Left,
                    new Color(0.8f, 0.8f, 0.8f), 18f);
                label.text = $"{item.label}: {item.amount}g";
            }
        }

        private void RebuildShipTally(RectTransform container, IReadOnlyList<ShipController> ships)
        {
            ClearChildren(container);

            if (ships.Count == 0)
            {
                TMP_Text label = CreateLabel(container, "None", 14f, TextAlignmentOptions.Left,
                    new Color(0.6f, 0.6f, 0.6f), 24f);
                label.text = "None";
                return;
            }

            // SourcePrefab is only ever used as a grouping key here - it's an asset reference whose
            // own segments never ran Awake(), so its cached SpriteRenderer is unset. The sprites must
            // always come from a live ship instance instead.
            var counts = new Dictionary<ShipController, int>();
            var segmentsByKey = new Dictionary<ShipController, Sprite[]>();
            var order = new List<ShipController>();
            foreach (ShipController ship in ships)
            {
                ShipController key = ship.SourcePrefab != null ? ship.SourcePrefab : ship;
                if (counts.TryGetValue(key, out int existing))
                {
                    counts[key] = existing + 1;
                }
                else
                {
                    counts[key] = 1;
                    segmentsByKey[key] = ship.SegmentSprites;
                    order.Add(key);
                }
            }

            foreach (ShipController key in order)
            {
                CreateTallyEntry(container, segmentsByKey[key], counts[key]);
            }
        }

        private static void ClearChildren(Transform container)
        {
            for (int i = container.childCount - 1; i >= 0; i--)
            {
                Destroy(container.GetChild(i).gameObject);
            }
        }

        private void CreateTallyEntry(RectTransform container, Sprite[] segmentSprites, int count)
        {
            int segmentCount = Mathf.Max(1, segmentSprites.Length);
            Vector2 entrySize = new Vector2(shipSegmentIconSize.x, shipSegmentIconSize.y * segmentCount);

            GameObject entryObject = new GameObject("ShipTallyEntry", typeof(RectTransform), typeof(ShipTallyEntryUI));
            entryObject.transform.SetParent(container, false);
            RectTransform entryRect = (RectTransform)entryObject.transform;
            entryRect.sizeDelta = entrySize;

            LayoutElement layoutElement = entryObject.AddComponent<LayoutElement>();
            layoutElement.preferredWidth = entrySize.x;
            layoutElement.preferredHeight = entrySize.y;

            GameObject stackObject = new GameObject("SegmentStack", typeof(RectTransform), typeof(VerticalLayoutGroup));
            stackObject.transform.SetParent(entryRect, false);
            RectTransform stackRect = (RectTransform)stackObject.transform;
            stackRect.anchorMin = Vector2.zero;
            stackRect.anchorMax = Vector2.one;
            stackRect.offsetMin = Vector2.zero;
            stackRect.offsetMax = Vector2.zero;

            VerticalLayoutGroup stackLayout = stackObject.GetComponent<VerticalLayoutGroup>();
            stackLayout.spacing = 0f;
            stackLayout.childAlignment = TextAnchor.UpperCenter;
            stackLayout.childForceExpandWidth = true;
            stackLayout.childForceExpandHeight = false;
            stackLayout.childControlWidth = true;
            stackLayout.childControlHeight = true;

            GameObject countObject = new GameObject("Count", typeof(RectTransform), typeof(TextMeshProUGUI));
            countObject.transform.SetParent(entryRect, false);
            RectTransform countRect = (RectTransform)countObject.transform;
            countRect.anchorMin = new Vector2(1f, 0f);
            countRect.anchorMax = new Vector2(1f, 0f);
            countRect.pivot = new Vector2(1f, 0f);
            countRect.sizeDelta = new Vector2(24f, 14f);
            countRect.anchoredPosition = new Vector2(-2f, 2f);
            TextMeshProUGUI countLabel = countObject.GetComponent<TextMeshProUGUI>();
            countLabel.alignment = TextAlignmentOptions.BottomRight;
            countLabel.fontSize = 11f;
            countLabel.color = Color.white;

            ShipTallyEntryUI entry = entryObject.GetComponent<ShipTallyEntryUI>();
            entry.Initialize(stackRect, countLabel);
            entry.Bind(segmentSprites, shipSegmentIconSize, count);
        }

        private void HandleContinueClicked()
        {
            backdropRoot.SetActive(false);
            onContinue?.Invoke();
        }

        private void BuildUI()
        {
            GameObject backdropObject = new GameObject("BattleSummaryBackdrop", typeof(RectTransform), typeof(Image));
            backdropRoot = backdropObject;
            backdropObject.transform.SetParent(canvas.transform, false);
            RectTransform backdropRect = (RectTransform)backdropObject.transform;
            backdropRect.anchorMin = Vector2.zero;
            backdropRect.anchorMax = Vector2.one;
            backdropRect.offsetMin = Vector2.zero;
            backdropRect.offsetMax = Vector2.zero;
            Image backdropImage = backdropObject.GetComponent<Image>();
            backdropImage.color = new Color(0f, 0f, 0f, 0.75f);

            GameObject panelObject = new GameObject("Panel", typeof(RectTransform), typeof(Image));
            panelObject.transform.SetParent(backdropRect, false);
            RectTransform panelRect = (RectTransform)panelObject.transform;
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = panelSize;
            Image panelImage = panelObject.GetComponent<Image>();
            panelImage.color = new Color(0.08f, 0.1f, 0.14f, 0.98f);

            GameObject contentObject = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            contentObject.transform.SetParent(panelRect, false);
            contentRect = (RectTransform)contentObject.transform;
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.offsetMin = new Vector2(panelPadding, panelPadding);
            contentRect.offsetMax = new Vector2(-panelPadding, -panelPadding);

            VerticalLayoutGroup layout = contentObject.GetComponent<VerticalLayoutGroup>();
            layout.spacing = sectionSpacing;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            layout.childControlWidth = true;
            layout.childControlHeight = true;

            ContentSizeFitter fitter = contentObject.GetComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

            titleLabel = CreateLabel(contentRect, "Title", 28f, TextAlignmentOptions.Center, Color.white, 36f);

            CreateSectionHeader(contentRect, "Profit");
            profitTotalLabel = CreateLabel(contentRect, "ProfitTotal", 22f, TextAlignmentOptions.Center,
                new Color(1f, 0.85f, 0.3f), 30f);
            profitBreakdownContainer = CreateVerticalContainer(contentRect, "ProfitBreakdown");

            CreateSectionHeader(contentRect, "Enemy Ships Destroyed");
            enemyKillsContainer = CreateHorizontalContainer(contentRect, "EnemyKills");

            CreateSectionHeader(contentRect, "Your Ships Lost");
            playerLossesContainer = CreateHorizontalContainer(contentRect, "PlayerLosses");

            shotsFiredLabel = CreateLabel(contentRect, "ShotsFired", 16f, TextAlignmentOptions.Center, Color.white, 24f);

            BuildContinueButton(contentRect);
        }

        private void CreateSectionHeader(Transform parent, string text)
        {
            TMP_Text label = CreateLabel(parent, text, 16f, TextAlignmentOptions.Left,
                new Color(0.7f, 0.75f, 0.85f), 22f);
            label.text = text;
            label.fontStyle = FontStyles.Bold;
        }

        private TMP_Text CreateLabel(Transform parent, string name, float fontSize, TextAlignmentOptions alignment,
            Color color, float preferredHeight)
        {
            GameObject labelObject = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            labelObject.transform.SetParent(parent, false);
            TextMeshProUGUI label = labelObject.GetComponent<TextMeshProUGUI>();
            label.fontSize = fontSize;
            label.alignment = alignment;
            label.color = color;
            label.textWrappingMode = TextWrappingModes.Normal;

            LayoutElement layoutElement = labelObject.AddComponent<LayoutElement>();
            layoutElement.preferredHeight = preferredHeight;

            return label;
        }

        private RectTransform CreateVerticalContainer(Transform parent, string name)
        {
            GameObject containerObject = new GameObject(name, typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            containerObject.transform.SetParent(parent, false);
            RectTransform containerRect = (RectTransform)containerObject.transform;

            VerticalLayoutGroup layout = containerObject.GetComponent<VerticalLayoutGroup>();
            layout.spacing = 2f;
            layout.padding = new RectOffset(16, 0, 0, 0);
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            layout.childControlWidth = true;
            layout.childControlHeight = true;

            ContentSizeFitter fitter = containerObject.GetComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

            return containerRect;
        }

        private RectTransform CreateHorizontalContainer(Transform parent, string name)
        {
            GameObject containerObject = new GameObject(name, typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(ContentSizeFitter));
            containerObject.transform.SetParent(parent, false);
            RectTransform containerRect = (RectTransform)containerObject.transform;

            HorizontalLayoutGroup layout = containerObject.GetComponent<HorizontalLayoutGroup>();
            layout.spacing = 6f;
            layout.padding = new RectOffset(16, 0, 0, 0);
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childAlignment = TextAnchor.MiddleLeft;

            // Ships vary in segment count, so the tally row's height must adapt to whichever
            // entry (2 vs 3+ segment ship) ends up tallest rather than assuming a fixed icon size.
            ContentSizeFitter fitter = containerObject.GetComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

            return containerRect;
        }

        private void BuildContinueButton(Transform parent)
        {
            GameObject buttonObject = new GameObject("ContinueButton", typeof(RectTransform), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(parent, false);

            LayoutElement layoutElement = buttonObject.AddComponent<LayoutElement>();
            layoutElement.preferredHeight = continueButtonSize.y;

            Image buttonImage = buttonObject.GetComponent<Image>();
            buttonImage.color = new Color(0.2f, 0.6f, 0.2f);

            continueButton = buttonObject.GetComponent<Button>();
            continueButton.onClick.AddListener(HandleContinueClicked);

            GameObject labelObject = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            labelObject.transform.SetParent(buttonObject.transform, false);
            RectTransform labelRect = (RectTransform)labelObject.transform;
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;

            TextMeshProUGUI label = labelObject.GetComponent<TextMeshProUGUI>();
            label.text = "Continue";
            label.alignment = TextAlignmentOptions.Center;
            label.fontSize = 20f;
            label.color = Color.white;
        }
    }
}
