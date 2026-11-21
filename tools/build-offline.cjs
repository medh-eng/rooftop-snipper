// Pack the static game and its font licences into one self-contained HTML file.
const fs = require('node:fs');
const path = require('node:path');
const root = path.resolve(__dirname, '..');
const target = process.argv[2];
if (!target) throw new Error('Pass an output HTML path.');
const read = name => fs.readFileSync(path.join(root, name), 'utf8');
const data = (name, type) => `data:${type};base64,${fs.readFileSync(path.join(root, name)).toString('base64')}`;
const media = {};
for (const folder of ['scenes', 'ui']) {
  for (const file of fs.readdirSync(path.join(root, 'assets', folder))) {
    if (file.endsWith('.webp')) {
      const key = `assets/${folder}/${file}`;
      media[key] = data(key, 'image/webp');
    }
  }
}
const css = read('style.css').replace(/url\(['"]?(assets\/fonts\/[^)'"\s]+)['"]?\)/g,
  (_, name) => `url('${data(name, 'font/woff')}')`);
const source = read('index.html');
const scripts = [...source.matchAll(/<script defer src="([^"]+)"><\/script>/g)].map(match => read(match[1]));
const script = `globalThis.EMBEDDED_ASSETS=${JSON.stringify(media)};\n${scripts.join('\n')}`;
const fontLicences = ['OFL-Barlow.txt', 'OFL-Barlow-Condensed.txt'].map(name => read(`assets/fonts/${name}`)).join('\n');
const html = source.replace(/<script defer src="[^"]+"><\/script>/g, '')
  .replace('<link rel="stylesheet" href="style.css">', () => `<style>${css}</style>`)
  .replaceAll('assets/ui/mark.svg', data('assets/ui/mark.svg', 'image/svg+xml'))
  .replace('</body>', () => `<script type="text/plain" id="font-licences">${fontLicences}</script>\n<script>${script.replace(/<\/script/gi,'<\\/script')}</script>\n</body>`);
const output = path.resolve(target);
if (output === path.join(root,'index.html')) throw new Error('Choose a different path from the source index.html.');
fs.writeFileSync(output, html);
console.log(`${output} (${Math.ceil(Buffer.byteLength(html)/1024)} KiB)`);
