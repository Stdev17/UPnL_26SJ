using System;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UPnL.SignalRush.Combo;
using UPnL.SignalRush.Run;
using UPnL.SignalRush.UI;
using Object = UnityEngine.Object;

namespace UPnL.SignalRush.Tests.UI
{
    public sealed class RunViewTests
    {
        [Test]
        public void HudRendersCurrentBestAndElapsedFromOwners()
        {
            var root = new GameObject();
            root.SetActive(false);
            var counter = root.AddComponent<ComboCounter>();
            var run = root.AddComponent<RunController>();
            var hud = root.AddComponent<RunHud>();
            var comboText = CreateText("ComboText");
            var elapsedText = CreateText("ElapsedText");
            Assign(hud, "_comboText", comboText);
            Assign(hud, "_elapsedText", elapsedText);
            Assign(hud, "_combo", counter);
            Assign(hud, "_runController", run);
            root.SetActive(true);
            Invoke(hud, "OnEnable");

            counter.RecordBreak();
            counter.RecordParry();
            run.Tick(1.25f);
            Invoke(hud, "Update");

            Assert.That(Text(comboText), Is.EqualTo("Combo 2  Best 2"));
            Assert.That(Text(elapsedText), Is.EqualTo("Time 1.3"));

            Object.DestroyImmediate(root);
            Object.DestroyImmediate(comboText.gameObject);
            Object.DestroyImmediate(elapsedText.gameObject);
        }

        [Test]
        public void ResultShowsEachFinishAndHidesOnRestart()
        {
            var root = new GameObject();
            root.SetActive(false);
            var run = root.AddComponent<RunController>();
            var view = root.AddComponent<ResultView>();
            var resultRoot = new GameObject("ResultRoot");
            var resultText = CreateText("ResultText");
            Assign(view, "_resultRoot", resultRoot);
            Assign(view, "_resultText", resultText);
            Assign(view, "_runController", run);
            root.SetActive(true);
            Invoke(view, "OnEnable");

            Assert.That(resultRoot.activeSelf, Is.False);

            run.ReportGoalReached();
            run.ResolveFixedStep();
            Assert.That(resultRoot.activeSelf, Is.True);
            Assert.That(Text(resultText), Is.EqualTo("GoalReached"));

            run.Restart();
            Assert.That(resultRoot.activeSelf, Is.False);

            run.ReportPlayerDead();
            run.ResolveFixedStep();
            Assert.That(resultRoot.activeSelf, Is.True);
            Assert.That(Text(resultText), Is.EqualTo("Dead"));

            Object.DestroyImmediate(root);
            Object.DestroyImmediate(resultRoot);
            Object.DestroyImmediate(resultText.gameObject);
        }

        private static Component CreateText(string name)
        {
            var textType = Type.GetType("UnityEngine.UI.Text, UnityEngine.UI");
            Assert.That(textType, Is.Not.Null);
            return new GameObject(name).AddComponent(textType);
        }

        private static string Text(Component text)
        {
            return (string)text.GetType().GetProperty("text").GetValue(text);
        }

        private static void Assign(Object target, string propertyName, Object value)
        {
            var serialized = new SerializedObject(target);
            serialized.FindProperty(propertyName).objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void Invoke(object target, string methodName)
        {
            target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(target, null);
        }
    }
}
