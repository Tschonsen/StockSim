import { useState, useCallback, useMemo } from 'react';
import { WebSocketClient } from '@/services/websocket';
import { createLogger } from '@/services/logger';

const log = createLogger('NewGame');

export interface GameConfig {
  difficulty: 'easy' | 'normal' | 'hard' | 'custom';
  playerName: string;
  startingCash: number;
  commission: number;
  stockCount: number;
  seed: string;
  showTutorial: boolean;
  enableDividends: boolean;
  enableShortSelling: boolean;
  enableEvents: boolean;
  enableMargin: boolean;
  enableBankruptcy: boolean;
  marketSpeed: number;
  volatility: number;       // 0.5 = low, 1 = normal, 2 = high
  eventFrequency: number;   // 0.5 = rare, 1 = normal, 2 = frequent
  aiAggression: number;     // 0.5 = passive, 1 = normal, 2 = aggressive
  marketHours: 'realistic' | 'extended';
}

interface Preset {
  key: string;
  label: string;
  sub: string;
  cash: number;
  commission: number;
  stocks: number;
  color: string;
  icon: string;
  volatility: number;
  eventFrequency: number;
  aiAggression: number;
  enableMargin: boolean;
  enableBankruptcy: boolean;
}

const PRESETS: Preset[] = [
  { key: 'easy', label: 'Beginner', sub: 'Learn the ropes', cash: 100_000, commission: 0, stocks: 150,
    color: '#10B981', icon: '\u25B6', volatility: 0.7, eventFrequency: 0.5, aiAggression: 0.5,
    enableMargin: false, enableBankruptcy: false },
  { key: 'normal', label: 'Trader', sub: 'The standard experience', cash: 50_000, commission: 4.95, stocks: 250,
    color: '#60A5FA', icon: '\u25C6', volatility: 1.0, eventFrequency: 1.0, aiAggression: 1.0,
    enableMargin: false, enableBankruptcy: false },
  { key: 'hard', label: 'Wall Street', sub: 'Tight margins, high stakes', cash: 25_000, commission: 9.95, stocks: 350,
    color: '#F59E0B', icon: '\u2605', volatility: 1.4, eventFrequency: 1.5, aiAggression: 1.5,
    enableMargin: true, enableBankruptcy: true },
];

const DEFAULT_CONFIG: GameConfig = {
  difficulty: 'normal', playerName: '', startingCash: 50_000,
  commission: 4.95, stockCount: 250, seed: '', showTutorial: true,
  enableDividends: true, enableShortSelling: true, enableEvents: true,
  enableMargin: false, enableBankruptcy: false, marketSpeed: 1,
  volatility: 1.0, eventFrequency: 1.0, aiAggression: 1.0,
  marketHours: 'realistic',
};

// --- Difficulty score calculation (0-100) ---
function calcDifficulty(c: GameConfig): number {
  let score = 50;
  // Cash: 500k = -30, 50k = 0, 5k = +20
  score += ((50_000 - c.startingCash) / 50_000) * 20;
  // Commission: 0 = -15, 5 = 0, 20 = +10
  score += ((c.commission - 5) / 15) * 10;
  // Stocks: more = harder to track
  score += ((c.stockCount - 250) / 250) * 8;
  // Volatility
  score += (c.volatility - 1) * 15;
  // Event frequency
  score += (c.eventFrequency - 1) * 8;
  // AI aggression
  score += (c.aiAggression - 1) * 10;
  // Toggles
  if (c.enableMargin) score += 5;
  if (c.enableBankruptcy) score += 8;
  if (!c.enableDividends) score += 3;
  if (!c.enableEvents) score -= 5;
  if (!c.enableShortSelling) score += 2;

  return Math.max(0, Math.min(100, Math.round(score)));
}

function getDiffLabel(score: number): { label: string; color: string } {
  if (score <= 20) return { label: 'Very Easy', color: '#10B981' };
  if (score <= 40) return { label: 'Easy', color: '#34D399' };
  if (score <= 55) return { label: 'Normal', color: '#60A5FA' };
  if (score <= 70) return { label: 'Challenging', color: '#F59E0B' };
  if (score <= 85) return { label: 'Hard', color: '#F97316' };
  return { label: 'Brutal', color: '#EF4444' };
}

