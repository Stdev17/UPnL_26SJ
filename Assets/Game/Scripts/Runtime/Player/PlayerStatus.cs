using System;
using UnityEngine;
using UPnL.SignalRush.Tuning;

namespace UPnL.SignalRush.Player
{
    public enum DamageCause { Projectile, OutOfScreen }

    public enum PlayerState { Active, Hit, Respawning, Dead }

    public sealed class PlayerStatus : MonoBehaviour
    {
        [SerializeField] private SignalRushTuning _tuning;

        private float _hitLockRemaining;
        private float _invulnerabilityRemaining;
        private float _respawnLockRemaining;

        public PlayerState State { get; private set; } = PlayerState.Active;
        public bool IsInvulnerable => _invulnerabilityRemaining > 0f;
        public bool IsControlLocked => State != PlayerState.Active;

        public event Action<PlayerState> StateChanged;
        public event Action<DamageCause> Hit;

        private void Update()
        {
            Tick(Time.deltaTime);
        }

        public void Tick(float deltaTime)
        {
            if (deltaTime <= 0f)
                return;

            _invulnerabilityRemaining -= deltaTime;

            if (State == PlayerState.Hit && (_hitLockRemaining -= deltaTime) <= 0f)
                SetState(PlayerState.Active);

            if (State == PlayerState.Respawning && (_respawnLockRemaining -= deltaTime) <= 0f)
                SetState(PlayerState.Active);
        }

        public void RequestDamage(DamageCause cause)
        {
            if (State != PlayerState.Active || IsInvulnerable)
                return;

            _hitLockRemaining = _tuning.HitLockSeconds;
            _invulnerabilityRemaining = _tuning.InvulnerabilitySeconds;
            SetState(PlayerState.Hit);
            Hit?.Invoke(cause);
        }

        public void RequestRespawn()
        {
            if (State == PlayerState.Respawning || State == PlayerState.Dead)
                return;

            _hitLockRemaining = 0f;
            _respawnLockRemaining = _tuning.RespawnLockSeconds;
            SetState(PlayerState.Respawning);
        }

        public void MarkDead()
        {
            if (State == PlayerState.Dead)
                return;

            _hitLockRemaining = 0f;
            _respawnLockRemaining = 0f;
            SetState(PlayerState.Dead);
        }

        public void ResetStatus()
        {
            _hitLockRemaining = 0f;
            _invulnerabilityRemaining = 0f;
            _respawnLockRemaining = 0f;
            SetState(PlayerState.Active);
        }

        private void SetState(PlayerState state)
        {
            if (State == state)
                return;

            State = state;
            StateChanged?.Invoke(state);
        }
    }
}
