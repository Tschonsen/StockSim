import { createLogger } from './logger';

const log = createLogger('Audio');

/**
 * Audio system using Web Audio API for synthesized sounds.
 * Bible 17: Dezent, professionell. Sounds for trading, news, market events.
 *
 * Uses synthesized tones as placeholders. Replace with real audio files
 * from Pixabay/Freesound/Mixkit for production (Bible 17.5).
 */
type MusicMood = 'calm' | 'steady' | 'momentum' | 'tension' | 'crisis';

class AudioManager {
  private ctx: AudioContext | null = null;
  private masterVolume = 0.5;
  private sfxVolume = 0.7;
  private musicVolume = 0.3;
  private enabled = true;

  // Background music state (Bible 17.2)
  private _currentMood: MusicMood = 'calm';
  private _musicOscillators: OscillatorNode[] = [];
  private _musicGains: GainNode[] = [];
  private _musicPlaying = false;
  private _musicEnabled = true;

  private getCtx(): AudioContext {
    if (!this.ctx) {
      this.ctx = new AudioContext();
    }
    return this.ctx;
  }

  setMasterVolume(v: number) { this.masterVolume = Math.max(0, Math.min(1, v)); this.updateMusicVolume(); }
  setSfxVolume(v: number) { this.sfxVolume = Math.max(0, Math.min(1, v)); }
  setMusicVolume(v: number) { this.musicVolume = Math.max(0, Math.min(1, v)); this.updateMusicVolume(); }
  setEnabled(e: boolean) { this.enabled = e; if (!e) this.stopMusic(); }
  setMusicEnabled(e: boolean) { this._musicEnabled = e; if (!e) this.stopMusic(); }

