using UnityEngine;
using UPnL.SignalRush.Combat;
using UPnL.SignalRush.Combo;
using UPnL.SignalRush.Player;
using UPnL.SignalRush.World;

namespace UPnL.SignalRush.Run
{
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class SignalRushPlayable : MonoBehaviour
    {
        private const string PlacementError = "SignalRushPlayable must be attached to its assigned player GameObject.";

        [SerializeField] private RunController _runController;
        [SerializeField] private GoalTrigger _goalTrigger;
        [SerializeField] private PlayerStatus _playerStatus;
        [SerializeField] private PlayerMotor2D _playerMotor;
        [SerializeField] private PlayerCombat _playerCombat;
        [SerializeField] private ComboCounter _comboCounter;
        [SerializeField] private ChunkSpawner _chunkSpawner;
        [SerializeField] private Transform _player;
        [SerializeField] private float _fallY = -10f;

        private Vector2 _initialPosition;
        private bool _hasInitialPosition;
        private PlayerState _lastPlayerState;
        private RunPhase _lastRunPhase;

        private void Awake()
        {
            if (_player != null)
                ValidatePlacement();
        }

        private void OnEnable()
        {
            if (!ValidatePlacement())
                return;

            if (!_hasInitialPosition && _player != null)
            {
                _initialPosition = _player.position;
                _hasInitialPosition = true;
            }

            if (_goalTrigger != null)
                _goalTrigger.Reached += HandleGoalReached;

            if (_comboCounter != null)
            {
                _comboCounter.Changed += HandleComboChanged;
                HandleComboChanged(0, 0, 0, _comboCounter.SpeedMultiplier);
            }

            if (_playerStatus != null)
            {
                _lastPlayerState = _playerStatus.State;
                _playerStatus.StateChanged += HandlePlayerStateChanged;
            }

            if (_playerCombat != null)
                _playerCombat.ObstacleBroken += HandleObstacleBroken;

            if (_runController != null)
            {
                _lastRunPhase = _runController.Phase;
                _runController.PhaseChanged += HandleRunPhaseChanged;
                _runController.RunFinished += HandleRunFinished;
            }

            if (_runController != null && _runController.Phase == RunPhase.Finished)
            {
                _playerCombat?.Interrupt();
                _playerMotor?.SetSimulationPaused(true);
            }
            else
            {
                _chunkSpawner?.Begin();
            }
        }

        private void Update()
        {
            if (_player != null && _player.position.y < _fallY)
                _playerStatus?.RequestRespawn();
        }

        private void OnDisable()
        {
            if (_goalTrigger != null)
                _goalTrigger.Reached -= HandleGoalReached;
            if (_comboCounter != null)
                _comboCounter.Changed -= HandleComboChanged;
            if (_playerStatus != null)
                _playerStatus.StateChanged -= HandlePlayerStateChanged;
            if (_playerCombat != null)
                _playerCombat.ObstacleBroken -= HandleObstacleBroken;
            if (_runController != null)
            {
                _runController.PhaseChanged -= HandleRunPhaseChanged;
                _runController.RunFinished -= HandleRunFinished;
            }

            _chunkSpawner?.Stop();
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.TryGetComponent<Projectile>(out var projectile))
                HandleProjectile(projectile);
            else if (other.TryGetComponent<BreakableObstacle>(out var obstacle))
                HandleObstacle(obstacle);
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (collision.collider.TryGetComponent<BreakableObstacle>(out var obstacle))
                HandleObstacle(obstacle);
        }

        public void HandleProjectile(Projectile projectile)
        {
            if (projectile != null && projectile.TryHitPlayer())
                _playerStatus?.RequestDamage(DamageCause.Projectile);
        }

        public void HandleObstacle(BreakableObstacle obstacle)
        {
            if (obstacle == null)
                return;

            _playerStatus?.RequestDamage(DamageCause.Obstacle);
            obstacle.gameObject.SetActive(false);
        }

        private void HandleGoalReached()
        {
            _runController?.ReportGoalReached();
        }

        private void HandleComboChanged(int current, int best, int interrupted, float speedMultiplier)
        {
            _playerMotor?.SetSpeedMultiplier(speedMultiplier);
        }

        private void HandlePlayerStateChanged(PlayerState state)
        {
            var previous = _lastPlayerState;
            _lastPlayerState = state;

            if (state == PlayerState.Respawning)
            {
                _runController?.BeginRespawn();
                _playerMotor?.SetSimulationPaused(true);
            }
            else if (state == PlayerState.Active && previous == PlayerState.Respawning)
            {
                if (_playerMotor != null)
                {
                    _playerMotor.Respawn(_playerMotor.SafePosition);
                    _playerMotor.SetSimulationPaused(false);
                }
                _runController?.EndRespawn();
            }
            else if (state == PlayerState.Dead)
            {
                _runController?.ReportPlayerDead();
            }
        }

        private void HandleObstacleBroken(BreakableObstacle obstacle)
        {
            if (obstacle != null)
                obstacle.gameObject.SetActive(false);
        }

        private void HandleRunPhaseChanged(RunPhase phase)
        {
            var previous = _lastRunPhase;
            _lastRunPhase = phase;

            if (phase == RunPhase.Running && previous == RunPhase.Finished)
            {
                _playerStatus?.ResetStatus();
                _comboCounter?.Reset();
                if (_hasInitialPosition)
                    _playerMotor?.Respawn(_initialPosition);
                _playerMotor?.SetSimulationPaused(false);
                _chunkSpawner?.Begin();
            }
        }

        private void HandleRunFinished(RunResult result)
        {
            _playerCombat?.Interrupt();
            _playerMotor?.SetSimulationPaused(true);
            _chunkSpawner?.Stop();
        }

        private bool ValidatePlacement()
        {
            if (_player != null && _player.gameObject == gameObject)
                return true;

            Debug.LogError(PlacementError, this);
            enabled = false;
            return false;
        }
    }
}
