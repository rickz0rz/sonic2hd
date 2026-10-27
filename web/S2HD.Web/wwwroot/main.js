import { dotnet } from './_framework/dotnet.js';
import * as webgl from './webgl.js';

const params = new URLSearchParams(location.search);
const canvas = document.getElementById('game');
const panel = document.getElementById('panel');
const status = document.getElementById('status');
const hud = document.getElementById('hud');

// Diagnostics: when served locally by tools/web-server (or with ?log), console output is also POSTed to /log
const logToServer = location.hostname === 'localhost' || params.has('log');
const report = (text) => logToServer ? fetch('/log', { method: 'POST', body: text }).catch(() => {}) : console.info(text);
for (const level of logToServer ? ['log', 'warn', 'error'] : []) {
  const original = console[level].bind(console);
  console[level] = (...args) => { original(...args); report((level === 'log' ? '' : level.toUpperCase() + ' ') + args.join(' ')); };
}
addEventListener('error', (e) => report('ERROR ' + e.message));
addEventListener('unhandledrejection', (e) => report('ERROR ' + (e.reason?.stack || e.reason)));
const setStatus = (text) => { status.textContent = text; console.log(text); };

// Keyboard state indexed by SDL scancode, which the engine uses
const keys = new Uint8Array(512);
const scancodes = {
  Enter: 40, Escape: 41, Backspace: 42, Tab: 43, Space: 44, Minus: 45, Equal: 46, BracketLeft: 47, BracketRight: 48,
  Backslash: 49, Semicolon: 51, Quote: 52, Backquote: 53, Comma: 54, Period: 55, Slash: 56, CapsLock: 57,
  Insert: 73, Home: 74, PageUp: 75, Delete: 76, End: 77, PageDown: 78,
  ArrowRight: 79, ArrowLeft: 80, ArrowDown: 81, ArrowUp: 82,
  ControlLeft: 224, ShiftLeft: 225, AltLeft: 226, MetaLeft: 227, ControlRight: 228, ShiftRight: 229, AltRight: 230, MetaRight: 231,
};
for (let i = 0; i < 26; i++) scancodes['Key' + String.fromCharCode(65 + i)] = 4 + i;
for (let i = 1; i <= 9; i++) scancodes['Digit' + i] = 29 + i;
scancodes.Digit0 = 39;
for (let i = 1; i <= 12; i++) scancodes['F' + i] = 57 + i;
const setKey = (e, down) => {
  const code = scancodes[e.code];
  if (code === undefined || panel.hidden === false) return;
  keys[code] = down ? 1 : 0;
  e.preventDefault();
};
addEventListener('keydown', (e) => setKey(e, true));
addEventListener('keyup', (e) => setKey(e, false));
addEventListener('blur', () => keys.fill(0));

// Canvas drawing buffer follows its layout size
function resizeCanvas() {
  const scale = window.devicePixelRatio || 1;
  canvas.width = Math.max(1, Math.round(canvas.clientWidth * scale));
  canvas.height = Math.max(1, Math.round(canvas.clientHeight * scale));
}
addEventListener('resize', resizeCanvas);
resizeCanvas();
webgl.init(canvas);

// Audio: the game mixes 44.1 kHz stereo on demand, and the page plays the chunks back to back with a short lead.
// Browsers allow audio only after a key press or click. Until then the mix is discarded, so sounds stay in step
const AUDIO_RATE = 44100, AUDIO_CHUNK = 1024, AUDIO_LEAD = 0.1;
const audio = { context: null, next: 0, last: 0, peak: 0, underruns: 0 };
function enableAudio() {
  audio.context ??= new AudioContext();
  if (audio.context.state !== 'running') audio.context.resume().catch(() => {});
}
addEventListener('keydown', enableAudio);
addEventListener('pointerdown', enableAudio);

function queueAudio(view) {
  const samples = new Int16Array(view.slice().buffer);
  const frames = samples.length / 2;
  const buffer = audio.context.createBuffer(2, frames, AUDIO_RATE);
  const left = buffer.getChannelData(0), right = buffer.getChannelData(1);
  for (let i = 0; i < frames; i++) {
    left[i] = samples[2 * i] / 32768;
    right[i] = samples[2 * i + 1] / 32768;
    audio.peak = Math.max(audio.peak, Math.abs(left[i]), Math.abs(right[i]));
  }
  const source = audio.context.createBufferSource();
  source.buffer = buffer;
  source.connect(audio.context.destination);
  source.start(audio.next);
  audio.next += frames / AUDIO_RATE;
}

