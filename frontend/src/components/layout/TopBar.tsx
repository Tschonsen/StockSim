import { useState, useEffect, useMemo, useRef } from 'react';
import { useMarketStore } from '@/stores/marketStore';
import { GameSpeed, ActiveTab, RegulatoryStatus } from '@/types/market';
import { WebSocketClient } from '@/services/websocket';
import { Pause, Play, FastForward, Save, Settings, SkipForward, Search, Shield, BookOpen, Menu } from 'lucide-react';
import { getCareerTitle } from '@/data/careerTitles';
import { audio } from '@/services/audio';

const TAB_GROUPS: { tabs: { id: ActiveTab; label: string; shortcut: string }[] }[] = [
  { tabs: [
    { id: 'dashboard', label: 'Dashboard', shortcut: 'D' },
    { id: 'portfolio', label: 'Portfolio', shortcut: 'P' },
    { id: 'market', label: 'Market', shortcut: 'M' },
  ]},
  { tabs: [
    { id: 'orders', label: 'Orders', shortcut: 'O' },
    { id: 'options', label: 'Options', shortcut: 'X' },
  ]},
  { tabs: [
    { id: 'news', label: 'News', shortcut: 'N' },
    { id: 'analytics', label: 'Analytics', shortcut: 'A' },
    { id: 'journal', label: 'Journal', shortcut: 'J' },
  ]},
];

const SPEEDS = [
  { speed: GameSpeed.Paused, icon: Pause, label: '▐▐', title: 'Pause' },
  { speed: GameSpeed.Normal, icon: Play, label: '▶', title: 'Real-Time' },
  { speed: GameSpeed.Fast, icon: FastForward, label: '▶▶', title: 'Fast' },
  { speed: GameSpeed.VeryFast, icon: FastForward, label: '▶▶▶', title: 'Very Fast' },
  { speed: GameSpeed.Maximum, icon: FastForward, label: 'MAX', title: 'Maximum' },
];

interface TopBarProps {
  wsClient: WebSocketClient;
  onOpenSettings?: () => void;
  onOpenCommandBar?: () => void;
  onOpenWiki?: () => void;
  onMainMenu?: () => void;
  onSave?: () => void;
}

function CareerBadge({ equity, trades }: { equity: number; trades: number }) {
  const approxDays = Math.max(trades, 1);
  const career = useMemo(() => getCareerTitle(equity, trades, approxDays), [equity, trades, approxDays]);
  const [prevId, setPrevId] = useState(career.id);
  const [promoted, setPromoted] = useState(false);

  useEffect(() => {
    if (career.id !== prevId) {
      setPrevId(career.id);
      setPromoted(true);
      const timer = setTimeout(() => setPromoted(false), 2000);
      return () => clearTimeout(timer);
    }
  }, [career.id, prevId]);

  return (
    <span
      className={promoted ? 'career-promoted' : ''}
      style={{
        fontSize: 10, fontWeight: 700, padding: '2px 8px', borderRadius: 4,
        background: `${career.color}22`, color: career.color,
        border: `1px solid ${career.color}44`, letterSpacing: '0.04em',
        whiteSpace: 'nowrap' as const,
        transition: 'all 0.5s ease',
      }}
    >
      {career.icon} {career.title}
    </span>
  );
}

