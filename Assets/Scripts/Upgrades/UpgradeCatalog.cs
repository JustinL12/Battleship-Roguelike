using System.Collections.Generic;
using UnityEngine;

namespace BattleshipRoguelike.Upgrades
{
    public static class UpgradeCatalog
    {
        private static readonly Dictionary<UpgradeId, UpgradeDefinition> definitions = new Dictionary<UpgradeId, UpgradeDefinition>
        {
            {
                UpgradeId.DoubleShot,
                new UpgradeDefinition { id = UpgradeId.DoubleShot, displayName = "Double Shot", rarity = UpgradeRarity.Epic, price = 10 }
            }
        };

        public static IReadOnlyCollection<UpgradeId> AllIds => definitions.Keys;

        public static UpgradeDefinition Get(UpgradeId id)
        {
            return definitions[id];
        }

        public static Color RarityColor(UpgradeRarity rarity)
        {
            switch (rarity)
            {
                case UpgradeRarity.Common:
                    return new Color(0.75f, 0.75f, 0.75f);
                case UpgradeRarity.Rare:
                    return new Color(0.25f, 0.55f, 1f);
                case UpgradeRarity.Epic:
                    return new Color(0.65f, 0.25f, 0.95f);
                case UpgradeRarity.Legendary:
                    return new Color(1f, 0.65f, 0.1f);
                default:
                    return Color.white;
            }
        }
    }
}
