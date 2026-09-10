using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BattleshipRoguelike.Shop
{
    public class ShopCardUI : MonoBehaviour
    {
        private Image background;
        private TMP_Text nameLabel;
        private TMP_Text priceLabel;
        private Button buyButton;
        private Action onBuyClicked;

        public void Initialize(Image backgroundImage, TMP_Text name, TMP_Text price, Button buy)
        {
            background = backgroundImage;
            nameLabel = name;
            priceLabel = price;
            buyButton = buy;
            buyButton.onClick.AddListener(HandleBuyClicked);
        }

        public void Bind(string displayName, int price, Color rarityColor, Action onBuy)
        {
            background.color = rarityColor;
            nameLabel.text = displayName;
            priceLabel.text = $"{price}g";
            onBuyClicked = onBuy;
        }

        private void HandleBuyClicked()
        {
            onBuyClicked?.Invoke();
        }
    }
}
