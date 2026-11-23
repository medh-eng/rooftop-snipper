# Rooftop Sniper · Red Signal

> **New direction (23 Nov 2026): _Red Signal: Find the One_.** Targets are no
> longer marked. You read a dossier, watch the crowd, provoke reactions and tag
> the one courier who matches every line. One shot, plus one more if you're wrong.
>
> * **Play the prototype:** open [`game/index.html`](game/index.html) (phone or PC).
> * **Unity mobile build:** `unity/HarborWatch` → `Assets/Scenes/FindTheOne.unity`.
> * **Plan · status · log:** [PLAN.md](PLAN.md) · [STATUS.md](STATUS.md) · [LOG.md](LOG.md)
> * **Checks:** `node --test tests/*.test.cjs` · `dotnet run --project tools/core-tests` · `dotnet build tools/unity-compile-check`
>
> The sections below describe the original marked-target campaign (root `index.html`)
> and the earlier Harbor Watch phases, kept for reference.

A single-player, touch-friendly arcade campaign set in three fictional cities. A rooftop operator tracks red-marked members of the Red Signal sabotage group through moving crowds. Later contracts put the player in a drone camera to disable rogue aircraft. All equipment, scoring and targeting use fictional arcade rules.

## Play

Open `index.html` in a modern browser. The game works from a downloaded checkout and on a static web host. For a local server, run `python3 -m http.server 8000` in the project directory and open `http://localhost:8000`. The game has no install step, account, ads or runtime network dependencies.

- Tap or click a red diamond or its marked actor to fire. Drag the scene to pan. The **Find** control brings the next signal into view.
- Use **Lens** to magnify the scene and **Reload** to refill the cartridge. Civilian contacts block the shot and break the chain.
- Keyboard controls include arrows or WASD to move the sight, Space to fire, F to find a signal, Z for the lens, R to reload and Escape to pause. In drone view the movement keys fly the camera.
- Earn credits for contracts, precision, clean crowds, chains and first clears. From contract two onward, a two-hit chain opens an eight-second gold relay. Tap it for a refill and 80 bonus credits on a successful clear. Clearing all three contracts in a district pays a one-time cache. Spend credits on the projector, lens, cartridge and pulse rounds in the armory.
- Campaign records, equipped upgrades, credits and sound preference are stored in the current browser. If storage is unavailable, play continues for the session.

## Development

The game uses HTML, CSS, JavaScript and Canvas 2D. Original city plates, transparent characters and equipment artwork ship as WebP. Barlow and Barlow Condensed ship locally under the SIL Open Font License in `assets/fonts`.

Run `node --test tests/*.test.cjs` for campaign, economy and targeting checks.

For a single offline file, run `node tools/build-offline.cjs Rooftop-Sniper.html`, then open the resulting HTML in a browser. It includes the entire campaign and artwork.

The optional browser test runs all nine contracts on desktop Chromium and checks touch interactions at a phone viewport. With Playwright and its Chromium installed, run `node --test tests/campaign-browser.cjs`. Set `BROWSER_PATH` to use an existing Chromium executable.

## Play the Unity Port Meridian shift

Open `unity/HarborWatch` in Unity **6000.3.28f1**, open **Assets → Scenes → HarborMission**, press **Play**, and select **Start Shift** in the **Game** tab. From **HarborCrowd**, press Play and select **Play Harbor Watch**.

1. Drag to centre a red relay and press **Scan**. Verify all three within 90 seconds. **Restart** retries a timed-out survey.
2. The survey pays credits and saves a flight checkpoint. **Decode** and **Photo** are optional activities. Choose **Fly Drone** to continue over the same crowd.
3. Press **Find** to face the moving survey drone. Press **Identify** with a clear view to file the shift report. Select **Play Another Shift** to replay.

To check the save on a phone, close and reopen after the rooftop survey. **Continue Flight** should take you straight to the aerial inspection with the same credit balance. Reopen after identification to see the report. For an APK, switch the Unity build target to Android, then select **Harbor Watch → Build Android Development APK**.

