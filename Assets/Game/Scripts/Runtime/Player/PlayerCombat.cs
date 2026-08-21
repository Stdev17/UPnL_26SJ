using System;
using System.Collections.Generic;
using UnityEngine;
using UPnL.SignalRush.Combo;
using UPnL.SignalRush.Combat;
using UPnL.SignalRush.Tuning;

namespace UPnL.SignalRush.Player
{
    public sealed class PlayerCombat : MonoBehaviour
    {
        [SerializeField] private Collider2D _attackHitbox;
        [SerializeField] private SignalRushTuning _tuning;
        [SerializeField] private ComboCounter _combo;
        [SerializeField] private PlayerStatus _status;

        private readonly List<Collider2D> _overlaps = new List<Collider2D>();
        private PlayerStatus _subscribedStatus;
        private float _attackRemaining;
        private bool _attackBuffered;

        public bool IsAttacking { get; private set; }

        public event Action<BreakableObstacle> ObstacleBroken;
        public event Action<Projectile> ProjectileParried;

        private void OnEnable()
        {
            SubscribeStatus();
        }

        private void Update()
        {
            Tick(Time.deltaTime);
        }

        private void OnDisable()
        {
            UnsubscribeStatus();
        }

        public void RequestAttack()
        {
            SubscribeStatus();

            if (_status != null && _status.State != PlayerState.Active)
                return;

            if (IsAttacking)
            {
                _attackBuffered = true;
                return;
            }

            StartAttack();
        }

        public void Interrupt()
        {
            IsAttacking = false;
            _attackRemaining = 0f;
            _attackBuffered = false;
        }

        public void Tick(float deltaTime)
        {
            SubscribeStatus();

            if (!IsAttacking || deltaTime <= 0f)
                return;

            _attackRemaining -= deltaTime;
            if (_attackRemaining > 0f)
                return;

            if (_attackBuffered && (_status == null || _status.State == PlayerState.Active))
            {
                _attackBuffered = false;
                StartAttack();
                return;
            }

            Interrupt();
        }

        private void StartAttack()
        {
            IsAttacking = true;
            _attackRemaining = _tuning == null ? 0f : _tuning.AttackWindowSeconds;
            ResolveOverlaps();
        }

        private void ResolveOverlaps()
        {
            if (_attackHitbox == null)
                return;

            _overlaps.Clear();
            var filter = new ContactFilter2D();
            filter.NoFilter();
            _attackHitbox.Overlap(filter, _overlaps);

            foreach (var overlap in _overlaps)
            {
                var obstacle = overlap.GetComponent<BreakableObstacle>();
                if (obstacle != null && obstacle.TryBreak())
                {
                    _combo?.RecordBreak();
                    ObstacleBroken?.Invoke(obstacle);
                }

                var projectile = overlap.GetComponent<Projectile>();
                if (projectile != null && projectile.TryParry())
                {
                    _combo?.RecordParry();
                    ProjectileParried?.Invoke(projectile);
                }
            }
        }

        private void SubscribeStatus()
        {
            if (_subscribedStatus == _status)
                return;

            UnsubscribeStatus();
            _subscribedStatus = _status;

            if (_subscribedStatus != null)
            {
                _subscribedStatus.StateChanged += HandleStateChanged;
                _subscribedStatus.Hit += HandleHit;
            }
        }

        private void UnsubscribeStatus()
        {
            if (_subscribedStatus == null)
                return;

            _subscribedStatus.StateChanged -= HandleStateChanged;
            _subscribedStatus.Hit -= HandleHit;
            _subscribedStatus = null;
        }

        private void HandleStateChanged(PlayerState state)
        {
            if (state != PlayerState.Active)
                Interrupt();
        }

        private void HandleHit(DamageCause cause)
        {
            _combo?.RecordHit();
        }
    }
}
