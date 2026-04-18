import { createLogger } from './logger';

const log = createLogger('DetachablePanel');

/**
 * Spec 3.0.14: Detachable Panels — Multi-Window Support.
 * Panels can be "torn off" into separate Electron BrowserWindows.
 *
 * Architecture:
 * - Main window keeps the WebSocket connection
 * - Detached windows receive data via Electron IPC (ipcMain/ipcRenderer)
 * - Each detached window renders a single panel component
 * - Max 8 detached windows (memory management)
 * - Layout persisted in %AppData%/StockSim/window-layout.json
 *
 * Usage:
 *   const manager = new DetachablePanelManager();
 *   manager.detach('chart', { symbol: 'AAPL' });
 *   manager.reattach('chart');
 *
 * Note: Requires Electron APIs. Falls back gracefully in browser mode.
 */

export type PanelType = 'chart' | 'portfolio' | 'orders' | 'news' | 'analytics' | 'orderbook';

interface DetachedPanel {
  id: string;
  type: PanelType;
  props: Record<string, unknown>;
  windowId?: number;
}

class DetachablePanelManager {
  private _panels: Map<string, DetachedPanel> = new Map();
  private _isElectron = false;
  private _maxWindows = 8;

  constructor() {
    // Check if running in Electron
    this._isElectron = typeof window !== 'undefined' &&
      typeof (window as unknown as Record<string, unknown>).electronAPI !== 'undefined';

    if (this._isElectron) {
      log.info('Electron detected — detachable panels available');
    } else {
      log.info('Browser mode — detachable panels disabled');
    }
  }

  get isAvailable(): boolean { return this._isElectron; }
  get detachedPanels(): DetachedPanel[] { return Array.from(this._panels.values()); }
  get canDetachMore(): boolean { return this._panels.size < this._maxWindows; }

  /**
   * Detach a panel into a new window.
   * @param type Panel type to detach
   * @param props Panel-specific props (e.g., { symbol: 'AAPL' } for chart)
   */
  detach(type: PanelType, props: Record<string, unknown> = {}): boolean {
    if (!this._isElectron) {
      log.warn('Cannot detach — not running in Electron');
      return false;
    }
    if (this._panels.size >= this._maxWindows) {
      log.warn('Maximum detached windows reached', { max: this._maxWindows });
      return false;
    }

    const id = `${type}_${Date.now()}`;
    const panel: DetachedPanel = { id, type, props };
    this._panels.set(id, panel);

    // Request Electron main process to create new BrowserWindow
    try {
      const electronAPI = (window as unknown as Record<string, unknown>).electronAPI as {
        createPanel: (panel: DetachedPanel) => Promise<number>;
      };
      electronAPI.createPanel(panel).then(windowId => {
        panel.windowId = windowId;
        log.info('Panel detached', { id, type, windowId });
      });
    } catch (err) {
      log.error('Failed to detach panel', { type, error: String(err) });
      this._panels.delete(id);
      return false;
    }

    return true;
  }

  /**
   * Reattach a detached panel back to the main window.
   */
  reattach(id: string): boolean {
    const panel = this._panels.get(id);
    if (!panel) return false;

    try {
      const electronAPI = (window as unknown as Record<string, unknown>).electronAPI as {
        closePanel: (windowId: number) => void;
      };
      if (panel.windowId) {
        electronAPI.closePanel(panel.windowId);
      }
    } catch (err) {
      log.error('Failed to reattach panel', { id, error: String(err) });
    }

    this._panels.delete(id);
    log.info('Panel reattached', { id, type: panel.type });
    return true;
  }

  /** Reattach all panels */
  reattachAll(): void {
    for (const id of Array.from(this._panels.keys())) {
      this.reattach(id);
    }
  }
}

export const panelManager = new DetachablePanelManager();
