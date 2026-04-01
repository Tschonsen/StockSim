import { useState, useEffect } from 'react';
import { WebSocketClient } from '@/services/websocket';

interface SaveSlot {
  fileName: string;
  filePath: string;
  saveName: string;
  saveDate: string;
  gameDate: string;
  cash: number;
  playerName: string;
  seed: number;
  marketPhase: string;
  tickCount: number;
}

interface SaveGame {
  gameId: string;
  seed: number;
  playerName: string;
  lastPlayed: string;
  totalSaves: number;
  saves: SaveSlot[];
}

interface SaveDialogProps {
  isOpen: boolean;
  onClose: () => void;
  wsClient: WebSocketClient;
}

export function SaveDialog({ isOpen, onClose, wsClient }: SaveDialogProps) {
  const [saveName, setSaveName] = useState('');
  const [saving, setSaving] = useState(false);

  useEffect(() => {
    if (isOpen) {
      setSaveName('');
      setSaving(false);
    }
  }, [isOpen]);

  useEffect(() => {
    if (!isOpen) return;
    const unsub = wsClient.on('GameSaved', (payload) => {
      const data = payload as { success: boolean };
      if (data.success) {
        setSaving(false);
        onClose();
      }
    });
    return unsub;
  }, [isOpen, wsClient, onClose]);

  if (!isOpen) return null;

  const handleSave = () => {
    setSaving(true);
    wsClient.send('SaveGame', { SaveName: saveName || 'Quicksave' });
  };

  return (
    <div style={S.overlay} onClick={onClose}>
      <div style={S.dialog} onClick={e => e.stopPropagation()}>
        <h2 style={S.title}>Save Game</h2>
        <input
          type="text"
          placeholder="Enter save name..."
          value={saveName}
          onChange={e => setSaveName(e.target.value)}
          onKeyDown={e => { if (e.key === 'Enter') handleSave(); }}
          style={S.input}
          autoFocus
          maxLength={40}
        />
        <p style={S.hint}>Leave empty for "Quicksave"</p>
        <div style={S.buttons}>
          <button style={S.cancelBtn} onClick={onClose}>Cancel</button>
          <button style={S.saveBtn} onClick={handleSave} disabled={saving}>
            {saving ? 'Saving...' : 'Save'}
          </button>
        </div>
      </div>
    </div>
  );
}

interface LoadScreenProps {
  isOpen: boolean;
  onClose: () => void;
  onLoad: (filePath: string) => void;
  wsClient: WebSocketClient;
}

export function LoadScreen({ isOpen, onClose, onLoad, wsClient }: LoadScreenProps) {
  const [games, setGames] = useState<SaveGame[]>([]);
  const [expandedGame, setExpandedGame] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    if (!isOpen) return;
    setLoading(true);
    wsClient.send('ListSaves', {});

    const unsub = wsClient.on('SaveList', (payload) => {
      const data = payload as { games: SaveGame[] };
      setGames(data.games ?? []);
      setLoading(false);
      // Auto-expand first game
      if (data.games?.length > 0) setExpandedGame(data.games[0].gameId);
    });
    return unsub;
  }, [isOpen, wsClient]);

  if (!isOpen) return null;

  const formatDate = (iso: string) => {
    if (!iso) return '—';
    const d = new Date(iso);
    return d.toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: 'numeric', hour: '2-digit', minute: '2-digit' });
  };

  const formatGameDate = (iso: string) => {
    if (!iso) return '—';
    const d = new Date(iso);
    return d.toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: 'numeric' });
  };

  const formatCash = (cash: number) => {
    if (cash >= 1_000_000) return `$${(cash / 1_000_000).toFixed(1)}M`;
    if (cash >= 1_000) return `$${(cash / 1_000).toFixed(1)}K`;
    return `$${cash.toFixed(0)}`;
  };

  const tradingDays = (ticks: number) => Math.floor(ticks / 390);

  return (
    <div style={S.overlay} onClick={onClose}>
      <div style={S.loadPanel} onClick={e => e.stopPropagation()}>
        <div style={S.loadHeader}>
          <h2 style={S.title}>Load Game</h2>
          <button style={S.closeBtn} onClick={onClose}>×</button>
        </div>

        {loading ? (
          <p style={S.hint}>Loading saves...</p>
        ) : games.length === 0 ? (
          <p style={S.hint}>No saved games found.</p>
        ) : (
          <div style={S.gameList}>
            {games.map(game => (
              <div key={game.gameId} style={S.gameGroup}>
                <div
                  style={S.gameHeader}
                  onClick={() => setExpandedGame(expandedGame === game.gameId ? null : game.gameId)}
                >
                  <div>
                    <span style={S.gameName}>{game.playerName}'s Game</span>
                    <span style={S.gameInfo}> — {game.totalSaves} save{game.totalSaves !== 1 ? 's' : ''}</span>
                  </div>
                  <span style={S.gameDate}>Last: {formatDate(game.lastPlayed)}</span>
                </div>

                {expandedGame === game.gameId && (
                  <div style={S.saveList}>
                    {game.saves.map(save => (
                      <div
                        key={save.filePath}
                        style={S.saveRow}
                        onClick={() => { setLoading(true); onLoad(save.filePath); }}
                      >
                        <div style={S.saveMain}>
                          <span style={S.saveName}>{save.saveName}</span>
                          <span style={S.saveMeta}>
                            {formatGameDate(save.gameDate)} • Day {tradingDays(save.tickCount)} • {save.marketPhase}
                          </span>
                        </div>
                        <div style={S.saveRight}>
                          <span style={S.saveCash}>{formatCash(save.cash)}</span>
                          <span style={S.saveTime}>{formatDate(save.saveDate)}</span>
                        </div>
                      </div>
                    ))}
                  </div>
                )}
              </div>
            ))}
          </div>
        )}
      </div>
    </div>
  );
}

