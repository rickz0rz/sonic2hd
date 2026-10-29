// Plays the mix of the game on the audio thread. The page sends chunks of 16-bit stereo samples at the rate of the
// context. The processor plays them back to back and reports the frames that it played, so the page can keep a
// short lead. When the queue is empty, it plays silence and counts an underrun.
const REPORT_BLOCKS = 4;

class GameAudioProcessor extends AudioWorkletProcessor {
  constructor() {
    super();
    this.chunks = [];
    this.offset = 0;
    this.played = 0;
    this.peak = 0;
    this.underruns = 0;
    this.starved = true;
    this.blocks = 0;
    this.port.onmessage = (e) => this.chunks.push(e.data);
  }

  process(inputs, outputs) {
    const [left, right] = outputs[0];
    const length = left.length;
    let i = 0;
    while (i < length && this.chunks.length > 0) {
      const chunk = this.chunks[0];
      const count = Math.min(length - i, chunk.length / 2 - this.offset);
      for (let k = 0; k < count; k++) {
        const l = chunk[2 * (this.offset + k)] / 32768, r = chunk[2 * (this.offset + k) + 1] / 32768;
        left[i + k] = l;
        right[i + k] = r;
        this.peak = Math.max(this.peak, Math.abs(l), Math.abs(r));
      }
      i += count;
      this.offset += count;
      if (this.offset * 2 >= chunk.length) {
        this.chunks.shift();
        this.offset = 0;
      }
    }
    this.played += i;
    if (i < length) {
      left.fill(0, i);
      right.fill(0, i);
      // Count an underrun only when the queue runs dry during playback, not before the first chunk
      if (!this.starved) this.underruns++;
      this.starved = true;
    } else {
      this.starved = false;
    }
    if (++this.blocks % REPORT_BLOCKS === 0) {
      this.port.postMessage({ played: this.played, peak: this.peak, underruns: this.underruns });
      this.peak = 0;
      this.underruns = 0;
    }
    return true;
  }
}

registerProcessor('game-audio', GameAudioProcessor);
