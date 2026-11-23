// Pack Red Signal: Find the One (game/) into ONE self-contained HTML file
// with all artwork and fonts embedded. Open it on any phone or PC, no server.
//   node tools/build-game-offline.cjs Red-Signal-Find-the-One.html
const fs = require('node:fs');
const path = require('node:path');
const root = path.resolve(__dirname, '..');
const target = process.argv[2];
if (!target) throw new Error('Pass an output HTML path.');
const read = name => fs.readFileSync(path.join(root, name), 'utf8');
const data = (name, type) => `data:${type};base64,${fs.readFileSync(path.join(root, name)).toString('base64')}`;
const media = {};
const scenes = ['meridian', 'vesper', 'solara', 'meridian-roof', 'vesper-roof', 'solara-roof', 'harbor-cover'];
for (const n of scenes) media[`../assets/scenes/${n}.webp`] = data(`assets/scenes/${n}.webp`, 'image/webp');
for (const n of ['walker-0', 'walker-1', 'walker-2', 'walker-3', 'passing-0', 'passing-1', 'passing-2', 'passing-3', 'projector-view'])
  media[`../assets/ui/${n}.webp`] = data(`assets/ui/${n}.webp`, 'image/webp');
const mark = data('assets/ui/mark.svg', 'image/svg+xml');
const css = read('game/style.css')
  .replace(/url\((\.\.\/assets\/fonts\/[^)]+)\)/g, (_, p) => `url('${data(p.replace('../', ''), 'font/woff')}')`)
  .replace(/url\((\.\.\/assets\/scenes\/[^)]+)\)/g, (_, p) => `url('${media[p]}')`);
const source = read('game/index.html');
const scripts = [...source.matchAll(/<script defer src="([^"]+)"><\/script>/g)].map(m => read('game/' + m[1]));
const script = `globalThis.EMBEDDED_ASSETS=${JSON.stringify(media)};\n${scripts.join('\n')}`;
const licences = ['OFL-Barlow.txt', 'OFL-Barlow-Condensed.txt'].map(n => read('assets/fonts/' + n)).join('\n');
const html = source.replace(/<script defer src="[^"]+"><\/script>/g, '')
  .replace('<link rel="stylesheet" href="style.css">', () => `<style>${css}</style>`)
  .replaceAll('../assets/ui/mark.svg', mark)
  .replace('</body>', () => `<script type="text/plain" id="font-licences">${licences}</script>\n<script>${script.replace(/<\/script/gi, '<\\/script')}</script>\n</body>`);
const output = path.resolve(target);
fs.writeFileSync(output, html);
console.log(`${output} (${Math.ceil(Buffer.byteLength(html) / 1024)} KiB)`);
