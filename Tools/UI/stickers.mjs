// Original sticker designs (vector, drawn for this project). Writes ArtSource/Stickers/<id>.svg and renders 256 px PNGs
// into Assets/_Project/UI/Stickers. Usage: node stickers.mjs <repoRoot>
import { Resvg } from '@resvg/resvg-js';
import fs from 'node:fs';
import path from 'node:path';

const root = process.argv[2];
const svgDir = path.join(root, 'ArtSource', 'Stickers');
const pngDir = path.join(root, 'Assets', '_Project', 'UI', 'Stickers');
const font = path.join(root, 'Assets', '_Project', 'Art', 'Fonts', 'font-montserrat', 'Montserrat-Bold.ttf');
fs.mkdirSync(svgDir, { recursive: true });
fs.mkdirSync(pngDir, { recursive: true });

const INK = '#141824';
// Die-cut look: every sticker body is drawn twice, first as a fat white outline, then filled.
const cut = (shape) => `<g stroke="#fff" stroke-width="22" stroke-linejoin="round" fill="#fff">${shape}</g>`;
const ink = (shape) => `<g stroke="${INK}" stroke-width="7" stroke-linejoin="round" stroke-linecap="round">${shape}</g>`;
const text = (x, y, size, fill, content, rotate = 0) =>
  `<text x="${x}" y="${y}" font-family="Montserrat" font-weight="700" font-size="${size}" fill="${fill}" stroke="${INK}" stroke-width="6" paint-order="stroke" text-anchor="middle" transform="rotate(${rotate} ${x} ${y})">${content}</text>`;
const svg = (body) => `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 256 256" width="256" height="256">${body}</svg>`;

const ball = (cx, cy, r, color, num) => `
  <circle cx="${cx}" cy="${cy}" r="${r}" fill="${color}"/>
  <circle cx="${cx}" cy="${cy}" r="${r * 0.45}" fill="#fff" stroke="none"/>
  <text x="${cx}" y="${cy + r * 0.17}" font-family="Montserrat" font-weight="700" font-size="${r * 0.55}" fill="${INK}" stroke="none" text-anchor="middle">${num}</text>
  <ellipse cx="${cx - r * 0.42}" cy="${cy - r * 0.5}" rx="${r * 0.2}" ry="${r * 0.11}" fill="#fff" opacity="0.7" stroke="none" transform="rotate(-35 ${cx - r * 0.42} ${cy - r * 0.5})"/>`;

const flame = 'M128 22 C150 60 196 78 196 140 C196 186 166 222 128 222 C90 222 60 186 60 140 C60 108 78 92 88 70 C96 96 108 104 118 104 C110 74 116 46 128 22 Z';
const bubble = 'M40 52 H216 Q236 52 236 72 V156 Q236 176 216 176 H120 L78 214 L86 176 H40 Q20 176 20 156 V72 Q20 52 40 52 Z';
const crown = 'M44 176 L30 82 L84 126 L128 58 L172 126 L226 82 L212 176 Z';
const dome = 'M58 214 V150 Q58 92 128 64 Q198 92 198 150 V214 Z';
const shield = 'M128 26 L214 58 V124 Q214 192 128 232 Q42 192 42 124 V58 Z';
const bolt = 'M146 18 L64 142 H118 L100 238 L192 104 H136 Z';
const teapot = 'M70 112 Q70 92 128 92 Q186 92 186 112 L196 190 Q196 214 128 214 Q60 214 60 190 Z';
const pomegranate = 'M128 70 C186 70 222 110 222 154 C222 204 180 232 128 232 C76 232 34 204 34 154 C34 110 70 70 128 70 Z';
const star = (cx, cy, r) => {
  const pts = [];
  for (let i = 0; i < 10; i++) {
    const a = -Math.PI / 2 + (i * Math.PI) / 5;
    const rr = i % 2 === 0 ? r : r * 0.45;
    pts.push(`${(cx + Math.cos(a) * rr).toFixed(1)},${(cy + Math.sin(a) * rr).toFixed(1)}`);
  }
  return `M${pts.join(' L')} Z`;
};

