import { createLogger } from './logger';

const log = createLogger('Audio');

/**
 * Audio system: real sound files with synthesized fallback.
 * Spec 17: Dezent, professionell. Trading floor atmosphere.
 *
 * Sound files in /public/sounds/ (Pixabay License, free commercial use).
 * If a file is missing, falls back to Web Audio API synthesized tone.
 */
type MusicMood = 'calm' | 'steady' | 'momentum' | 'tension' | 'crisis';

// Sound file paths (relative — works with both dev server and Electron file://)
const SOUNDS = {
  bell: './sounds/bell.mp3',
  ticker: './sounds/ticker.mp3',
  click: './sounds/click.mp3',
  buy: './sounds/buy.mp3',
  sell: './sounds/sell.mp3',
  error: './sounds/error.mp3',
  news: './sounds/news.mp3',
  alarm: './sounds/alarm.mp3',
  achievement: './sounds/achievement.mp3',
  gavel: './sounds/gavel.mp3',
  coins: './sounds/coins.mp3',
  champagne: './sounds/champagne.mp3',
  ambienceOffice: './sounds/ambience-office.mp3',
  ambienceCrisis: './sounds/ambience-crisis.mp3',
} as const;

type SoundKey = keyof typeof SOUNDS;

class AudioManager {
  private ctx: AudioContext | null = null;
  private masterVolume = 0.5;
  private sfxVolume = 0.7;
  private musicVolume = 0.3;
  private enabled = true;

  // Preloaded audio buffers
  private buffers: Map<SoundKey, AudioBuffer> = new Map();
  private loadedSounds: Set<SoundKey> = new Set();

  // Background music / ambience
  private _currentMood: MusicMood = 'calm';
  private _ambienceSource: AudioBufferSourceNode | null = null;
  private _ambienceGain: GainNode | null = null;
  private _ambiencePlaying = false;
  private _musicEnabled = true;

  // Synthesizer fallback state
  private _musicOscillators: OscillatorNode[] = [];
  private _musicGains: GainNode[] = [];
  private _synthMusicPlaying = false;

  private getCtx(): AudioContext {
    if (!this.ctx) {
      this.ctx = new AudioContext();
    }
    return this.ctx;
  }

  /** Preload all sound files. Call once at app start. */
  async preload() {
    const ctx = this.getCtx();
    const entries = Object.entries(SOUNDS) as [SoundKey, string][];

    await Promise.allSettled(entries.map(async ([key, path]) => {
      try {
        const res = await fetch(path);
        if (!res.ok) return; // File not found — will use synth fallback
        const arrayBuf = await res.arrayBuffer();
        const audioBuf = await ctx.decodeAudioData(arrayBuf);
        this.buffers.set(key, audioBuf);
        this.loadedSounds.add(key);
      } catch {
        // Silent fail — synth fallback will be used
      }
    }));

    log.info('Audio preloaded', {
      loaded: this.loadedSounds.size,
      total: entries.length,
      files: [...this.loadedSounds],
    });
  }

  setMasterVolume(v: number) { this.masterVolume = Math.max(0, Math.min(1, v)); }
  setSfxVolume(v: number) { this.sfxVolume = Math.max(0, Math.min(1, v)); }
  setMusicVolume(v: number) { this.musicVolume = Math.max(0, Math.min(1, v)); this.updateAmbienceVolume(); }
  setEnabled(e: boolean) { this.enabled = e; if (!e) this.stopMusic(); }
  setMusicEnabled(e: boolean) { this._musicEnabled = e; if (!e) this.stopMusic(); }

  private vol(): number { return this.masterVolume * this.sfxVolume; }

  /** Play a preloaded sound file, or fall back to synth tone. */
  private playSound(key: SoundKey, volume = 1, fallbackFreq?: number, fallbackDur = 0.15) {
    if (!this.enabled) return;
    try {
      const ctx = this.getCtx();
      const buf = this.buffers.get(key);
      if (buf) {
        const source = ctx.createBufferSource();
        const gain = ctx.createGain();
        source.buffer = buf;
        gain.gain.value = this.vol() * volume * 0.5;
        source.connect(gain);
        gain.connect(ctx.destination);
        source.start();
      } else if (fallbackFreq) {
        this.playTone(fallbackFreq, fallbackDur, 'sine', volume);
      }
    } catch { /* Audio may not be available */ }
  }

  private playTone(freq: number, duration: number, type: OscillatorType = 'sine', volume = 1) {
    if (!this.enabled) return;
    try {
      const ctx = this.getCtx();
      const osc = ctx.createOscillator();
      const gain = ctx.createGain();
      osc.type = type;
      osc.frequency.value = freq;
      gain.gain.value = this.vol() * volume * 0.3;
      gain.gain.exponentialRampToValueAtTime(0.001, ctx.currentTime + duration);
      osc.connect(gain);
      gain.connect(ctx.destination);
      osc.start();
      osc.stop(ctx.currentTime + duration);
    } catch { /* */ }
  }