function pumpAudio(now) {
  const context = audio.context;
  if (context?.state !== 'running') {
    const frames = Math.min(Math.round((now - audio.last) * AUDIO_RATE / 1000), AUDIO_RATE / 10);
    audio.last = now;
    if (frames > 0) game.MixAudio(frames, false);
    return;
  }
  audio.last = now;
  // After a stall (or a hidden tab) the queue has run dry: start again just ahead of the playback position
  if (audio.next < context.currentTime) {
    if (audio.next > 0) audio.underruns++;
    audio.next = context.currentTime + 0.02;
  }
  while (audio.next < context.currentTime + AUDIO_LEAD) game.MixAudio(AUDIO_CHUNK, true);
}

const runtime = await dotnet.create();
runtime.setModuleImports('webgl', webgl);
runtime.setModuleImports('host', {
  getCanvasWidth: () => canvas.width,
  getCanvasHeight: () => canvas.height,
  getTitle: () => document.title,
  setTitle: (title) => { document.title = title; },
  isFullScreen: () => !!document.fullscreenElement,
  setFullScreen: (on) => { if (on) canvas.requestFullscreen?.().catch(() => {}); else if (document.fullscreenElement) document.exitFullscreen(); },
  getKeyState: (view) => view.set(keys),
  alert: (text) => setStatus(text),
  queueAudio,
});
await runtime.runMain();
const game = (await runtime.getAssemblyExports(runtime.getConfig().mainAssemblyName)).S2HD.Program;
const FS = runtime.Module.FS;
setStatus('Choose the game data to start.');

// Browser storage (IndexedDB) for the zip of the Sonic 2 disassembly and the mods that the page makes from it. The
// files stay in this browser. 'mods' holds { version, files: { name: Uint8Array } }
const storage = {
  db: new Promise((resolve, reject) => {
    const request = indexedDB.open('s2hd', 1);
    request.onupgradeneeded = () => request.result.createObjectStore('files');
    request.onsuccess = () => resolve(request.result);
    request.onerror = () => reject(request.error);
  }),
  async run(mode, action) {
    const db = await this.db;
    return new Promise((resolve, reject) => {
      const request = action(db.transaction('files', mode).objectStore('files'));
      request.onsuccess = () => resolve(request.result);
      request.onerror = () => reject(request.error);
    });
  },
  get(key) { return this.run('readonly', (store) => store.get(key)); },
  put(key, value) { return this.run('readwrite', (store) => store.put(value, key)); },
  clear() { return this.run('readwrite', (store) => store.clear()); },
};
const storedText = document.getElementById('stored');
const forget = document.getElementById('forget');

async function showStored() {
  try {
    const disasm = await storage.get('s2disasm.zip');
    const mods = await storage.get('mods');
    const count = mods ? Object.keys(mods.files).length : 0;
    const parts = [];
    if (disasm) parts.push(`the disassembly zip (${(disasm.size / 1048576).toFixed(0)} MB)`);
    if (count) parts.push(`${count} imported mods`);
    storedText.textContent = parts.length ? `Stored in this browser: ${parts.join(' and ')}.` : 'No disassembly is stored.';
    forget.hidden = parts.length === 0;
    console.log(storedText.textContent);
  } catch (e) {
    storedText.textContent = `Browser storage is not available: ${e}`;
  }
}
showStored();

// The chosen zip of the disassembly, also when the browser cannot store it (for example in some private windows)
let disasmZip = null;
document.getElementById('disasm').addEventListener('change', async (e) => {
  disasmZip = e.target.files[0];
  if (!disasmZip) return;
  try {
    await storage.put('s2disasm.zip', disasmZip);
    await showStored();
    setStatus('The disassembly is stored. Choose the game data to start.');
  } catch (error) {
    setStatus(`The browser cannot store the disassembly (${error}). It is used for this visit only. Choose the game data to start.`);
  }
});
forget.addEventListener('click', async () => {
  await storage.clear();
  await showStored();
  setStatus('The stored files are deleted.');
});

