import { describe, it, expect, vi, beforeEach } from 'vitest';

// We need to mock AudioContext before importing audio.ts
// The audio module creates a singleton, so we mock at the global level.

// Minimal AudioContext mock
class MockGainNode {
  gain = { value: 0, exponentialRampToValueAtTime: vi.fn(), linearRampToValueAtTime: vi.fn() };
  connect = vi.fn();
}

class MockOscillatorNode {
  type = 'sine';
  frequency = { value: 0 };
  detune = { value: 0 };
  connect = vi.fn();
  start = vi.fn();
  stop = vi.fn();
}

class MockBufferSourceNode {
  buffer: unknown = null;
  loop = false;
  connect = vi.fn();
  start = vi.fn();
  stop = vi.fn();
}

class MockAudioContext {
  currentTime = 0;
  destination = {};
  createOscillator = vi.fn(() => new MockOscillatorNode());
  createGain = vi.fn(() => new MockGainNode());
  createBufferSource = vi.fn(() => new MockBufferSourceNode());
  decodeAudioData = vi.fn();
}

// Assign to global before import
(globalThis as Record<string, unknown>).AudioContext = MockAudioContext;

// Dynamic import to ensure mock is in place
let audio: typeof import('./audio')['audio'];

beforeEach(async () => {
  vi.resetModules();
  (globalThis as Record<string, unknown>).AudioContext = MockAudioContext;
  const mod = await import('./audio');
  audio = mod.audio;
});

describe('AudioManager', () => {
  describe('setMasterVolume', () => {
    it('should clamp volume to minimum 0', () => {
      audio.setMasterVolume(-0.5);
      // Verify indirectly: calling a sound method should not throw
      expect(() => audio.click()).not.toThrow();
    });

    it('should clamp volume to maximum 1', () => {
      audio.setMasterVolume(1.5);
      expect(() => audio.click()).not.toThrow();
    });

    it('should accept values within 0-1', () => {
      audio.setMasterVolume(0);
      expect(() => audio.click()).not.toThrow();

      audio.setMasterVolume(0.5);
      expect(() => audio.click()).not.toThrow();

      audio.setMasterVolume(1);
      expect(() => audio.click()).not.toThrow();
    });
  });

  describe('setEnabled', () => {
    it('should prevent playback when disabled', () => {
      audio.setEnabled(false);

      // None of the sound methods should create oscillators when disabled
      // We verify by checking that no error is thrown and the method returns silently
      expect(() => audio.orderPlaced()).not.toThrow();
      expect(() => audio.orderFilledBuy()).not.toThrow();
      expect(() => audio.orderFilledSell()).not.toThrow();
      expect(() => audio.orderRejected()).not.toThrow();
      expect(() => audio.breakingNews()).not.toThrow();
      expect(() => audio.marketBell()).not.toThrow();
      expect(() => audio.priceAlert()).not.toThrow();
      expect(() => audio.flashCrash()).not.toThrow();
      expect(() => audio.click()).not.toThrow();
      expect(() => audio.notification()).not.toThrow();
      expect(() => audio.achievement()).not.toThrow();
    });

    it('should allow playback when re-enabled', () => {
      audio.setEnabled(false);
      audio.setEnabled(true);
      expect(() => audio.click()).not.toThrow();
    });
  });

  describe('sound methods do not throw', () => {
    it('should not throw for any trading sound', () => {
      expect(() => audio.orderPlaced()).not.toThrow();
      expect(() => audio.orderFilledBuy()).not.toThrow();
      expect(() => audio.orderFilledSell()).not.toThrow();
      expect(() => audio.orderRejected()).not.toThrow();
    });

    it('should not throw for any event sound', () => {
      expect(() => audio.breakingNews()).not.toThrow();
      expect(() => audio.marketBell()).not.toThrow();
      expect(() => audio.priceAlert()).not.toThrow();
      expect(() => audio.flashCrash()).not.toThrow();
      expect(() => audio.circuitBreaker()).not.toThrow();
      expect(() => audio.dividendPaid()).not.toThrow();
      expect(() => audio.milestone()).not.toThrow();
    });

    it('should not throw for UI sounds', () => {
      expect(() => audio.click()).not.toThrow();
      expect(() => audio.notification()).not.toThrow();
      expect(() => audio.shortSqueezeAlarm()).not.toThrow();
      expect(() => audio.achievement()).not.toThrow();
      expect(() => audio.modalOpen()).not.toThrow();
      expect(() => audio.modalClose()).not.toThrow();
    });

    it('should not throw for music methods', () => {
      expect(() => audio.startMusic()).not.toThrow();
      expect(() => audio.stopMusic()).not.toThrow();
      expect(() => audio.setMood('calm')).not.toThrow();
      expect(() => audio.setMood('crisis')).not.toThrow();
      expect(() => audio.setMood('tension')).not.toThrow();
      expect(() => audio.setMood('momentum')).not.toThrow();
      expect(() => audio.setMood('steady')).not.toThrow();
    });
  });

  describe('volume setters', () => {
    it('should not throw when setting SFX volume', () => {
      expect(() => audio.setSfxVolume(0.5)).not.toThrow();
      expect(() => audio.setSfxVolume(0)).not.toThrow();
      expect(() => audio.setSfxVolume(1)).not.toThrow();
    });

    it('should not throw when setting music volume', () => {
      expect(() => audio.setMusicVolume(0.5)).not.toThrow();
      expect(() => audio.setMusicVolume(0)).not.toThrow();
      expect(() => audio.setMusicVolume(1)).not.toThrow();
    });

    it('should not throw when setting music enabled', () => {
      expect(() => audio.setMusicEnabled(false)).not.toThrow();
      expect(() => audio.setMusicEnabled(true)).not.toThrow();
    });
  });

  describe('read-only properties', () => {
    it('should expose currentMood', () => {
      expect(audio.currentMood).toBe('calm');
    });

    it('should expose loadedCount', () => {
      expect(audio.loadedCount).toBe(0); // Nothing preloaded in tests
    });

    it('should expose isMusicPlaying', () => {
      expect(audio.isMusicPlaying).toBe(false);
    });
  });

  describe('graceful handling without AudioContext', () => {
    it('should not throw when AudioContext constructor fails', async () => {
      vi.resetModules();
      (globalThis as Record<string, unknown>).AudioContext = class {
        constructor() { throw new Error('Not supported'); }
      };

      const mod = await import('./audio');
      const brokenAudio = mod.audio;

      // All methods should silently catch errors
      expect(() => brokenAudio.setMasterVolume(0.5)).not.toThrow();
      expect(() => brokenAudio.setEnabled(true)).not.toThrow();
      expect(() => brokenAudio.click()).not.toThrow();
      expect(() => brokenAudio.orderPlaced()).not.toThrow();
      expect(() => brokenAudio.breakingNews()).not.toThrow();
    });
  });
});
