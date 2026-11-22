using UnityEngine;

namespace HarborWatch
{
    /// <summary>
    /// Pure scoring rules for the first non-violent signal-survey mission.
    /// Matches the first contract's ideal browser payout: 576 credits.
    /// A signal is an invented device, never a pedestrian.
    /// </summary>
    public static class HarborMissionRules
    {
        public const int TimeLimitSeconds = 90;
        public const int RequiredSignals = 3;
        public const int StartingCredits = 350;

        public struct Payout
        {
            public int Total, Bounty, Precision, Safety, Chain, FirstClear;
            public int PrecisionPercent;
            public int Stars;
        }

        public static Payout Settle(int signals, int attempts, int bestChain, bool firstClear)
        {
            if (signals != RequiredSignals || attempts < signals)
                return new Payout();

            int precision = Mathf.RoundToInt(100f * signals / attempts);
            bool clean = attempts == signals;
            var payout = new Payout
            {
                Bounty = firstClear ? 240 : 120,
                Precision = Mathf.RoundToInt(precision * .8f),
                Safety = clean ? 100 : 0,
                Chain = Mathf.Clamp(bestChain, 0, RequiredSignals) * 12,
                FirstClear = firstClear ? 120 : 0,
                PrecisionPercent = precision,
                Stars = 1 + (precision >= 80 ? 1 : 0) + (clean ? 1 : 0)
            };
            payout.Total = payout.Bounty + payout.Precision + payout.Safety
                         + payout.Chain + payout.FirstClear;
            return payout;
        }
    }
}
