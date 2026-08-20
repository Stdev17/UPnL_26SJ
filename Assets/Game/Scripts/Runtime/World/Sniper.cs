using System;
using UnityEngine;
using UPnL.SignalRush.Combat;
using UPnL.SignalRush.Tuning;

namespace UPnL.SignalRush.World
{
    public sealed class Sniper : MonoBehaviour
    {
        [SerializeField] private SignalRushTuning _tuning;
        [SerializeField] private Transform _playerTarget;
        [SerializeField] private Transform _muzzle;
        [SerializeField] private Projectile _projectilePrefab;

        private float _warningRemaining;
        private Vector2 _targetPosition;
        private Projectile _activeProjectile;

        public bool IsTargetting { get; private set; }
        internal bool HasUnresolvedProjectile => _activeProjectile != null && !_activeProjectile.IsResolved;

        public event Action<Projectile> ProjectileSpawned;

        private void Update()
        {
            Tick(Time.deltaTime);
        }

        private void OnDestroy()
        {
            ReleaseProjectile();
        }

        public bool TryActivate()
        {
            if (IsTargetting || HasUnresolvedProjectile || !HasValidConfiguration())
                return false;

            ReleaseProjectile();
            IsTargetting = true;
            _warningRemaining = _tuning.SniperWarningSeconds;
            _targetPosition = _playerTarget.position;
            return true;
        }

        public void Tick(float deltaTime)
        {
            if (!IsTargetting)
                return;

            _targetPosition = _playerTarget.position;
            _warningRemaining -= Mathf.Max(0f, deltaTime);
            if (_warningRemaining <= 0f)
                Fire();
        }

        private bool HasValidConfiguration()
        {
            return _tuning != null && _playerTarget != null && _muzzle != null &&
                _projectilePrefab != null && _projectilePrefab.TryGetComponent<Rigidbody2D>(out _);
        }

        private void Fire()
        {
            IsTargetting = false;
            _activeProjectile = Instantiate(_projectilePrefab, _muzzle.position, _muzzle.rotation);
            var direction = (_targetPosition - (Vector2)_muzzle.position).normalized;
            _activeProjectile.GetComponent<Rigidbody2D>().linearVelocity = direction * _tuning.ProjectileSpeed;
            _activeProjectile.HitPlayer += HandleProjectileResolved;
            _activeProjectile.Parried += HandleProjectileResolved;
            _activeProjectile.Missed += HandleProjectileResolved;
            ProjectileSpawned?.Invoke(_activeProjectile);
        }

        private void HandleProjectileResolved(Projectile projectile)
        {
            if (projectile == _activeProjectile)
                ReleaseProjectile();
        }

        private void ReleaseProjectile()
        {
            if (_activeProjectile != null)
            {
                _activeProjectile.HitPlayer -= HandleProjectileResolved;
                _activeProjectile.Parried -= HandleProjectileResolved;
                _activeProjectile.Missed -= HandleProjectileResolved;
            }

            _activeProjectile = null;
        }
    }
}
