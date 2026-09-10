using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using BattleshipRoguelike.Ships;
using BattleshipRoguelike.Upgrades;

namespace BattleshipRoguelike.Map
{
    public class RunManager : MonoBehaviour
    {
        public static RunManager Instance { get; private set; }

        [SerializeField] private List<GameObject> battleEnemyPrefabs = new List<GameObject>();
        [SerializeField] private string mapSceneName = "MapScene";
        [SerializeField] private string battleSceneName = "BattleScene";
        [SerializeField] private string shopSceneName = "ShopScene";
        [SerializeField] private int stretchLength = 10;
        [SerializeField] private int maxLaneDeviation = 3;
        [SerializeField] private float branchChance = 0.35f;
        [SerializeField] private float shopZoneChance = 0.2f;
        [SerializeField] private int startingGold = 20;
        [SerializeField] private int baseBattleReward = 5;
        [SerializeField] private List<UpgradeStack> startingUpgrades = new List<UpgradeStack>();
        [SerializeField] private List<ShipController> startingShipPrefabs = new List<ShipController>();

        private readonly Dictionary<ZoneNodeId, ZoneGraphNode> nodesById = new Dictionary<ZoneNodeId, ZoneGraphNode>();
        private readonly HashSet<int> generatedStretchStarts = new HashSet<int>();
        private readonly List<UpgradeStack> upgrades = new List<UpgradeStack>();
        private readonly List<ShipController> playerShipPrefabs = new List<ShipController>();
        private readonly List<ShipController> sunkShipPrefabs = new List<ShipController>();
        private ZoneNodeId currentNodeId;
        private int gold;

        public ZoneNodeId CurrentNodeId => currentNodeId;
        public int StretchLength => stretchLength;
        public IReadOnlyList<UpgradeStack> Upgrades => upgrades;
        public IReadOnlyList<ShipController> PlayerShipPrefabs => playerShipPrefabs;
        public IReadOnlyList<ShipController> SunkShipPrefabs => sunkShipPrefabs;
        public int Gold => gold;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            EnsureInitialized();

            foreach (UpgradeStack starting in startingUpgrades)
            {
                for (int i = 0; i < starting.count; i++)
                {
                    AddUpgrade(starting.id);
                }
            }

            playerShipPrefabs.AddRange(startingShipPrefabs);
            gold = startingGold;
        }

        public void EnsureInitialized()
        {
            if (nodesById.Count > 0)
            {
                return;
            }

            // A virtual anchor sits one depth before the real run so the depth-0 zone itself is the
            // single directly-clickable node on a fresh run, matching the "only 1 option to start" rule -
            // without it, currentNodeId would need to point at depth 0 as if already played, which would
            // expose depth 0's own (branching) children as the first choice instead of depth 0 itself.
            ZoneNodeId anchor = new ZoneNodeId(-1, 0);
            ZoneNodeId root = new ZoneNodeId(0, 0);
            var anchorNode = new ZoneGraphNode(anchor, GenerateZoneData());
            anchorNode.Children.Add(root);
            nodesById[anchor] = anchorNode;
            nodesById[root] = new ZoneGraphNode(root, GenerateZoneData());
            currentNodeId = anchor;
            GenerateStretch(0);
        }

