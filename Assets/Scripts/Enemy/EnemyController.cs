using System.Collections.Generic;
using UnityEngine;
using BattleshipRoguelike.Grid;
using BattleshipRoguelike.Ships;

namespace BattleshipRoguelike.Enemy
{
    public class EnemyController : MonoBehaviour
    {
        [SerializeField] private GridManager board;
        [SerializeField] private List<ShipController> shipPrefabs = new List<ShipController>();
        [SerializeField] private EnemyAttackPattern attackPattern;

        private readonly List<ShipController> ships = new List<ShipController>();

        public GridManager Board => board;
        public IReadOnlyList<ShipController> Ships => ships;

        public bool AllSunk
        {
            get
            {
                if (ships.Count == 0)
                {
                    return false;
                }

                foreach (ShipController ship in ships)
                {
                    if (!ship.IsSunk)
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        private void Start()
        {
            foreach (ShipController prefab in shipPrefabs)
            {
                ShipController ship = Instantiate(prefab, transform);
                ship.SetSourcePrefab(prefab);
                if (board.TryGetRandomFreeFootprint(ship.SegmentCount, out int gx, out int gy, out bool horizontal))
                {
                    ship.PlaceAt(board, gx, gy, horizontal);
                }

                ship.SetInteractable(false);
                ship.SetVisible(false);
                ships.Add(ship);
            }
        }

        public bool TryGetNextTarget(GridManager playerBoard, out int gx, out int gy)
        {
            return attackPattern.TryGetNextTarget(playerBoard, out gx, out gy);
        }
    }
}