const S: Record<string, React.CSSProperties> = {
  overlay: {
    position: 'fixed', inset: 0, background: 'rgba(0,0,0,0.7)',
    display: 'flex', alignItems: 'center', justifyContent: 'center', zIndex: 9999,
  },
  dialog: {
    background: 'var(--bg-secondary)', border: '1px solid var(--border-color)',
    borderRadius: 8, padding: '24px 32px', width: 380, maxWidth: '90vw',
  },
  title: { margin: '0 0 16px', fontSize: 18, fontWeight: 700, color: 'var(--text-primary)' },
  input: {
    width: '100%', padding: '10px 12px', borderRadius: 6,
    border: '1px solid var(--border-color)', background: 'var(--bg-tertiary)',
    color: 'var(--text-primary)', fontSize: 14, fontFamily: 'var(--font-mono)',
    outline: 'none', boxSizing: 'border-box',
  },
  hint: { color: 'var(--text-disabled)', fontSize: 12, margin: '8px 0 16px' },
  buttons: { display: 'flex', gap: 8, justifyContent: 'flex-end' },
  cancelBtn: {
    padding: '8px 16px', borderRadius: 6, border: '1px solid var(--border-color)',
    background: 'transparent', color: 'var(--text-secondary)', fontSize: 13, cursor: 'pointer',
  },
  saveBtn: {
    padding: '8px 20px', borderRadius: 6, border: 'none',
    background: 'var(--text-accent)', color: '#fff', fontSize: 13,
    fontWeight: 600, cursor: 'pointer',
  },
  // Load screen
  loadPanel: {
    background: 'var(--bg-secondary)', border: '1px solid var(--border-color)',
    borderRadius: 8, padding: 24, width: 560, maxWidth: '90vw', maxHeight: '80vh',
    display: 'flex', flexDirection: 'column',
  },
  loadHeader: {
    display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 16,
  },
  closeBtn: {
    background: 'transparent', border: 'none', color: 'var(--text-disabled)',
    fontSize: 24, cursor: 'pointer', padding: '0 4px', lineHeight: 1,
  },
  gameList: { flex: 1, overflow: 'auto' },
  gameGroup: {
    marginBottom: 8, borderRadius: 6, border: '1px solid var(--border-color)',
    overflow: 'hidden',
  },
  gameHeader: {
    display: 'flex', justifyContent: 'space-between', alignItems: 'center',
    padding: '10px 14px', background: 'var(--bg-tertiary)', cursor: 'pointer',
  },
  gameName: { fontWeight: 700, fontSize: 14, color: 'var(--text-primary)' },
  gameInfo: { fontSize: 12, color: 'var(--text-disabled)' },
  gameDate: { fontSize: 11, color: 'var(--text-disabled)', fontFamily: 'var(--font-mono)' },
  saveList: { },
  saveRow: {
    display: 'flex', justifyContent: 'space-between', alignItems: 'center',
    padding: '8px 14px', borderTop: '1px solid rgba(31,41,55,0.3)',
    cursor: 'pointer', transition: 'background 150ms',
  },
  saveMain: { display: 'flex', flexDirection: 'column', gap: 2 },
  saveName: { fontSize: 13, fontWeight: 600, color: 'var(--text-primary)' },
  saveMeta: { fontSize: 11, color: 'var(--text-disabled)', fontFamily: 'var(--font-mono)' },
  saveRight: { display: 'flex', flexDirection: 'column', alignItems: 'flex-end', gap: 2 },
  saveCash: { fontSize: 13, fontWeight: 700, color: 'var(--green-primary)', fontFamily: 'var(--font-mono)' },
  saveTime: { fontSize: 10, color: 'var(--text-disabled)', fontFamily: 'var(--font-mono)' },
};
