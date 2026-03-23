import { useEffect } from 'react';
import { useMarketStore } from '@/stores/marketStore';
import { GameSpeed, ActiveTab } from '@/types/market';
import { WebSocketClient } from '@/services/websocket';
import { createLogger } from '@/services/logger';

const log = createLogger('Shortcuts');

/**
 * Global keyboard shortcuts as defined in Bible section 18.
 *
 * Time Control:
 *   Space = Pause/Resume, 1-4 = Speed, +/- = Faster/Slower
 *
 * Navigation:
 *   D = Dashboard, P = Portfolio, M = Market, O = Orders, N = News, A = Analytics
 *
 * Trading:
 *   B = Buy, S = Sell, H = Short
 *
 * System:
 *   Ctrl+F or / = Search, Escape = Back/Close/Pause Menu
 */
export function useKeyboardShortcuts(wsClient: WebSocketClient) {
  const setActiveTab = useMarketStore((s) => s.setActiveTab);
  const speed = useMarketStore((s) => s.speed);

  useEffect(() => {
    const handleKeyDown = (e: KeyboardEvent) => {
      // Don't handle shortcuts when typing in an input
      const target = e.target as HTMLElement;
      if (
        target.tagName === 'INPUT' ||
        target.tagName === 'TEXTAREA' ||
        target.tagName === 'SELECT' ||
        target.isContentEditable
      ) {
        return;
      }

      // Time Control (Bible 18.1)
      switch (e.key) {
        case ' ': // Space = Pause/Resume toggle
          e.preventDefault();
          const newSpeed = speed === GameSpeed.Paused ? GameSpeed.Normal : GameSpeed.Paused;
          wsClient.send('SetSpeed', { speed: newSpeed });
          log.info('Speed toggle', { from: speed, to: newSpeed });
          return;

        case '1': // 1x speed
          wsClient.send('SetSpeed', { speed: GameSpeed.Normal });
          return;
        case '2': // 2x speed
          wsClient.send('SetSpeed', { speed: GameSpeed.Fast });
          return;
        case '3': // 5x speed
          wsClient.send('SetSpeed', { speed: GameSpeed.VeryFast });
          return;
        case '4': // 10x speed
          wsClient.send('SetSpeed', { speed: GameSpeed.Maximum });
          return;

        case '+':
        case 'ArrowRight': // Faster
          {
            const speeds = [GameSpeed.Paused, GameSpeed.Normal, GameSpeed.Fast, GameSpeed.VeryFast, GameSpeed.Maximum];
            const idx = speeds.indexOf(speed);
            if (idx < speeds.length - 1) {
              wsClient.send('SetSpeed', { speed: speeds[idx + 1] });
            }
          }
          return;

        case '-':
        case 'ArrowLeft': // Slower
          {
            const speeds = [GameSpeed.Paused, GameSpeed.Normal, GameSpeed.Fast, GameSpeed.VeryFast, GameSpeed.Maximum];
            const idx = speeds.indexOf(speed);
            if (idx > 0) {
              wsClient.send('SetSpeed', { speed: speeds[idx - 1] });
            }
          }
          return;
      }

      // Navigation (Bible 18.2) — case insensitive
      const key = e.key.toLowerCase();

      if (!e.ctrlKey && !e.altKey && !e.metaKey) {
        const tabMap: Record<string, ActiveTab> = {
          d: 'dashboard',
          p: 'portfolio',
          m: 'market',
          o: 'orders',
          n: 'news',
          a: 'analytics',
        };

        if (tabMap[key]) {
          setActiveTab(tabMap[key]);
          log.debug('Tab switch', { tab: tabMap[key] });
          return;
        }
      }

      // Save (Ctrl+S)
      if (e.ctrlKey && key === 's') {
        e.preventDefault();
        wsClient.send('SaveGame', {});
        log.info('Quick save triggered');
        return;
      }

      // Search (Bible 18.4)
      if ((e.ctrlKey && key === 'f') || (key === '/' && !e.ctrlKey)) {
        e.preventDefault();
        // TODO: Open search overlay
        log.debug('Search shortcut triggered');
        return;
      }

      // Escape hierarchy (Bible 3.0.8)
      if (e.key === 'Escape') {
        // TODO: Implement full escape hierarchy
        // For now: just log
        log.debug('Escape pressed');
        return;
      }
    };

    window.addEventListener('keydown', handleKeyDown);
    return () => window.removeEventListener('keydown', handleKeyDown);
  }, [speed, wsClient, setActiveTab]);
}
