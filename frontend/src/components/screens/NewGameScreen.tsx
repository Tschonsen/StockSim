import { useState, useCallback } from 'react';
import { WebSocketClient } from '@/services/websocket';
import { createLogger } from '@/services/logger';

const log = createLogger('NewGame');

export interface GameConfig {
  difficulty: 'easy' | 'normal' | 'hard' | 'custom';
  startingCash: number;
  commission: number;
  stockCount: number;
  seed: string;
  showTutorial: boolean;
}

const PRESETS: Record<string, { cash: number; commission: number; stocks: number; desc: string; icon: string }> = {
  easy:   { cash: 100_000, commission: 0,    stocks: 200, desc: 'No fees, more capital. Perfect for learning.', icon: '🌱' },
  normal: { cash: 50_000,  commission: 4.95, stocks: 250, desc: 'Standard trading experience. Realistic fees.', icon: '📈' },
  hard:   { cash: 25_000,  commission: 9.95, stocks: 300, desc: 'High fees, less capital. For experienced traders.', icon: '🔥' },
};

interface NewGameScreenProps {
  onStart: (config: GameConfig) => void;
  onBack: () => void;
  wsClient: WebSocketClient;
}

/**
 * Full-screen New Game setup. Bible 3.0.3.
 * Replaces the modal — proper dedicated screen with all options.
 */