  private vol(): number { return this.masterVolume * this.sfxVolume; }

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
    } catch {
      // Audio may not be available
    }
  }

  private playChord(freqs: number[], duration: number, type: OscillatorType = 'sine', volume = 1) {
    freqs.forEach(f => this.playTone(f, duration, type, volume / freqs.length));
  }

  // --- Trading Sounds (Bible 17.3.2) ---

  /** Confirmation blip when order is placed */
  orderPlaced() {
    this.playTone(800, 0.1, 'sine', 0.6);
    setTimeout(() => this.playTone(1000, 0.1, 'sine', 0.4), 80);
    log.debug('SFX: order_placed');
  }

  /** Buy order filled - ascending cha-ching */
  orderFilledBuy() {
    this.playTone(600, 0.08, 'sine', 0.5);
    setTimeout(() => this.playTone(800, 0.08, 'sine', 0.5), 60);
    setTimeout(() => this.playTone(1200, 0.15, 'sine', 0.4), 120);
    log.debug('SFX: order_filled_buy');
  }

  /** Sell order filled - descending tone */
  orderFilledSell() {
    this.playTone(1000, 0.08, 'sine', 0.5);
    setTimeout(() => this.playTone(800, 0.08, 'sine', 0.5), 60);
    setTimeout(() => this.playTone(600, 0.15, 'sine', 0.4), 120);
    log.debug('SFX: order_filled_sell');
  }

  /** Order rejected - dull error */
  orderRejected() {
    this.playTone(200, 0.2, 'square', 0.3);
    log.debug('SFX: order_rejected');
  }

  // --- Event Sounds (Bible 17.3.3) ---

  /** Breaking news jingle */
  breakingNews() {
    this.playChord([523, 659, 784], 0.15, 'sine', 0.6); // C major
    setTimeout(() => this.playChord([587, 740, 880], 0.3, 'sine', 0.5), 150); // D major
    log.debug('SFX: breaking_news');
  }

  /** Market bell (open/close) */
  marketBell() {
    this.playTone(880, 0.5, 'sine', 0.6);
    setTimeout(() => this.playTone(880, 0.4, 'sine', 0.4), 300);
    setTimeout(() => this.playTone(880, 0.3, 'sine', 0.3), 550);
    log.debug('SFX: market_bell');
  }

  /** Price alert triggered */
  priceAlert() {
    this.playTone(1047, 0.1, 'sine', 0.5);
    setTimeout(() => this.playTone(1319, 0.1, 'sine', 0.5), 100);
    setTimeout(() => this.playTone(1568, 0.2, 'sine', 0.4), 200);
    log.debug('SFX: price_alert');
  }

  /** Flash crash alarm */
  flashCrash() {
    this.playTone(400, 0.3, 'sawtooth', 0.4);
    setTimeout(() => this.playTone(300, 0.3, 'sawtooth', 0.4), 250);
    setTimeout(() => this.playTone(200, 0.5, 'sawtooth', 0.3), 500);
    log.debug('SFX: flash_crash');
  }

  // --- UI Sounds (Bible 17.3.1) ---

  /** Soft click */
  click() {
    this.playTone(1200, 0.03, 'sine', 0.2);
    log.debug('SFX: click');
  }

  /** Notification ping */
  notification() {
    this.playTone(1400, 0.1, 'sine', 0.3);
    log.debug('SFX: notification');
  }

  /** Short squeeze alarm - urgent, siren-like (Bible 17.3.3) */
  shortSqueezeAlarm() {
    this.playTone(600, 0.15, 'sawtooth', 0.4);
    setTimeout(() => this.playTone(800, 0.15, 'sawtooth', 0.4), 150);
    setTimeout(() => this.playTone(600, 0.15, 'sawtooth', 0.3), 300);
    setTimeout(() => this.playTone(800, 0.15, 'sawtooth', 0.3), 450);
    setTimeout(() => this.playTone(1000, 0.3, 'sawtooth', 0.3), 600);
    log.debug('SFX: short_squeeze_alarm');
  }

  /** Achievement unlocked - triumphant fanfare */
  achievement() {
    this.playChord([523, 659, 784], 0.2, 'sine', 0.5); // C major
    setTimeout(() => this.playChord([587, 740, 880], 0.2, 'sine', 0.5), 200); // D major
    setTimeout(() => this.playChord([659, 831, 988], 0.3, 'sine', 0.5), 400); // E major
    setTimeout(() => this.playTone(1047, 0.5, 'sine', 0.6), 600); // High C
    log.debug('SFX: achievement');
  }

  // --- UI Sounds (Bible 17.3.1) ---

  /** Modal open sound */
  modalOpen() {
    this.playTone(800, 0.06, 'sine', 0.15);
    setTimeout(() => this.playTone(1000, 0.08, 'sine', 0.12), 40);
    log.debug('SFX: modal_open');
  }

  /** Modal close sound */
  modalClose() {
    this.playTone(1000, 0.06, 'sine', 0.12);
    setTimeout(() => this.playTone(800, 0.08, 'sine', 0.10), 40);
    log.debug('SFX: modal_close');
  }

  // --- Background Music System (Bible 17.2) ---
  // Synthesized ambient drones as placeholders for real audio tracks.
  // 5 moods mapped to volatility regime: calm, steady, momentum, tension, crisis.

  private static readonly MOOD_CONFIGS: Record<MusicMood, { freqs: number[]; type: OscillatorType; tempo: number }> = {
    calm:     { freqs: [130.81, 196.00, 261.63], type: 'sine', tempo: 0.5 },     // C3+G3+C4, slow
    steady:   { freqs: [146.83, 220.00, 293.66], type: 'sine', tempo: 0.7 },     // D3+A3+D4
    momentum: { freqs: [164.81, 246.94, 329.63], type: 'triangle', tempo: 1.0 }, // E3+B3+E4
    tension:  { freqs: [138.59, 207.65, 277.18], type: 'sawtooth', tempo: 1.3 }, // Db3+Ab3+Db4, dark
    crisis:   { freqs: [123.47, 185.00, 246.94], type: 'sawtooth', tempo: 2.0 }, // B2+Gb3+B3, urgent
  };

  /** Set music mood and crossfade. Called from game based on volatility regime. */
  setMood(mood: MusicMood) {
    if (mood === this._currentMood && this._musicPlaying) return;
    this._currentMood = mood;
    if (this._musicPlaying) {
      this.stopMusic();
      this.startMusic();
    }
  }

  /** Start ambient background music */
  startMusic() {
    if (!this._musicEnabled || !this.enabled || this._musicPlaying) return;
    try {
      const ctx = this.getCtx();
      const config = AudioManager.MOOD_CONFIGS[this._currentMood];
      const vol = this.masterVolume * this.musicVolume * 0.08; // Very quiet

      config.freqs.forEach((freq, i) => {
        const osc = ctx.createOscillator();
        const gain = ctx.createGain();
        osc.type = config.type;
        osc.frequency.value = freq;
        // Slight detuning for richness
        osc.detune.value = (i - 1) * 5;
        gain.gain.value = 0;
        // Fade in over 3 seconds
        gain.gain.linearRampToValueAtTime(vol, ctx.currentTime + 3);
        osc.connect(gain);
        gain.connect(ctx.destination);
        osc.start();
        this._musicOscillators.push(osc);
        this._musicGains.push(gain);
      });

      this._musicPlaying = true;
      log.info('Music started', { mood: this._currentMood });
    } catch {
      // Audio not available
    }
  }

  /** Stop background music with fade-out */
  stopMusic() {
    if (!this._musicPlaying) return;
    try {
      const ctx = this.getCtx();
      this._musicGains.forEach(g => {
        g.gain.linearRampToValueAtTime(0, ctx.currentTime + 2);
      });
      // Clean up after fade
      setTimeout(() => {
        this._musicOscillators.forEach(o => { try { o.stop(); } catch { /* */ } });
        this._musicOscillators = [];
        this._musicGains = [];
      }, 2500);
    } catch { /* */ }
    this._musicPlaying = false;
    log.info('Music stopped');
  }

  private updateMusicVolume() {
    const vol = this.masterVolume * this.musicVolume * 0.08;
    this._musicGains.forEach(g => {
      try { g.gain.value = vol; } catch { /* */ }
    });
  }

  get currentMood(): MusicMood { return this._currentMood; }
  get isMusicPlaying(): boolean { return this._musicPlaying; }
}

export const audio = new AudioManager();
