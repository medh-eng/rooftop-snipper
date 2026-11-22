namespace HarborWatch
{
    // The terminal echoes the survey log from newest relay to oldest.
    public sealed class HarborRelayDecode
    {
        readonly int[] log = new int[HarborMissionRules.RequiredSignals];
        int count;
        public int Matched { get; private set; }
        public bool Complete => Matched == log.Length;
        public string Trace => string.Join("  /  ", log);

        public void Reset()
        {
            count = 0;
            Matched = 0;
        }

        public void Record(int relay)
        {
            if (count < log.Length) log[count++] = relay;
        }

        public bool Submit(int relay)
        {
            if (count != log.Length || Complete) return false;
            if (relay != log[count - 1 - Matched]) return false;
            Matched++;
            return true;
        }
    }
}
