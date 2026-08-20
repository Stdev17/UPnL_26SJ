using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UPnL.SignalRush.Combat;

namespace UPnL.SignalRush.Tests.Combat
{
    public sealed class CombatTargetTests
    {
        [Test]
        public void TryBreakResolvesOnceAndEmitsBrokenOnce()
        {
            var targetObject = new GameObject();
            var target = targetObject.AddComponent<BreakableObstacle>();
            var brokenCount = 0;
            BreakableObstacle brokenTarget = null;
            target.Broken += obstacle =>
            {
                brokenCount++;
                brokenTarget = obstacle;
            };

            Assert.That(target.TryBreak(), Is.True);
            Assert.That(target.TryBreak(), Is.False);
            Assert.That(target.IsBroken, Is.True);
            Assert.That(brokenCount, Is.EqualTo(1));
            Assert.That(brokenTarget, Is.SameAs(target));

            Object.DestroyImmediate(targetObject);
        }

        [Test]
        public void TryHitPlayerWinsOverLaterParryAndEmitsOnlyHitPlayer()
        {
            var targetObject = new GameObject();
            var target = targetObject.AddComponent<Projectile>();
            var hitCount = 0;
            var parryCount = 0;
            target.HitPlayer += projectile => hitCount++;
            target.Parried += projectile => parryCount++;

            Assert.That(target.TryHitPlayer(), Is.True);
            Assert.That(target.TryParry(), Is.False);
            Assert.That(target.IsResolved, Is.True);
            Assert.That(hitCount, Is.EqualTo(1));
            Assert.That(parryCount, Is.Zero);

            Object.DestroyImmediate(targetObject);
        }

        [Test]
        public void TryParryWinsOverLaterHitPlayerAndEmitsOnlyParried()
        {
            var targetObject = new GameObject();
            var target = targetObject.AddComponent<Projectile>();
            var hitCount = 0;
            var parryCount = 0;
            target.HitPlayer += projectile => hitCount++;
            target.Parried += projectile => parryCount++;

            Assert.That(target.TryParry(), Is.True);
            Assert.That(target.TryHitPlayer(), Is.False);
            Assert.That(target.IsResolved, Is.True);
            Assert.That(hitCount, Is.Zero);
            Assert.That(parryCount, Is.EqualTo(1));

            Object.DestroyImmediate(targetObject);
        }

        [Test]
        public void TryResolveMissedResolvesOnceAndEmitsMissedOnce()
        {
            var targetObject = new GameObject();
            var target = targetObject.AddComponent<Projectile>();
            var missedCount = 0;
            target.Missed += projectile => missedCount++;

            Assert.That(target.TryResolveMissed(), Is.True);
            Assert.That(target.TryResolveMissed(), Is.False);
            Assert.That(target.TryParry(), Is.False);
            Assert.That(target.IsResolved, Is.True);
            Assert.That(missedCount, Is.EqualTo(1));

            Object.DestroyImmediate(targetObject);
        }

        [Test]
        public void BecomingInvisibleResolvesProjectileAsMissed()
        {
            var targetObject = new GameObject();
            var target = targetObject.AddComponent<Projectile>();
            var missedCount = 0;
            target.Missed += projectile => missedCount++;

            typeof(Projectile).GetMethod("OnBecameInvisible", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(target, null);

            Assert.That(target.IsResolved, Is.True);
            Assert.That(missedCount, Is.EqualTo(1));
            Object.DestroyImmediate(targetObject);
        }
    }
}
