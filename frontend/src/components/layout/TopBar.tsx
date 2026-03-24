import { useState, useEffect } from 'react';
import { useMarketStore } from '@/stores/marketStore';
import { GameSpeed, ActiveTab } from '@/types/market';
import { WebSocketClient } from '@/services/websocket';
import { Pause, Play, FastForward, Save, Settings, SkipForward, Search } from 'lucide-react';

const TABS: { id: ActiveTab; label: string; shortcut: string }[] = [
  { id: 'dashboard', label: 'Dashboard', shortcut: 'D' },
  { id: 'portfolio', label: 'Portfolio', shortcut: 'P' },
  { id: 'market', label: 'Market', shortcut: 'M' },
  { id: 'orders', label: 'Orders', shortcut: 'O' },
  { id: 'news', label: 'News', shortcut: 'N' },
  { id: 'analytics', label: 'Analytics', shortcut: 'A' },
  { id: 'journal', label: 'Journal', shortcut: 'J' },
];

const SPEEDS = [
  { speed: GameSpeed.Paused, icon: Pause, label: '▐▐' },
  { speed: GameSpeed.Normal, icon: Play, label: '1x' },
  { speed: GameSpeed.Fast, icon: FastForward, label: '2x' },
  { speed: GameSpeed.VeryFast, icon: FastForward, label: '5x' },
  { speed: GameSpeed.Maximum, icon: FastForward, label: '10x' },
];

interface TopBarProps {
  wsClient: WebSocketClient;
  onOpenSettings?: () => void;
  onOpenCommandBar?: () => void;
}