// Writes the stored mods that the game data does not have. When mods are still missing, makes them from the stored
// disassembly, which takes a minute or two, and stores them. The game starts without the parts that are missing
async function prepareMods() {
  const version = game.GetImportVersion();
  const missing = () => game.GetMissingMods().split(' ').filter(Boolean);
  if (missing().length === 0) return;
  const stored = await storage.get('mods').catch(() => null);
  if (stored?.version === version) {
    for (const name of missing()) {
      if (stored.files[name]) writeFile(`/content/mods/${name}.dat`, stored.files[name]);
    }
  }
  if (missing().length === 0) return;
  const disasm = disasmZip ?? await storage.get('s2disasm.zip').catch(() => null);
  if (!disasm) {
    console.log(`Starting without the parts from the disassembly: ${missing().join(', ')}`);
    return;
  }
  setStatus('Reading the disassembly…');
  writeFile('/tmp/s2disasm.zip', new Uint8Array(await disasm.arrayBuffer()));
  await new Promise(requestAnimationFrame);
  const steps = game.PrepareImport('/tmp/s2disasm.zip');
  if (steps < 0) {
    setStatus('The stored zip does not contain the Sonic 2 disassembly (s2.asm). Starting without it…');
    await new Promise((resolve) => setTimeout(resolve, 3000));
    return;
  }
  const t0 = performance.now();
  for (let i = 0; i < steps; i++) {
    setStatus(`Making the parts from the disassembly (${i + 1} of ${steps}): ${game.GetImportStepDescription(i)}…`);
    await new Promise(requestAnimationFrame);
    const error = game.RunImportStep(i);
    if (error) {
      setStatus(`The import failed: ${error}. Starting without the missing parts…`);
      await new Promise((resolve) => setTimeout(resolve, 3000));
      break;
    }
  }
  game.FinishImport();
  console.log(`Import took ${((performance.now() - t0) / 1000).toFixed(1)} s`);
  // Keep the stored mods of this version, and add the new ones
  const files = stored?.version === version ? stored.files : {};
  for (const name of game.GetImportMods().split(' ')) {
    const path = `/content/mods/${name}.dat`;
    if (FS.analyzePath(path).exists) files[name] = FS.readFile(path);
  }
  await storage.put('mods', { version, files }).catch((e) => console.warn(`Cannot store the mods: ${e}`));
  await showStored();
}

function writeFile(path, data) {
  FS.mkdirTree(path.substring(0, path.lastIndexOf('/')));
  FS.writeFile(path, data);
}

async function loadZip(blob) {
  setStatus(`Reading ${(blob.size / 1048576).toFixed(0)} MB zip…`);
  writeFile('/tmp/game.zip', new Uint8Array(await blob.arrayBuffer()));
  setStatus('Extracting…');
  await new Promise(requestAnimationFrame);
  const count = game.ImportZip('/tmp/game.zip');
  console.log(`Extracted ${count} files`);
  await prepareMods();
  start();
}

async function loadFolder(files) {
  let count = 0;
  for (const file of files) {
    const path = file.webkitRelativePath.replace(/\\/g, '/');
    const dat = path.match(/(?:^|\/)(data|mods)\/([^/]+\.dat)$/i);
    if (dat) writeFile(`/content/${dat[1].toLowerCase()}/${dat[2]}`, new Uint8Array(await file.arrayBuffer()));
    else if (/(?:^|\/)credits\.film$/i.test(path)) writeFile('/content/credits.film', new Uint8Array(await file.arrayBuffer()));
    else continue;
    count++;
  }
  console.log(`Copied ${count} files`);
  await prepareMods();
  start();
}

// Returns null when the site has no file at url (some hosts answer missing files with index.html)
async function downloadZip(url) {
  const response = await fetch(url);
  if (!response.ok || (response.headers.get('Content-Type') || '').startsWith('text/html')) return null;
  const total = Number(response.headers.get('Content-Length')) || 0;
  const reader = response.body.getReader();
  const chunks = [];
  let received = 0, shownMB = -1;
  for (;;) {
    const { done, value } = await reader.read();
    if (done) break;
    chunks.push(value);
    received += value.length;
    const mb = Math.floor(received / 1048576);
    if (mb === shownMB) continue;
    shownMB = mb;
    setStatus(`Downloading game data… ${mb}${total ? ` of ${Math.floor(total / 1048576)}` : ''} MB`);
  }
  return new Blob(chunks);
}

