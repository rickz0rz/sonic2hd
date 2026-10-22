import { dotnet } from './_framework/dotnet.js';
import * as webgl from './webgl.js';

const params = new URLSearchParams(location.search);
const canvas = document.getElementById('game');
const panel = document.getElementById('panel');
const status = document.getElementById('status');
const hud = document.getElementById('hud');

// Diagnostics: when served by tools/web-server, console output is also POSTed to /log
const report = (text) => fetch('/log', { method: 'POST', body: text }).catch(() => {});
for (const level of ['log', 'warn', 'error']) {
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
});
await runtime.runMain();
const game = (await runtime.getAssemblyExports(runtime.getConfig().mainAssemblyName)).S2HD.Program;
const FS = runtime.Module.FS;
setStatus('Choose the game data to start.');

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
  start();
}

async function loadFolder(files) {
  let count = 0;
  for (const file of files) {
    const path = file.webkitRelativePath.replace(/\\/g, '/');
    const dat = path.match(/(?:^|\/)data\/([^/]+\.dat)$/i);
    if (dat) writeFile('/content/data/' + dat[1], new Uint8Array(await file.arrayBuffer()));
    else if (/(?:^|\/)credits\.film$/i.test(path)) writeFile('/content/credits.film', new Uint8Array(await file.arrayBuffer()));
    else continue;
    count++;
  }
  console.log(`Copied ${count} files`);
  start();
}

document.getElementById('zip').addEventListener('change', (e) => e.target.files[0] && loadZip(e.target.files[0]));
document.getElementById('folder').addEventListener('change', (e) => loadFolder(e.target.files));
if (params.has('data')) fetch(params.get('data')).then((r) => r.blob()).then(loadZip);
if (params.has('shadertest')) { report(game.CompileAllShaders()); report('DONE'); }

// Game loop: logic runs at a fixed 60 Hz regardless of the display's refresh rate
const STEP = 1000 / 60;
const perf = { steps: [], intervals: [] };
const shotFrames = new Set((params.get('shots') || '').split(',').filter(Boolean).map(Number));
let last = 0, accumulator = 0, frames = 0, running = false;

async function start() {
  setStatus('Starting…');
  await new Promise(requestAnimationFrame);
  const t0 = performance.now();
  const error = game.Start();
  if (error) { setStatus(error); return; }
  console.log(`Started in ${(performance.now() - t0).toFixed(0)} ms`);
  panel.hidden = true;
  canvas.focus();
  running = true;
  last = performance.now();
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
  requestAnimationFrame(loop);
}

function stats(values) {
  const sorted = [...values].sort((a, b) => a - b);
  const avg = sorted.reduce((s, v) => s + v, 0) / (sorted.length || 1);
  return { avg, p95: sorted[Math.floor(sorted.length * 0.95)] ?? 0, max: sorted[sorted.length - 1] ?? 0 };
}

function reportPerformance() {
  const step = stats(perf.steps), interval = stats(perf.intervals);
  const over = perf.steps.filter((v) => v > STEP).length;
  const line = `PERF frame ${frames}: step avg ${step.avg.toFixed(2)} p95 ${step.p95.toFixed(2)} max ${step.max.toFixed(1)} ms | ` +
    `over budget ${over}/${perf.steps.length} | rAF interval avg ${interval.avg.toFixed(2)} p95 ${interval.p95.toFixed(2)} ms`;
  report(line);
  hud.textContent = `${(1000 / interval.avg).toFixed(0)} fps · step ${step.avg.toFixed(1)} ms (p95 ${step.p95.toFixed(1)})`;
  perf.steps.length = 0;
  perf.intervals.length = 0;
}