        private void GenerateStretch(int startDepth)
        {
            if (!generatedStretchStarts.Add(startDepth))
            {
                return;
            }

            for (int d = startDepth; d < startDepth + stretchLength; d++)
            {
                int localNext = (d + 1) - startDepth;
                int boundNext = Mathf.Min(maxLaneDeviation, stretchLength - localNext);
                bool isConvergenceDepth = d + 1 == startDepth + stretchLength;

                var parents = new List<ZoneGraphNode>();
                foreach (ZoneGraphNode node in nodesById.Values)
                {
                    if (node.Id.depth == d)
                    {
                        parents.Add(node);
                    }
                }

                foreach (ZoneGraphNode parent in parents)
                {
                    var candidates = new List<int>();
                    for (int shift = -1; shift <= 1; shift++)
                    {
                        int candidateLane = parent.Id.lane + shift;
                        if (Mathf.Abs(candidateLane) <= boundNext)
                        {
                            candidates.Add(candidateLane);
                        }
                    }

                    var chosenLanes = new List<int>();
                    if (candidates.Count == 1)
                    {
                        // Only one lane survives the convergence bound - this move is forced, not optional.
                        chosenLanes.Add(candidates[0]);
                    }
                    else
                    {
                        foreach (int candidateLane in candidates)
                        {
                            bool isCenterContinuation = parent.Id.lane == 0 && candidateLane == 0;
                            if (isCenterContinuation || Random.value < branchChance)
                            {
                                chosenLanes.Add(candidateLane);
                            }
                        }

                        if (chosenLanes.Count == 0)
                        {
                            chosenLanes.Add(candidates[Random.Range(0, candidates.Count)]);
                        }
                    }

                    foreach (int candidateLane in chosenLanes)
                    {
                        ZoneNodeId childId = new ZoneNodeId(d + 1, candidateLane);
                        if (!nodesById.TryGetValue(childId, out ZoneGraphNode child))
                        {
                            ZoneData data = isConvergenceDepth
                                ? new ZoneData { type = ZoneType.Boss, enemyPrefabIndex = -1 }
                                : GenerateZoneData();
                            child = new ZoneGraphNode(childId, data);
                            nodesById[childId] = child;
                        }

                        if (!parent.Children.Contains(childId))
                        {
                            parent.Children.Add(childId);
                        }
                    }
                }
            }
        }

        private ZoneData GenerateZoneData()
        {
            if (Random.value < shopZoneChance)
            {
                return new ZoneData { type = ZoneType.Shop, enemyPrefabIndex = -1 };
            }

            return new ZoneData
            {
                type = ZoneType.Battle,
                enemyPrefabIndex = battleEnemyPrefabs.Count > 0 ? Random.Range(0, battleEnemyPrefabs.Count) : -1
            };
        }

        public ZoneGraphNode GetNode(ZoneNodeId id)
        {
            return nodesById[id];
        }

        public IReadOnlyList<ZoneNodeId> GetChildren(ZoneNodeId id)
        {
            return nodesById[id].Children;
        }

        public IEnumerable<ZoneGraphNode> GetStretchNodes(int stretchStart)
        {
            foreach (ZoneGraphNode node in nodesById.Values)
            {
                if (node.Id.depth >= stretchStart && node.Id.depth <= stretchStart + stretchLength)
                {
                    yield return node;
                }
            }
        }

        public GameObject GetEnemyPrefabForZone(ZoneNodeId id)
        {
            ZoneData zone = nodesById[id].Data;
            if (zone.enemyPrefabIndex < 0 || zone.enemyPrefabIndex >= battleEnemyPrefabs.Count)
            {
                return null;
            }

            return battleEnemyPrefabs[zone.enemyPrefabIndex];
        }

        public void EnterZone(ZoneNodeId target)
        {
            currentNodeId = target;
            ZoneData zone = nodesById[target].Data;
            SceneManager.LoadScene(zone.type == ZoneType.Shop ? shopSceneName : battleSceneName);
        }

        public void CompleteCurrentZone()
        {
            if (currentNodeId.depth > 0 && currentNodeId.depth % stretchLength == 0)
            {
                GenerateStretch(currentNodeId.depth);
            }

            SceneManager.LoadScene(mapSceneName);
        }

        public void AddUpgrade(UpgradeId id)
        {
            UpgradeStack existing = upgrades.Find(u => u.id == id);
            if (existing != null)
            {
                existing.count++;
            }
            else
            {
                upgrades.Add(new UpgradeStack { id = id, count = 1 });
            }
        }

        public int GetUpgradeCount(UpgradeId id)
        {
            UpgradeStack existing = upgrades.Find(u => u.id == id);
            return existing != null ? existing.count : 0;
        }

        public void RemoveShip(ShipController prefab)
        {
            if (playerShipPrefabs.Remove(prefab))
            {
                sunkShipPrefabs.Add(prefab);
            }
        }

        public bool TrySpendGold(int amount)
        {
            if (gold < amount)
            {
                return false;
            }

            gold -= amount;
            return true;
        }

        public void AddGold(int amount)
        {
            gold += amount;
        }

        public List<ProfitLineItem> ApplyBattleProfit()
        {
            var breakdown = new List<ProfitLineItem>
            {
                new ProfitLineItem("Base", baseBattleReward)
            };

            int total = 0;
            foreach (ProfitLineItem item in breakdown)
            {
                total += item.amount;
            }

            AddGold(total);
            return breakdown;
        }
    }
}