export function TopBar({ wsClient, onOpenSettings, onOpenCommandBar, onOpenWiki, onMainMenu, onSave }: TopBarProps) {
  const activeTab = useMarketStore((s) => s.activeTab);
  const setActiveTab = useMarketStore((s) => s.setActiveTab);
  const speed = useMarketStore((s) => s.speed);
  const gameTime = useMarketStore((s) => s.gameTime);
  const isMarketOpen = useMarketStore((s) => s.isMarketOpen);
  const portfolio = useMarketStore((s) => s.portfolio);
  const smaStatus = useMarketStore((s) => s.smaStatus);
  const smaData = useMarketStore((s) => s.smaData);
  const stockList = useMarketStore((s) => s.stockList);
  const economicData = useMarketStore((s) => s.economicData);

  const [saveFlash, setSaveFlash] = useState(false);
  const [saveError, setSaveError] = useState(false);
  const [autosaveFlash, setAutosaveFlash] = useState(false);
  const [showSMAPanel, setShowSMAPanel] = useState(false);
  const [showGameMenu, setShowGameMenu] = useState(false);
  const [milestoneHit, setMilestoneHit] = useState(false);
  const prevEquityRef = useRef(0);

  // Portfolio milestone detection ($100K, $250K, $500K, $1M, $5M)
  const MILESTONES = [100_000, 250_000, 500_000, 1_000_000, 5_000_000];
  useEffect(() => {
    if (portfolio) {
      const prev = prevEquityRef.current;
      const curr = portfolio.totalEquity;
      if (prev > 0 && MILESTONES.some(m => prev < m && curr >= m)) {
        setMilestoneHit(true);
        audio.milestone();
        const timer = setTimeout(() => setMilestoneHit(false), 1500);
        return () => clearTimeout(timer);
      }
      prevEquityRef.current = curr;
    }
  }, [portfolio?.totalEquity]);

  // ESC toggles game menu
  useEffect(() => {
    const handleKeyDown = (e: KeyboardEvent) => {
      if (e.key === 'Escape') {
        setShowGameMenu(prev => !prev);
      }
    };
    window.addEventListener('keydown', handleKeyDown);
    return () => window.removeEventListener('keydown', handleKeyDown);
  }, []);

  const handleSpeedChange = (newSpeed: GameSpeed) => {
    wsClient.send('SetSpeed', { speed: newSpeed });
  };

  const handleSave = () => {
    if (onSave) {
      onSave(); // Open save dialog
    } else {
      wsClient.send('SaveGame', {}); // Fallback: quicksave
      setSaveFlash(true);
    }
  };

  useEffect(() => {
    if (saveFlash) {
      const timer = setTimeout(() => setSaveFlash(false), 1500);
      return () => clearTimeout(timer);
    }
  }, [saveFlash]);

  useEffect(() => {
    if (autosaveFlash) {
      const timer = setTimeout(() => setAutosaveFlash(false), 3000);
      return () => clearTimeout(timer);
    }
  }, [autosaveFlash]);

  useEffect(() => {
    const unsub = wsClient.on('Autosaved', () => {
      setAutosaveFlash(true);
    });
    return unsub;
  }, [wsClient]);

  useEffect(() => {
    const unsub = wsClient.on('GameSaved', (payload) => {
      const data = payload as { success: boolean; error?: string };
      if (!data.success) {
        setSaveFlash(false);
        setSaveError(true);
      }
    });
    return unsub;
  }, [wsClient]);

  useEffect(() => {
    if (saveError) {
      const timer = setTimeout(() => setSaveError(false), 3000);
      return () => clearTimeout(timer);
    }
  }, [saveError]);

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
          {TAB_GROUPS.map((group, gi) => (
            <div key={gi} style={{ display: 'flex', gap: 'var(--space-1)', alignItems: 'center' }}>
              {gi > 0 && <div style={{ width: '1px', height: '18px', background: 'var(--border)', margin: '0 4px' }} />}
              {group.tabs.map((tab) => (
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
            </div>
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
              color = 'var(--warning)';
              const openMin = 9 * 60 + 30;
              const remaining = openMin - totalMin;
              countdown = `${Math.floor(remaining / 60)}h ${remaining % 60}m to open`;
            } else if (totalMin >= 16 * 60 && totalMin < 20 * 60) {
              label = 'AFTER-HOURS';
              color = 'var(--chart-purple)';
            }

            const badgeStyle: React.CSSProperties = {
              padding: '2px 8px',
              borderRadius: '4px',
              fontSize: '11px',
              fontWeight: 700,
              fontFamily: 'var(--font-mono)',
              letterSpacing: '0.5px',
              color,
              textShadow: glow,
              ...(isMarketOpen ? {
                background: 'rgba(16, 185, 129, 0.12)',
                border: '1px solid rgba(16, 185, 129, 0.3)',
              } : label === 'PRE-MARKET' ? {
                background: 'rgba(245, 158, 11, 0.12)',
                border: '1px solid rgba(245, 158, 11, 0.3)',
                animation: 'pulse 2s ease-in-out infinite',
              } : label === 'AFTER-HOURS' ? {
                background: 'rgba(139, 92, 246, 0.12)',
                border: '1px solid rgba(139, 92, 246, 0.3)',
              } : {
                background: 'rgba(239, 68, 68, 0.12)',
                border: '1px solid rgba(239, 68, 68, 0.3)',
              }),
            };

            return (
              <div style={{ display: 'flex', flexDirection: 'column', alignItems: 'flex-end', gap: '2px' }}>
                <span style={badgeStyle}>
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
              title={s.title}
            >
              {s.label}
            </button>
          ))}
        </div>

        {/* Market Index Ticker */}
        {(() => {
          const simx = stockList.find(s => s.symbol === 'SIMX');
          const vixVal = economicData?.fearGreedIndex;
          const oilVal = economicData?.indicators?.oilPrice;
          const goldVal = economicData?.indicators?.goldPrice;
          return (
            <div style={{ display: 'flex', gap: '10px', alignItems: 'center', fontSize: '11px', fontFamily: 'var(--font-mono)' }}>
              {simx && (
                <span style={{ display: 'flex', gap: '4px', alignItems: 'baseline' }}>
                  <span style={{ color: 'var(--text-disabled)', fontWeight: 600, fontSize: '9px' }}>SIMX</span>
                  <span style={{ color: 'var(--text-primary)', fontWeight: 700 }}>{simx.price.toFixed(2)}</span>
                  <span style={{ color: simx.changePercent >= 0 ? 'var(--green-primary)' : 'var(--red-primary)', fontWeight: 600, fontSize: '10px' }}>
                    {simx.changePercent >= 0 ? '+' : ''}{simx.changePercent.toFixed(2)}%
                  </span>
                </span>
              )}
              {vixVal !== undefined && (
                <span style={{ display: 'flex', gap: '4px', alignItems: 'baseline' }}>
                  <span style={{ color: 'var(--text-disabled)', fontWeight: 600, fontSize: '9px' }}>F&G</span>
                  <span style={{ color: vixVal < 30 ? 'var(--red-primary)' : vixVal > 70 ? 'var(--green-primary)' : 'var(--text-primary)', fontWeight: 700 }}>{vixVal.toFixed(0)}</span>
                </span>
              )}
              {goldVal !== undefined && (
                <span style={{ display: 'flex', gap: '4px', alignItems: 'baseline' }}>
                  <span style={{ color: 'var(--text-disabled)', fontWeight: 600, fontSize: '9px' }}>GOLD</span>
                  <span style={{ color: 'var(--warning)', fontWeight: 700 }}>${goldVal.toFixed(0)}</span>
                </span>
              )}
              {oilVal !== undefined && (
                <span style={{ display: 'flex', gap: '4px', alignItems: 'baseline' }}>
                  <span style={{ color: 'var(--text-disabled)', fontWeight: 600, fontSize: '9px' }}>OIL</span>
                  <span style={{ color: 'var(--text-primary)', fontWeight: 700 }}>${oilVal.toFixed(2)}</span>
                </span>
              )}
            </div>
          );
        })()}

        {portfolio && (
          <div style={{ display: 'flex', alignItems: 'center', gap: '6px' }}>
            <div
              className={milestoneHit ? 'portfolio-milestone' : ''}
              style={{ display: 'flex', flexDirection: 'column', alignItems: 'flex-end', borderRadius: '6px', padding: '2px 6px' }}
            >
              <span className="mono" style={styles.cashDisplay}>
                ${portfolio.totalEquity.toFixed(0)}
              </span>
              <span className="mono" style={{
                fontSize: '10px', fontWeight: 600,
                color: 'var(--text-muted)',
              }}>
                Cash: ${portfolio.cash.toFixed(0)}
              </span>
            </div>
          </div>
        )}

        {/* Skip to Open (only when market is closed) */}
        {!isMarketOpen && (
          <button
            style={{ ...styles.iconBtn, color: 'var(--warning)' }}
            onClick={() => wsClient.send('SkipToOpen', {})}
            title="Skip to Market Open"
          >
            <SkipForward size={16} />
            <span style={{ fontSize: '10px', marginLeft: '2px', fontWeight: 600 }}>OPEN</span>
          </button>
        )}

        {/* SMA Shield Icon (Bible 9.2) */}
        <div style={{ position: 'relative' }}>
          <button
            style={{
              ...styles.iconBtn,
              color: smaShieldColor(smaStatus),
              animation: smaStatus === 'UnderInvestigation' || smaStatus === 'EnforcementPending'
                ? 'smaPulse 2s ease-in-out infinite' : 'none',
            }}
            onClick={() => {
              wsClient.send('GetSMAStatus', {});
              setShowSMAPanel(!showSMAPanel);
            }}
            title={`Regulatory Status: ${smaStatusLabel(smaStatus)}`}
            aria-label={`Regulatory Status: ${smaStatusLabel(smaStatus)}`}
          >
            <Shield size={16} />
          </button>
          {showSMAPanel && (
            <SMAPanel
              smaData={smaData}
              smaStatus={smaStatus}
              onClose={() => setShowSMAPanel(false)}
            />
          )}
        </div>

        {saveError && (
          <span className="mono" style={{ fontSize: '11px', color: 'var(--red-primary)', fontWeight: 600 }}>Save failed!</span>
        )}
        {!saveError && saveFlash && (
          <span className="mono" style={{ fontSize: '11px', color: 'var(--green-primary)', fontWeight: 600 }}>Saved!</span>
        )}
        {!saveError && !saveFlash && autosaveFlash && (
          <span className="mono" style={{ fontSize: '11px', color: 'var(--green-primary)', fontWeight: 600, opacity: 0.8 }}>Autosaved</span>
        )}

        <button style={styles.iconBtn} title="Search (Ctrl+K)" aria-label="Open search" onClick={onOpenCommandBar}>
          <Search size={16} />
        </button>

        <button
          style={{ ...styles.menuBtn, ...(showGameMenu ? { background: 'var(--bg-tertiary)', color: 'var(--text-accent)' } : {}) }}
          onClick={() => setShowGameMenu(!showGameMenu)}
          title="Menu (Esc)"
          aria-label="Open game menu"
        >
          <Menu size={20} />
        </button>

        {showGameMenu && (
          <GameMenu
            onClose={() => setShowGameMenu(false)}
            onResume={() => setShowGameMenu(false)}
            onSettings={() => { setShowGameMenu(false); onOpenSettings?.(); }}
            onWiki={() => { setShowGameMenu(false); onOpenWiki?.(); }}
            onSave={() => { handleSave(); setShowGameMenu(false); }}
            onMainMenu={() => { setShowGameMenu(false); onMainMenu?.(); }}
            canRetire={!!portfolio && portfolio.totalEquity >= 1_000_000}
            onRetire={() => { setShowGameMenu(false); wsClient.send('Retire', {}); }}
            equity={portfolio?.totalEquity}
            trades={portfolio?.tradeCount}
          />
        )}
      </div>
    </header>
  );
}

function smaShieldColor(status: RegulatoryStatus): string {
  switch (status) {
    case 'Clear': return 'var(--text-disabled)';
    case 'UnderReview': return 'var(--text-secondary)';
    case 'UnderInvestigation': return 'var(--warning)';
    case 'EnforcementPending': return 'var(--red-primary)';
    default: return 'var(--text-disabled)';
  }
}

function smaStatusLabel(status: RegulatoryStatus): string {
  switch (status) {
    case 'Clear': return 'Clear';
    case 'UnderReview': return 'Under Review';
    case 'UnderInvestigation': return 'Under Investigation';
    case 'EnforcementPending': return 'Enforcement Action Pending';
    default: return 'Clear';
  }
}

function SMAPanel({ smaData, smaStatus, onClose }: {
  smaData: import('@/types/market').SMAStatusResponse | null;
  smaStatus: RegulatoryStatus;
  onClose: () => void;
}) {
  return (
    <div style={smaPanelStyles.overlay} onClick={onClose}>
      <div style={smaPanelStyles.panel} onClick={(e) => e.stopPropagation()}>
        <div style={smaPanelStyles.header}>
          <Shield size={18} style={{ color: smaShieldColor(smaStatus) }} />
          <span style={smaPanelStyles.title}>SMA Regulatory Status</span>
          <button style={smaPanelStyles.closeBtn} onClick={onClose} aria-label="Close regulatory status panel">×</button>
        </div>

        <div style={smaPanelStyles.statusBar}>
          <span style={{ color: smaShieldColor(smaStatus), fontWeight: 600 }}>
            {smaStatusLabel(smaStatus)}
          </span>
          {smaData?.accountFrozen && (
            <span style={{ color: 'var(--red-primary)', fontWeight: 700 }}>ACCOUNT FROZEN</span>
          )}
        </div>

        {smaData && (
          <>
            {/* Active Investigations */}
            {smaData.investigations.length > 0 && (
              <div style={smaPanelStyles.section}>
                <div style={smaPanelStyles.sectionTitle}>Active Investigations</div>
                {smaData.investigations.map((inv) => (
                  <div key={inv.id} style={smaPanelStyles.item}>
                    <span style={{ color: 'var(--warning)' }}>⚠</span>
                    <span>{inv.type.replace(/([A-Z])/g, ' $1').trim()} in {inv.symbol}</span>
                    <span className="mono" style={{ color: 'var(--text-disabled)', fontSize: '11px' }}>
                      {inv.daysRemaining}d remaining
                    </span>
                  </div>
                ))}
              </div>
            )}

            {/* Trading Restrictions */}
            {smaData.tradingRestrictions.length > 0 && (
              <div style={smaPanelStyles.section}>
                <div style={smaPanelStyles.sectionTitle}>Trading Restrictions</div>
                {smaData.tradingRestrictions.map((r, i) => (
                  <div key={i} style={smaPanelStyles.item}>
                    <span style={{ color: 'var(--red-primary)' }}>🚫</span>
                    <span>{r.symbol}: {r.closeOnly ? 'Close-Only' : 'Restricted'}</span>
                    <span className="mono" style={{ color: 'var(--text-disabled)', fontSize: '11px' }}>
                      until {new Date(r.expiresAt).toLocaleDateString()}
                    </span>
                  </div>
                ))}
              </div>
            )}

            {smaData.tradingBanUntil && (
              <div style={{ ...smaPanelStyles.item, color: 'var(--red-primary)' }}>
                Trading Ban until {new Date(smaData.tradingBanUntil).toLocaleDateString()}
              </div>
            )}

            {smaData.marginBanUntil && (
              <div style={{ ...smaPanelStyles.item, color: 'var(--red-primary)' }}>
                Margin Revoked until {new Date(smaData.marginBanUntil).toLocaleDateString()}
              </div>
            )}

            {/* Past Penalties */}
            {smaData.penalties.length > 0 && (
              <div style={smaPanelStyles.section}>
                <div style={smaPanelStyles.sectionTitle}>Penalties</div>
                {smaData.penalties.map((p) => (
                  <div key={p.id} style={smaPanelStyles.item}>
                    <span style={{ color: 'var(--red-primary)' }}>💰</span>
                    <span>{p.description}</span>
                    <span className="mono" style={{ color: 'var(--red-primary)', fontSize: '12px', fontWeight: 600 }}>
                      -${p.fineAmount.toLocaleString()}
                    </span>
                  </div>
                ))}
              </div>
            )}

            {/* Violation History */}
            {smaData.violations.length > 0 && (
              <div style={smaPanelStyles.section}>
                <div style={smaPanelStyles.sectionTitle}>Violation History</div>
                {smaData.violations.slice(-5).reverse().map((v) => (
                  <div key={v.id} style={{ ...smaPanelStyles.item, fontSize: '11px' }}>
                    <span>{v.description}</span>
                    <span className="mono" style={{ color: 'var(--text-disabled)' }}>
                      {new Date(v.detectedAt).toLocaleDateString()}
                    </span>
                  </div>
                ))}
              </div>
            )}

            {/* Clean state message */}
            {smaData.violations.length === 0 && smaData.investigations.length === 0 && smaData.penalties.length === 0 && (
              <div style={{ padding: '16px', color: 'var(--text-disabled)', textAlign: 'center' as const, fontSize: '13px' }}>
                No regulatory issues. Your trading activity is clean.
              </div>
            )}
          </>
        )}

        {!smaData && (
          <div style={{ padding: '16px', color: 'var(--text-disabled)', textAlign: 'center' as const, fontSize: '13px' }}>
            Loading regulatory data...
          </div>
        )}
      </div>
    </div>
  );
}

function GameMenu({ onClose, onResume, onSettings, onWiki, onSave, onMainMenu, canRetire, onRetire, equity, trades }: {
  onClose: () => void;
  onResume: () => void;
  onSettings: () => void;
  onWiki: () => void;
  onSave: () => void;
  onMainMenu: () => void;
  canRetire: boolean;
  onRetire: () => void;
  equity?: number;
  trades?: number;
}) {
  const menuItems = [
    { label: 'Resume', icon: Play, action: onResume, shortcut: 'Esc' },
    { label: 'Save Game', icon: Save, action: onSave, shortcut: 'Ctrl+S' },
    { label: 'Settings', icon: Settings, action: onSettings },
    { label: 'Wiki', icon: BookOpen, action: onWiki, shortcut: 'Ctrl+W' },
    ...(canRetire ? [{ label: 'Retire', icon: FastForward, action: onRetire, color: 'var(--gold-primary)' as string }] : []),
    { label: 'Main Menu', icon: Menu, action: onMainMenu, color: 'var(--red-primary)' as string },
  ];

  return (
    <div style={gameMenuStyles.overlay} onClick={onClose}>
      <div style={gameMenuStyles.modal} onClick={(e) => e.stopPropagation()}>
        <h2 style={gameMenuStyles.title}>STOCKSIM</h2>
        {equity !== undefined && trades !== undefined && (
          <div style={{ display: 'flex', justifyContent: 'center', margin: '8px 0' }}>
            <CareerBadge equity={equity} trades={trades} />
          </div>
        )}
        <div style={gameMenuStyles.divider} />
        <div style={gameMenuStyles.items}>
          {menuItems.map((item) => (
            <button
              key={item.label}
              style={{
                ...gameMenuStyles.item,
                color: (item as { color?: string }).color || 'var(--text-primary)',
              }}
              onClick={item.action}
              onMouseEnter={(e) => {
                e.currentTarget.style.background = 'var(--bg-tertiary)';
                e.currentTarget.style.borderColor = 'var(--border)';
              }}
              onMouseLeave={(e) => {
                e.currentTarget.style.background = 'transparent';
                e.currentTarget.style.borderColor = 'transparent';
              }}
            >
              <item.icon size={18} />
              <span style={{ flex: 1 }}>{item.label}</span>
              {(item as { shortcut?: string }).shortcut && (
                <span style={gameMenuStyles.shortcut}>{(item as { shortcut?: string }).shortcut}</span>
              )}
            </button>
          ))}
        </div>
      </div>
    </div>
  );
}

const gameMenuStyles: Record<string, React.CSSProperties> = {
  overlay: {
    position: 'fixed',
    top: 0, left: 0, right: 0, bottom: 0,
    background: 'rgba(0, 0, 0, 0.6)',
    backdropFilter: 'blur(4px)',
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'center',
    zIndex: 2000,
  },
  modal: {
    background: 'var(--bg-secondary)',
    border: '1px solid var(--border)',
    borderRadius: '12px',
    padding: '32px',
    width: '320px',
    boxShadow: '0 16px 64px rgba(0, 0, 0, 0.8)',
  },
  title: {
    fontFamily: 'var(--font-mono)',
    fontSize: '24px',
    fontWeight: 700,
    color: 'var(--text-accent)',
    letterSpacing: '4px',
    textAlign: 'center' as const,
    textShadow: '0 0 20px rgba(96, 165, 250, 0.3)',
    margin: 0,
  },
  divider: {
    height: '1px',
    background: 'var(--border)',
    margin: '16px 0',
  },
  items: {
    display: 'flex',
    flexDirection: 'column' as const,
    gap: '4px',
  },
  item: {
    display: 'flex',
    alignItems: 'center',
    gap: '12px',
    padding: '12px 16px',
    background: 'transparent',
    border: '1px solid transparent',
    borderRadius: '8px',
    cursor: 'pointer',
    fontSize: '15px',
    fontWeight: 500,
    fontFamily: 'var(--font-ui)',
    transition: 'background 150ms, border-color 150ms',
    width: '100%',
    textAlign: 'left' as const,
  },
  shortcut: {
    fontSize: '11px',
    color: 'var(--text-disabled)',
    fontFamily: 'var(--font-mono)',
    fontWeight: 400,
  },
};

const smaPanelStyles: Record<string, React.CSSProperties> = {
  overlay: {
    position: 'fixed',
    top: 0,
    left: 0,
    right: 0,
    bottom: 0,
    zIndex: 1000,
  },
  panel: {
    position: 'absolute',
    top: 'var(--topbar-height)',
    right: '120px',
    width: '380px',
    maxHeight: '500px',
    overflowY: 'auto' as const,
    background: 'var(--bg-secondary)',
    border: '1px solid var(--border)',
    borderRadius: '8px',
    boxShadow: '0 8px 32px rgba(0, 0, 0, 0.6)',
    zIndex: 1001,
  },
  header: {
    display: 'flex',
    alignItems: 'center',
    gap: '8px',
    padding: '12px 16px',
    borderBottom: '1px solid var(--border)',
  },
  title: {
    flex: 1,
    fontWeight: 600,
    fontSize: '14px',
    color: 'var(--text-primary)',
  },
  closeBtn: {
    background: 'none',
    border: 'none',
    color: 'var(--text-secondary)',
    fontSize: '18px',
    cursor: 'pointer',
    padding: '0 4px',
  },
  statusBar: {
    padding: '8px 16px',
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'center',
    borderBottom: '1px solid var(--border)',
    fontSize: '13px',
  },
  section: {
    padding: '8px 0',
    borderBottom: '1px solid var(--border)',
  },
  sectionTitle: {
    padding: '4px 16px',
    fontSize: '11px',
    fontWeight: 600,
    color: 'var(--text-disabled)',
    textTransform: 'uppercase' as const,
    letterSpacing: '0.5px',
  },
  item: {
    padding: '6px 16px',
    display: 'flex',
    alignItems: 'center',
    gap: '8px',
    fontSize: '12px',
    color: 'var(--text-primary)',
  },
};

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
  menuBtn: {
    background: 'transparent',
    border: '1px solid var(--border)',
    color: 'var(--text-secondary)',
    cursor: 'pointer',
    padding: '6px 10px',
    borderRadius: '6px',
    display: 'flex',
    alignItems: 'center',
    transition: 'all 150ms',
  },
};