interface Props {
  onStart: (config: GameConfig) => void;
  onBack: () => void;
  wsClient: WebSocketClient;
}

const SCENARIOS = [
  { id: 'the_crash', name: 'The Crash', desc: 'Market is crashing -40%. Survive with positive portfolio.', diff: 'Hard', cash: 50_000, time: '6 months', color: '#EF4444' },
  { id: 'bull_run', name: 'Bull Run', desc: 'Strong bull market. Turn $25k into $100k.', diff: 'Normal', cash: 25_000, time: '3 months', color: '#10B981' },
  { id: 'short_squeeze', name: 'Short Squeeze', desc: 'Spot and profit from the squeeze. Target: $80k.', diff: 'Hard', cash: 30_000, time: '1 month', color: '#F59E0B' },
  { id: 'one_stock', name: 'One Stock', desc: 'Only trade ONE stock. Reach $150k.', diff: 'Hard', cash: 50_000, time: '1 year', color: '#8B5CF6' },
  { id: 'recession', name: 'Recession', desc: 'Economy in recession. Survive with less than -10% loss.', diff: 'Hard', cash: 100_000, time: '1 year', color: '#DC2626' },
  { id: 'penny_stocks', name: 'Penny Stocks', desc: 'Only stocks under $5. Turn $10k into $50k.', diff: 'Hard', cash: 10_000, time: '6 months', color: '#EC4899' },
  { id: 'dividend_king', name: 'Dividend King', desc: 'Build $5k/quarter passive dividend income.', diff: 'Normal', cash: 100_000, time: '2 years', color: '#06B6D4' },
  { id: 'speed_run', name: 'Speed Run', desc: 'Reach $1M as fast as possible. No time limit.', diff: 'Normal', cash: 50_000, time: 'Unlimited', color: '#F97316' },
  { id: 'iron_man', name: 'Iron Man', desc: 'No saving. Survive 1 year with $50k+.', diff: 'Brutal', cash: 50_000, time: '1 year', color: '#B91C1C' },
  { id: 'day_trader', name: 'Day Trader', desc: 'Make $50k through active day trading. High volatility.', diff: 'Hard', cash: 25_000, time: '3 months', color: '#D97706' },
];

