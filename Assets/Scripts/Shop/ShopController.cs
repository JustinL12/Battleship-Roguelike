using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using BattleshipRoguelike.Map;
using BattleshipRoguelike.Upgrades;

namespace BattleshipRoguelike.Shop
{
    public class ShopController : MonoBehaviour
    {
        [SerializeField] private Canvas canvas;
        [SerializeField] private int cardCount = 3;
        [SerializeField] private int maxVisibleCards = 5;
        [SerializeField] private float cardAspect = 0.72f;
        [SerializeField] private float cardSideMargin = 24f;
        [SerializeField] private float cardSpacing = 24f;
        [SerializeField] private Vector2 leaveButtonSize = new Vector2(160f, 50f);
        [SerializeField] private float screenMargin = 5f;

        private readonly List<ShopCardUI> cards = new List<ShopCardUI>();

        private void Awake()
        {
            if (canvas == null)
            {
                canvas = FindAnyObjectByType<Canvas>();
            }

            BuildCards();
            BuildLeaveButton();
        }

        private void BuildCards()
        {
            var ids = new List<UpgradeId>(UpgradeCatalog.AllIds);

            RectTransform canvasRect = (RectTransform)canvas.transform;
            float screenWidth = canvasRect.rect.width;
            float maxCardWidth = (screenWidth - cardSideMargin * 2f - (maxVisibleCards - 1) * cardSpacing) / maxVisibleCards;
            Vector2 cardSize = new Vector2(maxCardWidth, maxCardWidth / cardAspect);

            float totalWidth = cardCount * cardSize.x + (cardCount - 1) * cardSpacing;
            float startX = -totalWidth * 0.5f + cardSize.x * 0.5f;

            for (int i = 0; i < cardCount; i++)
            {
                UpgradeId id = ids[Random.Range(0, ids.Count)];
                UpgradeDefinition definition = UpgradeCatalog.Get(id);

                ShopCardUI card = CreateCard(startX + i * (cardSize.x + cardSpacing), cardSize);
                card.Bind(definition.displayName, definition.price, UpgradeCatalog.RarityColor(definition.rarity),
                    () => TryPurchase(card, id, definition.price));
                cards.Add(card);
            }
        }

