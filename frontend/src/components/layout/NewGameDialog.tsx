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

const PRESETS: Record<string, Omit<GameConfig, 'seed' | 'showTutorial'>> = {
  easy: { difficulty: 'easy', startingCash: 100_000, commission: 0, stockCount: 200 },
  normal: { difficulty: 'normal', startingCash: 50_000, commission: 4.95, stockCount: 250 },
  hard: { difficulty: 'hard', startingCash: 25_000, commission: 9.95, stockCount: 300 },
};

interface NewGameDialogProps {
  isOpen: boolean;
  onClose: () => void;
  onStart: (config: GameConfig) => void;
  wsClient: WebSocketClient;
}

/**
 * New Game dialog with difficulty presets and custom sliders.
 * Bible 16.3 + 20.1: Difficulty settings affect starting capital,
 * commissions, AI aggressiveness, event frequency.
 */
export function NewGameDialog({ isOpen, onClose, onStart, wsClient }: NewGameDialogProps) {
  const [config, setConfig] = useState<GameConfig>({
    ...PRESETS.normal,
    seed: '',
    showTutorial: true,
  });

  const selectPreset = useCallback((key: string) => {
    const preset = PRESETS[key];
    if (preset) {
      setConfig(prev => ({ ...prev, ...preset }));
    }
  }, []);

  const handleStart = useCallback(() => {
    log.info('Starting new game', config);

    const payload: Record<string, unknown> = {
      stockCount: config.stockCount,
      startingCash: config.startingCash,
    };
    if (config.seed) {
      payload.seed = parseInt(config.seed) || config.seed.split('').reduce((a, c) => a + c.charCodeAt(0), 0);
    }

    wsClient.send('NewGame', payload);
    onStart(config);
    onClose();
  }, [config, wsClient, onStart, onClose]);

  if (!isOpen) return null;

  return (
    <div style={styles.overlay}>
      <div style={styles.modal}>
        {/* Header */}
        <div style={styles.header}>
          <span style={styles.title}>New Game</span>
          <button style={styles.closeBtn} onClick={onClose}>x</button>
        </div>

        {/* Difficulty Presets */}
        <div style={styles.section}>
          <label style={styles.sectionLabel}>Difficulty</label>
          <div style={styles.presetRow}>
            {(['easy', 'normal', 'hard'] as const).map(d => (
              <button
                key={d}
                onClick={() => selectPreset(d)}
                style={{
                  ...styles.presetBtn,
                  background: config.difficulty === d ? difficultyColor(d) : 'var(--bg-tertiary)',
                  color: config.difficulty === d ? '#FFF' : 'var(--text-secondary)',
                  borderColor: config.difficulty === d ? difficultyColor(d) : 'var(--border)',
                }}
              >
                <span style={{ fontWeight: 700, fontSize: '15px', display: 'block' }}>
                  {d === 'easy' ? 'Easy' : d === 'normal' ? 'Normal' : 'Hard'}
                </span>
                <span style={{ fontSize: '11px', opacity: 0.8 }}>
                  {d === 'easy' ? '$100K, No fees' : d === 'normal' ? '$50K, $4.95/trade' : '$25K, $9.95/trade'}
                </span>
              </button>
            ))}
            <button
              onClick={() => setConfig(prev => ({ ...prev, difficulty: 'custom' }))}
              style={{
                ...styles.presetBtn,
                background: config.difficulty === 'custom' ? 'var(--text-accent)' : 'var(--bg-tertiary)',
                color: config.difficulty === 'custom' ? '#FFF' : 'var(--text-secondary)',
                borderColor: config.difficulty === 'custom' ? 'var(--text-accent)' : 'var(--border)',
              }}
            >
              <span style={{ fontWeight: 700, fontSize: '15px', display: 'block' }}>Custom</span>
              <span style={{ fontSize: '11px', opacity: 0.8 }}>Your rules</span>
            </button>
          </div>
        </div>

        {/* Sliders (always visible, custom unlocks editing) */}
        <div style={styles.section}>
          <SliderSetting
            label="Starting Capital"
            value={config.startingCash}
            min={5000} max={500000} step={5000}
            display={`$${config.startingCash.toLocaleString()}`}
            onChange={v => setConfig(prev => ({ ...prev, startingCash: v, difficulty: 'custom' }))}
            disabled={config.difficulty !== 'custom'}
          />
          <SliderSetting
            label="Commission per Trade"
            value={config.commission}
            min={0} max={20} step={0.05}
            display={`$${config.commission.toFixed(2)}`}
            onChange={v => setConfig(prev => ({ ...prev, commission: v, difficulty: 'custom' }))}
            disabled={config.difficulty !== 'custom'}
          />
          <SliderSetting
            label="Number of Stocks"
            value={config.stockCount}
            min={50} max={500} step={10}
            display={`${config.stockCount}`}
            onChange={v => setConfig(prev => ({ ...prev, stockCount: v, difficulty: 'custom' }))}
            disabled={config.difficulty !== 'custom'}
          />
        </div>

        {/* Seed + Tutorial */}
        <div style={styles.section}>
          <div style={styles.inlineRow}>
            <div style={{ flex: 1 }}>
              <label style={styles.fieldLabel}>Seed (optional)</label>
              <input
                type="text"
                value={config.seed}
                onChange={e => setConfig(prev => ({ ...prev, seed: e.target.value }))}
                placeholder="Random"
                style={styles.input}
              />
              <span style={{ fontSize: '10px', color: 'var(--text-disabled)' }}>Same seed = same market</span>
            </div>
            <div style={{ display: 'flex', alignItems: 'center', gap: '8px', paddingTop: '16px' }}>
              <input
                type="checkbox"
                checked={config.showTutorial}
                onChange={e => setConfig(prev => ({ ...prev, showTutorial: e.target.checked }))}
                id="tutorialCheck"
              />
              <label htmlFor="tutorialCheck" style={{ fontSize: '13px', color: 'var(--text-secondary)', cursor: 'pointer' }}>
                Show Tutorial
              </label>
            </div>
          </div>
        </div>

        {/* Summary */}
        <div style={styles.summary}>
          <div style={styles.summaryItem}>
            <span style={styles.summaryLabel}>Capital</span>
            <span className="mono" style={styles.summaryValue}>${config.startingCash.toLocaleString()}</span>
          </div>
          <div style={styles.summaryItem}>
            <span style={styles.summaryLabel}>Fees</span>
            <span className="mono" style={styles.summaryValue}>${config.commission.toFixed(2)}/trade</span>
          </div>
          <div style={styles.summaryItem}>
            <span style={styles.summaryLabel}>Stocks</span>
            <span className="mono" style={styles.summaryValue}>{config.stockCount}</span>
          </div>
        </div>

        {/* Start Button */}
        <div style={styles.footer}>
          <button style={styles.cancelBtn} onClick={onClose}>Cancel</button>
          <button style={styles.startBtn} onClick={handleStart}>
            Start Trading
          </button>
        </div>
      </div>
    </div>
  );
}