const stickers = {
  sticker_hot_eight: svg(`${cut(`<path d="${flame}"/>`)}${ink(`<path d="${flame}" fill="#FF7A00"/><path d="M128 70 C140 96 170 110 170 150 C170 180 152 200 128 200 C104 200 86 180 86 150 C86 128 100 118 106 102 C112 118 120 120 126 118 C122 100 122 86 128 70 Z" fill="#FFC24A"/>`)}${ink(ball(128, 160, 38, INK, 8))}`),
  sticker_nice_shot: svg(`${cut(`<path d="${bubble}"/>`)}${ink(`<path d="${bubble}" fill="#3D8BFF"/>`)}${text(128, 106, 40, '#fff', 'NICE')}${text(128, 152, 40, '#FFC24A', 'SHOT!')}`),
  sticker_crown_cue: svg(`${cut(`<path d="${crown}"/><rect x="40" y="186" width="176" height="26" rx="10"/>`)}${ink(`<path d="${crown}" fill="#FFB020"/><rect x="40" y="186" width="176" height="26" rx="10" fill="#C77800"/><circle cx="128" cy="140" r="16" fill="#FF3B5C"/><circle cx="80" cy="152" r="10" fill="#3D8BFF"/><circle cx="176" cy="152" r="10" fill="#3D8BFF"/>`)}`),
  sticker_pomegranate: svg(`${cut(`<path d="${pomegranate}"/><path d="M104 74 L110 40 L128 58 L146 40 L152 74 Z"/>`)}${ink(`<path d="${pomegranate}" fill="#D7263D"/><path d="M104 74 L110 40 L128 58 L146 40 L152 74 Z" fill="#9E1B2C"/>`)}<path d="M92 120 Q128 100 164 120" stroke="#fff" stroke-width="8" fill="none" opacity="0.5" stroke-linecap="round"/>${[[96, 160], [128, 150], [160, 160], [112, 188], [144, 188]].map(([x, y]) => `<circle cx="${x}" cy="${y}" r="11" fill="#FF8FA3" stroke="${INK}" stroke-width="4"/>`).join('')}`),
  sticker_blue_dome: svg(`${cut(`<path d="${dome}"/><rect x="40" y="196" width="176" height="30" rx="8"/>`)}${ink(`<path d="${dome}" fill="#1CA7C9"/><rect x="40" y="196" width="176" height="30" rx="8" fill="#E8D3A2"/><path d="M128 64 V36" fill="none"/><circle cx="128" cy="32" r="8" fill="#FFC24A"/>`)}<path d="M76 150 Q128 120 180 150 M70 176 Q128 146 186 176" stroke="#fff" stroke-width="6" fill="none" opacity="0.7"/>${[84, 128, 172].map((x) => `<path d="M${x - 10} 226 V210 Q${x} 198 ${x + 10} 210 V226 Z" fill="${INK}"/>`).join('')}`),
  sticker_gg: svg(`${cut(`<path d="${shield}"/>`)}${ink(`<path d="${shield}" fill="#A64DFF"/><path d="M128 50 L192 74 V124 Q192 176 128 208 Q64 176 64 124 V74 Z" fill="#7A2FD0"/>`)}${text(128, 152, 72, '#fff', 'GG')}`),
  sticker_lol_ball: svg(`${cut('<circle cx="128" cy="128" r="96"/>')}${ink('<circle cx="128" cy="128" r="96" fill="#FFD23F"/>')}${ink('<path d="M78 110 Q92 92 106 110" fill="none"/><path d="M150 110 Q164 92 178 110" fill="none"/><path d="M72 140 Q128 210 184 140 Z" fill="#141824"/>')}<path d="M96 160 Q128 186 160 160 Z" fill="#FF5C7A"/><path d="M58 104 q-14 26 6 36 M198 104 q14 26 -6 36" stroke="#4FC3F7" stroke-width="10" fill="none" stroke-linecap="round"/>`),
  sticker_break_bolt: svg(`${cut(`<path d="${bolt}"/>`)}${ink(`<path d="${bolt}" fill="#FFC24A"/>`)}${ink(ball(70, 196, 28, '#3D8BFF', 2))}${ink(ball(196, 70, 26, '#FF3B5C', 3))}`),
  sticker_choy: svg(`${cut(`<path d="${teapot}"/><path d="M186 124 Q232 120 222 160 Q214 178 194 176"/><path d="M70 134 Q30 128 28 96"/>`)}${ink(`<path d="M186 124 Q232 120 222 160 Q214 178 194 176" fill="none" stroke-width="14"/><path d="M70 134 Q34 130 30 98" fill="none" stroke-width="14"/><path d="${teapot}" fill="#2B7DE9"/><ellipse cx="128" cy="92" rx="40" ry="12" fill="#1C5DB8"/><circle cx="128" cy="76" r="10" fill="#FFC24A"/>`)}<path d="M84 140 Q128 160 172 140 M80 172 Q128 192 176 172" stroke="#fff" stroke-width="7" fill="none" opacity="0.85"/>${text(128, 58, 34, '#fff', 'CHOY?', -6)}`),
  sticker_gold_star: svg(`${cut(`<path d="${star(128, 128, 104)}"/>`)}${ink(`<path d="${star(128, 128, 104)}" fill="#FFB020"/><path d="${star(128, 128, 62)}" fill="#FFD66B"/>`)}${text(128, 144, 40, '#fff', 'PRO')}`),
};

for (const [id, body] of Object.entries(stickers)) {
  fs.writeFileSync(path.join(svgDir, `${id}.svg`), body);
  const png = new Resvg(body, { fitTo: { mode: 'width', value: 256 }, font: { fontFiles: [font], loadSystemFonts: false, defaultFontFamily: 'Montserrat' } }).render().asPng();
  fs.writeFileSync(path.join(pngDir, `${id}.png`), png);
}
console.log(`stickers: ${Object.keys(stickers).length}`);
