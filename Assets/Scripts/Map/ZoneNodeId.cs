using System;

namespace BattleshipRoguelike.Map
{
    public readonly struct ZoneNodeId : IEquatable<ZoneNodeId>
    {
        public readonly int depth;
        public readonly int lane;

        public ZoneNodeId(int depth, int lane)
        {
            this.depth = depth;
            this.lane = lane;
        }

        public int Cycle => depth / 10;

        public bool Equals(ZoneNodeId other) => depth == other.depth && lane == other.lane;

        public override bool Equals(object obj) => obj is ZoneNodeId other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(depth, lane);

        public static bool operator ==(ZoneNodeId a, ZoneNodeId b) => a.Equals(b);

        public static bool operator !=(ZoneNodeId a, ZoneNodeId b) => !a.Equals(b);

        public override string ToString() => $"(d{depth}, l{lane})";
    }
}