## Unity Android prototype

The [Harbor Watch Unity plan](docs/UNITY_HARBOR_WATCH_PLAN.md) covers opening the project on your PC, testing the first pedestrian's walk, and the tasks for you and ChatGPT in each phase.


### Phase 3: playable Unity signal survey (feature branch)

The Unity project is at `unity/HarborWatch`; select Editor **6000.3.28f1**.
On `chatgpt/harbor-phase3-mission`, the **first Android build scene** is
`Assets/Scenes/HarborMission.unity`. It uses the same procedural Port Meridian
concourse and pedestrian crowd as the optional review scene.

The playable first mission is **non-violent**: pan/zoom the rooftop camera,
centre three fictional red signal devices, tap **SCAN**, and clear the
90-second survey. Pause, restart, timeout, rewards and a local save are
implemented in source. A perfect first clear awards **576 credits** and grows
a fresh **350-credit** wallet to **926**. None of the pedestrians is a target.

**Unity menus:** `Harbor Watch → Verify Phase 3 Mission`, then switch to
Android in Build Profiles and choose `Harbor Watch → Build Android
Development APK` to compile on your PC. The APK requires the installed Unity
Android Build Support modules and has **not** been generated or device-tested
by GitHub Actions. See [Phase 3 verification](docs/harbor-watch/PHASE_3_MISSION.md).

The approved crowd scene (`HarborCrowd.unity`) and walk review
(`WalkReview.unity`) remain in the project. Phase 3 is isolated in
[PR #2](https://github.com/medh-eng/rooftop-snipper/pull/2), based on the
unmerged [Phase 2 PR #1](https://github.com/medh-eng/rooftop-snipper/pull/1).


### Phase 4: standalone drone-camera proof (feature branch)

Open `Assets/Scenes/HarborDrone.unity` in the existing Unity
`unity/HarborWatch` project (**Unity 6000.3.28f1**). The aerial observer
can fly over and around Port Meridian, return to the rooftop viewpoint
and inspect the **same moving pedestrian crowd**. Use a left thumb
stick to fly, drag on the right to look, hold UP/DOWN to change altitude,
and identify the harmless fictional aerial signal. WASD, Q/E and arrows
are available for Editor testing.

Phase 4 adds **Harbor Watch → Verify Phase 4 Drone** and
**Harbor Watch → Build Drone Review APK**. The latter builds only this
review scene as an Android development APK, without replacing the first
scene of the normal mission. See
[Phase 4 setup and device checks](docs/harbor-watch/PHASE_4_DRONE.md).

This feature is in
[Phase 4 PR #3](https://github.com/medh-eng/rooftop-snipper/pull/3);
its earlier dependencies are Phase 2 PR #1 and Phase 3 PR #2.
The Unity camera must still be visually checked and the APK must be
compiled and profiled on the intended Android phone.


### Rich game roadmap

The proposed next-stage design lives in
[Harbor Watch — Rich Game Roadmap](docs/harbor-watch/RICH_GAME_ROADMAP.md).
It details three district art directions, five mission archetypes,
narrative progression, daily/weekly challenges, player-friendly rewards,
technical architecture, Android performance gates and acceptance criteria.
This is a **production plan**, not a claim that expansion features are built.
The next coding priority remains validating the current Unity feature stack
on a real Android handset before large-scale art/content expansion.

### Phase 5A: Living Port Meridian harbor (feature branch)

The shared Unity concourse now has a **decorative animated ferry**, harbor
water glints, seabirds, crane/cargo and terminal/quay detailing in the
[Phase 5A living harbor work](docs/harbor-watch/PHASE_5_LIVING_HARBOR.md).
This is a source-only visual slice in [draft PR #4](https://github.com/medh-eng/rooftop-snipper/pull/4),
stacked after Phase 4 PR #3. Existing pedestrian routes, mission and drone
controllers remain in place. Test in Unity 6000.3.28f1 with
**Harbor Watch → Verify Phase 5 Living Harbor**, then review portrait Android
performance; neither visual validation nor a finished APK is claimed yet.
