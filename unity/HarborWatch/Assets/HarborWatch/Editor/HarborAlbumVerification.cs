using System;
using UnityEditor;
using UnityEngine;

namespace HarborWatch
{
    public static class HarborAlbumVerification
    {
        [MenuItem("Harbor Watch/Verify Landmark Album")]
        public static void Verify()
        {
            var album = new HarborLandmarkAlbum();
            var centred = new Vector3(.5f, .5f, 3f);
            for (int saved = 0; saved <= HarborLandmarkAlbum.FullMask; saved++)
            {
                album.Load(saved);
                if (album.Mask != saved) throw new Exception("Mask restore failed");
                int initial = album.CapturedCount;
                if (album.TryCapture(new Vector3(.5f, .5f, -1f), .56f, 40f))
                    throw new Exception("Captured behind camera");
                if (album.TryCapture(new Vector3(.8f, .7f, 3f), .56f, 40f))
                    throw new Exception("Off-centre photo accepted");
                if (album.TryCapture(centred, .56f, 54f))
                    throw new Exception("Unzoomed photo accepted");
                if (album.CapturedCount != initial)
                    throw new Exception("Failed capture changed album");
                while (!album.Complete)
                {
                    if (!album.TryCapture(centred, .56f, 44f))
                        throw new Exception("Framed photo rejected");
                }
                if (album.CapturedCount != 3 ||
                    album.TryCapture(centred, .56f, 44f))
                    throw new Exception("Completion accepted duplicate");
            }
            album.Load(255);
            if (!album.Complete || album.Mask != 7)
                throw new Exception("Unrecognised save bits not discarded");
            Debug.Log("Port Meridian album: all 8 save masks and framing gates passed.");
        }
    }
}
