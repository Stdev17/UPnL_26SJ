using UnityEngine;

namespace UPnL.SignalRush.World
{
    public sealed class Chunk : MonoBehaviour
    {
        [SerializeField] private Sniper _sniper;

        public ChunkRole Role { get; private set; }
        public bool CanDespawn => _sniper == null || (!_sniper.IsTargetting && !_sniper.HasUnresolvedProjectile);

        public void Place(ChunkSlot slot)
        {
            Role = slot.Role;
            transform.position = slot.Position;
        }

        public void ConfigurePlayerTarget(Transform playerTarget)
        {
            _sniper?.ConfigurePlayerTarget(playerTarget);
        }

        public bool TryActivateSniper()
        {
            return Role == ChunkRole.SniperRear && _sniper != null && _sniper.TryActivate();
        }
    }
}