export function NewGameScreen({ onStart, onBack, wsClient }: NewGameScreenProps) {
  const [config, setConfig] = useState<GameConfig>({
    difficulty: 'normal',
    startingCash: 50_000,
    commission: 4.95,
    stockCount: 250,
    seed: '',
    showTutorial: true,
  });

  const selectPreset = (key: string) => {
    const p = PRESETS[key];
    if (p) setConfig(prev => ({ ...prev, difficulty: key as GameConfig['difficulty'], startingCash: p.cash, commission: p.commission, stockCount: p.stocks }));
  };

  const handleStart = useCallback(() => {
    log.info('Starting new game', config);
    const payload: Record<string, unknown> = { stockCount: config.stockCount, startingCash: config.startingCash };
    if (config.seed) payload.seed = parseInt(config.seed) || config.seed.split('').reduce((a, c) => a + c.charCodeAt(0), 0);
    wsClient.send('NewGame', payload);
    onStart(config);
  }, [config, wsClient, onStart]);

  return (
    <div style={styles.container}>
      {/* Background gradient */}
      <div style={styles.bgGradient} />

      <div style={styles.content}>
        {/* Header */}
        <div style={styles.header}>
          <button style={styles.backBtn} onClick={onBack}>← Back</button>
          <h1 style={styles.title}>New Game</h1>
          <div style={{ width: '80px' }} />
        </div>

        {/* Two-column layout */}
        <div style={styles.columns}>
          {/* Left: Difficulty Selection */}
          <div style={styles.leftCol}>
            <h2 style={styles.sectionTitle}>Choose Difficulty</h2>

            {Object.entries(PRESETS).map(([key, preset]) => (
              <button
                key={key}
                onClick={() => selectPreset(key)}
                style={{
                  ...styles.presetCard,
                  ...(config.difficulty === key ? styles.presetCardActive : {}),
                  borderColor: config.difficulty === key
                    ? (key === 'easy' ? '#10B981' : key === 'normal' ? '#60A5FA' : '#EF4444')
                    : 'var(--border)',
                }}
              >
                <div style={styles.presetHeader}>
                  <span style={{ fontSize: '24px' }}>{preset.icon}</span>
                  <div>
                    <span style={styles.presetName}>{key.charAt(0).toUpperCase() + key.slice(1)}</span>
                    <span style={styles.presetMeta}>
                      ${preset.cash.toLocaleString()} | ${preset.commission}/trade | {preset.stocks} stocks
                    </span>
                  </div>
                </div>
                <p style={styles.presetDesc}>{preset.desc}</p>
              </button>
            ))}

            <button
              onClick={() => setConfig(prev => ({ ...prev, difficulty: 'custom' }))}
              style={{
                ...styles.presetCard,
                ...(config.difficulty === 'custom' ? styles.presetCardActive : {}),
                borderColor: config.difficulty === 'custom' ? '#F59E0B' : 'var(--border)',
              }}
            >
              <div style={styles.presetHeader}>
                <span style={{ fontSize: '24px' }}>⚙</span>
                <div>
                  <span style={styles.presetName}>Custom</span>
                  <span style={styles.presetMeta}>Set your own rules</span>
                </div>
              </div>
            </button>
          </div>

          {/* Right: Settings & Summary */}
          <div style={styles.rightCol}>
            <h2 style={styles.sectionTitle}>Game Settings</h2>

            {/* Sliders */}
            <div style={styles.sliderGroup}>
              <Slider label="Starting Capital" value={config.startingCash} min={5000} max={500000} step={5000}
                display={`$${config.startingCash.toLocaleString()}`}
                onChange={v => setConfig(p => ({ ...p, startingCash: v, difficulty: 'custom' }))}
                disabled={config.difficulty !== 'custom'} />
              <Slider label="Commission per Trade" value={config.commission} min={0} max={20} step={0.05}
                display={config.commission === 0 ? 'Free' : `$${config.commission.toFixed(2)}`}
                onChange={v => setConfig(p => ({ ...p, commission: v, difficulty: 'custom' }))}
                disabled={config.difficulty !== 'custom'} />
              <Slider label="Number of Stocks" value={config.stockCount} min={50} max={500} step={10}
                display={`${config.stockCount}`}
                onChange={v => setConfig(p => ({ ...p, stockCount: v, difficulty: 'custom' }))}
                disabled={config.difficulty !== 'custom'} />
            </div>

            {/* Seed + Options */}
            <div style={styles.optionsRow}>
              <div style={{ flex: 1 }}>
                <label style={styles.fieldLabel}>Market Seed</label>
                <input type="text" value={config.seed} placeholder="Random"
                  onChange={e => setConfig(p => ({ ...p, seed: e.target.value }))}
                  style={styles.input} />
                <span style={styles.hint}>Same seed = same market. Leave empty for random.</span>
              </div>
              <label style={styles.checkLabel}>
                <input type="checkbox" checked={config.showTutorial}
                  onChange={e => setConfig(p => ({ ...p, showTutorial: e.target.checked }))} />
                Show Tutorial
              </label>
            </div>

            {/* Summary Box */}
            <div style={styles.summary}>
              <div style={styles.summaryRow}>
                <span>Capital</span>
                <span className="mono" style={{ fontWeight: 700, color: 'var(--green-primary)' }}>
                  ${config.startingCash.toLocaleString()}
                </span>
              </div>
              <div style={styles.summaryRow}>
                <span>Fees</span>
                <span className="mono">{config.commission === 0 ? 'Free' : `$${config.commission.toFixed(2)}/trade`}</span>
              </div>
              <div style={styles.summaryRow}>
                <span>Market</span>
                <span className="mono">{config.stockCount} stocks, 12 sectors</span>
              </div>
            </div>

            {/* Start Button */}
            <button style={styles.startBtn} onClick={handleStart}>
              Start Trading
            </button>
          </div>
        </div>
      </div>
    </div>
  );
}

function Slider({ label, value, min, max, step, display, onChange, disabled }: {
  label: string; value: number; min: number; max: number; step: number;
  display: string; onChange: (v: number) => void; disabled: boolean;
}) {
  return (
    <div style={{ marginBottom: '16px', opacity: disabled ? 0.5 : 1 }}>
      <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: '4px' }}>
        <span style={{ fontSize: '13px', color: 'var(--text-secondary)' }}>{label}</span>
        <span className="mono" style={{ fontSize: '14px', fontWeight: 700, color: 'var(--text-primary)' }}>{display}</span>
      </div>
      <input type="range" min={min} max={max} step={step} value={value}
        onChange={e => onChange(parseFloat(e.target.value))} disabled={disabled}
        style={{ width: '100%', accentColor: 'var(--text-accent)' }} />
    </div>
  );
}

