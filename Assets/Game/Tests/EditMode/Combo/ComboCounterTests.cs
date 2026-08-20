using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UPnL.SignalRush.Combo;
using UPnL.SignalRush.Tuning;

namespace UPnL.SignalRush.Tests.Combo
{
    public sealed class ComboCounterTests
    {
        [Test]
        public void BreakAndParryIncreaseCurrentAndBestUpToTwenty()
        {
            var counter = CreateCounter(out var gameObject, out var tuning);

            counter.RecordBreak();
            counter.RecordParry();

            for (var index = 0; index < 20; index++)
            {
                counter.RecordBreak();
            }

            Assert.That(counter.Current, Is.EqualTo(20));
            Assert.That(counter.Best, Is.EqualTo(20));

            Destroy(gameObject, tuning);
        }

        [Test]
        public void CappedIncrementDoesNotChangeStateOrRaiseChanged()
        {
            var counter = CreateCounter(out var gameObject, out var tuning);
            var changes = 0;
            counter.Changed += (_, _, _, _) => changes++;

            for (var index = 0; index < 20; index++)
            {
                counter.RecordBreak();
            }

            counter.RecordParry();

            Assert.That(changes, Is.EqualTo(20));
            Assert.That(counter.Current, Is.EqualTo(20));

            Destroy(gameObject, tuning);
        }

        [Test]
        public void HitRecordsThePreHitComboAndResetsCurrentInOneChange()
        {
            var counter = CreateCounter(out var gameObject, out var tuning);
            counter.RecordBreak();
            counter.RecordParry();
            var changes = 0;
            var observedCurrent = -1;
            var observedBest = -1;
            var observedInterrupted = -1;
            counter.Changed += (current, best, interrupted, _) =>
            {
                changes++;
                observedCurrent = current;
                observedBest = best;
                observedInterrupted = interrupted;
            };

            counter.RecordHit();

            Assert.That(changes, Is.EqualTo(1));
            Assert.That(counter.Current, Is.Zero);
            Assert.That(counter.Best, Is.EqualTo(2));
            Assert.That(counter.Interrupted, Is.EqualTo(2));
            Assert.That(observedCurrent, Is.Zero);
            Assert.That(observedBest, Is.EqualTo(2));
            Assert.That(observedInterrupted, Is.EqualTo(2));

            Destroy(gameObject, tuning);
        }

        [Test]
        public void ZeroComboHitRaisesChangedWithZeroInterrupted()
        {
            var counter = CreateCounter(out var gameObject, out var tuning);
            var changes = 0;
            var current = -1;
            var interrupted = -1;
            counter.Changed += (eventCurrent, _, eventInterrupted, _) =>
            {
                changes++;
                current = eventCurrent;
                interrupted = eventInterrupted;
            };

            counter.RecordHit();

            Assert.That(changes, Is.EqualTo(1));
            Assert.That(counter.Current, Is.Zero);
            Assert.That(counter.Interrupted, Is.Zero);
            Assert.That(current, Is.Zero);
            Assert.That(interrupted, Is.Zero);

            Destroy(gameObject, tuning);
        }

        [Test]
        public void SpeedMultiplierLinearlyMapsZeroAndTwentyComboToTuningSpeeds()
        {
            var counter = CreateCounter(out var gameObject, out var tuning);

            Assert.That(counter.SpeedMultiplier, Is.EqualTo(1f));

            for (var index = 0; index < 10; index++)
            {
                counter.RecordBreak();
            }

            Assert.That(counter.SpeedMultiplier, Is.EqualTo(4f / 3f).Within(0.0001f));

            for (var index = 0; index < 10; index++)
            {
                counter.RecordBreak();
            }

            Assert.That(counter.SpeedMultiplier, Is.EqualTo(10f / 6f).Within(0.0001f));

            Destroy(gameObject, tuning);
        }

        private static ComboCounter CreateCounter(out GameObject gameObject, out SignalRushTuning tuning)
        {
            gameObject = new GameObject();
            var counter = gameObject.AddComponent<ComboCounter>();
            tuning = ScriptableObject.CreateInstance<SignalRushTuning>();
            var serialized = new SerializedObject(counter);
            serialized.FindProperty("_tuning").objectReferenceValue = tuning;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return counter;
        }

        private static void Destroy(GameObject gameObject, SignalRushTuning tuning)
        {
            Object.DestroyImmediate(gameObject);
            Object.DestroyImmediate(tuning);
        }
    }
}
