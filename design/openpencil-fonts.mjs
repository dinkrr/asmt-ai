import { readFile } from 'node:fs/promises';
import { pathToFileURL } from 'node:url';

const { fontManager } = await import(pathToFileURL(process.env.OPENPENCIL_FONT_MODULE).href);
const fonts = [
  ['Segoe UI', 'Regular', 'C:/Windows/Fonts/segoeui.ttf'],
  ['Segoe UI', 'Bold', 'C:/Windows/Fonts/segoeuib.ttf'],
  ['Georgia', 'Bold', 'C:/Windows/Fonts/georgiab.ttf']
];

for (const [family, style, file] of fonts) {
  const data = await readFile(file);
  await fontManager.markLoaded(family, style, data.buffer.slice(data.byteOffset, data.byteOffset + data.byteLength));
}