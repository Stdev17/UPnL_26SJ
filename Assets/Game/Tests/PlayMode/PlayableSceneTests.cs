using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UPnL.SignalRush.Combo;
using UPnL.SignalRush.Player;
using UPnL.SignalRush.Run;

namespace UPnL.SignalRush.Tests.PlayMode
{
    public sealed class PlayableSceneTests
    {
        [UnityTest]
        public IEnumerator SceneLoadsAndPlayerRunsAutomatically()
        {
            yield return LoadPlayableScene();
            var player = GameObject.Find("Player");
            var startX = player.transform.position.x;

            yield return new WaitForSeconds(0.25f);

            Assert.That(player.GetComponent<SignalRushPlayable>(), Is.Not.Null);
            Assert.That(player.transform.position.x, Is.GreaterThan(startX));
        }

        [UnityTest]
        public IEnumerator AttackBreaksAnOverlappingObstacle()
        {
            yield return LoadPlayableScene();
            var player = GameObject.Find("Player");
            var obstacle = GameObject.Find("Obstacles").transform.GetChild(0).gameObject;
            player.transform.position = obstacle.transform.position - new Vector3(0.9f, 0f, 0f);
            Physics2D.SyncTransforms();

            player.GetComponent<PlayerCombat>().RequestAttack();

            Assert.That(obstacle.activeSelf, Is.False);
            Assert.That(player.GetComponent<ComboCounter>().Current, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator FinishedRunReusesAttackToRestart()
        {
            yield return LoadPlayableScene();
            var player = GameObject.Find("Player");
            var run = Object.FindFirstObjectByType<RunController>();
            run.ReportGoalReached();

            yield return new WaitForFixedUpdate();
            Assert.That(run.Phase, Is.EqualTo(RunPhase.Finished));

            player.GetComponent<PlayerInput>().HandleAttack();

            Assert.That(run.Phase, Is.EqualTo(RunPhase.Running));
            Assert.That(player.transform.position.x, Is.LessThan(1f));
        }

        private static IEnumerator LoadPlayableScene()
        {
            yield return SceneManager.LoadSceneAsync("SCN_SignalRush_Playable", LoadSceneMode.Single);
            yield return null;
            Assert.That(GameObject.Find("Player"), Is.Not.Null);
        }
    }
}