export function NewGameScreen({ onStart, onBack, wsClient }: Props) {
  const [config, setConfig] = useState<GameConfig>({ ...DEFAULT_CONFIG });
  const [hoveredPreset, setHoveredPreset] = useState<string | null>(null);
  const [mode, setMode] = useState<'sandbox' | 'scenarios'>('sandbox');
  const [selectedScenario, setSelectedScenario] = useState<string | null>(null);

  const selectPreset = (p: Preset) => {
    setConfig(prev => ({
      ...prev,
      difficulty: p.key as GameConfig['difficulty'],
      startingCash: p.cash,
      commission: p.commission,
      stockCount: p.stocks,
      volatility: p.volatility,
      eventFrequency: p.eventFrequency,
      aiAggression: p.aiAggression,
      enableMargin: p.enableMargin,
      enableBankruptcy: p.enableBankruptcy,
    }));
  };

  const set = <K extends keyof GameConfig>(key: K, val: GameConfig[K]) => {
    setConfig(prev => ({ ...prev, [key]: val, difficulty: 'custom' as const }));
  };

  const diffScore = useMemo(() => calcDifficulty(config), [config]);
  const diffInfo = useMemo(() => getDiffLabel(diffScore), [diffScore]);

  const handleStart = useCallback(() => {
    log.info('Starting new game', config);
    const payload: Record<string, unknown> = {
      stockCount: config.stockCount,
      startingCash: config.startingCash,
      commission: config.commission,
      volatility: config.volatility,
      eventFrequency: config.eventFrequency,
      aiAggression: config.aiAggression,
      enableMargin: config.enableMargin,
      enableBankruptcy: config.enableBankruptcy,
      enableDividends: config.enableDividends,
      enableShortSelling: config.enableShortSelling,
      enableEvents: config.enableEvents,
      marketHours: config.marketHours,
    };
    if (config.seed) {
      payload.seed = parseInt(config.seed) || config.seed.split('').reduce((a, c) => a + c.charCodeAt(0), 0);
    }
    wsClient.send('NewGame', payload);
    onStart(config);
  }, [config, wsClient, onStart]);

  const activePreset = PRESETS.find(p => p.key === config.difficulty);

  return (
    <div style={S.screen}>
      <div style={S.bgGrad} />

      {/* Header */}
      <div style={S.header}>
        <button style={S.backBtn} onClick={onBack}
          onMouseEnter={e => { e.currentTarget.style.borderColor = 'var(--text-accent)'; e.currentTarget.style.color = 'var(--text-primary)'; }}
          onMouseLeave={e => { e.currentTarget.style.borderColor = 'var(--border)'; e.currentTarget.style.color = 'var(--text-secondary)'; }}
        >Back</button>
        <div style={{ display: 'flex', alignItems: 'center', gap: '16px' }}>
          <h1 style={S.title}>NEW GAME</h1>
          <div style={{ display: 'flex', background: 'var(--bg-tertiary)', borderRadius: '6px', overflow: 'hidden' }}>
            <button onClick={() => setMode('sandbox')} style={{
              padding: '6px 16px', border: 'none', cursor: 'pointer', fontSize: '12px', fontWeight: 600,
              fontFamily: 'var(--font-ui)', letterSpacing: '1px',
              background: mode === 'sandbox' ? 'var(--text-accent)' : 'transparent',
              color: mode === 'sandbox' ? '#FFF' : 'var(--text-secondary)',
            }}>SANDBOX</button>
            <button onClick={() => setMode('scenarios')} style={{
              padding: '6px 16px', border: 'none', cursor: 'pointer', fontSize: '12px', fontWeight: 600,
              fontFamily: 'var(--font-ui)', letterSpacing: '1px',
              background: mode === 'scenarios' ? 'var(--text-accent)' : 'transparent',
              color: mode === 'scenarios' ? '#FFF' : 'var(--text-secondary)',
            }}>SCENARIOS</button>
          </div>
        </div>
        <div style={{ width: 70 }} />
      </div>

      {/* Scenarios Mode */}
      {mode === 'scenarios' && (
        <div style={{ flex: 1, padding: '24px 40px', overflowY: 'auto' }}>
          <div style={{
            display: 'grid', gridTemplateColumns: 'repeat(2, 1fr)', gap: '12px',
            maxWidth: '900px', margin: '0 auto',
          }}>
            {SCENARIOS.map(sc => {
              const active = selectedScenario === sc.id;
              return (
                <button key={sc.id} onClick={() => setSelectedScenario(active ? null : sc.id)}
                  style={{
                    background: active ? `${sc.color}10` : 'var(--bg-secondary)',
                    border: `1px solid ${active ? sc.color : 'var(--border)'}`,
                    borderRadius: '8px', padding: '16px 20px', textAlign: 'left',
                    cursor: 'pointer', transition: 'all 150ms',
                    boxShadow: active ? `0 0 15px ${sc.color}15` : 'none',
                  }}
                >
                  <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '6px' }}>
                    <span style={{ fontSize: '15px', fontWeight: 700, color: active ? sc.color : 'var(--text-primary)' }}>{sc.name}</span>
                    <span style={{
                      fontSize: '10px', fontWeight: 600, padding: '2px 8px', borderRadius: '3px',
                      background: sc.diff === 'Brutal' ? 'rgba(239,68,68,0.2)' : sc.diff === 'Hard' ? 'rgba(245,158,11,0.2)' : 'rgba(96,165,250,0.2)',
                      color: sc.diff === 'Brutal' ? '#EF4444' : sc.diff === 'Hard' ? '#F59E0B' : '#60A5FA',
                    }}>{sc.diff}</span>
                  </div>
                  <p style={{ fontSize: '12px', color: 'var(--text-secondary)', margin: '0 0 8px', lineHeight: 1.4 }}>{sc.desc}</p>
                  <div style={{ display: 'flex', gap: '12px', fontSize: '11px', color: 'var(--text-disabled)' }}>
                    <span className="mono">${sc.cash.toLocaleString()}</span>
                    <span>{sc.time}</span>
                  </div>
                </button>
              );
            })}
          </div>

          {selectedScenario && (
            <div style={{ textAlign: 'center', marginTop: '24px' }}>
              <button onClick={() => {
                wsClient.send('StartScenario', { scenarioId: selectedScenario });
                onStart({ ...DEFAULT_CONFIG });
              }}
                style={{
                  padding: '12px 48px', borderRadius: '8px', fontSize: '16px', fontWeight: 700,
                  background: 'linear-gradient(135deg, var(--green-primary), #059669)',
                  color: '#FFF', border: 'none', cursor: 'pointer', letterSpacing: '1px',
                  boxShadow: '0 0 20px rgba(16,185,129,0.3)',
                }}
              >Start Scenario</button>
            </div>
          )}
        </div>
      )}

      {/* Sandbox Mode — Main content */}
      {mode === 'sandbox' && <div style={S.content}>

        {/* LEFT COLUMN — Presets + Difficulty Meter */}
        <div style={S.colLeft}>
          <SectionHeader label="Difficulty Preset" />
          <div style={S.presetList}>
            {PRESETS.map(p => {
              const active = config.difficulty === p.key;
              const hov = hoveredPreset === p.key;
              return (
                <button key={p.key} onClick={() => selectPreset(p)}
                  onMouseEnter={() => setHoveredPreset(p.key)}
                  onMouseLeave={() => setHoveredPreset(null)}
                  style={{
                    ...S.presetCard,
                    borderColor: active ? p.color : hov ? 'var(--border-hover)' : 'var(--border)',
                    background: active ? `${p.color}0A` : hov ? 'rgba(255,255,255,0.02)' : 'var(--bg-secondary)',
                    boxShadow: active ? `inset 0 0 20px ${p.color}08, 0 0 12px ${p.color}10` : 'none',
                  }}>
                  <div style={S.presetTop}>
                    <span style={{ ...S.presetIcon, color: active ? p.color : 'var(--text-disabled)' }}>{p.icon}</span>
                    <span style={{ ...S.presetLabel, color: active ? p.color : 'var(--text-primary)' }}>{p.label}</span>
                  </div>
                  <span style={S.presetSub}>{p.sub}</span>
                  <div style={S.presetStats}>
                    <span className="mono">${p.cash.toLocaleString()}</span>
                    <span style={S.presetDot} />
                    <span className="mono">{p.commission === 0 ? 'No fees' : `$${p.commission} fee`}</span>
                    <span style={S.presetDot} />
                    <span className="mono">{p.stocks} stocks</span>
                  </div>
                </button>
              );
            })}
          </div>

          {/* Difficulty Meter */}
          <div style={S.meterBox}>
            <SectionHeader label="Difficulty Rating" />
            <div style={S.meterRow}>
              <div style={S.meterTrack}>
                <div style={{
                  ...S.meterFill,
                  width: `${diffScore}%`,
                  background: `linear-gradient(90deg, #10B981 0%, #60A5FA 40%, #F59E0B 70%, #EF4444 100%)`,
                }} />
                <div style={{
                  ...S.meterThumb,
                  left: `${diffScore}%`,
                  borderColor: diffInfo.color,
                  boxShadow: `0 0 8px ${diffInfo.color}60`,
                }} />
              </div>
            </div>
            <div style={S.meterLabels}>
              <span style={{ fontSize: '10px', color: '#10B981', fontFamily: 'var(--font-mono)' }}>EASY</span>
              <div style={{ textAlign: 'center' }}>
                <span className="mono" style={{ fontSize: '28px', fontWeight: 700, color: diffInfo.color, textShadow: `0 0 20px ${diffInfo.color}40` }}>
                  {diffScore}%
                </span>
                <div style={{ fontSize: '11px', color: diffInfo.color, fontWeight: 600, letterSpacing: '1px', marginTop: '-2px' }}>
                  {diffInfo.label.toUpperCase()}
                </div>
              </div>
              <span style={{ fontSize: '10px', color: '#EF4444', fontFamily: 'var(--font-mono)' }}>HARD</span>
            </div>
          </div>

          {/* Player Name */}
          <div style={{ marginTop: '16px' }}>
            <SectionHeader label="Player" />
            <div style={S.fieldRow}>
              <label style={S.fieldLabel}>Name</label>
              <input style={S.input} type="text" placeholder="Trader"
                value={config.playerName}
                onChange={e => setConfig(p => ({ ...p, playerName: e.target.value }))} />
            </div>
          </div>
        </div>

        {/* RIGHT COLUMN — All settings */}
        <div style={S.colRight}>
          <div style={S.scrollArea}>

            {/* Economy */}
            <SectionHeader label="Economy" />
            <SliderRow label="Starting Capital" value={config.startingCash} min={5_000} max={500_000} step={5_000}
              fmt={v => `$${v.toLocaleString()}`} onChange={v => set('startingCash', v)} />
            <SliderRow label="Commission" value={config.commission} min={0} max={25} step={0.05}
              fmt={v => v === 0 ? 'Free' : `$${v.toFixed(2)}`} onChange={v => set('commission', v)} />
            <SliderRow label="Number of Stocks" value={config.stockCount} min={50} max={500} step={10}
              fmt={v => `${v}`} onChange={v => set('stockCount', v)} />

            {/* Market Behavior */}
            <SectionHeader label="Market Behavior" />
            <SliderRow label="Volatility" value={config.volatility} min={0.3} max={2.5} step={0.1}
              fmt={v => v <= 0.5 ? 'Very Low' : v <= 0.8 ? 'Low' : v <= 1.2 ? 'Normal' : v <= 1.7 ? 'High' : 'Extreme'}
              onChange={v => set('volatility', v)} />
            <SliderRow label="Event Frequency" value={config.eventFrequency} min={0.2} max={3.0} step={0.1}
              fmt={v => v <= 0.4 ? 'Very Rare' : v <= 0.7 ? 'Rare' : v <= 1.3 ? 'Normal' : v <= 2.0 ? 'Frequent' : 'Chaotic'}
              onChange={v => set('eventFrequency', v)} />
            <SliderRow label="AI Aggression" value={config.aiAggression} min={0.3} max={2.5} step={0.1}
              fmt={v => v <= 0.5 ? 'Passive' : v <= 0.8 ? 'Calm' : v <= 1.2 ? 'Normal' : v <= 1.7 ? 'Aggressive' : 'Ruthless'}
              onChange={v => set('aiAggression', v)} />

            {/* Rules */}
            <SectionHeader label="Rules" />
            <ToggleRow label="Dividends" desc="Companies pay quarterly dividends" value={config.enableDividends} onChange={v => set('enableDividends', v)} />
            <ToggleRow label="Short Selling" desc="Bet against stocks" value={config.enableShortSelling} onChange={v => set('enableShortSelling', v)} />
            <ToggleRow label="Market Events" desc="News, crashes, IPOs" value={config.enableEvents} onChange={v => set('enableEvents', v)} />
            <ToggleRow label="Margin Trading" desc="Borrow money to trade" value={config.enableMargin} onChange={v => set('enableMargin', v)} />
            <ToggleRow label="Bankruptcy" desc="Game over at $0" value={config.enableBankruptcy} onChange={v => set('enableBankruptcy', v)} />
            <ToggleRow label="Tutorial" desc="Show hints for new players" value={config.showTutorial} onChange={v => set('showTutorial', v)} />

            {/* Advanced */}
            <SectionHeader label="Advanced" />
            <div style={S.fieldRow}>
              <div>
                <label style={S.fieldLabel}>Market Seed</label>
                <div style={S.fieldHint}>Same seed = same market</div>
              </div>
              <input style={{ ...S.input, width: '140px' }} type="text" placeholder="Random"
                value={config.seed} onChange={e => setConfig(p => ({ ...p, seed: e.target.value }))} />
            </div>
            <div style={{ ...S.fieldRow, marginTop: '8px' }}>
              <div>
                <label style={S.fieldLabel}>Market Hours</label>
                <div style={S.fieldHint}>Trading session length</div>
              </div>
              <div style={{ display: 'flex', gap: '4px' }}>
                <SegBtn label="Realistic" active={config.marketHours === 'realistic'} onClick={() => set('marketHours', 'realistic')} />
                <SegBtn label="Extended" active={config.marketHours === 'extended'} onClick={() => set('marketHours', 'extended')} />
              </div>
            </div>
          </div>

          {/* Summary Bar + Start */}
          <div style={S.bottomBar}>
            <div style={S.summaryRow}>
              <SumCell label="Capital" value={`$${config.startingCash.toLocaleString()}`} color="var(--green-primary)" />
              <SumCell label="Fees" value={config.commission === 0 ? 'Free' : `$${config.commission.toFixed(2)}`} />
              <SumCell label="Stocks" value={`${config.stockCount}`} />
              <SumCell label="Difficulty" value={activePreset?.label || 'Custom'} color={activePreset?.color || '#F59E0B'} />
              <SumCell label="Rating" value={`${diffScore}%`} color={diffInfo.color} />
            </div>
            <button style={S.startBtn} onClick={handleStart}
              onMouseEnter={e => { e.currentTarget.style.filter = 'brightness(1.15)'; e.currentTarget.style.transform = 'translateY(-1px)'; }}
              onMouseLeave={e => { e.currentTarget.style.filter = 'none'; e.currentTarget.style.transform = 'none'; }}
            >Start Trading</button>
          </div>
        </div>
      </div>}
    </div>
  );
}