export function TopBar({ wsClient, onOpenSettings, onOpenCommandBar }: TopBarProps) {
  const activeTab = useMarketStore((s) => s.activeTab);
  const setActiveTab = useMarketStore((s) => s.setActiveTab);
  const speed = useMarketStore((s) => s.speed);
  const gameTime = useMarketStore((s) => s.gameTime);
  const isMarketOpen = useMarketStore((s) => s.isMarketOpen);
  const portfolio = useMarketStore((s) => s.portfolio);

  const [saveFlash, setSaveFlash] = useState(false);

  const handleSpeedChange = (newSpeed: GameSpeed) => {
    wsClient.send('SetSpeed', { speed: newSpeed });
  };

  const handleSave = () => {
    wsClient.send('SaveGame', {});
    setSaveFlash(true);
  };

  useEffect(() => {
    if (saveFlash) {
      const timer = setTimeout(() => setSaveFlash(false), 1500);
      return () => clearTimeout(timer);
    }
  }, [saveFlash]);

  const formatGameTime = (iso: string): string => {
    if (!iso) return '—';
    const d = new Date(iso);
    const days = ['Sun', 'Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat'];
    const months = ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun',
                    'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec'];
    const h = d.getHours();
    const m = d.getMinutes().toString().padStart(2, '0');
    const ampm = h >= 12 ? 'PM' : 'AM';
    const h12 = h % 12 || 12;
    return `${days[d.getDay()]}, ${months[d.getMonth()]} ${d.getDate()} ${d.getFullYear()} — ${h12}:${m} ${ampm}`;
  };

  return (
    <header style={styles.topBar}>
      {/* Logo + Navigation */}
      <div style={styles.left}>
        <span style={styles.logo}>STOCKSIM</span>
        <div style={styles.divider} />
        <nav style={styles.tabs}>
          {TABS.map((tab) => (
            <button
              key={tab.id}
              style={{
                ...styles.tab,
                ...(activeTab === tab.id ? styles.tabActive : {}),
              }}
              onClick={() => setActiveTab(tab.id)}
              title={`${tab.label} (${tab.shortcut})`}
            >
              {tab.label}
            </button>
          ))}
        </nav>
      </div>

      {/* Right: Time + Speed + Cash */}
      <div style={styles.right}>
        <div style={styles.timeSection}>
          <span style={styles.gameTime}>{formatGameTime(gameTime)}</span>
          {(() => {
            // Determine market phase from gameTime
            const d = new Date(gameTime);
            const h = d.getHours();
            const m = d.getMinutes();
            const totalMin = h * 60 + m;
            const day = d.getDay();
            const isWeekend = day === 0 || day === 6;

            let label = 'CLOSED';
            let color = 'var(--red-primary)';
            let glow = 'none';
            let countdown = '';

            if (isWeekend) {
              label = 'WEEKEND';
            } else if (isMarketOpen) {
              label = 'LIVE';
              color = 'var(--green-primary)';
              glow = '0 0 8px var(--green-glow)';
              const closeMin = 16 * 60;
              const remaining = closeMin - totalMin;
              if (remaining > 0 && remaining <= 60) countdown = `${remaining}m to close`;
            } else if (totalMin >= 7 * 60 && totalMin < 9 * 60 + 30) {
              label = 'PRE-MARKET';
              color = '#F59E0B';
              const openMin = 9 * 60 + 30;
              const remaining = openMin - totalMin;
              countdown = `${Math.floor(remaining / 60)}h ${remaining % 60}m to open`;
            } else if (totalMin >= 16 * 60 && totalMin < 20 * 60) {
              label = 'AFTER-HOURS';
              color = '#8B5CF6';
            }

            return (
              <div style={{ display: 'flex', flexDirection: 'column', alignItems: 'flex-end' }}>
                <span style={{
                  ...styles.marketStatus, color, textShadow: glow,
                }}>
                  {isMarketOpen ? '●' : '○'} {label}
                </span>
                {countdown && (
                  <span className="mono" style={{ fontSize: '9px', color: 'var(--text-disabled)' }}>{countdown}</span>
                )}
              </div>
            );
          })()}
        </div>

        <div style={styles.speedGroup}>
          {SPEEDS.map((s) => (
            <button
              key={s.speed}
              style={{
                ...styles.speedBtn,
                ...(speed === s.speed ? styles.speedBtnActive : {}),
              }}
              onClick={() => handleSpeedChange(s.speed)}
              title={s.label}
            >
              {s.label}
            </button>
          ))}
        </div>

        {portfolio && (
          <div style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
            <div style={{ display: 'flex', flexDirection: 'column', alignItems: 'flex-end' }}>
              <span className="mono" style={styles.cashDisplay}>
                ${portfolio.totalEquity.toFixed(0)}
              </span>
              <span className="mono" style={{
                fontSize: '10px', fontWeight: 600,
                color: portfolio.realizedPnL >= 0 ? 'var(--green-primary)' : 'var(--red-primary)',
                textShadow: `0 0 6px ${portfolio.realizedPnL >= 0 ? 'var(--green-glow)' : 'var(--red-glow)'}`,
              }}>
                {portfolio.realizedPnL >= 0 ? '+' : ''}{portfolio.realizedPnL.toFixed(0)} P&L
              </span>
            </div>
          </div>
        )}

        {/* Skip to Open (only when market is closed) */}
        {!isMarketOpen && (
          <button
            style={{ ...styles.iconBtn, color: '#F59E0B' }}
            onClick={() => wsClient.send('SkipToOpen', {})}
            title="Skip to Market Open"
          >
            <SkipForward size={16} />
            <span style={{ fontSize: '10px', marginLeft: '2px', fontWeight: 600 }}>OPEN</span>
          </button>
        )}

        <button
          style={{
            ...styles.iconBtn,
            color: saveFlash ? 'var(--green-primary)' : 'var(--text-secondary)',
          }}
          onClick={handleSave}
          title="Save (Ctrl+S)"
        >
          <Save size={18} />
          {saveFlash && <span style={{ fontSize: '10px', marginLeft: '4px' }}>Saved!</span>}
        </button>
        {portfolio && portfolio.totalEquity >= 1_000_000 && (
          <button
            style={{ ...styles.iconBtn, color: '#D4AF37' }}
            onClick={() => wsClient.send('Retire', {})}
            title="Retire (Portfolio > $1M)"
          >
            <span style={{ fontSize: '11px', fontWeight: 700, letterSpacing: '0.5px' }}>RETIRE</span>
          </button>
        )}
        <button style={styles.iconBtn} title="Search (Ctrl+K)" aria-label="Open search" onClick={onOpenCommandBar}>
          <Search size={16} />
        </button>
        <button style={styles.iconBtn} title="Settings" aria-label="Open settings" onClick={onOpenSettings}>
          <Settings size={18} />
        </button>
      </div>
    </header>
  );
}