document.getElementById('zip').addEventListener('change', (e) => e.target.files[0] && loadZip(e.target.files[0]));
document.getElementById('folder').addEventListener('change', (e) => loadFolder(e.target.files));
// ?disasm=<url> stores a zip of the disassembly from the site first, as the button does, for tests.
// ?data=<url> loads a zip from the site. Otherwise game.zip is loaded when the site was published with
// -p:BundleGameData=true, and the player is asked for the files when it is not there
(params.has('disasm') ? downloadZip(params.get('disasm')).then((blob) => (disasmZip = blob) && storage.put('s2disasm.zip', blob)) : Promise.resolve())
  .then(() => downloadZip(params.get('data') || 'game.zip'))
  .then((blob) => blob ? loadZip(blob) : params.has('data') && setStatus(`Could not download ${params.get('data')}.`))
  .catch((e) => console.warn(`Game data download failed: ${e}`));
if (params.has('shadertest')) { report(game.CompileAllShaders()); report('DONE'); }

// Game loop: logic runs at a fixed 60 Hz regardless of the display's refresh rate
const STEP = 1000 / 60;
const perf = { steps: [], intervals: [], mixes: [] };
const shotFrames = new Set((params.get('shots') || '').split(',').filter(Boolean).map(Number));
let last = 0, accumulator = 0, frames = 0, running = false;

async function start() {
  setStatus('Starting…');
  await new Promise(requestAnimationFrame);
  const t0 = performance.now();
  // ?args=--halfpipe%201 passes options as on the desktop command line. ?profile turns on the frame profiler
  const error = game.Start(params.get('args') || '', params.has('profile'));
  if (error) { setStatus(error); return; }
  console.log(`Started in ${(performance.now() - t0).toFixed(0)} ms`);
  panel.hidden = true;
  canvas.focus();
  running = true;
  last = audio.last = performance.now();
  enableAudio();
  requestAnimationFrame(loop);
}

function loop(now) {
  if (!running) return;
  perf.intervals.push(now - last);
  accumulator += Math.min(now - last, 250);
  last = now;
  // Small tolerance so a 60 Hz display runs exactly one step per frame despite timer jitter
  for (let n = 0; accumulator >= STEP - 2 && n < 4; n++) {
    const t0 = performance.now();
    const error = game.Frame();
    perf.steps.push(performance.now() - t0);
    accumulator -= STEP;
    frames++;
    if (error) { running = false; setStatus('Stopped: ' + error); panel.hidden = false; return; }
    if (frames % 300 === 0) reportPerformance();
    // ?shots=600,1800 reports the canvas at those frames (captured before the browser clears the buffer)
    if (shotFrames.has(frames)) report(`SHOT ${frames} ${canvas.toDataURL('image/png')}`);
  }
  const mixStart = performance.now();
  pumpAudio(now);
  perf.mixes.push(performance.now() - mixStart);
  requestAnimationFrame(loop);
}

function stats(values) {
  const sorted = [...values].sort((a, b) => a - b);
  const avg = sorted.reduce((s, v) => s + v, 0) / (sorted.length || 1);
  return { avg, p95: sorted[Math.floor(sorted.length * 0.95)] ?? 0, max: sorted[sorted.length - 1] ?? 0 };
}

function reportPerformance() {
  const step = stats(perf.steps), interval = stats(perf.intervals), mix = stats(perf.mixes);
  const over = perf.steps.filter((v) => v > STEP).length;
  const line = `PERF frame ${frames}: step avg ${step.avg.toFixed(2)} p95 ${step.p95.toFixed(2)} max ${step.max.toFixed(1)} ms | ` +
    `over budget ${over}/${perf.steps.length} | audio mix avg ${mix.avg.toFixed(2)} p95 ${mix.p95.toFixed(2)} max ${mix.max.toFixed(1)} ms | rAF interval avg ${interval.avg.toFixed(2)} p95 ${interval.p95.toFixed(2)} ms | ` +
    `audio ${audio.context?.state ?? 'off'} peak ${audio.peak.toFixed(2)} underruns ${audio.underruns}`;
  audio.peak = 0;
  audio.underruns = 0;
  report(line);
  hud.textContent = `${(1000 / interval.avg).toFixed(0)} fps · step ${step.avg.toFixed(1)} ms (p95 ${step.p95.toFixed(1)}) · audio ${mix.avg.toFixed(1)} ms`;
  perf.steps.length = 0;
  perf.intervals.length = 0;
  perf.mixes.length = 0;
}
