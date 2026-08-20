using UnityEngine;

namespace UPnL.SignalRush.World
{
    public readonly struct ChunkSlot
    {
        public ChunkSlot(ChunkRole role, Vector2 position)
        {
            Role = role;
            Position = position;
        }

        public ChunkRole Role { get; }
        public Vector2 Position { get; }
    }
}
