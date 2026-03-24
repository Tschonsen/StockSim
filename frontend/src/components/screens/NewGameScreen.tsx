import { useState, useCallback } from 'react';
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
  marketSpeed: number;
}

const DIFFICULTIES = [
  { key: 'easy', label: 'Beginner', cash: 100_000, commission: 0, stocks: 200,
    color: '#10B981', desc: 'No trading fees. Higher starting capital. Fewer stocks for a simpler market. Best for learning.' },
  { key: 'normal', label: 'Trader', cash: 50_000, commission: 4.95, stocks: 250,
    color: '#60A5FA', desc: 'Standard fees and capital. Full market with 250 stocks. The intended experience.' },
  { key: 'hard', label: 'Wall Street', cash: 25_000, commission: 9.95, stocks: 300,
    color: '#EF4444', desc: 'High fees, low capital, more stocks. Tight margins. For experienced traders only.' },
] as const;

interface Props {
  onStart: (config: GameConfig) => void;
  onBack: () => void;
  wsClient: WebSocketClient;
}

export function NewGameScreen({ onStart, onBack, wsClient }: Props) {
  const [config, setConfig] = useState<GameConfig>({
    difficulty: 'normal', playerName: '', startingCash: 50_000,
    commission: 4.95, stockCount: 250, seed: '', showTutorial: true,
    enableDividends: true, enableShortSelling: true, enableEvents: true, marketSpeed: 1,
  });

  const selectDifficulty = (d: typeof DIFFICULTIES[number]) => {
    setConfig(p => ({ ...p, difficulty: d.key as GameConfig['difficulty'], startingCash: d.cash, commission: d.commission, stockCount: d.stocks }));
  };

  const isCustom = config.difficulty === 'custom';

  const handleStart = useCallback(() => {
    log.info('Starting new game', config);
    const payload: Record<string, unknown> = { stockCount: config.stockCount, startingCash: config.startingCash };
    if (config.seed) payload.seed = parseInt(config.seed) || config.seed.split('').reduce((a, c) => a + c.charCodeAt(0), 0);
    wsClient.send('NewGame', payload);
    onStart(config);
  }, [config, wsClient, onStart]);

  const activeDiff = DIFFICULTIES.find(d => d.key === config.difficulty);

  return (
    <div style={S.screen}>
      <div style={S.bg} />
      <div style={S.wrap}>
        {/* Top bar */}
        <div style={S.topBar}>
          <button style={S.backBtn} onClick={onBack}>Back</button>
          <h1 style={S.pageTitle}>New Game</h1>
          <div style={{ width: 60 }} />
        </div>

        <div style={S.body}>
          {/* LEFT — Difficulty + Player */}
          <div style={S.left}>
            <Section label="Player">
              <div style={S.fieldRow}>
                <label style={S.fieldLabel}>Name</label>
                <input style={S.input} type="text" placeholder="Trader" value={config.playerName}
                  onChange={e => setConfig(p => ({ ...p, playerName: e.target.value }))} />
              </div>
            </Section>

            <Section label="Difficulty">
              {DIFFICULTIES.map(d => (
                <button key={d.key} onClick={() => selectDifficulty(d)}
                  style={{ ...S.diffCard, borderColor: config.difficulty === d.key ? d.color : 'var(--border)',
                    ...(config.difficulty === d.key ? { background: `${d.color}08` } : {}) }}>
                  <div style={S.diffTop}>
                    <span style={{ ...S.diffLabel, color: config.difficulty === d.key ? d.color : 'var(--text-primary)' }}>{d.label}</span>
                    <span className="mono" style={S.diffMeta}>${d.cash.toLocaleString()} · ${d.commission || 'Free'}</span>
                  </div>
                  <span style={S.diffDesc}>{d.desc}</span>
                </button>
              ))}
              <button onClick={() => setConfig(p => ({ ...p, difficulty: 'custom' }))}
                style={{ ...S.diffCard, borderColor: isCustom ? '#F59E0B' : 'var(--border)',
                  ...(isCustom ? { background: 'rgba(245,158,11,0.04)' } : {}) }}>
                <div style={S.diffTop}>
                  <span style={{ ...S.diffLabel, color: isCustom ? '#F59E0B' : 'var(--text-primary)' }}>Custom</span>
                  <span className="mono" style={S.diffMeta}>Your rules</span>
                </div>
              </button>
            </Section>
          </div>

          {/* RIGHT — Settings + Summary */}
          <div style={S.right}>
            <Section label="Market">
              <Slider label="Starting Capital" val={config.startingCash} min={5000} max={500000} step={5000}
                fmt={v => `$${v.toLocaleString()}`} set={v => setConfig(p => ({ ...p, startingCash: v, difficulty: 'custom' }))} off={!isCustom} />
              <Slider label="Commission" val={config.commission} min={0} max={20} step={0.05}
                fmt={v => v === 0 ? 'Free' : `$${v.toFixed(2)}`} set={v => setConfig(p => ({ ...p, commission: v, difficulty: 'custom' }))} off={!isCustom} />
              <Slider label="Stocks" val={config.stockCount} min={50} max={500} step={10}
                fmt={v => `${v}`} set={v => setConfig(p => ({ ...p, stockCount: v, difficulty: 'custom' }))} off={!isCustom} />
            </Section>

            <Section label="Rules">
              <Toggle label="Dividends" val={config.enableDividends} set={v => setConfig(p => ({ ...p, enableDividends: v }))} />
              <Toggle label="Short Selling" val={config.enableShortSelling} set={v => setConfig(p => ({ ...p, enableShortSelling: v }))} />
              <Toggle label="Market Events" val={config.enableEvents} set={v => setConfig(p => ({ ...p, enableEvents: v }))} />
              <Toggle label="Tutorial" val={config.showTutorial} set={v => setConfig(p => ({ ...p, showTutorial: v }))} />
            </Section>

            <Section label="Advanced">
              <div style={S.fieldRow}>
                <label style={S.fieldLabel}>Market Seed</label>
                <input style={{ ...S.input, width: '140px' }} type="text" placeholder="Random"
                  value={config.seed} onChange={e => setConfig(p => ({ ...p, seed: e.target.value }))} />
              </div>
              <span style={S.hint}>Same seed generates the same market. Leave empty for random.</span>
            </Section>

            {/* Summary + Start */}
            <div style={S.summary}>
              <div style={S.summaryGrid}>
                <SumItem label="Capital" value={`$${config.startingCash.toLocaleString()}`} color="var(--green-primary)" />
                <SumItem label="Fees" value={config.commission === 0 ? 'Free' : `$${config.commission.toFixed(2)}`} />
                <SumItem label="Stocks" value={`${config.stockCount}`} />
                <SumItem label="Difficulty" value={activeDiff?.label || 'Custom'} color={activeDiff?.color || '#F59E0B'} />
              </div>
              <button style={S.startBtn} onClick={handleStart}>Start Trading</button>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}

// --- Sub-components ---

function Section({ label, children }: { label: string; children: React.ReactNode }) {
  return (
    <div style={{ marginBottom: '18px' }}>
      <div style={S.sectionLabel}>{label.toUpperCase()}</div>
      {children}
    </div>
  );
}

function Slider({ label, val, min, max, step, fmt, set, off }: {
  label: string; val: number; min: number; max: number; step: number; fmt: (v: number) => string; set: (v: number) => void; off: boolean;
}) {
  return (
    <div style={{ ...S.row, opacity: off ? 0.45 : 1 }}>
      <span style={S.rowLabel}>{label}</span>
      <div style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
        <input type="range" min={min} max={max} step={step} value={val}
          onChange={e => set(parseFloat(e.target.value))} disabled={off}
          style={{ width: '90px', accentColor: 'var(--text-accent)' }} />
        <span className="mono" style={{ fontSize: '12px', color: 'var(--text-primary)', width: '70px', textAlign: 'right' }}>{fmt(val)}</span>
      </div>
    </div>
  );
}

function Toggle({ label, val, set }: { label: string; val: boolean; set: (v: boolean) => void }) {
  return (
    <div style={S.row}>
      <span style={S.rowLabel}>{label}</span>
      <button onClick={() => set(!val)}
        style={{ ...S.toggle, background: val ? 'var(--green-primary)' : 'var(--bg-tertiary)' }}>
        <div style={{ ...S.knob, transform: val ? 'translateX(16px)' : 'translateX(0)' }} />
      </button>
    </div>
  );
}

function SumItem({ label, value, color }: { label: string; value: string; color?: string }) {
  return (
    <div style={{ textAlign: 'center' }}>
      <div style={{ fontSize: '9px', color: 'var(--text-disabled)', letterSpacing: '1px' }}>{label.toUpperCase()}</div>
      <div className="mono" style={{ fontSize: '14px', fontWeight: 700, color: color || 'var(--text-primary)', marginTop: '2px' }}>{value}</div>
    </div>
  );
}

// --- Styles ---

const S: Record<string, React.CSSProperties> = {
  screen: { position: 'fixed', inset: 0, background: 'var(--bg-primary)', zIndex: 10000, overflow: 'auto' },
  bg: { position: 'absolute', inset: 0, background: 'radial-gradient(ellipse at 30% 20%, rgba(96,165,250,0.03) 0%, transparent 60%)', pointerEvents: 'none' },
  wrap: { position: 'relative', maxWidth: '900px', margin: '0 auto', padding: '30px 24px', height: '100%', display: 'flex', flexDirection: 'column' },
  topBar: { display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '24px', flexShrink: 0 },
  backBtn: { background: 'transparent', border: '1px solid var(--border)', borderRadius: '4px', color: 'var(--text-secondary)', padding: '6px 14px', cursor: 'pointer', fontFamily: 'var(--font-ui)', fontSize: '13px' },
  pageTitle: { fontFamily: 'var(--font-mono)', fontSize: '22px', fontWeight: 700, color: 'var(--text-primary)', letterSpacing: '3px', textShadow: '0 0 15px rgba(96,165,250,0.12)' },
  body: { display: 'flex', gap: '28px', flex: 1, minHeight: 0 },
  left: { width: '380px', flexShrink: 0, overflowY: 'auto' },
  right: { flex: 1, display: 'flex', flexDirection: 'column', overflowY: 'auto' },
  sectionLabel: { fontSize: '9px', fontWeight: 700, color: 'var(--text-accent)', letterSpacing: '2px', marginBottom: '8px' },
  // Difficulty cards
  diffCard: { width: '100%', padding: '12px 14px', marginBottom: '6px', background: 'var(--bg-secondary)', border: '1px solid var(--border)', borderRadius: '6px', cursor: 'pointer', textAlign: 'left' as const, transition: 'all 120ms', fontFamily: 'var(--font-ui)' },
  diffTop: { display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '4px' },
  diffLabel: { fontSize: '14px', fontWeight: 700 },
  diffMeta: { fontSize: '11px', color: 'var(--text-disabled)' },
  diffDesc: { fontSize: '11px', color: 'var(--text-secondary)', lineHeight: '1.4', display: 'block' },
  // Fields
  fieldRow: { display: 'flex', justifyContent: 'space-between', alignItems: 'center', padding: '4px 0' },
  fieldLabel: { fontSize: '13px', color: 'var(--text-secondary)' },
  input: { height: '30px', background: 'var(--bg-input)', color: 'var(--text-primary)', border: '1px solid var(--border)', borderRadius: '4px', padding: '0 8px', fontFamily: 'var(--font-mono)', fontSize: '13px', width: '180px' },
  hint: { fontSize: '10px', color: 'var(--text-disabled)', display: 'block', marginTop: '4px' },
  // Rows
  row: { display: 'flex', justifyContent: 'space-between', alignItems: 'center', padding: '5px 0', borderBottom: '1px solid rgba(31,41,55,0.15)' },
  rowLabel: { fontSize: '13px', color: 'var(--text-primary)' },
  toggle: { width: '36px', height: '20px', borderRadius: '10px', border: 'none', cursor: 'pointer', position: 'relative', padding: 0, transition: 'background 150ms', flexShrink: 0 },
  knob: { width: '16px', height: '16px', borderRadius: '50%', background: '#FFF', position: 'absolute', top: '2px', left: '2px', transition: 'transform 150ms' },
  // Summary
  summary: { marginTop: 'auto', paddingTop: '16px', borderTop: '1px solid var(--border)' },
  summaryGrid: { display: 'flex', justifyContent: 'space-between', marginBottom: '14px' },
  startBtn: { width: '100%', height: '46px', borderRadius: '6px', background: 'linear-gradient(135deg, #3B82F6, #60A5FA)', color: '#FFF', border: 'none', cursor: 'pointer', fontSize: '16px', fontWeight: 700, fontFamily: 'var(--font-ui)', letterSpacing: '1.5px', boxShadow: '0 4px 16px rgba(96,165,250,0.2)', transition: 'all 150ms' },
};
