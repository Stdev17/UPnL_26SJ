using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UPnL.SignalRush.Run;

namespace UPnL.SignalRush.Tests.Run
{
    public sealed class RunLifecycleTests
    {
        [Test]
        public void TickAccumulatesOnlyWhileRunning()
        {
            var controllerObject = new GameObject();
            var controller = controllerObject.AddComponent<RunController>();

            controller.Tick(1.25f);
            controller.BeginRespawn();
            controller.Tick(2f);
            controller.EndRespawn();
            controller.Tick(.75f);

            Assert.That(controller.Phase, Is.EqualTo(RunPhase.Running));
            Assert.That(controller.ElapsedSeconds, Is.EqualTo(2f));
            Assert.That(controller.Result, Is.Null);

            Object.DestroyImmediate(controllerObject);
        }

        [Test]
        public void ResolveFixedStepFinishesOnceWithGoalWinningSameStepTie()
        {
            var controllerObject = new GameObject();
            var controller = controllerObject.AddComponent<RunController>();
            var phases = new List<RunPhase>();
            var results = new List<RunResult>();
            controller.PhaseChanged += phases.Add;
            controller.RunFinished += results.Add;

            controller.ReportPlayerDead();
            controller.ReportGoalReached();
            controller.ResolveFixedStep();
            controller.ReportPlayerDead();
            controller.ResolveFixedStep();

            Assert.That(controller.Phase, Is.EqualTo(RunPhase.Finished));
            Assert.That(controller.Result, Is.EqualTo(RunResult.GoalReached));
            Assert.That(phases, Is.EqualTo(new[] { RunPhase.Finished }));
            Assert.That(results, Is.EqualTo(new[] { RunResult.GoalReached }));

            Object.DestroyImmediate(controllerObject);
        }

        [Test]
        public void RespawnTransitionsGateReportsWithoutFinishingTheRun()
        {
            var controllerObject = new GameObject();
            var controller = controllerObject.AddComponent<RunController>();
            var phases = new List<RunPhase>();
            var finishCount = 0;
            controller.PhaseChanged += phases.Add;
            controller.RunFinished += _ => finishCount++;

            controller.BeginRespawn();
            controller.ReportGoalReached();
            controller.ReportPlayerDead();
            controller.ResolveFixedStep();
            controller.EndRespawn();

            Assert.That(controller.Phase, Is.EqualTo(RunPhase.Running));
            Assert.That(controller.Result, Is.Null);
            Assert.That(phases, Is.EqualTo(new[] { RunPhase.Respawning, RunPhase.Running }));
            Assert.That(finishCount, Is.Zero);

            Object.DestroyImmediate(controllerObject);
        }

        [Test]
        public void RestartClearsTimePendingFinishAndResult()
        {
            var controllerObject = new GameObject();
            var controller = controllerObject.AddComponent<RunController>();

            controller.Tick(3f);
            controller.ReportPlayerDead();
            controller.Restart();
            controller.ResolveFixedStep();

            Assert.That(controller.Phase, Is.EqualTo(RunPhase.Running));
            Assert.That(controller.ElapsedSeconds, Is.Zero);
            Assert.That(controller.Result, Is.Null);

            Object.DestroyImmediate(controllerObject);
        }

        [Test]
        public void GoalTriggerEmitsOnceUntilReset()
        {
            var triggerObject = new GameObject();
            var trigger = triggerObject.AddComponent<GoalTrigger>();
            var reachedCount = 0;
            trigger.Reached += () => reachedCount++;

            Assert.That(trigger.TryReach(), Is.True);
            Assert.That(trigger.TryReach(), Is.False);
            trigger.ResetTrigger();
            Assert.That(trigger.TryReach(), Is.True);

            Assert.That(reachedCount, Is.EqualTo(2));

            Object.DestroyImmediate(triggerObject);
        }
    }
}
