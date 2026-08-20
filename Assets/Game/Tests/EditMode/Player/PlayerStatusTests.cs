using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UPnL.SignalRush.Player;
using UPnL.SignalRush.Tuning;

namespace UPnL.SignalRush.Tests.Player
{
    public sealed class PlayerStatusTests
    {
        [Test]
        public void DamageEntersHitOnceAndRecoversBeforeInvulnerabilityEnds()
        {
            var (status, gameObject, tuning) = CreateStatus();
            var hits = 0;
            status.Hit += _ => hits++;

            status.RequestDamage(DamageCause.Projectile);
            status.RequestDamage(DamageCause.Projectile);

            Assert.That(status.State, Is.EqualTo(PlayerState.Hit));
            Assert.That(status.IsControlLocked, Is.True);
            Assert.That(status.IsInvulnerable, Is.True);
            Assert.That(hits, Is.EqualTo(1));

            status.Tick(tuning.HitLockSeconds);

            Assert.That(status.State, Is.EqualTo(PlayerState.Active));
            Assert.That(status.IsControlLocked, Is.False);
            Assert.That(status.IsInvulnerable, Is.True);

            Object.DestroyImmediate(gameObject);
            Object.DestroyImmediate(tuning);
        }

        [Test]
        public void DamageIsAcceptedAgainAfterInvulnerabilityExpires()
        {
            var (status, gameObject, tuning) = CreateStatus();
            var hits = 0;
            status.Hit += _ => hits++;

            status.RequestDamage(DamageCause.Projectile);
            status.Tick(tuning.InvulnerabilitySeconds);
            status.RequestDamage(DamageCause.Projectile);

            Assert.That(hits, Is.EqualTo(2));
            Assert.That(status.State, Is.EqualTo(PlayerState.Hit));

            Object.DestroyImmediate(gameObject);
            Object.DestroyImmediate(tuning);
        }

        [Test]
        public void RespawnLocksControlsThenReturnsToActive()
        {
            var (status, gameObject, tuning) = CreateStatus();

            status.RequestRespawn();

            Assert.That(status.State, Is.EqualTo(PlayerState.Respawning));
            Assert.That(status.IsControlLocked, Is.True);

            status.Tick(tuning.RespawnLockSeconds);

            Assert.That(status.State, Is.EqualTo(PlayerState.Active));
            Assert.That(status.IsControlLocked, Is.False);

            Object.DestroyImmediate(gameObject);
            Object.DestroyImmediate(tuning);
        }

        [Test]
        public void RepeatedRespawnDoesNotExtendTheCurrentRespawnLock()
        {
            var (status, gameObject, tuning) = CreateStatus();

            status.RequestRespawn();
            status.Tick(tuning.RespawnLockSeconds / 2f);
            status.RequestRespawn();
            status.Tick(tuning.RespawnLockSeconds / 2f);

            Assert.That(status.State, Is.EqualTo(PlayerState.Active));

            Object.DestroyImmediate(gameObject);
            Object.DestroyImmediate(tuning);
        }

        [Test]
        public void DeadIgnoresDamageAndRespawnUntilReset()
        {
            var (status, gameObject, tuning) = CreateStatus();
            var changes = 0;
            status.StateChanged += _ => changes++;

            status.MarkDead();
            status.RequestDamage(DamageCause.Projectile);
            status.RequestRespawn();

            Assert.That(status.State, Is.EqualTo(PlayerState.Dead));
            Assert.That(status.IsControlLocked, Is.True);
            Assert.That(changes, Is.EqualTo(1));

            status.ResetStatus();

            Assert.That(status.State, Is.EqualTo(PlayerState.Active));
            Assert.That(status.IsControlLocked, Is.False);
            Assert.That(status.IsInvulnerable, Is.False);

            Object.DestroyImmediate(gameObject);
            Object.DestroyImmediate(tuning);
        }

        private static (PlayerStatus status, GameObject gameObject, SignalRushTuning tuning) CreateStatus()
        {
            var tuning = ScriptableObject.CreateInstance<SignalRushTuning>();
            var gameObject = new GameObject("PlayerStatusTests");
            var status = gameObject.AddComponent<PlayerStatus>();
            var serialized = new SerializedObject(status);
            serialized.FindProperty("_tuning").objectReferenceValue = tuning;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return (status, gameObject, tuning);
        }
    }
}
