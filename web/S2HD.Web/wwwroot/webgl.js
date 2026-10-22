// WebGL2 functions imported by Platform/GL.cs ([JSImport(..., "webgl")]).
// WebGL objects are JavaScript objects; C# refers to them by integer handles (0 means none).

let gl = null;
const objects = [null];
const freeHandles = [];
const uniformLocations = [];

const RGB = 0x1907, RGBA = 0x1908, BGR = 0x80E0, BGRA = 0x80E1;
const COMPILE_STATUS = 0x8B81, LINK_STATUS = 0x8B82;

function add(object) {
  if (!object) return 0;
  const handle = freeHandles.length ? freeHandles.pop() : objects.length;
  objects[handle] = object;
  return handle;
}
const get = (handle) => (handle ? objects[handle] : null);
function remove(handle) {
  const object = get(handle);
  if (handle) { objects[handle] = undefined; freeHandles.push(handle); }
  return object;
}

// A Span<byte> arrives as a memory view over WebAssembly memory, valid for the duration of the call
const bytes = (view) => (view._unsafe_create_view ? view._unsafe_create_view() : view.slice());
function floats(view) {
  const u8 = bytes(view);
  return new Float32Array(u8.buffer, u8.byteOffset, u8.byteLength >> 2);
}
function ints(view) {
  const u8 = bytes(view);
  return new Int32Array(u8.buffer, u8.byteOffset, u8.byteLength >> 2);
}

export function init(canvas) {
  gl = canvas.getContext('webgl2', {
    alpha: false, antialias: false, depth: true, stencil: true,
    premultipliedAlpha: false, preserveDrawingBuffer: false, powerPreference: 'high-performance',
  });
  if (!gl) throw new Error('WebGL2 is not available in this browser.');
  return gl;
}

export const enable = (cap) => gl.enable(cap);
export const disable = (cap) => gl.disable(cap);
export const isEnabled = (cap) => gl.isEnabled(cap);
export const blendFunc = (s, d) => gl.blendFunc(s, d);
export const viewport = (x, y, w, h) => gl.viewport(x, y, w, h);
export const clearColor = (r, g, b, a) => gl.clearColor(r, g, b, a);
export const clear = (mask) => gl.clear(mask);
export const clearBufferColor = (drawBuffer) => gl.clearBufferfv(gl.COLOR, drawBuffer, [0, 0, 0, 0]);
export const stencilFunc = (fn, ref, mask) => gl.stencilFunc(fn, ref, mask);
export const stencilOp = (fail, zfail, pass) => gl.stencilOp(fail, zfail, pass);
export const stencilMask = (mask) => gl.stencilMask(mask);
export function getInteger(name) {
  const value = gl.getParameter(name);
  return typeof value === 'number' ? value : value ? objects.indexOf(value) : 0;
}
export const getString = (name) => String(gl.getParameter(name));
export const finish = () => gl.finish();

export const createBuffer = () => add(gl.createBuffer());
export const deleteBuffer = (h) => gl.deleteBuffer(remove(h));
export const bindBuffer = (target, h) => gl.bindBuffer(target, get(h));
export const bufferData = (target, view, usage) => gl.bufferData(target, bytes(view), usage);

export const createVertexArray = () => add(gl.createVertexArray());
export const deleteVertexArray = (h) => gl.deleteVertexArray(remove(h));
export const bindVertexArray = (h) => gl.bindVertexArray(get(h));
export const vertexAttribPointer = (i, size, type, norm, stride, offset) => gl.vertexAttribPointer(i, size, type, norm, stride, offset);
export const enableVertexAttribArray = (i) => gl.enableVertexAttribArray(i);
export const drawArrays = (mode, first, count) => gl.drawArrays(mode, first, count);
export const drawElements = (mode, count, type, offset) => gl.drawElements(mode, count, type, offset);

export const createTexture = () => add(gl.createTexture());
export const deleteTexture = (h) => gl.deleteTexture(remove(h));
export const bindTexture = (target, h) => gl.bindTexture(target, get(h));
export const activeTexture = (unit) => gl.activeTexture(unit);
export const texParameter = (target, name, value) => gl.texParameteri(target, name, value);

