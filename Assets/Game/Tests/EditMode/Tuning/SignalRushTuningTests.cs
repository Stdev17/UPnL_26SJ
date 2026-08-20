using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UPnL.SignalRush.Tuning;

namespace UPnL.SignalRush.Tests.Tuning
{
    public sealed class SignalRushTuningTests
    {
        [Test]
        public void DefaultsMatchApprovedContract()
        {
            var tuning = ScriptableObject.CreateInstance<SignalRushTuning>();

            Assert.That(tuning.PixelsPerUnit, Is.EqualTo(32));
            Assert.That(tuning.BaseRunSpeed, Is.EqualTo(6f));
            Assert.That(tuning.MaxRunSpeed, Is.EqualTo(10f));
            Assert.That(tuning.HorizontalCorrectionSpeed, Is.EqualTo(3f));
            Assert.That(tuning.JumpVelocity, Is.EqualTo(8f));
            Assert.That(tuning.FallGravityMultiplier, Is.EqualTo(2f));
            Assert.That(tuning.AttackWindowSeconds, Is.EqualTo(0.15f));
            Assert.That(tuning.HitLockSeconds, Is.EqualTo(0.25f));
            Assert.That(tuning.InvulnerabilitySeconds, Is.EqualTo(1f));
            Assert.That(tuning.TargetRunSeconds, Is.EqualTo(75f));
            Assert.That(tuning.RespawnLockSeconds, Is.EqualTo(1f));
            Assert.That(tuning.ProjectileSpeed, Is.EqualTo(18f));
            Assert.That(tuning.SpawnAheadChunkCount, Is.EqualTo(2));
            Assert.That(tuning.MaxChunkHeightDelta, Is.EqualTo(1.5f));
            Assert.That(tuning.MaxChunkGap, Is.EqualTo(2f));
            Assert.That(tuning.SniperWarningSeconds, Is.EqualTo(0.8f));

            Object.DestroyImmediate(tuning);
        }

        [Test]
        public void OnValidateNormalizesInvalidScalarValues()
        {
            var tuning = ScriptableObject.CreateInstance<SignalRushTuning>();
            var serialized = new SerializedObject(tuning);
            serialized.FindProperty("_pixelsPerUnit").intValue = 0;
            serialized.FindProperty("_baseRunSpeed").floatValue = 0f;
            serialized.FindProperty("_maxRunSpeed").floatValue = 0f;
            serialized.FindProperty("_horizontalCorrectionSpeed").floatValue = 0f;
            serialized.FindProperty("_jumpVelocity").floatValue = 0f;
            serialized.FindProperty("_fallGravityMultiplier").floatValue = 0f;
            serialized.FindProperty("_attackWindowSeconds").floatValue = 0f;
            serialized.FindProperty("_hitLockSeconds").floatValue = 0f;
            serialized.FindProperty("_invulnerabilitySeconds").floatValue = 0f;
            serialized.FindProperty("_targetRunSeconds").floatValue = 0f;
            serialized.FindProperty("_respawnLockSeconds").floatValue = 0f;
            serialized.FindProperty("_projectileSpeed").floatValue = 0f;
            serialized.FindProperty("_spawnAheadChunkCount").intValue = 0;
            serialized.FindProperty("_maxChunkHeightDelta").floatValue = -1f;
            serialized.FindProperty("_maxChunkGap").floatValue = -1f;
            serialized.FindProperty("_sniperWarningSeconds").floatValue = 0f;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            Assert.That(tuning.PixelsPerUnit, Is.EqualTo(1));
            Assert.That(tuning.BaseRunSpeed, Is.GreaterThan(0f));
            Assert.That(tuning.MaxRunSpeed, Is.GreaterThan(tuning.BaseRunSpeed));
            Assert.That(tuning.HorizontalCorrectionSpeed, Is.GreaterThan(0f));
            Assert.That(tuning.JumpVelocity, Is.GreaterThan(0f));
            Assert.That(tuning.FallGravityMultiplier, Is.GreaterThanOrEqualTo(1f));
            Assert.That(tuning.AttackWindowSeconds, Is.GreaterThan(0f));
            Assert.That(tuning.HitLockSeconds, Is.GreaterThan(0f));
            Assert.That(tuning.InvulnerabilitySeconds, Is.GreaterThanOrEqualTo(tuning.HitLockSeconds));
            Assert.That(tuning.TargetRunSeconds, Is.GreaterThan(0f));
            Assert.That(tuning.RespawnLockSeconds, Is.GreaterThan(0f));
            Assert.That(tuning.ProjectileSpeed, Is.GreaterThan(0f));
            Assert.That(tuning.SpawnAheadChunkCount, Is.EqualTo(1));
            Assert.That(tuning.MaxChunkHeightDelta, Is.Zero);
            Assert.That(tuning.MaxChunkGap, Is.Zero);
            Assert.That(tuning.SniperWarningSeconds, Is.GreaterThan(0f));

            Object.DestroyImmediate(tuning);
        }
    }
}
