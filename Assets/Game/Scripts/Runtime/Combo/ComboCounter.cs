using System;
using UPnL.SignalRush.Tuning;
using UnityEngine;

namespace UPnL.SignalRush.Combo
{
    public sealed class ComboCounter : MonoBehaviour
    {
        private const int Cap = 20;

        [SerializeField] private SignalRushTuning _tuning;

        public int Current { get; private set; }
        public int Best { get; private set; }
        public int Interrupted { get; private set; }
        public float SpeedMultiplier => _tuning == null
            ? 1f
            : Mathf.Lerp(1f, _tuning.MaxRunSpeed / _tuning.BaseRunSpeed, Current / (float)Cap);

        public event Action<int, int, int, float> Changed;

        public void RecordBreak()
        {
            Increment();
        }

        public void RecordParry()
        {
            Increment();
        }

        public void RecordHit()
        {
            var interrupted = Current;
            Current = 0;
            Interrupted = interrupted;
            NotifyChanged();
        }

        public void Reset()
        {
            Current = 0;
            Best = 0;
            Interrupted = 0;
            NotifyChanged();
        }

        private void Increment()
        {
            if (Current == Cap)
            {
                return;
            }

            Current++;
            Best = Mathf.Max(Best, Current);
            NotifyChanged();
        }

        private void NotifyChanged()
        {
            Changed?.Invoke(Current, Best, Interrupted, SpeedMultiplier);
        }
    }
}
