// EditMode tests for the shared case engine. Run in Unity: Window > General >
// Test Runner > EditMode. Also runs outside Unity: dotnet test tools/core-tests
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using HarborWatch.Core;
using NUnit.Framework;

namespace HarborWatch.Tests
{
    public class CaseEngineTests
    {
        static object vectors;

        static object Vectors()
        {
            if (vectors != null) return vectors;
            foreach (var start in new[] { Directory.GetCurrentDirectory(), TestContext.CurrentContext.TestDirectory })
            {
                var dir = new DirectoryInfo(start);
                for (int i = 0; dir != null && i < 8; i++, dir = dir.Parent)
                {
                    string path = Path.Combine(dir.FullName, "game", "vectors.json");
                    if (File.Exists(path)) return vectors = MiniJson.Parse(File.ReadAllText(path));
                }
            }
            Assert.Ignore("game/vectors.json not found above " + Directory.GetCurrentDirectory());
            return null;
        }

        static List<object> L(object o, string key) { return (List<object>)((Dictionary<string, object>)o)[key]; }
        static double D(object o) { return Convert.ToDouble(o, CultureInfo.InvariantCulture); }
        static double Round6(double v) { return Math.Floor(v * 1e6 + 0.5) / 1e6; }

        [Test]
        public void RngMatchesJavaScript()
        {
            foreach (var entry in L(Vectors(), "rng"))
            {
                var e = (Dictionary<string, object>)entry;
                var r = new CaseRng((uint)D(e["seed"]));
                foreach (var v in (List<object>)e["values"]) Assert.AreEqual(D(v), Round6(r.Next()), 1e-9);
            }
            Assert.AreEqual(979728268, (int)Math.Floor(new CaseRng(12345).Next() * 1e9 + 0.5));
        }

        static void AssertCase(Dictionary<string, object> e, CaseConfig cfg)
        {
            var k = CaseEngine.Generate((uint)D(e["seed"]), cfg);
            string label = (string)e["label"];
            Assert.AreEqual((int)D(e["target"]), k.Target, label);
            var counts = (List<object>)e["counts"];
            Assert.AreEqual(counts.Count, k.Counts.Length, label);
            for (int i = 0; i < counts.Count; i++) Assert.AreEqual((int)D(counts[i]), k.Counts[i], label);
            var clues = (List<object>)e["clues"];
            for (int i = 0; i < clues.Count; i++) Assert.AreEqual((string)clues[i], k.Clues[i].ToString(), label);
            var people = (List<object>)e["people"];
            Assert.AreEqual(people.Count, k.People.Count, label);
            for (int i = 0; i < people.Count; i++)
            {
                var row = (List<object>)people[i];
                var p = k.People[i];
                for (int a = 0; a < 7; a++) Assert.AreEqual((int)D(row[a]), p.Traits[a], label + " person " + i);
                Assert.AreEqual((int)D(row[7]), p.Headphones, label);
                Assert.AreEqual(D(row[8]), Round6(p.X), 1e-9, label);
                Assert.AreEqual(D(row[9]), Round6(p.Y), 1e-9, label);
                Assert.AreEqual((int)D(row[10]), p.Dir, label);
                Assert.AreEqual(D(row[11]), Round6(p.Jitter), 1e-9, label);
                Assert.AreEqual(D(row[12]), Round6(p.Period), 1e-9, label);
                Assert.AreEqual(D(row[13]), Round6(p.Offset), 1e-9, label);
            }
        }

        [Test]
        public void CampaignCasesMatchJavaScript()
        {
            var cases = L(Vectors(), "cases");
            Assert.AreEqual(CaseEngine.Cases.Length, cases.Count);
            for (int i = 0; i < cases.Count; i++)
            {
                var e = (Dictionary<string, object>)cases[i];
                StringAssert.EndsWith(CaseEngine.Cases[i].Name, (string)e["label"]);
                AssertCase(e, CaseEngine.Cases[i]);
            }
        }

        [Test]
        public void DailyCasesMatchJavaScript()
        {
            foreach (var entry in L(Vectors(), "daily"))
            {
                var e = (Dictionary<string, object>)entry;
                var date = (List<object>)e["date"];
                int y = (int)D(date[0]), m = (int)D(date[1]), d = (int)D(date[2]);
                Assert.AreEqual((int)D(e["number"]), CaseEngine.DailyNumber(y, m, d));
                var cfg = CaseEngine.DailyConfig(y, m, d);
                Assert.AreEqual((int)D(e["crowd"]), cfg.Crowd);
                Assert.AreEqual((int)D(e["district"]), cfg.District);
                Assert.AreEqual((uint)D(e["seed"]), CaseEngine.DayKey(y, m, d));
                AssertCase(e, cfg);
            }
        }

