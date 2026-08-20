using System;
using UnityEngine;

namespace UPnL.SignalRush.Run
{
    public sealed class RunController : MonoBehaviour
    {
        [SerializeField] private GoalTrigger _goalTrigger;

        private bool _goalRequested;
        private bool _deadRequested;

        public RunPhase Phase { get; private set; }
        public float ElapsedSeconds { get; private set; }
        public RunResult? Result { get; private set; }

        public event Action<RunPhase> PhaseChanged;
        public event Action<RunResult> RunFinished;

        private void Update()
        {
            Tick(Time.deltaTime);
        }

        private void FixedUpdate()
        {
            ResolveFixedStep();
        }

        public void ReportGoalReached()
        {
            if (Phase == RunPhase.Running)
            {
                _goalRequested = true;
            }
        }

        public void ReportPlayerDead()
        {
            if (Phase == RunPhase.Running)
            {
                _deadRequested = true;
            }
        }

        public void BeginRespawn()
        {
            if (Phase == RunPhase.Running)
            {
                SetPhase(RunPhase.Respawning);
            }
        }

        public void EndRespawn()
        {
            if (Phase == RunPhase.Respawning)
            {
                SetPhase(RunPhase.Running);
            }
        }

        public void Restart()
        {
            ElapsedSeconds = 0f;
            Result = null;
            _goalRequested = false;
            _deadRequested = false;
            _goalTrigger?.ResetTrigger();
            SetPhase(RunPhase.Running);
        }

        public void Tick(float deltaSeconds)
        {
            if (Phase == RunPhase.Running)
            {
                ElapsedSeconds += deltaSeconds;
            }
        }

        public void ResolveFixedStep()
        {
            if (Phase != RunPhase.Running || (!_goalRequested && !_deadRequested))
            {
                return;
            }

            Finish(_goalRequested ? RunResult.GoalReached : RunResult.Dead);
        }

        private void Finish(RunResult result)
        {
            _goalRequested = false;
            _deadRequested = false;
            Result = result;
            SetPhase(RunPhase.Finished);
            RunFinished?.Invoke(result);
        }

        private void SetPhase(RunPhase phase)
        {
            if (Phase == phase)
            {
                return;
            }

            Phase = phase;
            PhaseChanged?.Invoke(phase);
        }
    }
}