// ── Sub-components ──

function SectionHeader({ label }: { label: string }) {
  return <div style={S.sectionHead}>{label.toUpperCase()}</div>;
}

function SliderRow({ label, value, min, max, step, fmt, onChange }: {
  label: string; value: number; min: number; max: number; step: number;
  fmt: (v: number) => string; onChange: (v: number) => void;
}) {
  const pct = ((value - min) / (max - min)) * 100;
  return (
    <div style={S.settingRow}>
      <span style={S.settingLabel}>{label}</span>
      <div style={S.sliderWrap}>
        <div style={S.sliderTrackOuter}>
          <input type="range" min={min} max={max} step={step} value={value}
            onChange={e => onChange(parseFloat(e.target.value))}
            style={S.sliderInput} />
          <div style={{ ...S.sliderTrackFill, width: `${pct}%` }} />
        </div>
        <span className="mono" style={S.sliderVal}>{fmt(value)}</span>
      </div>
    </div>
  );
}

function ToggleRow({ label, desc, value, onChange }: {
  label: string; desc: string; value: boolean; onChange: (v: boolean) => void;
}) {
  return (
    <div style={S.settingRow}>
      <div>
        <span style={S.settingLabel}>{label}</span>
        <div style={S.toggleDesc}>{desc}</div>
      </div>
      <button onClick={() => onChange(!value)} style={{
        ...S.toggleTrack,
        background: value ? 'var(--green-primary)' : 'var(--bg-tertiary)',
        borderColor: value ? 'var(--green-primary)' : 'var(--border)',
      }}>
        <div style={{ ...S.toggleKnob, transform: value ? 'translateX(16px)' : 'translateX(1px)' }} />
      </button>
    </div>
  );
}

