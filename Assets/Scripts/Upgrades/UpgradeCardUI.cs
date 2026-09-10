using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BattleshipRoguelike.Upgrades
{
    public class UpgradeCardUI : MonoBehaviour
    {
        private Image background;
        private TMP_Text nameLabel;
        private TMP_Text countLabel;

        public void Initialize(Image backgroundImage, TMP_Text name, TMP_Text count)
        {
            background = backgroundImage;
            nameLabel = name;
            countLabel = count;
        }

        public void Bind(UpgradeStack stack)
        {
            UpgradeDefinition definition = UpgradeCatalog.Get(stack.id);
            background.color = UpgradeCatalog.RarityColor(definition.rarity);
            nameLabel.text = definition.displayName;
            countLabel.gameObject.SetActive(stack.count > 1);
            countLabel.text = $"x{stack.count}";
        }
    }
}
