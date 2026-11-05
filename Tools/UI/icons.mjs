// Downloads icon SVGs (Material Symbols Rounded, Apache-2.0; game-icons.net, CC BY 3.0), keeps the sources in
// ArtSource/Icons and renders white 128 px PNGs into Assets/_Project/UI/Icons. Usage: node icons.mjs <repoRoot>
import { Resvg } from '@resvg/resvg-js';
import fs from 'node:fs';
import path from 'node:path';

const root = process.argv[2];
const svgDir = path.join(root, 'ArtSource', 'Icons');
const pngDir = path.join(root, 'Assets', '_Project', 'UI', 'Icons');
fs.mkdirSync(svgDir, { recursive: true });
fs.mkdirSync(pngDir, { recursive: true });

const material = {
  settings: 'settings', mail: 'mail', friends: 'group', person: 'person', add: 'add', close: 'close', back: 'arrow_back',
  chevron_left: 'chevron_left', chevron_right: 'chevron_right', check: 'check', lock: 'lock', copy: 'content_copy',
  edit: 'edit', star: 'star', inventory: 'inventory_2', home: 'home', daily: 'calendar_month', missions: 'task_alt',
  events: 'celebration', pass: 'workspace_premium', chat: 'chat', club: 'groups', volume: 'volume_up', music: 'music_note',
  sfx: 'graphic_eq', mic: 'mic', vibration: 'vibration', controls: 'tune', language: 'translate', account: 'account_circle',
  logout: 'logout', delete: 'delete', notifications: 'notifications', privacy: 'policy', terms: 'description', help: 'help',
  info: 'info', fps: 'speed', quality: 'high_quality', battery: 'battery_saver', shadows: 'contrast', effects_fx: 'auto_awesome',
  visibility: 'visibility', touch: 'touch_app', left_hand: 'back_hand', timer: 'timer', refresh: 'refresh', offline: 'wifi_off',
  error: 'error', history: 'history', stats: 'bar_chart', sort: 'sort', filter: 'filter_list', play: 'play_arrow', emote: 'mood',
  flag: 'flag', accuracy: 'my_location', frame: 'filter_frames', palette: 'palette', outfit: 'checkroom', bolt: 'bolt',
  upgrade: 'upgrade', wall: 'wallpaper', floor: 'grid_view', decor: 'weekend', globe: 'public', gift: 'redeem',
  graphics: 'sports_esports', link: 'link', shield: 'shield', newbadge: 'new_releases', pause: 'pause', fire: 'local_fire_department',
  sticker: 'sticky_note_2', credits: 'groups_3', version: 'deployed_code', streak: 'trending_up', cue_icon: 'straighten',
};
const gameIcons = {
  eight_ball: 'delapouite/eight-ball', rack: 'delapouite/pool-triangle', table: 'delapouite/pool-table-corner',
  coin: 'delapouite/two-coins', gem: 'lorc/cut-diamond', crate: 'skoll/open-treasure-chest', crate_locked: 'lorc/locked-chest',
  trophy: 'delapouite/laurels-trophy', cup: 'delapouite/trophy-cup', ranked: 'lorc/crossed-swords', crown: 'lorc/crown',
  medal: 'delapouite/star-medal', ticket: 'delapouite/ticket', target: 'skoll/bullseye', shop: 'delapouite/shop',
  present: 'delapouite/present', laurels: 'lorc/laurels',
};

async function fetchText(url) {
  const res = await fetch(url);
  if (!res.ok) throw new Error(`${res.status} ${url}`);
  return res.text();
}

function render(svg, file) {
  const png = new Resvg(svg, { fitTo: { mode: 'width', value: 128 }, background: 'rgba(0,0,0,0)' }).render().asPng();
  fs.writeFileSync(file, png);
}

const manifest = [];
for (const [key, name] of Object.entries(material)) {
  const url = `https://raw.githubusercontent.com/google/material-design-icons/master/symbols/web/${name}/materialsymbolsrounded/${name}_fill1_48px.svg`;
  let svg = await fetchText(url);
  fs.writeFileSync(path.join(svgDir, `${key}.svg`), svg);
  svg = svg.replace('<svg ', '<svg fill="#ffffff" ');
  render(svg, path.join(pngDir, `${key}.png`));
  manifest.push({ key, source: 'material-symbols', name, url });
}
for (const [key, name] of Object.entries(gameIcons)) {
  const url = `https://raw.githubusercontent.com/game-icons/icons/master/${name}.svg`;
  let svg = await fetchText(url);
  fs.writeFileSync(path.join(svgDir, `${key}.svg`), svg);
  // Repo SVGs are a black square behind the glyph on some exports; drop it and paint the glyph white.
  svg = svg.replace(/<path d="M0 0h512v512H0z"\s*\/>/g, '').replace('<svg ', '<svg fill="#ffffff" ');
  render(svg, path.join(pngDir, `${key}.png`));
  manifest.push({ key, source: 'game-icons.net', name, url });
}
fs.writeFileSync(path.join(svgDir, 'manifest.json'), JSON.stringify(manifest, null, 2));
console.log(`icons: ${manifest.length}`);