function SliderSetting({ label, value, min, max, step, display, onChange, disabled }: {
  label: string; value: number; min: number; max: number; step: number;
  display: string; onChange: (v: number) => void; disabled: boolean;
}) {
  return (
    <div style={styles.sliderRow}>
      <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: '4px' }}>
        <span style={styles.fieldLabel}>{label}</span>
        <span className="mono" style={{ fontSize: '14px', fontWeight: 700, color: 'var(--text-primary)' }}>{display}</span>
      </div>
      <input
        type="range"
        min={min} max={max} step={step}
        value={value}
        onChange={e => onChange(parseFloat(e.target.value))}
        disabled={disabled}
        style={{ width: '100%', accentColor: 'var(--text-accent)', opacity: disabled ? 0.4 : 1 }}
      />
    </div>
  );
}

function difficultyColor(d: string): string {
  return d === 'easy' ? '#10B981' : d === 'normal' ? '#60A5FA' : '#EF4444';
}

const styles: Record<string, React.CSSProperties> = {
  overlay: {
    position: 'fixed', inset: 0, background: 'rgba(0,0,0,0.7)',
    display: 'flex', alignItems: 'center', justifyContent: 'center', zIndex: 3000,
  },
  modal: {
    width: '580px', background: 'var(--bg-secondary)',
    border: '1px solid var(--border)', borderRadius: '12px', overflow: 'hidden',
  },
  header: {
    display: 'flex', justifyContent: 'space-between', alignItems: 'center',
    padding: '20px 24px', borderBottom: '1px solid var(--border)',
  },
  title: { fontSize: '22px', fontWeight: 800, color: 'var(--text-primary)', fontFamily: 'var(--font-ui)' },
  closeBtn: {
    background: 'transparent', border: 'none', color: 'var(--text-secondary)',
    fontSize: '18px', cursor: 'pointer',
  },
  section: { padding: '16px 24px' },
  sectionLabel: { fontSize: '11px', fontWeight: 600, color: 'var(--text-secondary)', textTransform: 'uppercase' as const, display: 'block', marginBottom: '8px' },
  presetRow: { display: 'flex', gap: '8px' },
  presetBtn: {
    flex: 1, padding: '12px 8px', borderRadius: '8px', border: '2px solid var(--border)',
    cursor: 'pointer', textAlign: 'center' as const, fontFamily: 'var(--font-ui)',
    transition: 'all 150ms',
  },
  sliderRow: { marginBottom: '12px' },
  fieldLabel: { fontSize: '12px', color: 'var(--text-secondary)' },
  inlineRow: { display: 'flex', gap: '24px', alignItems: 'flex-start' },
  input: {
    width: '100%', height: '32px', background: 'var(--bg-input)', color: 'var(--text-primary)',
    border: '1px solid var(--border)', borderRadius: '4px', padding: '0 8px',
    fontFamily: 'var(--font-mono)', fontSize: '13px', marginTop: '4px', display: 'block',
  },
  summary: {
    display: 'flex', gap: '16px', padding: '12px 24px',
    background: 'var(--bg-primary)', borderTop: '1px solid var(--border)',
  },
  summaryItem: { flex: 1, textAlign: 'center' as const },
  summaryLabel: { fontSize: '10px', color: 'var(--text-disabled)', textTransform: 'uppercase' as const, display: 'block' },
  summaryValue: { fontSize: '16px', fontWeight: 700, color: 'var(--text-primary)' },
  footer: {
    display: 'flex', justifyContent: 'flex-end', gap: '12px',
    padding: '16px 24px', borderTop: '1px solid var(--border)',
  },
  cancelBtn: {
    padding: '10px 20px', borderRadius: '6px', background: 'var(--bg-tertiary)',
    color: 'var(--text-secondary)', border: 'none', cursor: 'pointer',
    fontSize: '14px', fontFamily: 'var(--font-ui)',
  },
  startBtn: {
    padding: '10px 32px', borderRadius: '6px', background: 'var(--text-accent)',
    color: '#FFF', border: 'none', cursor: 'pointer',
    fontSize: '15px', fontWeight: 700, fontFamily: 'var(--font-ui)',
  },
};
