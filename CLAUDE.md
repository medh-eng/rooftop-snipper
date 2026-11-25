# Red Signal: Find the One (repo guide for Claude Code)

A mobile game: a courier hides in a crowd; the player reads a dossier, uses a lens, a horn, a ping and an
Echo replay, then tags one person. Browser prototype in `game/`, Unity 6 port in `unity/HarborWatch/`.
The owner (Pon) directs the work and is not a programmer: report in plain words, say what you ran and what
you could not run, and never claim something works in Unity unless you actually ran it in Unity.

## Read first, every session
1. `STATUS.md` (what works, what is unverified, what to check)
2. `PLAN.md` (design rules and phases)
3. the newest entry of `LOG.md` (and its "Next" list)

Work on branch `redesign/find-the-one`. `main` is an older browser game; do not build on it.

## Rules that must not drift
* Game rules live in `game/case-engine.js` and are mirrored in
  `unity/HarborWatch/Assets/HarborWatch/Core/CaseEngine.cs`. Change both together, then run
  `node tools/case-vectors.cjs` and `dotnet run --project tools/core-tests`.
* The browser build is the design lab: new mechanics are tried there first (fast to test), then ported.
* Every case adds at most one new idea (Echo lens from case 4, counter-sniper later, etc.).
* Do not edit `.meta` GUIDs or scene YAML by hand unless there is no other way; prefer Editor scripts.

## Checks to run before every commit
```
node --test tests/*.test.cjs                      # rules, 44 tests
dotnet run --project tools/core-tests             # C# rules, 6 tests
dotnet build tools/unity-compile-check            # compiles Unity scripts against stand-ins
node tools/route-check.cjs                        # crowd routes clear every solid object
# browser play-through (needs Playwright + Chrome):
BROWSER_PATH=<chrome> node --test tests/find-the-one-browser.cjs
```
The compile check uses permissive stand-ins (`tools/unity-compile-check/UnityStubs.cs`). It proves structure,
not behaviour. If a script uses a Unity API the stubs lack, add the member (signature only).
Unity itself is the authority: see `unity/HarborWatch/CLAUDE.md` to run its PlayMode tests.

## Housekeeping each session
* Update `STATUS.md` (table row, "Needs checking"), append a dated `LOG.md` entry (added, fixed, verified,
  NOT verified, next), tick `PLAN.md`.
* Commit small, with a message that says what changed and what was not run. Push to `redesign/find-the-one`.
* Ready-made prompts for the owner are in `docs/CLAUDE_CODE_PROMPTS.md`.
