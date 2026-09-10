using System.Collections.Generic;

namespace BattleshipRoguelike.Map
{
    public sealed class ZoneGraphNode
    {
        public ZoneNodeId Id { get; }
        public ZoneData Data { get; }
        public List<ZoneNodeId> Children { get; } = new List<ZoneNodeId>();

        public ZoneGraphNode(ZoneNodeId id, ZoneData data)
        {
            Id = id;
            Data = data;
        }
    }
}
