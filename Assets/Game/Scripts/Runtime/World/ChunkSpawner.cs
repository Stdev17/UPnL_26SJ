using System;
using System.Collections.Generic;
using UnityEngine;
using UPnL.SignalRush.Tuning;

namespace UPnL.SignalRush.World
{
    public sealed class ChunkSpawner : MonoBehaviour
    {
        private const string ConfigurationError = "ChunkSpawner requires tuning, origin, player, and non-empty role prefab lists.";

        [SerializeField] private SignalRushTuning _tuning;
        [SerializeField] private Transform _origin;
        [SerializeField] private Transform _player;
        [SerializeField] private Chunk[] _gameplayFrontPrefabs = Array.Empty<Chunk>();
        [SerializeField] private Chunk[] _decorFrontPrefabs = Array.Empty<Chunk>();
        [SerializeField] private Chunk[] _sniperRearPrefabs = Array.Empty<Chunk>();

        private bool _isRunning;
        private int _nextRole;
        private int _nextGameplay;
        private int _nextDecor;
        private int _nextSniper;
        private Vector2 _lastGameplayPosition;
        private readonly List<Chunk> _spawned = new List<Chunk>();

        private void Update()
        {
            Tick();
        }

        public void Begin()
        {
            _isRunning = false;
            ClearSpawned();
            if (!HasValidConfiguration())
            {
                Debug.LogError(ConfigurationError, this);
                return;
            }

            _nextRole = 0;
            _nextGameplay = 0;
            _nextDecor = 0;
            _nextSniper = 0;
            _lastGameplayPosition = _origin.position;
            _isRunning = true;
        }

        public void Stop()
        {
            _isRunning = false;
            ClearSpawned();
        }

        public void Tick()
        {
            if (!_isRunning)
                return;

            CleanupBehindPlayer();
            if (_lastGameplayPosition.x < _player.position.x + MaxGameplayGap * _tuning.SpawnAheadChunkCount)
                SpawnNext();
        }

        public Chunk SpawnNext()
        {
            if (!_isRunning)
                return null;

            var role = (ChunkRole)_nextRole;
            _nextRole = (_nextRole + 1) % 3;
            var prefab = NextPrefab(role);
            var slot = NextSlot(role);
            var chunk = Instantiate(prefab, slot.Position, Quaternion.identity, transform);
            chunk.Place(slot);
            if (role == ChunkRole.SniperRear)
            {
                chunk.ConfigurePlayerTarget(_player);
                chunk.TryActivateSniper();
            }
            _spawned.Add(chunk);
            return chunk;
        }

        private float MaxGameplayGap => Mathf.Min(
            _tuning.MaxChunkGap,
            JumpReachability.MaxGap(
                _tuning.BaseRunSpeed,
                _tuning.JumpVelocity,
                Physics2D.gravity.magnitude,
                _tuning.FallGravityMultiplier));

        private bool HasValidConfiguration()
        {
            return _tuning != null && _origin != null && _player != null &&
                HasCandidates(_gameplayFrontPrefabs) && HasCandidates(_decorFrontPrefabs) && HasCandidates(_sniperRearPrefabs);
        }

        private static bool HasCandidates(Chunk[] prefabs)
        {
            if (prefabs == null || prefabs.Length == 0)
                return false;

            for (var i = 0; i < prefabs.Length; i++)
            {
                if (prefabs[i] == null)
                    return false;
            }

            return true;
        }

        private Chunk NextPrefab(ChunkRole role)
        {
            switch (role)
            {
                case ChunkRole.GameplayFront:
                    return _gameplayFrontPrefabs[_nextGameplay++ % _gameplayFrontPrefabs.Length];
                case ChunkRole.DecorFront:
                    return _decorFrontPrefabs[_nextDecor++ % _decorFrontPrefabs.Length];
                default:
                    return _sniperRearPrefabs[_nextSniper++ % _sniperRearPrefabs.Length];
            }
        }

        private ChunkSlot NextSlot(ChunkRole role)
        {
            if (role != ChunkRole.GameplayFront)
                return new ChunkSlot(role, _lastGameplayPosition);

            var maxHeight = Mathf.Max(0f, Mathf.Min(
                _tuning.MaxChunkHeightDelta,
                JumpReachability.MaxHeight(_tuning.JumpVelocity, Physics2D.gravity.magnitude) - 0.25f));
            _lastGameplayPosition += new Vector2(MaxGameplayGap, maxHeight);
            return new ChunkSlot(role, _lastGameplayPosition);
        }

        private void CleanupBehindPlayer()
        {
            for (var i = _spawned.Count - 1; i >= 0; i--)
            {
                var chunk = _spawned[i];
                if (chunk == null)
                {
                    _spawned.RemoveAt(i);
                    continue;
                }

                if (chunk.transform.position.x < _player.position.x && chunk.CanDespawn)
                {
                    _spawned.RemoveAt(i);
                    DestroyChunk(chunk);
                }
            }
        }

        private void ClearSpawned()
        {
            for (var i = 0; i < _spawned.Count; i++)
            {
                if (_spawned[i] != null)
                    DestroyChunk(_spawned[i]);
            }

            _spawned.Clear();
        }

        private static void DestroyChunk(Chunk chunk)
        {
            if (Application.isPlaying)
                Destroy(chunk.gameObject);
            else
                DestroyImmediate(chunk.gameObject);
        }
    }
}
