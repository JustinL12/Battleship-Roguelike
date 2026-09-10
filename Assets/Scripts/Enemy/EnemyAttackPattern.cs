using UnityEngine;
using BattleshipRoguelike.Grid;

namespace BattleshipRoguelike.Enemy
{
    public abstract class EnemyAttackPattern : MonoBehaviour
    {
        public abstract bool TryGetNextTarget(GridManager targetBoard, out int gx, out int gy);
    }
}
