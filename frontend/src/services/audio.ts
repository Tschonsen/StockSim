import { createLogger } from './logger';

const log = createLogger('Audio');

/**
 * Audio system using Web Audio API for synthesized sounds.
 * Bible 17: Dezent, professionell. Sounds for trading, news, market events.
 *
 * Uses synthesized tones as placeholders. Replace with real audio files
 * from Pixabay/Freesound/Mixkit for production (Bible 17.5).
 */
class AudioManager {
  private ctx: AudioContext | null = null;
  private masterVolume = 0.5;
  private sfxVolume = 0.7;
  private enabled = true;

  private getCtx(): AudioContext {
    if (!this.ctx) {
      this.ctx = new AudioContext();
    }
    return this.ctx;
  }

  setMasterVolume(v: number) { this.masterVolume = Math.max(0, Math.min(1, v)); }
  setSfxVolume(v: number) { this.sfxVolume = Math.max(0, Math.min(1, v)); }
  setEnabled(e: boolean) { this.enabled = e; }

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
}

export const audio = new AudioManager();
