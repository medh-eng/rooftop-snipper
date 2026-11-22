using System;
using UnityEditor;
using UnityEngine;

namespace HarborWatch
{
    public static class HarborDecodeVerification
    {
        [MenuItem("Harbor Watch/Verify Relay Decode")]
        public static void Verify()
        {
            var decode = new HarborRelayDecode();
            for (int first = 1; first <= 3; first++)
            for (int second = 1; second <= 3; second++)
            {
                if (first == second) continue;
                int third = 6 - first - second;
                decode.Reset();
                decode.Record(first);
                if (decode.Submit(first)) throw new Exception("Incomplete log accepted");
                decode.Record(second);
                decode.Record(third);
                if (decode.Submit(first) || decode.Matched != 0 ||
                    !decode.Submit(third) || !decode.Submit(second) ||
                    !decode.Submit(first) || !decode.Complete || decode.Submit(first))
                    throw new Exception("Decode failed for trace " + decode.Trace);
            }
            Debug.Log("All six relay traces passed, including retry and reset.");
        }
    }
}
