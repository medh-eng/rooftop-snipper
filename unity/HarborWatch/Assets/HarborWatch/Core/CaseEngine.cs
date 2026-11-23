// RED SIGNAL · FIND THE ONE — shared case engine (C# port).
//
// Line-for-line port of game/case-engine.js. Pure C#: no UnityEngine, no
// System.Random, no clock. Same seed => same crowd, courier and dossier as the
// browser build. tests (CaseEngineTests) check this against game/vectors.json.
using System;
using System.Collections.Generic;

namespace HarborWatch.Core
{
    public enum Attr { Outfit = 0, Headwear = 1, Carry = 2, Pace = 3, Habit = 4, Calm = 5, Cell = 6 }

    /// <summary>mulberry32, bit-identical to the JS version.</summary>
    public sealed class CaseRng
    {
        uint a;
        public CaseRng(uint seed) { a = seed; }

        public double Next()
        {
            unchecked
            {
                a += 0x6D2B79F5u;
                uint t = a;
                t = (t ^ (t >> 15)) * (t | 1u);
                t ^= t + (t ^ (t >> 7)) * (t | 61u);
                return (t ^ (t >> 14)) / 4294967296.0;
            }
        }

        public int Int(int n) { return (int)Math.Floor(Next() * n); }
    }

    public sealed class Person
    {
        public int Id;
        public readonly int[] Traits = new int[CaseEngine.AttrCount];
        public int Headphones;
        public int Depth;
        public double X, Y, Jitter, Period, Offset;
        public int Dir;

        public int this[Attr attr] { get { return Traits[(int)attr]; } set { Traits[(int)attr] = value; } }
        public int Outfit { get { return this[Attr.Outfit]; } }
        public int Headwear { get { return this[Attr.Headwear]; } }
        public int Carry { get { return this[Attr.Carry]; } }
        public int Pace { get { return this[Attr.Pace]; } }
        public int Habit { get { return this[Attr.Habit]; } }
        public bool Calm { get { return this[Attr.Calm] == 1; } }
        public bool Cell { get { return this[Attr.Cell] == 1; } }
    }

    public struct Clue
    {
        public Attr Attr;
        public int Value;
        public Clue(Attr attr, int value) { Attr = attr; Value = value; }
        public override string ToString() { return CaseEngine.AttrName(Attr) + "=" + Value; }
    }

    public sealed class CaseConfig
    {
        public string Name, Brief;
        public int District, Crowd, Near;
        public Attr[] Clues;
        public float Reveal, Time, Heat;
        public bool Noise, Ping, Hunter;
    }

    public sealed class District
    {
        public string Id, Name, Noise;
        public double YMin, YMax;
    }

    public sealed class Case
    {
        public uint Seed;
        public int District;
        public List<Person> People = new List<Person>();
        public int Target;
        public Clue[] Clues;
        public int[] Counts;
        public Person Courier { get { return People[Target]; } }
    }

    public struct RunStats
    {
        public bool Cleared;
        public int Total, Revealed, Provocations, WrongTags, IntelBought;
        public double SecondsLeft, Elapsed;
    }

    public struct ScoreResult
    {
        public int Score, Stars, Base, Early, Time, Provoke, Intel, Penalty;
    }

    public static class CaseEngine
    {
        public const int AttrCount = 7;
        static readonly string[] Names = { "outfit", "headwear", "carry", "pace", "habit", "calm", "cell" };
        public static string AttrName(Attr a) { return Names[(int)a]; }
        public static Attr ParseAttr(string name) { return (Attr)Array.IndexOf(Names, name); }

        // JS Math.round (half up), not banker's rounding.
        public static int JsRound(double v) { return (int)Math.Floor(v + 0.5); }

        public static readonly int[][] Weights =
        {
            new[] { 25, 25, 25, 25 },
            new[] { 45, 11, 11, 11, 11, 11 },
            new[] { 50, 30, 20 },
            new[] { 20, 60, 20 },
            new[] { 40, 25, 20, 15 },
            new[] { 88, 12 },
            new[] { 1, 0 }
        };

