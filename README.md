# Red Signal: Find the One

A mobile game. A courier hides in a crowd. You read a dossier, use a lens, a horn, a ping and an Echo
replay, then tag one person. One shot, plus one more if you are wrong.

* **Play the prototype:** open `game/index.html` in a browser (phone or PC).
* **Unity mobile build:** open `unity/HarborWatch` in Unity 6.3 LTS, then
  Harbor Watch > Find The One > Open scene, and press Play.
* **Plan, status, log:** [PLAN.md](PLAN.md), [STATUS.md](STATUS.md), [LOG.md](LOG.md)
* **Working with Claude Code:** [CLAUDE.md](CLAUDE.md) and [docs/CLAUDE_CODE_PROMPTS.md](docs/CLAUDE_CODE_PROMPTS.md)
* **Making the 3D pedestrian:** [docs/PEDESTRIAN_MODEL_GUIDE.md](docs/PEDESTRIAN_MODEL_GUIDE.md)

## Folders

| Folder | What it is |
|---|---|
| `game/` | Browser game and the rules (`case-engine.js`), the design lab |
| `unity/HarborWatch/` | The Unity project (the real mobile game) |
| `assets/` | Art and fonts used by the browser game |
| `tests/`, `tools/` | Automatic checks (see CLAUDE.md) |
| `docs/` | Guides for Claude Code and for making the pedestrian model |

The old marked-target campaign was removed on 25 Nov 2026. It is still in the git history before commit
`eb00f5c` if it is ever needed.