const styles: Record<string, React.CSSProperties> = {
  topBar: {
    height: 'var(--topbar-height)',
    background: 'var(--bg-secondary)',
    borderBottom: '1px solid var(--border)',
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'space-between',
    padding: '0 var(--space-4)',
    flexShrink: 0,
  },
  left: {
    display: 'flex',
    alignItems: 'center',
    gap: 'var(--space-4)',
  },
  logo: {
    fontFamily: 'var(--font-mono)',
    fontWeight: 700,
    fontSize: '16px',
    color: 'var(--text-accent)',
    letterSpacing: '2px',
    textShadow: '0 0 12px var(--accent-glow)',
  },
  divider: {
    width: '1px',
    height: '24px',
    background: 'var(--border)',
  },
  tabs: {
    display: 'flex',
    gap: 'var(--space-1)',
  },
  tab: {
    background: 'transparent',
    border: 'none',
    borderBottomWidth: '2px',
    borderBottomStyle: 'solid' as const,
    borderBottomColor: 'transparent',
    color: 'var(--text-secondary)',
    fontFamily: 'var(--font-ui)',
    fontWeight: 500,
    fontSize: '14px',
    padding: '12px 8px',
    cursor: 'pointer',
    transition: 'color 150ms, border-color 150ms',
    outline: 'none',
  },
  tabActive: {
    color: 'var(--text-accent)',
    borderBottomColor: 'var(--text-accent)',
  },
  right: {
    display: 'flex',
    alignItems: 'center',
    gap: 'var(--space-4)',
  },
  timeSection: {
    display: 'flex',
    flexDirection: 'column' as const,
    alignItems: 'flex-end',
  },
  gameTime: {
    fontFamily: 'var(--font-mono)',
    fontSize: '13px',
    color: 'var(--text-primary)',
  },
  marketStatus: {
    fontSize: '10px',
    fontWeight: 600,
  },
  speedGroup: {
    display: 'flex',
    background: 'var(--bg-tertiary)',
    borderRadius: '6px',
    overflow: 'hidden',
  },
  speedBtn: {
    width: '36px',
    height: '32px',
    background: 'transparent',
    border: 'none',
    color: 'var(--text-secondary)',
    fontFamily: 'var(--font-mono)',
    fontSize: '11px',
    cursor: 'pointer',
    transition: 'background 150ms, color 150ms',
  },
  speedBtnActive: {
    background: 'var(--bg-primary)',
    color: 'var(--text-accent)',
    textShadow: '0 0 8px var(--accent-glow)',
    boxShadow: 'inset 0 0 8px rgba(96, 165, 250, 0.1)',
  },
  cashDisplay: {
    fontSize: '15px',
    fontWeight: 700,
    color: 'var(--green-primary)',
    textShadow: '0 0 10px rgba(16, 185, 129, 0.3)',
    letterSpacing: '0.5px',
  },
  iconBtn: {
    background: 'transparent',
    border: 'none',
    color: 'var(--text-secondary)',
    cursor: 'pointer',
    padding: 'var(--space-1)',
    borderRadius: '4px',
    display: 'flex',
    alignItems: 'center',
    transition: 'color 150ms',
  },
};