        public static readonly int[][] ClueExclude =
        {
            new int[0], new int[0], new[] { 0 }, new[] { 1 }, new[] { 0 }, new[] { 0 }, new[] { 0 }
        };

        /// <summary>Provocation needed to check this clue in the field, or null.</summary>
        public static string Needs(Attr a)
        {
            if (a == Attr.Calm) return "noise";
            if (a == Attr.Cell) return "ping";
            return null;
        }

        public static readonly District[] Districts =
        {
            new District { Id = "meridian", Name = "Port Meridian", Noise = "Ferry horn", YMin = .60, YMax = .89 },
            new District { Id = "vesper", Name = "Vesper City", Noise = "Neon surge", YMin = .60, YMax = .90 },
            new District { Id = "solara", Name = "Solara", Noise = "Church bell", YMin = .60, YMax = .90 }
        };

        static CaseConfig C(string name, int district, int crowd, string clues, float reveal, float time,
            int near, int noise, int ping, int hunter, float heat, string brief)
        {
            var parts = clues.Split(',');
            var attrs = new Attr[parts.Length];
            for (int i = 0; i < parts.Length; i++) attrs[i] = ParseAttr(parts[i]);
            return new CaseConfig
            {
                Name = name, District = district, Crowd = crowd, Clues = attrs, Reveal = reveal, Time = time,
                Near = near, Noise = noise == 1, Ping = ping == 1, Hunter = hunter == 1, Heat = heat, Brief = brief
            };
        }

        // Keep in step with CASES in game/case-engine.js (checked by tests).
        public static readonly CaseConfig[] Cases =
        {
            C("First Light", 0, 10, "outfit,carry", 9, 70, 1, 0, 0, 0, 30,
              "A courier is carrying a Red Signal drive across the ferry concourse. Read the dossier. Find the one person who matches every line."),
            C("Ferry Rush", 0, 13, "carry,outfit,habit", 11, 75, 1, 0, 0, 0, 24,
              "Habits give people away. Watch how they move, not only what they wear."),
            C("Horn Blast", 0, 15, "outfit,habit,calm", 12, 80, 2, 1, 0, 0, 22,
              "Our courier is trained not to flinch. Sound the ferry horn: everyone turns to look, except the one we want. Watch for headphones."),
            C("Last Sailing", 0, 17, "headwear,carry,pace,outfit", 12, 85, 2, 1, 0, 0, 20,
              "Four lines of intel. Tag early for a bigger bonus, or wait for certainty."),
            C("Neon Static", 1, 16, "outfit,cell,habit", 13, 85, 2, 0, 1, 0, 20,
              "We cracked the cell radio. Send a ping and every Red Signal member touches an earpiece. Only one of them is the courier."),
            C("Night Market", 1, 19, "carry,headwear,cell,outfit", 13, 90, 2, 1, 1, 0, 18,
              "The market is packed. Use both provocations wisely: each one costs points."),
            C("Rooftop Glint", 1, 18, "habit,outfit,calm,carry", 13, 90, 2, 1, 0, 1, 16,
              "Red Signal posted a counter-sniper. If you see a glint on a rooftop, tap it before they line you up."),
            C("Blackout", 1, 20, "pace,outfit,carry,cell,habit", 12, 95, 3, 1, 1, 1, 16,
              "Five lines, a crowded square and a counter-sniper. Trust the dossier."),
            C("Bell Tower", 2, 20, "headwear,habit,calm,outfit", 13, 90, 2, 1, 1, 0, 16,
              "Ring the church bell. Pigeons scatter, people look up. The courier keeps walking."),
            C("Market Day", 2, 22, "outfit,carry,habit,cell,headwear", 12, 95, 3, 1, 1, 1, 14,
              "The busiest market of the year. Several cell members are working the square."),
            C("Procession", 2, 24, "carry,pace,outfit,calm,habit", 12, 95, 3, 1, 1, 1, 14,
              "A festival procession floods the square. Keep your lens cool and your shot clean."),
            C("Last Signal", 2, 26, "outfit,headwear,cell,carry,calm,habit", 11, 100, 3, 1, 1, 1, 12,
              "The Red Signal leader is moving the master drive. Six lines of intel. One shot that matters.")
        };

