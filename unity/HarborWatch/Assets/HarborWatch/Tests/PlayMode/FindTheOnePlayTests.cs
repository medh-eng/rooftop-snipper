using System;
using System.Collections;
using System.Reflection;
using HarborWatch.Core;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace HarborWatch.Tests
{
    /// <summary>
    /// Plays the real FindTheOne scene: every campaign case must start and run, and the Echo
    /// lens must replay and restore. It drives the private controller through reflection
    /// because the controller lives in Assembly-CSharp, which test assemblies cannot reference.
    ///
    /// Run from the Editor (Window > General > Test Runner > PlayMode) or from a terminal:
    ///   Unity -batchmode -projectPath unity/HarborWatch -runTests -testPlatform PlayMode \
    ///         -testResults results.xml -logFile -
    /// (the Editor must be closed for the batch run: a project can only be open once).
    /// </summary>
    public class FindTheOnePlayTests
    {
        const BindingFlags Any = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
        Component controller;

        static object Get(object o, string field) { return o.GetType().GetField(field, Any).GetValue(o); }
        static void Call(object o, string method, params object[] args) { o.GetType().GetMethod(method, Any).Invoke(o, args); }

        IEnumerator Load()
        {
            yield return SceneManager.LoadSceneAsync("FindTheOne", LoadSceneMode.Single);
            yield return null; yield return null;   // Start() of the review and the controller
            var type = Type.GetType("HarborWatch.FindTheOneController, Assembly-CSharp");
            Assert.NotNull(type, "FindTheOneController type not found (compile error?)");
            controller = UnityEngine.Object.FindFirstObjectByType(type) as Component;
            Assert.NotNull(controller, "No FindTheOneController in the FindTheOne scene: is findTheOneMode ticked on HarborCrowdReview?");
            Assert.AreEqual("", (string)Get(controller, "errorText"), "The menu reports an error on load");
        }

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            LogAssert.ignoreFailingMessages = false;
            yield return Load();
        }

        [UnityTest]
        public IEnumerator EveryCampaignCaseStartsAndPlays()
        {
            for (int i = 0; i < CaseEngine.Cases.Length; i++)
            {
                Call(controller, "StartCase", i);
                Assert.AreEqual("", (string)Get(controller, "errorText"), "Case " + (i + 1) + " failed to start");
                Assert.AreEqual("Play", Get(controller, "page").ToString(), "Case " + (i + 1) + " is not in Play");
                float before = (float)Get(controller, "timeLeft");
                yield return new WaitForSeconds(.6f);
                Assert.Less((float)Get(controller, "timeLeft"), before, "Case " + (i + 1) + ": the clock is not running");
                Assert.AreEqual("", (string)Get(controller, "errorText"), "Case " + (i + 1) + " hit a runtime error");
            }
        }

        [UnityTest]
        public IEnumerator DailyCaseStartsAndPlays()
        {
            Call(controller, "StartDaily");
            Assert.AreEqual("", (string)Get(controller, "errorText"));
            Assert.AreEqual("Play", Get(controller, "page").ToString());
            yield return new WaitForSeconds(.5f);
            Assert.AreEqual("", (string)Get(controller, "errorText"));
        }

        [UnityTest]
        public IEnumerator EchoReplaysAndRestoresTheCrowd()
        {
            Call(controller, "StartCase", 3);   // case 4: first case with Echo
            Assert.IsTrue((bool)Get(controller, "echoOK"), "Echo should be available on case 4");
            yield return new WaitForSeconds(3f);

            float left = (float)Get(controller, "timeLeft");
            var crowd = Get(controller, "crowd");
            var walkers = (System.Collections.IList)crowd.GetType().GetField("Walkers", Any).GetValue(crowd);
            Vector3[] live = new Vector3[walkers.Count];
            for (int i = 0; i < live.Length; i++) live[i] = ((Component)walkers[i].GetType().GetField("Anchor", Any).GetValue(walkers[i])).transform.position;

            Call(controller, "StartEcho");
            Assert.IsTrue((bool)Get(controller, "echoing"), "Echo did not start");
            Assert.AreEqual(left - 6f, (float)Get(controller, "timeLeft"), .3f, "Echo should cost 6 s");
            yield return new WaitForSeconds(.5f);
            bool moved = false;
            for (int i = 0; i < live.Length; i++)
            {
                var anchor = ((Component)walkers[i].GetType().GetField("Anchor", Any).GetValue(walkers[i])).transform;
                if ((anchor.position - live[i]).sqrMagnitude > .25f) moved = true;
            }
            Assert.IsTrue(moved, "During the Echo people should be at earlier positions");

            Call(controller, "StopEcho");
            Assert.IsFalse((bool)Get(controller, "echoing"));
            for (int i = 0; i < live.Length; i++)
            {
                var anchor = ((Component)walkers[i].GetType().GetField("Anchor", Any).GetValue(walkers[i])).transform;
                Assert.Less((anchor.position - live[i]).magnitude, 1.5f, "Person " + i + " was not restored to the live position");
            }

            Call(controller, "StartEcho");   // once per case
            Assert.IsFalse((bool)Get(controller, "echoing"), "A second Echo must be refused");
        }
    }
}
