# Rooftop Sniper · Red Signal

A single-player, touch-friendly arcade campaign set in three fictional cities. A rooftop operator tracks red-marked members of the Red Signal sabotage group through moving crowds. Later contracts put the player in a drone camera to disable rogue aircraft. All equipment, scoring and targeting use fictional arcade rules.

## Play

Open `index.html` in a modern browser. The game works from a downloaded checkout and on a static web host. For a local server, run `python3 -m http.server 8000` in the project directory and open `http://localhost:8000`. The game has no install step, account, ads or runtime network dependencies.

- Tap or click a red diamond or its marked actor to fire. Drag the scene to pan. The **Find** control brings the next signal into view.
- Use **Lens** to magnify the scene and **Reload** to refill the cartridge. Civilian contacts block the shot and break the chain.
- Keyboard controls include arrows or WASD to move the sight, Space to fire, F to find a signal, Z for the lens, R to reload and Escape to pause. In drone view the movement keys fly the camera.
- Earn credits for contracts, precision, clean crowds, chains and first clears. Clearing all three contracts in a district pays a one-time cache. Spend credits on the projector, lens, cartridge and pulse rounds in the armory.
- Campaign records, equipped upgrades, credits and sound preference are stored in the current browser. If storage is unavailable, play continues for the session.

## Development

The game uses HTML, CSS, JavaScript and Canvas 2D. Original city plates, transparent characters and equipment artwork ship as WebP. Barlow and Barlow Condensed ship locally under the SIL Open Font License in `assets/fonts`.

Run `node --test tests/*.test.cjs` for campaign, economy and targeting checks.
