using UnityEngine;

namespace UPnL.SignalRush.Tuning
{
    [CreateAssetMenu(fileName = "SignalRushTuning", menuName = "Signal Rush/Tuning")]
    public sealed class SignalRushTuning : ScriptableObject
    {
        [SerializeField] private int _pixelsPerUnit = 32;
        [SerializeField] private float _baseRunSpeed = 6f;
        [SerializeField] private float _maxRunSpeed = 10f;
        [SerializeField] private float _respawnLockSeconds = 1f;
        [SerializeField] private float _projectileSpeed = 18f;
        [SerializeField] private int _spawnAheadChunkCount = 2;
        [SerializeField] private float _maxChunkHeightDelta = 1.5f;
        [SerializeField] private float _maxChunkGap = 2f;
        [SerializeField] private float _sniperWarningSeconds = 0.8f;

        public int PixelsPerUnit => _pixelsPerUnit;
        public float BaseRunSpeed => _baseRunSpeed;
        public float MaxRunSpeed => _maxRunSpeed;
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
            _respawnLockSeconds = Mathf.Max(0.01f, _respawnLockSeconds);
            _projectileSpeed = Mathf.Max(0.01f, _projectileSpeed);
            _spawnAheadChunkCount = Mathf.Max(1, _spawnAheadChunkCount);
            _maxChunkHeightDelta = Mathf.Max(0f, _maxChunkHeightDelta);
            _maxChunkGap = Mathf.Max(0f, _maxChunkGap);
            _sniperWarningSeconds = Mathf.Max(0.01f, _sniperWarningSeconds);
        }
    }
}