// Always uploads RGBA: WebGL has no BGR formats, and unsized RGB textures cannot be framebuffer attachments
export function texImage2D(target, level, width, height, format, hasPixels, view) {
  let pixels = null;
  if (hasPixels) {
    const source = bytes(view);
    if (format === RGBA) {
      pixels = source;
    } else {
      const channels = format === RGB || format === BGR ? 3 : 4;
      const swap = format === BGR || format === BGRA;
      const stride = (width * channels + 3) & ~3; // rows are 4-byte aligned (GL_UNPACK_ALIGNMENT)
      pixels = new Uint8Array(width * height * 4);
      for (let y = 0; y < height; y++) {
        let s = y * stride, d = y * width * 4;
        for (let x = 0; x < width; x++, s += channels, d += 4) {
          pixels[d] = source[swap ? s + 2 : s];
          pixels[d + 1] = source[s + 1];
          pixels[d + 2] = source[swap ? s : s + 2];
          pixels[d + 3] = channels === 4 ? source[s + 3] : 255;
        }
      }
    }
  }
  gl.texImage2D(target, level, RGBA, width, height, 0, RGBA, gl.UNSIGNED_BYTE, pixels);
}

export const createFramebuffer = () => add(gl.createFramebuffer());
export const deleteFramebuffer = (h) => gl.deleteFramebuffer(remove(h));
export const bindFramebuffer = (target, h) => gl.bindFramebuffer(target, get(h));
export const checkFramebufferStatus = (target) => gl.checkFramebufferStatus(target);
export const framebufferTexture2D = (target, attachment, h, level) => gl.framebufferTexture2D(target, attachment, gl.TEXTURE_2D, get(h), level);
export const drawBuffers = (view) => gl.drawBuffers(Array.from(ints(view)));
export const createRenderbuffer = () => add(gl.createRenderbuffer());
export const bindRenderbuffer = (target, h) => gl.bindRenderbuffer(target, get(h));
export const renderbufferStorage = (target, format, w, h) => gl.renderbufferStorage(target, format, w, h);
export const framebufferRenderbuffer = (target, attachment, rbTarget, h) => gl.framebufferRenderbuffer(target, attachment, rbTarget, get(h));

export const createShader = (type) => add(gl.createShader(type));
export const shaderSource = (h, source) => gl.shaderSource(get(h), source);
export const compileShader = (h) => gl.compileShader(get(h));
export function getShaderParameter(h, name) {
  const value = gl.getShaderParameter(get(h), name);
  return typeof value === 'boolean' ? (value ? 1 : 0) : value;
}
export function getShaderInfoLog(h) {
  const log = gl.getShaderInfoLog(get(h)) || '';
  if (log && !gl.getShaderParameter(get(h), COMPILE_STATUS)) console.error('Shader compile error: ' + log);
  return log;
}
export const deleteShader = (h) => gl.deleteShader(remove(h));
export const createProgram = () => add(gl.createProgram());
export const attachShader = (p, s) => gl.attachShader(get(p), get(s));
export const linkProgram = (p) => gl.linkProgram(get(p));
export function getProgramParameter(h, name) {
  const value = gl.getProgramParameter(get(h), name);
  return typeof value === 'boolean' ? (value ? 1 : 0) : value;
}
export function getProgramInfoLog(h) {
  const log = gl.getProgramInfoLog(get(h)) || '';
  if (log && !gl.getProgramParameter(get(h), LINK_STATUS)) console.error('Program link error: ' + log);
  return log;
}
export const deleteProgram = (h) => gl.deleteProgram(remove(h));
export const useProgram = (h) => gl.useProgram(get(h));
export const getAttribLocation = (p, name) => gl.getAttribLocation(get(p), name);
// Uniform locations are objects too; the desktop code caches them per program and treats -1 as missing
export function getUniformLocation(p, name) {
  const location = gl.getUniformLocation(get(p), name);
  if (!location) return -1;
  uniformLocations.push(location);
  return uniformLocations.length - 1;
}
export const uniform1i = (l, v) => gl.uniform1i(uniformLocations[l], v);
export const uniform1f = (l, v) => gl.uniform1f(uniformLocations[l], v);
export const uniform2f = (l, x, y) => gl.uniform2f(uniformLocations[l], x, y);
export const uniform3f = (l, x, y, z) => gl.uniform3f(uniformLocations[l], x, y, z);
export const uniform4f = (l, x, y, z, w) => gl.uniform4f(uniformLocations[l], x, y, z, w);
export const uniformMatrix4fv = (l, transpose, view) => gl.uniformMatrix4fv(uniformLocations[l], transpose, floats(view));
