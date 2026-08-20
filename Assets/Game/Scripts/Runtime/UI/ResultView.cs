using UnityEngine;
using UnityEngine.UI;
using UPnL.SignalRush.Run;

namespace UPnL.SignalRush.UI
{
    public sealed class ResultView : MonoBehaviour
    {
        [SerializeField] private RunController _runController;
        [SerializeField] private Text _resultText;
        [SerializeField] private GameObject _resultRoot;

        private void OnEnable()
        {
            if (_runController != null)
            {
                _runController.RunFinished += Show;
                _runController.PhaseChanged += HandlePhaseChanged;
            }

            if (_runController != null && _runController.Result.HasValue)
                Show(_runController.Result.Value);
            else
                SetVisible(false);
        }

        private void OnDisable()
        {
            if (_runController == null)
                return;

            _runController.RunFinished -= Show;
            _runController.PhaseChanged -= HandlePhaseChanged;
        }

        public void Show(RunResult result)
        {
            if (_resultText != null)
                _resultText.text = result.ToString();

            SetVisible(true);
        }

        private void HandlePhaseChanged(RunPhase phase)
        {
            if (phase == RunPhase.Running)
                SetVisible(false);
        }

        private void SetVisible(bool visible)
        {
            if (_resultRoot != null)
                _resultRoot.SetActive(visible);
            else if (_resultText != null)
                _resultText.enabled = visible;
        }
    }
}