const styles: Record<string, React.CSSProperties> = {
  container: { position: 'fixed', inset: 0, background: 'var(--bg-primary)', zIndex: 10000, overflow: 'auto' },
  bgGradient: {
    position: 'absolute', inset: 0,
    background: 'radial-gradient(ellipse at 30% 20%, rgba(96, 165, 250, 0.04) 0%, transparent 60%), radial-gradient(ellipse at 70% 80%, rgba(16, 185, 129, 0.03) 0%, transparent 60%)',
    pointerEvents: 'none',
  },
  content: { position: 'relative', maxWidth: '960px', margin: '0 auto', padding: '40px 32px' },
  header: { display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '40px' },
  backBtn: {
    background: 'transparent', border: '1px solid var(--border)', borderRadius: '6px',
    color: 'var(--text-secondary)', padding: '8px 16px', cursor: 'pointer',
    fontFamily: 'var(--font-ui)', fontSize: '14px',
  },
  title: {
    fontFamily: 'var(--font-mono)', fontSize: '28px', fontWeight: 700,
    color: 'var(--text-primary)', letterSpacing: '4px',
    textShadow: '0 0 20px rgba(96, 165, 250, 0.15)',
  },
  columns: { display: 'flex', gap: '32px' },
  leftCol: { flex: 1 },
  rightCol: { flex: 1 },
  sectionTitle: { fontSize: '12px', fontWeight: 700, color: 'var(--text-accent)', textTransform: 'uppercase' as const, letterSpacing: '2px', marginBottom: '16px' },
  presetCard: {
    width: '100%', padding: '16px', marginBottom: '8px',
    background: 'var(--bg-secondary)', border: '2px solid var(--border)', borderRadius: '10px',
    cursor: 'pointer', textAlign: 'left' as const, transition: 'all 150ms',
    fontFamily: 'var(--font-ui)',
  },
  presetCardActive: { background: 'rgba(96, 165, 250, 0.06)', boxShadow: '0 0 20px rgba(96, 165, 250, 0.08)' },
  presetHeader: { display: 'flex', alignItems: 'center', gap: '12px' },
  presetName: { fontSize: '16px', fontWeight: 700, color: 'var(--text-primary)', display: 'block' },
  presetMeta: { fontSize: '11px', color: 'var(--text-disabled)', fontFamily: 'var(--font-mono)' },
  presetDesc: { fontSize: '12px', color: 'var(--text-secondary)', margin: '8px 0 0 36px' },
  sliderGroup: { marginBottom: '16px' },
  optionsRow: { display: 'flex', gap: '20px', alignItems: 'flex-start', marginBottom: '20px' },
  fieldLabel: { fontSize: '12px', color: 'var(--text-secondary)', display: 'block', marginBottom: '4px' },
  input: {
    width: '100%', height: '34px', background: 'var(--bg-input)', color: 'var(--text-primary)',
    border: '1px solid var(--border)', borderRadius: '6px', padding: '0 10px',
    fontFamily: 'var(--font-mono)', fontSize: '13px',
  },
  hint: { fontSize: '10px', color: 'var(--text-disabled)', display: 'block', marginTop: '4px' },
  checkLabel: { fontSize: '13px', color: 'var(--text-secondary)', cursor: 'pointer', display: 'flex', alignItems: 'center', gap: '6px', paddingTop: '20px', whiteSpace: 'nowrap' as const },
  summary: {
    background: 'var(--bg-tertiary)', borderRadius: '8px', padding: '16px', marginBottom: '20px',
    border: '1px solid var(--border)',
  },
  summaryRow: { display: 'flex', justifyContent: 'space-between', padding: '4px 0', fontSize: '13px', color: 'var(--text-secondary)' },
  startBtn: {
    width: '100%', height: '52px', borderRadius: '8px',
    background: 'linear-gradient(135deg, #3B82F6, #60A5FA)',
    color: '#FFF', border: 'none', cursor: 'pointer',
    fontSize: '18px', fontWeight: 700, fontFamily: 'var(--font-ui)',
    letterSpacing: '2px',
    boxShadow: '0 4px 20px rgba(96, 165, 250, 0.25)',
    transition: 'all 150ms',
  },
};