  private playChord(freqs: number[], duration: number, type: OscillatorType = 'sine', volume = 1) {
    freqs.forEach(f => this.playTone(f, duration, type, volume / freqs.length));
  }

  // === Trading Sounds ===

  orderPlaced() {
    this.playSound('click', 0.6, 800, 0.1);
    log.debug('SFX: order_placed');
  }

  orderFilledBuy() {
    this.playSound('buy', 0.7, 1200, 0.15);
    log.debug('SFX: order_filled_buy');
  }

  orderFilledSell() {
    this.playSound('sell', 0.7, 600, 0.15);
    log.debug('SFX: order_filled_sell');
  }

  orderRejected() {
    this.playSound('error', 0.5, 200, 0.2);
    log.debug('SFX: order_rejected');
  }

  // === Event Sounds ===

  breakingNews() {
    this.playSound('news', 0.6, undefined);
    if (!this.loadedSounds.has('news')) {
      this.playChord([523, 659, 784], 0.15, 'sine', 0.6);
      setTimeout(() => this.playChord([587, 740, 880], 0.3, 'sine', 0.5), 150);
    }
    log.debug('SFX: breaking_news');
  }

  marketBell() {
    this.playSound('bell', 0.8, 880, 0.5);
    log.debug('SFX: market_bell');
  }

  priceAlert() {
    this.playTone(1047, 0.1, 'sine', 0.5);
    setTimeout(() => this.playTone(1319, 0.1, 'sine', 0.5), 100);
    setTimeout(() => this.playTone(1568, 0.2, 'sine', 0.4), 200);
    log.debug('SFX: price_alert');
  }

  flashCrash() {
    this.playSound('alarm', 0.7, undefined);
    if (!this.loadedSounds.has('alarm')) {
      this.playTone(400, 0.3, 'sawtooth', 0.4);
      setTimeout(() => this.playTone(300, 0.3, 'sawtooth', 0.4), 250);
      setTimeout(() => this.playTone(200, 0.5, 'sawtooth', 0.3), 500);
    }
    log.debug('SFX: flash_crash');
  }

  circuitBreaker() {
    this.playSound('gavel', 0.8, 300, 0.3);
    log.debug('SFX: circuit_breaker');
  }

  dividendPaid() {
    this.playSound('coins', 0.5, undefined);
    if (!this.loadedSounds.has('coins')) {
      this.playTone(800, 0.1, 'sine', 0.3);
      setTimeout(() => this.playTone(1000, 0.1, 'sine', 0.3), 80);
      setTimeout(() => this.playTone(1200, 0.1, 'sine', 0.3), 160);
    }
    log.debug('SFX: dividend_paid');
  }

  milestone() {
    this.playSound('champagne', 0.8, undefined);
    if (!this.loadedSounds.has('champagne')) {
      this.playChord([523, 659, 784], 0.2, 'sine', 0.5);
      setTimeout(() => this.playChord([659, 831, 988], 0.3, 'sine', 0.5), 200);
      setTimeout(() => this.playTone(1047, 0.5, 'sine', 0.6), 400);
    }
    log.debug('SFX: milestone');
  }

  // === UI Sounds ===

  click() {
    this.playSound('click', 0.2, 1200, 0.03);
  }

  notification() {
    this.playTone(1400, 0.1, 'sine', 0.3);
  }

  shortSqueezeAlarm() {
    this.playSound('alarm', 0.5, undefined);
    if (!this.loadedSounds.has('alarm')) {
      this.playTone(600, 0.15, 'sawtooth', 0.4);
      setTimeout(() => this.playTone(800, 0.15, 'sawtooth', 0.4), 150);
      setTimeout(() => this.playTone(1000, 0.3, 'sawtooth', 0.3), 300);
    }
    log.debug('SFX: short_squeeze_alarm');
  }

  achievement() {
    this.playSound('achievement', 0.8, undefined);
    if (!this.loadedSounds.has('achievement')) {
      this.playChord([523, 659, 784], 0.2, 'sine', 0.5);
      setTimeout(() => this.playChord([587, 740, 880], 0.2, 'sine', 0.5), 200);
      setTimeout(() => this.playChord([659, 831, 988], 0.3, 'sine', 0.5), 400);
      setTimeout(() => this.playTone(1047, 0.5, 'sine', 0.6), 600);
    }
    log.debug('SFX: achievement');
  }

  modalOpen() {
    this.playTone(800, 0.06, 'sine', 0.15);
    setTimeout(() => this.playTone(1000, 0.08, 'sine', 0.12), 40);
  }

