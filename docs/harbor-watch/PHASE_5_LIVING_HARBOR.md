# Phase 5A — Living Port Meridian Harbor

Repository: medh-eng/rooftop-snipper
Feature branch: chatgpt/harbor-phase5-port-meridian
Unity version: 6000.3.28f1
Status: Source implementation; Unity Editor and Android rendering are not verified.

## Scope

This is the first contained environment slice of the Rich Game Roadmap,
not the complete premium vertical slice.

The same concourse root now contains a decorative double-ended ferry with a
64-second dock/depart/offshore/return cycle, simple water highlights, two
flapping seabirds, pier bollards, lamps, crane, cargo containers, terminal
windows, kiosk awning, bench supports and rooftop details.

Environment motion is driven by the scene controller. When the crowd review is
paused or slowed, the ferry, glints, birds and visitors obey the same pacing.
Mission pause/completion and drone pause also stop ambient motion. No traffic
routes, pedestrians, signals or drone flight controls are modified. Decoration
has no colliders, casts no shadows, uses shared colour materials and does not
add external assets, audio dependencies or packages.

## Verification on desktop

1. Fetch and switch to the feature branch in GitHub Desktop.
2. In Unity Hub open the existing unity/HarborWatch directory with Unity 6000.3.28f1.
3. Select Harbor Watch > Verify Phase 5 Living Harbor and inspect Console.
4. In Assets/Scenes/HarborCrowd.unity, press Play for 64 seconds. The ferry
   should dock, depart and return without jumping. Check both Pause and Slow.
5. In HarborMission.unity verify that the three fictional devices remain
   scannable, the mission can complete and pause freezes the environment.
6. In HarborDrone.unity navigate round terminal and ferry and check same crowd
   persists. Confirm observation roof constraints and portrait controls.
7. Run node --test tests/*.test.cjs from the repository root.

## Android acceptance before merge

Use the existing Phase 3/Phase 4 development APK menus. Verify that quay props
do not visually overlap walkers, water glints do not flicker, birds and ferry
remain visible, and controls are unobstructed on an Android portrait screen.
Measure ten-minute frame pacing on the target phone (goal 30 FPS); record any
Console errors. This is not yet a production APK or complete art/audio kit.

Next planned slice: coherent harbor materials, original ambient sound and
improved mission feedback, followed by additional distinct objective types.
