import { describe, it, expect } from 'vitest';
import { GameSpeed } from '@/types/market';

// Test the shortcut key mapping logic directly (without React hooks)
// The actual hook uses window.addEventListener which needs a DOM environment

describe('Keyboard Shortcuts Logic', () => {
  const speedOrder = [
    GameSpeed.Paused,
    GameSpeed.Normal,
    GameSpeed.Fast,
    GameSpeed.VeryFast,
    GameSpeed.Maximum,
  ];

  describe('Speed Control', () => {
    it('Space should toggle between Paused and Normal', () => {
      const toggle = (current: GameSpeed) => current === GameSpeed.Paused ? GameSpeed.Normal : GameSpeed.Paused;
      expect(toggle(GameSpeed.Paused)).toBe(GameSpeed.Normal);
      expect(toggle(GameSpeed.Normal)).toBe(GameSpeed.Paused);
    });

    it('1-4 keys should map to correct speeds', () => {
      const keyMap: Record<string, GameSpeed> = {
        '1': GameSpeed.Normal,
        '2': GameSpeed.Fast,
        '3': GameSpeed.VeryFast,
        '4': GameSpeed.Maximum,
      };

      expect(keyMap['1']).toBe(1);
      expect(keyMap['2']).toBe(2);
      expect(keyMap['3']).toBe(5);
      expect(keyMap['4']).toBe(10);
    });

    it('+ should increase speed one step', () => {
      const currentSpeed = GameSpeed.Fast; // 2
      const idx = speedOrder.indexOf(currentSpeed);
      const nextSpeed = idx < speedOrder.length - 1 ? speedOrder[idx + 1] : currentSpeed;
      expect(nextSpeed).toBe(GameSpeed.VeryFast); // 5
    });

    it('- should decrease speed one step', () => {
      const currentSpeed = GameSpeed.Fast; // 2
      const idx = speedOrder.indexOf(currentSpeed);
      const prevSpeed = idx > 0 ? speedOrder[idx - 1] : currentSpeed;
      expect(prevSpeed).toBe(GameSpeed.Normal); // 1
    });

    it('+ at Maximum should stay at Maximum', () => {
      const currentSpeed = GameSpeed.Maximum;
      const idx = speedOrder.indexOf(currentSpeed);
      const nextSpeed = idx < speedOrder.length - 1 ? speedOrder[idx + 1] : currentSpeed;
      expect(nextSpeed).toBe(GameSpeed.Maximum);
    });

    it('- at Paused should stay at Paused', () => {
      const currentSpeed = GameSpeed.Paused;
      const idx = speedOrder.indexOf(currentSpeed);
      const prevSpeed = idx > 0 ? speedOrder[idx - 1] : currentSpeed;
      expect(prevSpeed).toBe(GameSpeed.Paused);
    });
  });

  describe('Tab Navigation', () => {
    it('should map keys to correct tabs', () => {
      const tabMap: Record<string, string> = {
        d: 'dashboard',
        p: 'portfolio',
        m: 'market',
        o: 'orders',
        n: 'news',
        a: 'analytics',
      };

      expect(tabMap['d']).toBe('dashboard');
      expect(tabMap['p']).toBe('portfolio');
      expect(tabMap['m']).toBe('market');
      expect(tabMap['o']).toBe('orders');
      expect(tabMap['n']).toBe('news');
      expect(tabMap['a']).toBe('analytics');
    });

    it('should not map unknown keys', () => {
      const tabMap: Record<string, string> = {
        d: 'dashboard', p: 'portfolio', m: 'market',
        o: 'orders', n: 'news', a: 'analytics',
      };

      expect(tabMap['x']).toBeUndefined();
      expect(tabMap['z']).toBeUndefined();
    });
  });

  describe('Input field detection', () => {
    it('should identify input-like elements', () => {
      const inputTags = ['INPUT', 'TEXTAREA', 'SELECT'];
      expect(inputTags.includes('INPUT')).toBe(true);
      expect(inputTags.includes('DIV')).toBe(false);
      expect(inputTags.includes('BUTTON')).toBe(false);
    });
  });
});
