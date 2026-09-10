using System.Collections.Generic;
using UnityEngine;
using BattleshipRoguelike.Grid;

namespace BattleshipRoguelike.Enemy
{
    public class RandomAttackPattern : EnemyAttackPattern
    {
        public override bool TryGetNextTarget(GridManager targetBoard, out int gx, out int gy)
        {
            var candidates = new List<Vector2Int>();
            for (int x = 0; x < targetBoard.Width; x++)
            {
                for (int y = 0; y < targetBoard.Height; y++)
                {
                    GridCell cell = targetBoard.GetCell(x, y);
                    if (cell != null && !cell.fired)
                    {
                        candidates.Add(new Vector2Int(x, y));
                    }
                }
            }

            if (candidates.Count == 0)
            {
                gx = 0;
                gy = 0;
                return false;
            }

            Vector2Int choice = candidates[Random.Range(0, candidates.Count)];
            gx = choice.x;
            gy = choice.y;
            return true;
        }
    }
}
