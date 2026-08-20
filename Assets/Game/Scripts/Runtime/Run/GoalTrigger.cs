using System;
using UnityEngine;

namespace UPnL.SignalRush.Run
{
    public sealed class GoalTrigger : MonoBehaviour
    {
        private bool _reached;

        public event Action Reached;

        private void OnTriggerEnter2D(Collider2D other)
        {
            TryReach();
        }

        public bool TryReach()
        {
            if (_reached)
            {
                return false;
            }

            _reached = true;
            Reached?.Invoke();
            return true;
        }

        public void ResetTrigger()
        {
            _reached = false;
        }
    }
}