        static int Pick(CaseRng r, int[] weights, int[] exclude)
        {
            int total = 0;
            for (int v = 0; v < weights.Length; v++) if (exclude == null || Array.IndexOf(exclude, v) < 0) total += weights[v];
            double roll = r.Next() * total;
            for (int v = 0; v < weights.Length; v++)
            {
                if (exclude != null && Array.IndexOf(exclude, v) >= 0) continue;
                roll -= weights[v];
                if (roll < 0) return v;
            }
            for (int v = weights.Length - 1; v >= 0; v--) if (exclude == null || Array.IndexOf(exclude, v) < 0) return v;
            return 0;
        }

        public static int[] PrefixCounts(int civilians, int k, int near)
        {
            var m = new List<int> { civilians };
            if (k == 1) return m.ToArray();
            int first = Math.Max(near, JsRound(civilians * 0.45));
            if (k == 2) { m.Add(first); return m.ToArray(); }
            for (int j = 1; j < k; j++)
            {
                if (j == k - 1) { m.Add(Math.Min(near, m[j - 1])); break; }
                double t = (j - 1) / (double)Math.Max(1, k - 2);
                int v = JsRound(first * Math.Pow(near / (double)first, t));
                m.Add(Math.Max(near, Math.Min(m[j - 1], v)));
            }
            return m.ToArray();
        }

        /// <summary>Exactly one person (Courier) matches every clue.</summary>
        public static Case Generate(uint seed, CaseConfig config)
        {
            var r = new CaseRng(seed);
            int k = config.Clues.Length;
            int civilians = config.Crowd - 1;
            var order = config.Clues;

            var courier = new Person();
            for (int ai = 0; ai < AttrCount; ai++)
            {
                var attr = (Attr)ai;
                bool inClues = Array.IndexOf(order, attr) >= 0;
                if (attr == Attr.Calm || attr == Attr.Cell) courier[attr] = inClues ? 1 : 0;
                else courier[attr] = Pick(r, Weights[ai], inClues ? ClueExclude[ai] : null);
            }

            int[] m = PrefixCounts(civilians, k, config.Near);
            var depths = new List<int>();
            for (int d = 0; d < k; d++)
            {
                int count = d == k - 1 ? m[k - 1] : m[d] - m[d + 1];
                for (int i = 0; i < count; i++) depths.Add(d);
            }

            var people = new List<Person>();
            foreach (int depth in depths)
            {
                var p = new Person();
                for (int ai = 0; ai < AttrCount; ai++)
                {
                    var attr = (Attr)ai;
                    int ci = Array.IndexOf(order, attr);
                    if (ci >= 0 && ci < depth) p[attr] = courier[attr];
                    else if (ci == depth) p[attr] = Pick(r, Weights[ai], new[] { courier[attr] });
                    else p[attr] = Pick(r, Weights[ai], null);
                }
                p.Headphones = p[Attr.Calm] == 1 ? 1 : 0;
                p.Depth = depth;
                people.Add(p);
            }
            courier.Headphones = 0;
            courier.Depth = k;
            people.Add(courier);

            var dist = Districts[config.District];
            foreach (var p in people)
            {
                p.Y = dist.YMin + r.Next() * (dist.YMax - dist.YMin);
                p.X = 90 + r.Next() * 1420;
                p.Dir = r.Next() < 0.5 ? -1 : 1;
                p.Jitter = 0.85 + r.Next() * 0.3;
                p.Period = 5.5 + r.Next() * 3;
                p.Offset = r.Next() * p.Period;
            }

            for (int i = people.Count - 1; i > 0; i--)
            {
                int j = r.Int(i + 1);
                var t = people[i]; people[i] = people[j]; people[j] = t;
            }
            int target = -1;
            for (int i = 0; i < people.Count; i++) { people[i].Id = i; if (people[i].Depth == k) target = i; }

            var clues = new Clue[k];
            for (int i = 0; i < k; i++) clues[i] = new Clue(order[i], courier[order[i]]);
            return new Case { Seed = seed, District = config.District, People = people, Target = target, Clues = clues, Counts = m };
        }

        public static bool Matches(Person p, Clue c) { return p[c.Attr] == c.Value; }

