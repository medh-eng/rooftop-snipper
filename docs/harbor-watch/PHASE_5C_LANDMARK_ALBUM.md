# Phase 5C — Port Meridian Landmark Album

Branch: `chatgpt/harbor-phase5c-landmark-album`
Base: Phase 5B draft PR #5 (`chatgpt/harbor-next-four`)
Unity: 6000.3.28f1
Status: source implementation; no claim of Unity/Android runtime verification.

## New optional objective (third archetype)

After completing the existing three-relay survey, tap **PHOTO** beside
**RESTART**. Aim the rooftop lens at the *ticket kiosk*, *terminal canopy* and
*harbor crane*, in that order. Zoom to **48° field of view or closer**, centre
the currently indicated landmark on the reticle, then tap **CAPTURE**. Use
**BACK** to return to the completed-survey and relay-decoding controls;
**RESTART** runs a new normal survey.

This is a non-violent photographic collectible. The game uses existing static
Port Meridian geometry, not pedestrians, and reuses the scene and touch camera.
The ferry, pedestrians and harbour ambience continue while the photo viewer is
active. The survey timer remains stopped after the original win. Photos are
stored as a 3-bit PlayerPrefs mask and survive restart and app reopen. A photo
can be collected only once. Photos provide **no additional credits** so the
original perfect first-clear 576-credit payout remains unchanged.

## Verification

1. Pull the branch and open `Assets/Scenes/HarborMission.unity` in
   Unity 6000.3.28f1; check Console for C# errors.
2. Run **Harbor Watch → Verify Landmark Album** (tests all 8 save masks,
   behind-camera, out-of-frame, too-wide lens, and duplicate captures).
3. Verify a normal mission still scores a perfect fresh survey 576 credits,
   awarding a 350-credit opening wallet a final balance of 926 credits.
4. After winning, test PHOTO, a misplaced photo, zoom restrictions,
   centre framing, all three captures, BACK, DECODE, and RESTART.
5. Reopen the game and repeat: saved photos must still show as collected,
   remaining photos must still be available and no bonus credits awarded.
6. Check portrait safe-area controls and zoom/pinch, moving pedestrians,
   audio loop, 10-minute 30-FPS target and tap accuracy on an Android handset.
7. Run `node --test tests/*.test.cjs` from the repository root.

The photo framing test uses projected landmark centre and lens angle only;
it does not yet detect scene occlusion. Review visibility manually at every
location and lighting condition. Unity Editor compilation, actual camera
visibility and Android performance remain pending until verified on device.

## Remaining roadmap

**Highest priority remains Milestone 0:** merge/test the stacked branches in
order after Unity and handset approval, capture build/version/device/FPS, and
fix any runtime defects. Milestone 1 now has signal survey, relay decode and
landmark photography prototypes in one district. It does not yet have
hand-crafted 3–6 minute progression contracts, narrative, upgraded UI/art,
weather or device-tested polish.