        [Test]
        public void EveryCaseHasExactlyOneCourier()
        {
            for (int c = 0; c < CaseEngine.Cases.Length; c++)
            {
                var cfg = CaseEngine.Cases[c];
                for (uint seed = 1; seed <= 300; seed++)
                {
                    var k = CaseEngine.Generate(seed * 7919u + (uint)c, cfg);
                    var all = CaseEngine.Candidates(k, cfg.Clues.Length);
                    Assert.AreEqual(1, all.Count, cfg.Name + " seed " + seed);
                    Assert.AreEqual(k.Target, all[0]);
                    Assert.AreEqual(0, k.Courier.Headphones);
                    Assert.GreaterOrEqual(CaseEngine.Candidates(k, 1).Count, 3, cfg.Name);
                }
            }
        }

        [Test]
        public void ScoringMatchesDesign()
        {
            Func<int, int, int, int, int, ScoreResult> s = (revealed, wrong, provoke, intel, secs) => CaseEngine.Score(new RunStats
            {
                Cleared = true, Total = 4, Revealed = revealed, SecondsLeft = secs, Provocations = provoke, WrongTags = wrong, IntelBought = intel
            });
            Assert.AreEqual(620, s(4, 0, 0, 0, 30).Score);
            Assert.AreEqual(1120, s(2, 0, 0, 0, 30).Score);
            Assert.AreEqual(3, s(2, 0, 0, 0, 30).Stars);
            Assert.AreEqual(2, s(4, 0, 0, 0, 30).Stars);
            Assert.AreEqual(1, s(4, 1, 0, 0, 30).Stars);
            Assert.AreEqual(248, s(4, 1, 0, 0, 30).Score);
            Assert.AreEqual(460, s(4, 0, 2, 1, 30).Score);
            Assert.AreEqual(50, s(4, 0, 30, 0, 0).Score);
            Assert.AreEqual(0, CaseEngine.Score(new RunStats()).Score);
        }

        [Test]
        public void ShareTextHasNoSpoilers()
        {
            var run = new RunStats { Cleared = true, Total = 4, Revealed = 2, Elapsed = 41.3, Provocations = 1, SecondsLeft = 40 };
            string text = CaseEngine.ShareText(9, run, CaseEngine.Score(run));
            StringAssert.Contains("Daily #9", text);
            StringAssert.Contains("0:41", text);
            StringAssert.Contains("🟥🟥⬛⬛ 2/4", text);
        }
    }

    /// <summary>Tiny JSON reader for test vectors (Unity has no JSON-to-dictionary parser built in).</summary>
    static class MiniJson
    {
        public static object Parse(string s) { int i = 0; return Value(s, ref i); }
        static void Ws(string s, ref int i) { while (i < s.Length && char.IsWhiteSpace(s[i])) i++; }
        static object Value(string s, ref int i)
        {
            Ws(s, ref i);
            char c = s[i];
            if (c == '{')
            {
                var d = new Dictionary<string, object>(); i++; Ws(s, ref i);
                if (s[i] == '}') { i++; return d; }
                while (true)
                {
                    Ws(s, ref i); string key = (string)Value(s, ref i); Ws(s, ref i); i++; // ':'
                    d[key] = Value(s, ref i); Ws(s, ref i);
                    if (s[i++] == '}') return d;
                }
            }
            if (c == '[')
            {
                var l = new List<object>(); i++; Ws(s, ref i);
                if (s[i] == ']') { i++; return l; }
                while (true) { l.Add(Value(s, ref i)); Ws(s, ref i); if (s[i++] == ']') return l; }
            }
            if (c == '"')
            {
                var sb = new System.Text.StringBuilder(); i++;
                while (s[i] != '"')
                {
                    if (s[i] == '\\')
                    {
                        i++;
                        if (s[i] == 'u') { sb.Append((char)Convert.ToInt32(s.Substring(i + 1, 4), 16)); i += 4; }
                        else sb.Append(s[i] == 'n' ? '\n' : s[i]);
                    }
                    else sb.Append(s[i]);
                    i++;
                }
                i++; return sb.ToString();
            }
            if (s.Substring(i).StartsWith("true")) { i += 4; return true; }
            if (s.Substring(i).StartsWith("false")) { i += 5; return false; }
            if (s.Substring(i).StartsWith("null")) { i += 4; return null; }
            int start = i;
            while (i < s.Length && "+-0123456789.eE".IndexOf(s[i]) >= 0) i++;
            return double.Parse(s.Substring(start, i - start), CultureInfo.InvariantCulture);
        }
    }
}
