using System;
using UnityEngine;

namespace UPnL.SignalRush.Combat
{
    public sealed class Projectile : MonoBehaviour
    {
        public bool IsResolved { get; private set; }
        public event Action<Projectile> HitPlayer;
        public event Action<Projectile> Parried;

        public bool TryHitPlayer()
        {
            return TryResolve(HitPlayer);
        }

        public bool TryParry()
        {
            return TryResolve(Parried);
        }

        private bool TryResolve(Action<Projectile> resolved)
        {
            if (IsResolved)
            {
                return false;
            }

            IsResolved = true;
            resolved?.Invoke(this);
            return true;
        }
    }
}
