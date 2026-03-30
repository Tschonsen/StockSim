import { useEffect, useState, useCallback } from 'react';
import { useMarketStore } from '@/stores/marketStore';
import { GameSpeed, ActiveTab } from '@/types/market';
import { WebSocketClient } from '@/services/websocket';
import { createLogger } from '@/services/logger';
import { KeyBindings, DEFAULT_KEYBINDINGS } from '@/components/layout/SettingsModal';

const log = createLogger('Shortcuts');

const SETTINGS_KEY = 'stocksim-settings';

/** Read keybindings from localStorage, falling back to defaults. */
function getKeyBindings(): KeyBindings {
  try {
    const raw = localStorage.getItem(SETTINGS_KEY);
    if (raw) {
      const parsed = JSON.parse(raw);
      if (parsed && parsed.keyBindings) {
        return { ...DEFAULT_KEYBINDINGS, ...parsed.keyBindings };
      }
    }
  } catch {
    // Corrupted data — fall back to defaults
  }
  return { ...DEFAULT_KEYBINDINGS };
}

/** Normalize an e.key value to the format used in KeyBindings (e.g. 'Space', 'D', 'Ctrl+S'). */
function normalizeKey(e: KeyboardEvent): string {
  let key = e.key === ' ' ? 'Space' : e.key.length === 1 ? e.key.toUpperCase() : e.key;
  if (e.ctrlKey && key !== 'Control') key = 'Ctrl+' + key;
  if (e.altKey && key !== 'Alt') key = 'Alt+' + key;
  return key;
}

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

  // Read keybindings from localStorage and re-read when settings change
  const [bindings, setBindings] = useState<KeyBindings>(getKeyBindings);

  const refreshBindings = useCallback(() => {
    setBindings(getKeyBindings());
  }, []);

  // Listen for storage events (cross-tab) and a custom event for same-tab updates
  useEffect(() => {
    const onStorage = (e: StorageEvent) => {
      if (e.key === SETTINGS_KEY) refreshBindings();
    };
    const onSettingsChanged = () => refreshBindings();
    window.addEventListener('storage', onStorage);
    window.addEventListener('settingsChanged', onSettingsChanged);
    return () => {
      window.removeEventListener('storage', onStorage);
      window.removeEventListener('settingsChanged', onSettingsChanged);
    };
  }, [refreshBindings]);

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

      const pressed = normalizeKey(e);

      // Time Control (Bible 18.1) — configurable keys
      if (pressed === bindings.pauseResume) {
        e.preventDefault();
        const newSpeed = speed === GameSpeed.Paused ? GameSpeed.Normal : GameSpeed.Paused;
        wsClient.send('SetSpeed', { speed: newSpeed });
        log.info('Speed toggle', { from: speed, to: newSpeed });
        return;
      }
      if (pressed === bindings.speed1) {
        wsClient.send('SetSpeed', { speed: GameSpeed.Normal });
        return;
      }
      if (pressed === bindings.speed2) {
        wsClient.send('SetSpeed', { speed: GameSpeed.Fast });
        return;
      }
      if (pressed === bindings.speed3) {
        wsClient.send('SetSpeed', { speed: GameSpeed.VeryFast });
        return;
      }
      if (pressed === bindings.speed4) {
        wsClient.send('SetSpeed', { speed: GameSpeed.Maximum });
        return;
      }

      // +/- and arrow keys for faster/slower (not rebindable)
      switch (e.key) {
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

      // Navigation (Bible 18.2) — configurable keys
      if (!e.ctrlKey && !e.altKey && !e.metaKey) {
        const tabBindings: Record<string, ActiveTab> = {
          [bindings.tabDashboard]: 'dashboard',
          [bindings.tabPortfolio]: 'portfolio',
          [bindings.tabMarket]: 'market',
          [bindings.tabOrders]: 'orders',
          [bindings.tabNews]: 'news',
          [bindings.tabAnalytics]: 'analytics',
        };

        // Also keep hardcoded X=options, J=journal (not in KeyBindings interface)
        const key = e.key.toLowerCase();
        if (key === 'x') { setActiveTab('options'); log.debug('Tab switch', { tab: 'options' }); return; }
        if (key === 'j') { setActiveTab('journal'); log.debug('Tab switch', { tab: 'journal' }); return; }

        if (tabBindings[pressed]) {
          setActiveTab(tabBindings[pressed]);
          log.debug('Tab switch', { tab: tabBindings[pressed] });
          return;
        }
      }

      // Save — configurable (default Ctrl+S)
      if (pressed === bindings.quickSave) {
        e.preventDefault();
        wsClient.send('SaveGame', {});
        log.info('Quick save triggered');
        return;
      }

      // Trading shortcuts (Bible 18.3) — not rebindable
      const keyLower = e.key.toLowerCase();
      if (keyLower === 'b') {
        window.dispatchEvent(new CustomEvent('tradingShortcut', { detail: 'Buy' }));
        log.debug('Trading shortcut: Buy');
        return;
      }
      if (keyLower === 's' && !e.ctrlKey) {
        window.dispatchEvent(new CustomEvent('tradingShortcut', { detail: 'Sell' }));
        log.debug('Trading shortcut: Sell');
        return;
      }
      if (keyLower === 'h') {
        window.dispatchEvent(new CustomEvent('tradingShortcut', { detail: 'Short' }));
        log.debug('Trading shortcut: Short');
        return;
      }
      if (keyLower === 'c') {
        window.dispatchEvent(new CustomEvent('tradingShortcut', { detail: 'Cover' }));
        log.debug('Trading shortcut: Cover');
        return;
      }

      // Ctrl+K = Command Bar (alternative to Ctrl+F)
      if (e.ctrlKey && keyLower === 'k') {
        e.preventDefault();
        window.dispatchEvent(new CustomEvent('openCommandBar'));
        log.debug('Command bar shortcut (Ctrl+K)');
        return;
      }

      // Help / Glossary — configurable help key, plus F1
      if (pressed === bindings.help || (e.shiftKey && keyLower === '/')) {
        window.dispatchEvent(new CustomEvent('toggleShortcutsHelp'));
        return;
      }
      if (e.key === 'F1') {
        e.preventDefault();
        window.dispatchEvent(new CustomEvent('toggleGlossary'));
        log.debug('Glossary shortcut (F1)');
        return;
      }

      // F11 — Fullscreen toggle
      if (e.key === 'F11') {
        e.preventDefault();
        window.dispatchEvent(new CustomEvent('toggleFullscreen'));
        log.debug('Fullscreen toggle (F11)');
        return;
      }

      // Ctrl+N — New Game
      if (e.ctrlKey && keyLower === 'n') {
        e.preventDefault();
        window.dispatchEvent(new CustomEvent('newGame'));
        log.debug('New game shortcut (Ctrl+N)');
        return;
      }

      // Ctrl+L — Load Game
      if (e.ctrlKey && keyLower === 'l') {
        e.preventDefault();
        window.dispatchEvent(new CustomEvent('loadGame'));
        log.debug('Load game shortcut (Ctrl+L)');
        return;
      }

      // Enter — Confirm order dialog if open
      if (e.key === 'Enter') {
        window.dispatchEvent(new CustomEvent('confirmOrder'));
        return;
      }

      // Search — open Command Bar (Bible 18.4)
      if ((e.ctrlKey && keyLower === 'f') || (keyLower === '/' && !e.ctrlKey && !e.shiftKey)) {
        e.preventDefault();
        window.dispatchEvent(new CustomEvent('openCommandBar'));
        log.debug('Search shortcut triggered');
        return;
      }

      // Escape hierarchy (Bible 3.0.8)
      if (e.key === 'Escape') {
        const { showStockDetail, selectedSymbol } = useMarketStore.getState();
        if (showStockDetail && selectedSymbol) {
          // Close stock detail view
          useMarketStore.getState().selectStock(null);
          log.debug('Escape: closed stock detail');
        } else {
          // Toggle pause
          const currentSpeed = useMarketStore.getState().speed;
          const pauseSpeed = currentSpeed === GameSpeed.Paused ? GameSpeed.Normal : GameSpeed.Paused;
          wsClient.send('SetSpeed', { speed: pauseSpeed });
          log.debug('Escape: toggled pause');
        }
        return;
      }
    };

    window.addEventListener('keydown', handleKeyDown);
    return () => window.removeEventListener('keydown', handleKeyDown);
  }, [speed, wsClient, setActiveTab, bindings]);
}