  modalClose() {
    this.playTone(1000, 0.06, 'sine', 0.12);
    setTimeout(() => this.playTone(800, 0.08, 'sine', 0.10), 40);
  }

  // === Ambience System ===

  /** Set ambience mood. Switches between office and crisis loops (or synth). */
  setMood(mood: MusicMood) {
    if (mood === this._currentMood && this._ambiencePlaying) return;
    this._currentMood = mood;
    this.stopMusic();
    this.startMusic();
  }

  startMusic() {
    if (!this._musicEnabled || !this.enabled || this._ambiencePlaying) return;

    // Try file-based ambience first
    const key: SoundKey = this._currentMood === 'crisis' || this._currentMood === 'tension'
      ? 'ambienceCrisis' : 'ambienceOffice';

    if (this.loadedSounds.has(key)) {
      const ctx = this.getCtx();
      this._ambienceGain = ctx.createGain();
      this._ambienceGain.gain.value = 0;
      this._ambienceGain.gain.linearRampToValueAtTime(
        this.masterVolume * this.musicVolume * 0.15, ctx.currentTime + 3
      );
      this._ambienceGain.connect(ctx.destination);

      const source = ctx.createBufferSource();
      source.buffer = this.buffers.get(key)!;
      source.loop = true;
      source.connect(this._ambienceGain);
      source.start();
      this._ambienceSource = source;
      this._ambiencePlaying = true;
      log.info('Ambience started (file)', { mood: this._currentMood });
    } else {
      // Fallback: synthesized ambient drones
      this.startSynthMusic();
    }
  }

  stopMusic() {
    // Stop file-based ambience
    if (this._ambienceSource) {
      try {
        this._ambienceGain?.gain.linearRampToValueAtTime(0, this.getCtx().currentTime + 2);
        setTimeout(() => {
          try { this._ambienceSource?.stop(); } catch { /* */ }
          this._ambienceSource = null;
          this._ambienceGain = null;
        }, 2500);
      } catch { /* */ }
    }
    this._ambiencePlaying = false;

    // Stop synth music
    this.stopSynthMusic();
    log.info('Music stopped');
  }

  private updateAmbienceVolume() {
    const vol = this.masterVolume * this.musicVolume * 0.15;
    if (this._ambienceGain) {
      try { this._ambienceGain.gain.value = vol; } catch { /* */ }
    }
    this._musicGains.forEach(g => {
      try { g.gain.value = this.masterVolume * this.musicVolume * 0.08; } catch { /* */ }
    });
  }

  // === Synthesized Ambient Fallback ===

  private static readonly MOOD_CONFIGS: Record<MusicMood, { freqs: number[]; type: OscillatorType }> = {
    calm:     { freqs: [130.81, 196.00, 261.63], type: 'sine' },
    steady:   { freqs: [146.83, 220.00, 293.66], type: 'sine' },
    momentum: { freqs: [164.81, 246.94, 329.63], type: 'triangle' },
    tension:  { freqs: [138.59, 207.65, 277.18], type: 'sawtooth' },
    crisis:   { freqs: [123.47, 185.00, 246.94], type: 'sawtooth' },
  };

  private startSynthMusic() {
    if (this._synthMusicPlaying) return;
    try {
      const ctx = this.getCtx();
      const config = AudioManager.MOOD_CONFIGS[this._currentMood];
      const vol = this.masterVolume * this.musicVolume * 0.08;
      config.freqs.forEach((freq, i) => {
        const osc = ctx.createOscillator();
        const gain = ctx.createGain();
        osc.type = config.type;
        osc.frequency.value = freq;
        osc.detune.value = (i - 1) * 5;
        gain.gain.value = 0;
        gain.gain.linearRampToValueAtTime(vol, ctx.currentTime + 3);
        osc.connect(gain);
        gain.connect(ctx.destination);
        osc.start();
        this._musicOscillators.push(osc);
        this._musicGains.push(gain);
      });
      this._synthMusicPlaying = true;
      this._ambiencePlaying = true;
    } catch { /* */ }
  }

  private stopSynthMusic() {
    if (!this._synthMusicPlaying) return;
    try {
      const ctx = this.getCtx();
      this._musicGains.forEach(g => g.gain.linearRampToValueAtTime(0, ctx.currentTime + 2));
      setTimeout(() => {
        this._musicOscillators.forEach(o => { try { o.stop(); } catch { /* */ } });
        this._musicOscillators = [];
        this._musicGains = [];
      }, 2500);
    } catch { /* */ }
    this._synthMusicPlaying = false;
  }

  get currentMood(): MusicMood { return this._currentMood; }
  get isMusicPlaying(): boolean { return this._ambiencePlaying; }
  get loadedCount(): number { return this.loadedSounds.size; }
}

export const audio = new AudioManager();