        public static List<int> Candidates(Case kase, int revealed)
        {
            var output = new List<int>();
            foreach (var p in kase.People)
            {
                bool ok = true;
                for (int i = 0; i < revealed && ok; i++) ok = Matches(p, kase.Clues[i]);
                if (ok) output.Add(p.Id);
            }
            return output;
        }

        public static ScoreResult Score(RunStats run)
        {
            if (!run.Cleared) return new ScoreResult();
            const int baseScore = 500;
            int early = JsRound(baseScore * 0.5 * Math.Max(0, run.Total - run.Revealed));
            int time = Math.Max(0, (int)Math.Floor(run.SecondsLeft)) * 4;
            int provoke = 60 * run.Provocations;
            int intel = 40 * run.IntelBought;
            int total = baseScore + early + time - provoke - intel;
            int penalty = 0;
            if (run.WrongTags > 0) { penalty = total - JsRound(total * 0.4); total -= penalty; }
            total = Math.Max(50, total);
            int stars = 1 + (run.WrongTags == 0 ? 1 : 0) + (run.WrongTags == 0 && run.Revealed < run.Total ? 1 : 0);
            return new ScoreResult { Score = total, Stars = stars, Base = baseScore, Early = early, Time = time, Provoke = provoke, Intel = intel, Penalty = penalty };
        }

        // ------------------------------------------------------------ Daily
        static readonly DateTime Launch = new DateTime(2026, 11, 23, 0, 0, 0, DateTimeKind.Utc);
        public static uint DayKey(int y, int m, int d) { return (uint)(y * 10000 + m * 100 + d); }
        public static int DailyNumber(int y, int m, int d)
        {
            return (int)Math.Floor((new DateTime(y, m, d, 0, 0, 0, DateTimeKind.Utc) - Launch).TotalDays) + 1;
        }

        static readonly string[] DailyPools =
        {
            "outfit,carry,habit",
            "carry,headwear,habit",
            "outfit,habit,calm",
            "headwear,carry,pace,outfit",
            "outfit,carry,cell,habit",
            "carry,outfit,habit,calm,headwear",
            "outfit,headwear,cell,carry,habit"
        };

        public static CaseConfig DailyConfig(int y, int m, int d)
        {
            int weekday = (int)new DateTime(y, m, d).DayOfWeek;   // 0 = Sunday, as in JS
            int level = weekday == 0 ? 6 : weekday;
            int n = DailyNumber(y, m, d);
            var cfg = C("Daily Case #" + n, ((n % 3) + 3) % 3, 12 + level * 2, DailyPools[level], 12, 0,
                level < 3 ? 1 : 2, 0, 0, level >= 5 ? 1 : 0, 20 - level,
                "One case a day, the same for every operator. Find the courier and share your result.");
            cfg.Time = 70 + cfg.Clues.Length * 6;
            cfg.Noise = Array.IndexOf(cfg.Clues, Attr.Calm) >= 0 || level >= 4;
            cfg.Ping = Array.IndexOf(cfg.Clues, Attr.Cell) >= 0;
            return cfg;
        }

        public static string ShareText(int number, RunStats run, ScoreResult result)
        {
            var cells = new System.Text.StringBuilder();
            for (int i = 0; i < run.Total; i++) cells.Append(i < run.Revealed ? "🟥" : "⬛");
            int mm = (int)Math.Floor(run.Elapsed / 60), ss = (int)Math.Floor(run.Elapsed % 60);
            string head = run.Cleared ? (run.WrongTags > 0 ? "🎯 Found (2nd shot)" : "🎯 Found") : "💨 Courier escaped";
            var stars = new System.Text.StringBuilder();
            for (int i = 0; i < result.Stars; i++) stars.Append("⭐");
            return "RED SIGNAL · Daily #" + number + "\n" +
                   head + " · " + mm + ":" + ss.ToString("00") + "\n" +
                   "Intel " + cells + " " + run.Revealed + "/" + run.Total + "\n" +
                   (run.Provocations > 0 ? "📣×" + run.Provocations + " " : "") + stars + " " + result.Score + " pts";
        }
    }
}
