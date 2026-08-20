using UnityEngine;

namespace UPnL.SignalRush.Tuning
{
    [CreateAssetMenu(fileName = "SignalRushTuning", menuName = "Signal Rush/Tuning")]
    public sealed class SignalRushTuning : ScriptableObject
    {
        [SerializeField] private int _pixelsPerUnit = 32;
        [SerializeField] private float _baseRunSpeed = 6f;
        [SerializeField] private float _maxRunSpeed = 10f;
        [SerializeField] private float _horizontalCorrectionSpeed = 3f;
        [SerializeField] private float _jumpVelocity = 8f;
        [SerializeField] private float _fallGravityMultiplier = 2f;
        [SerializeField] private float _attackWindowSeconds = 0.15f;
        [SerializeField] private float _hitLockSeconds = 0.25f;
        [SerializeField] private float _invulnerabilitySeconds = 1f;
        [SerializeField] private float _targetRunSeconds = 75f;
        [SerializeField] private float _respawnLockSeconds = 1f;
        [SerializeField] private float _projectileSpeed = 18f;
        [SerializeField] private int _spawnAheadChunkCount = 2;
        [SerializeField] private float _maxChunkHeightDelta = 1.5f;
        [SerializeField] private float _maxChunkGap = 2f;
        [SerializeField] private float _sniperWarningSeconds = 0.8f;

        public int PixelsPerUnit => _pixelsPerUnit;
        public float BaseRunSpeed => _baseRunSpeed;
        public float MaxRunSpeed => _maxRunSpeed;
        public float HorizontalCorrectionSpeed => _horizontalCorrectionSpeed;
        public float JumpVelocity => _jumpVelocity;
        public float FallGravityMultiplier => _fallGravityMultiplier;
        public float AttackWindowSeconds => _attackWindowSeconds;
        public float HitLockSeconds => _hitLockSeconds;
        public float InvulnerabilitySeconds => _invulnerabilitySeconds;
        public float TargetRunSeconds => _targetRunSeconds;
        public float RespawnLockSeconds => _respawnLockSeconds;
        public float ProjectileSpeed => _projectileSpeed;
        public int SpawnAheadChunkCount => _spawnAheadChunkCount;
        public float MaxChunkHeightDelta => _maxChunkHeightDelta;
        public float MaxChunkGap => _maxChunkGap;
        public float SniperWarningSeconds => _sniperWarningSeconds;

        private void OnValidate()
        {
            _pixelsPerUnit = Mathf.Max(1, _pixelsPerUnit);
            _baseRunSpeed = Mathf.Max(0.01f, _baseRunSpeed);
            _maxRunSpeed = Mathf.Max(_baseRunSpeed + 0.01f, _maxRunSpeed);
            _horizontalCorrectionSpeed = Mathf.Max(0.01f, _horizontalCorrectionSpeed);
            _jumpVelocity = Mathf.Max(0.01f, _jumpVelocity);
            _fallGravityMultiplier = Mathf.Max(1f, _fallGravityMultiplier);
            _attackWindowSeconds = Mathf.Max(0.01f, _attackWindowSeconds);
            _hitLockSeconds = Mathf.Max(0.01f, _hitLockSeconds);
            _invulnerabilitySeconds = Mathf.Max(_hitLockSeconds, _invulnerabilitySeconds);
            _targetRunSeconds = Mathf.Max(0.01f, _targetRunSeconds);
            _respawnLockSeconds = Mathf.Max(0.01f, _respawnLockSeconds);
            _projectileSpeed = Mathf.Max(0.01f, _projectileSpeed);
            _spawnAheadChunkCount = Mathf.Max(1, _spawnAheadChunkCount);
            _maxChunkHeightDelta = Mathf.Max(0f, _maxChunkHeightDelta);
            _maxChunkGap = Mathf.Max(0f, _maxChunkGap);
            _sniperWarningSeconds = Mathf.Max(0.01f, _sniperWarningSeconds);
        }
    }
}