function SegBtn({ label, active, onClick }: { label: string; active: boolean; onClick: () => void }) {
  return (
    <button onClick={onClick} style={{
      ...S.segBtn,
      background: active ? 'var(--text-accent)' : 'var(--bg-tertiary)',
      color: active ? '#FFF' : 'var(--text-secondary)',
      borderColor: active ? 'var(--text-accent)' : 'var(--border)',
    }}>{label}</button>
  );
}

function SumCell({ label, value, color }: { label: string; value: string; color?: string }) {
  return (
    <div style={{ textAlign: 'center', flex: 1 }}>
      <div style={{ fontSize: '9px', color: 'var(--text-disabled)', letterSpacing: '1px', fontWeight: 600 }}>{label.toUpperCase()}</div>
      <div className="mono" style={{ fontSize: '13px', fontWeight: 700, color: color || 'var(--text-primary)', marginTop: '2px' }}>{value}</div>
    </div>
  );
}

// ── Styles ──

const S: Record<string, React.CSSProperties> = {
  screen: {
    position: 'fixed', inset: 0, background: 'var(--bg-primary)', zIndex: 10000,
    display: 'flex', flexDirection: 'column', overflow: 'hidden',
  },
  bgGrad: {
    position: 'absolute', inset: 0, pointerEvents: 'none',
    background: 'radial-gradient(ellipse at 25% 15%, rgba(96,165,250,0.04) 0%, transparent 50%), radial-gradient(ellipse at 75% 85%, rgba(16,185,129,0.03) 0%, transparent 50%)',
  },

  // Header
  header: {
    position: 'relative', zIndex: 1, display: 'flex', justifyContent: 'space-between', alignItems: 'center',
    padding: '20px 40px', flexShrink: 0, borderBottom: '1px solid var(--border)',
  },
  backBtn: {
    background: 'transparent', border: '1px solid var(--border)', borderRadius: '4px',
    color: 'var(--text-secondary)', padding: '6px 16px', cursor: 'pointer',
    fontFamily: 'var(--font-ui)', fontSize: '13px', transition: 'all 150ms',
  },
  title: {
    fontFamily: 'var(--font-mono)', fontSize: '20px', fontWeight: 700, color: 'var(--text-primary)',
    letterSpacing: '4px', margin: 0, textShadow: '0 0 20px rgba(96,165,250,0.15)',
  },

  // Layout
  content: {
    position: 'relative', zIndex: 1, display: 'flex', flex: 1, minHeight: 0, overflow: 'hidden',
  },
  colLeft: {
    width: '340px', flexShrink: 0, padding: '20px 24px', borderRight: '1px solid var(--border)',
    overflowY: 'auto', display: 'flex', flexDirection: 'column',
  },
  colRight: {
    flex: 1, display: 'flex', flexDirection: 'column', minHeight: 0,
  },
  scrollArea: {
    flex: 1, overflowY: 'auto', padding: '20px 32px',
  },

  // Section header
  sectionHead: {
    fontSize: '10px', fontWeight: 700, color: 'var(--text-accent)', letterSpacing: '2px',
    marginBottom: '10px', marginTop: '18px', paddingBottom: '6px',
    borderBottom: '1px solid rgba(96,165,250,0.1)',
  },

  // Preset cards
  presetList: { display: 'flex', flexDirection: 'column', gap: '6px' },
  presetCard: {
    width: '100%', padding: '14px 16px', background: 'var(--bg-secondary)',
    border: '1px solid var(--border)', borderRadius: '6px', cursor: 'pointer',
    textAlign: 'left' as const, transition: 'all 150ms', fontFamily: 'var(--font-ui)',
  },
  presetTop: {
    display: 'flex', alignItems: 'center', gap: '8px', marginBottom: '4px',
  },
  presetIcon: { fontSize: '12px' },
  presetLabel: { fontSize: '15px', fontWeight: 700 },
  presetSub: { fontSize: '11px', color: 'var(--text-secondary)', display: 'block', marginBottom: '6px' },
  presetStats: {
    display: 'flex', alignItems: 'center', gap: '6px', fontSize: '10px', color: 'var(--text-disabled)',
    fontFamily: 'var(--font-mono)',
  },
  presetDot: {
    width: '3px', height: '3px', borderRadius: '50%', background: 'var(--text-disabled)',
    display: 'inline-block',
  },

  // Difficulty Meter
  meterBox: { marginTop: '6px' },
  meterRow: {
    position: 'relative', height: '18px', display: 'flex', alignItems: 'center',
  },
  meterTrack: {
    position: 'relative', flex: 1, height: '6px', borderRadius: '3px',
    background: 'var(--bg-tertiary)', overflow: 'visible',
  },
  meterFill: {
    position: 'absolute', top: 0, left: 0, height: '100%', borderRadius: '3px', transition: 'width 300ms ease',
  },
  meterThumb: {
    position: 'absolute', top: '50%', width: '14px', height: '14px', borderRadius: '50%',
    background: 'var(--bg-primary)', border: '2px solid', transform: 'translate(-50%, -50%)',
    transition: 'left 300ms ease, border-color 300ms ease, box-shadow 300ms ease',
  },
  meterLabels: {
    display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginTop: '8px',
  },

  // Settings rows
  settingRow: {
    display: 'flex', justifyContent: 'space-between', alignItems: 'center',
    padding: '8px 0', borderBottom: '1px solid rgba(31,41,55,0.3)',
  },
  settingLabel: { fontSize: '13px', color: 'var(--text-primary)', fontWeight: 500 },
  sliderWrap: { display: 'flex', alignItems: 'center', gap: '10px' },
  sliderTrackOuter: {
    position: 'relative', width: '120px', height: '20px', display: 'flex', alignItems: 'center',
  },
  sliderInput: {
    width: '120px', height: '4px', accentColor: 'var(--text-accent)',
    position: 'relative', zIndex: 1,
  },
  sliderTrackFill: {
    position: 'absolute', top: '50%', left: 0, height: '4px', borderRadius: '2px',
    background: 'var(--text-accent)', transform: 'translateY(-50%)', pointerEvents: 'none',
    opacity: 0.3,
  },
  sliderVal: {
    fontSize: '12px', color: 'var(--text-primary)', width: '80px', textAlign: 'right',
  },

  // Toggle
  toggleDesc: { fontSize: '10px', color: 'var(--text-disabled)', marginTop: '1px' },
  toggleTrack: {
    width: '38px', height: '22px', borderRadius: '11px', border: '1px solid',
    cursor: 'pointer', position: 'relative', padding: 0, transition: 'all 150ms', flexShrink: 0,
  },
  toggleKnob: {
    width: '16px', height: '16px', borderRadius: '50%', background: '#FFF',
    position: 'absolute', top: '2px', transition: 'transform 150ms',
  },

  // Segmented button
  segBtn: {
    padding: '5px 12px', fontSize: '11px', fontFamily: 'var(--font-ui)', fontWeight: 500,
    border: '1px solid', borderRadius: '4px', cursor: 'pointer', transition: 'all 120ms',
  },

  // Fields
  fieldRow: { display: 'flex', justifyContent: 'space-between', alignItems: 'center', padding: '8px 0' },
  fieldLabel: { fontSize: '13px', color: 'var(--text-primary)', fontWeight: 500 },
  fieldHint: { fontSize: '10px', color: 'var(--text-disabled)', marginTop: '1px' },
  input: {
    height: '32px', background: 'var(--bg-input)', color: 'var(--text-primary)',
    border: '1px solid var(--border)', borderRadius: '4px', padding: '0 10px',
    fontFamily: 'var(--font-mono)', fontSize: '13px', width: '180px',
    transition: 'border-color 150ms',
  },

  // Bottom bar
  bottomBar: {
    flexShrink: 0, padding: '14px 32px', borderTop: '1px solid var(--border)',
    background: 'rgba(10,14,23,0.9)',
  },
  summaryRow: {
    display: 'flex', justifyContent: 'space-between', marginBottom: '12px',
  },
  startBtn: {
    width: '100%', height: '48px', borderRadius: '6px',
    background: 'linear-gradient(135deg, #3B82F6, #60A5FA)', color: '#FFF',
    border: 'none', cursor: 'pointer', fontSize: '16px', fontWeight: 700,
    fontFamily: 'var(--font-ui)', letterSpacing: '2px',
    boxShadow: '0 4px 20px rgba(96,165,250,0.25)', transition: 'all 150ms',
  },
};