        private ShopCardUI CreateCard(float x, Vector2 cardSize)
        {
            GameObject cardObject = new GameObject("ShopCard", typeof(RectTransform), typeof(Image), typeof(ShopCardUI));
            cardObject.transform.SetParent(canvas.transform, false);
            RectTransform cardRect = (RectTransform)cardObject.transform;
            cardRect.anchorMin = new Vector2(0.5f, 0.5f);
            cardRect.anchorMax = new Vector2(0.5f, 0.5f);
            cardRect.pivot = new Vector2(0.5f, 0.5f);
            cardRect.sizeDelta = cardSize;
            cardRect.anchoredPosition = new Vector2(x, 0f);

            GameObject nameObject = new GameObject("Name", typeof(RectTransform), typeof(TextMeshProUGUI));
            nameObject.transform.SetParent(cardRect, false);
            RectTransform nameRect = (RectTransform)nameObject.transform;
            nameRect.anchorMin = new Vector2(0f, 0.4f);
            nameRect.anchorMax = new Vector2(1f, 1f);
            nameRect.offsetMin = new Vector2(2f, 0f);
            nameRect.offsetMax = new Vector2(-2f, -2f);
            TextMeshProUGUI nameLabel = nameObject.GetComponent<TextMeshProUGUI>();
            nameLabel.alignment = TextAlignmentOptions.Center;
            nameLabel.enableAutoSizing = true;
            nameLabel.fontSizeMin = 8f;
            nameLabel.fontSizeMax = 48f;
            nameLabel.textWrappingMode = TextWrappingModes.Normal;
            nameLabel.color = Color.white;

            GameObject priceObject = new GameObject("Price", typeof(RectTransform), typeof(TextMeshProUGUI));
            priceObject.transform.SetParent(cardRect, false);
            RectTransform priceRect = (RectTransform)priceObject.transform;
            priceRect.anchorMin = new Vector2(0f, 0.22f);
            priceRect.anchorMax = new Vector2(1f, 0.4f);
            priceRect.offsetMin = Vector2.zero;
            priceRect.offsetMax = Vector2.zero;
            TextMeshProUGUI priceLabel = priceObject.GetComponent<TextMeshProUGUI>();
            priceLabel.alignment = TextAlignmentOptions.Center;
            priceLabel.enableAutoSizing = true;
            priceLabel.fontSizeMin = 8f;
            priceLabel.fontSizeMax = 36f;
            priceLabel.color = new Color(1f, 0.85f, 0.3f);

            GameObject buyObject = new GameObject("BuyButton", typeof(RectTransform), typeof(Image), typeof(Button));
            buyObject.transform.SetParent(cardRect, false);
            RectTransform buyRect = (RectTransform)buyObject.transform;
            buyRect.anchorMin = new Vector2(0f, 0f);
            buyRect.anchorMax = new Vector2(1f, 0.22f);
            buyRect.offsetMin = new Vector2(2f, 2f);
            buyRect.offsetMax = new Vector2(-2f, 0f);
            Image buyImage = buyObject.GetComponent<Image>();
            buyImage.color = new Color(0.2f, 0.6f, 0.2f);
            Button buyButton = buyObject.GetComponent<Button>();

            GameObject buyLabelObject = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            buyLabelObject.transform.SetParent(buyRect, false);
            RectTransform buyLabelRect = (RectTransform)buyLabelObject.transform;
            buyLabelRect.anchorMin = Vector2.zero;
            buyLabelRect.anchorMax = Vector2.one;
            buyLabelRect.offsetMin = Vector2.zero;
            buyLabelRect.offsetMax = Vector2.zero;
            TextMeshProUGUI buyLabel = buyLabelObject.GetComponent<TextMeshProUGUI>();
            buyLabel.text = "Buy";
            buyLabel.alignment = TextAlignmentOptions.Center;
            buyLabel.enableAutoSizing = true;
            buyLabel.fontSizeMin = 8f;
            buyLabel.fontSizeMax = 32f;
            buyLabel.color = Color.white;

            ShopCardUI card = cardObject.GetComponent<ShopCardUI>();
            card.Initialize(cardObject.GetComponent<Image>(), nameLabel, priceLabel, buyButton);
            return card;
        }

        private void TryPurchase(ShopCardUI card, UpgradeId id, int price)
        {
            if (RunManager.Instance == null || !RunManager.Instance.TrySpendGold(price))
            {
                return;
            }

            RunManager.Instance.AddUpgrade(id);
            cards.Remove(card);
            Destroy(card.gameObject);
        }

        private void BuildLeaveButton()
        {
            GameObject buttonObject = new GameObject("LeaveShopButton", typeof(RectTransform), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(canvas.transform, false);

            RectTransform rect = (RectTransform)buttonObject.transform;
            rect.anchorMin = new Vector2(1f, 0f);
            rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(1f, 0f);
            rect.anchoredPosition = new Vector2(-screenMargin, screenMargin);
            rect.sizeDelta = leaveButtonSize;

            Image image = buttonObject.GetComponent<Image>();
            image.color = new Color(0.15f, 0.15f, 0.2f, 0.9f);

            Button button = buttonObject.GetComponent<Button>();
            button.onClick.AddListener(HandleLeaveClicked);

            GameObject labelObject = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            labelObject.transform.SetParent(buttonObject.transform, false);
            RectTransform labelRect = (RectTransform)labelObject.transform;
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;

            TextMeshProUGUI label = labelObject.GetComponent<TextMeshProUGUI>();
            label.text = "Leave Shop";
            label.alignment = TextAlignmentOptions.Center;
            label.fontSize = 20f;
            label.color = Color.white;
        }

        private void HandleLeaveClicked()
        {
            RunManager.Instance.CompleteCurrentZone();
        }
    }
}
