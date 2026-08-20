using UnityEngine;
using UnityEngine.UI;
using UPnL.SignalRush.Combo;
using UPnL.SignalRush.Run;

namespace UPnL.SignalRush.UI
{
    public sealed class RunHud : MonoBehaviour
    {
        [SerializeField] private ComboCounter _combo;
        [SerializeField] private RunController _runController;
        [SerializeField] private Text _comboText;
        [SerializeField] private Text _elapsedText;

        private void OnEnable()
        {
            if (_combo != null)
                _combo.Changed += HandleComboChanged;

            RefreshCombo();
            RefreshElapsed();
        }

        private void Update()
        {
            RefreshElapsed();
        }

        private void OnDisable()
        {
            if (_combo != null)
                _combo.Changed -= HandleComboChanged;
        }

        private void HandleComboChanged(int current, int best, int interrupted, float speedMultiplier)
        {
            SetCombo(current, best);
        }

        private void RefreshCombo()
        {
            if (_combo != null)
                SetCombo(_combo.Current, _combo.Best);
        }

        private void SetCombo(int current, int best)
        {
            if (_comboText != null)
                _comboText.text = $"Combo {current}  Best {best}";
        }

        private void RefreshElapsed()
        {
            if (_elapsedText != null && _runController != null)
                _elapsedText.text = $"Time {_runController.ElapsedSeconds:0.0}";
        }
    }
}
