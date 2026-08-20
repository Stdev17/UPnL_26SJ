using System;
using UnityEngine;

namespace UPnL.SignalRush.Combat
{
    public sealed class BreakableObstacle : MonoBehaviour
    {
        public bool IsBroken { get; private set; }
        public event Action<BreakableObstacle> Broken;

        public bool TryBreak()
        {
            if (IsBroken)
            {
                return false;
            }

            IsBroken = true;
            Broken?.Invoke(this);
            return true;
        }
    }
}
