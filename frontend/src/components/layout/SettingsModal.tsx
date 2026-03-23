import { useState } from 'react';

export interface GameSettings {
  autosave: boolean;
  confirmOrders: boolean;
  autoPauseOnNews: boolean;
  autoPauseOnAlert: boolean;
  autoPauseOnMarketOpen: boolean;
  skipWeekends: boolean;
  commission: number;
  newsTickerSpeed: number;
}

const DEFAULT_SETTINGS: GameSettings = {
  autosave: true,
  confirmOrders: true,
  autoPauseOnNews: true,
  autoPauseOnAlert: true,
  autoPauseOnMarketOpen: false,
  skipWeekends: false,
  commission: 4.95,
  newsTickerSpeed: 60,
};

interface SettingsModalProps {
  isOpen: boolean;
  onClose: () => void;
  settings: GameSettings;
  onSettingsChange: (settings: GameSettings) => void;
}

/**
 * Settings modal. Bible 16.1-16.6.
 */
export function SettingsModal({ isOpen, onClose, settings, onSettingsChange }: SettingsModalProps) {
  const [activeSection, setActiveSection] = useState('general');

  if (!isOpen) return null;

  const update = (key: keyof GameSettings, value: boolean | number) => {
    onSettingsChange({ ...settings, [key]: value });
  };

  const sections = [
    { id: 'general', label: 'General' },
    { id: 'simulation', label: 'Simulation' },
    { id: 'display', label: 'Display' },
  ];

  return (
    <div style={styles.overlay} onClick={onClose}>
      <div style={styles.modal} onClick={e => e.stopPropagation()}>
        {/* Header */}
        <div style={styles.header}>
          <span style={styles.title}>Settings</span>
          <button style={styles.closeBtn} onClick={onClose}>x</button>
        </div>

        <div style={styles.body}>
          {/* Left: Section tabs */}
          <div style={styles.sidebar}>
            {sections.map(s => (
              <button
                key={s.id}
                onClick={() => setActiveSection(s.id)}
                style={{
                  ...styles.sectionBtn,
                  background: activeSection === s.id ? 'var(--bg-tertiary)' : 'transparent',
                  color: activeSection === s.id ? 'var(--text-accent)' : 'var(--text-secondary)',
                }}
              >
                {s.label}
              </button>
            ))}
          </div>

          {/* Right: Settings content */}
          <div style={styles.content}>
            {activeSection === 'general' && (
              <>
                <ToggleSetting label="Autosave" value={settings.autosave} onChange={v => update('autosave', v)} />
                <ToggleSetting label="Confirm Orders" value={settings.confirmOrders} onChange={v => update('confirmOrders', v)} />
                <ToggleSetting label="Auto-Pause on Breaking News" value={settings.autoPauseOnNews} onChange={v => update('autoPauseOnNews', v)} />
                <ToggleSetting label="Auto-Pause on Price Alert" value={settings.autoPauseOnAlert} onChange={v => update('autoPauseOnAlert', v)} />
                <ToggleSetting label="Auto-Pause on Market Open" value={settings.autoPauseOnMarketOpen} onChange={v => update('autoPauseOnMarketOpen', v)} />
                <ToggleSetting label="Skip Weekends" value={settings.skipWeekends} onChange={v => update('skipWeekends', v)} />
              </>
            )}

            {activeSection === 'simulation' && (
              <>
                <div style={styles.settingRow}>
                  <span style={styles.settingLabel}>Trading Commission</span>
                  <div style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
                    <span style={styles.settingValue}>$</span>
                    <input
                      type="number"
                      value={settings.commission}
                      onChange={e => update('commission', parseFloat(e.target.value) || 0)}
                      min="0" step="0.01"
                      style={styles.input}
                    />
                  </div>
                </div>
              </>
            )}

            {activeSection === 'display' && (
              <>
                <div style={styles.settingRow}>
                  <span style={styles.settingLabel}>News Ticker Speed</span>
                  <div style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
                    <input
                      type="range" min="20" max="120"
                      value={settings.newsTickerSpeed}
                      onChange={e => update('newsTickerSpeed', parseInt(e.target.value))}
                      style={{ width: '120px' }}
                    />
                    <span className="mono" style={styles.settingValue}>{settings.newsTickerSpeed}px/s</span>
                  </div>
                </div>
              </>
            )}
          </div>
        </div>

        {/* Footer */}
        <div style={styles.footer}>
          <button
            style={{ ...styles.footerBtn, color: 'var(--text-disabled)' }}
            onClick={() => onSettingsChange(DEFAULT_SETTINGS)}
          >
            Reset to Defaults
          </button>
          <button style={{ ...styles.footerBtn, color: 'var(--text-accent)' }} onClick={onClose}>
            Close
          </button>
        </div>
      </div>
    </div>
  );
}

function ToggleSetting({ label, value, onChange }: { label: string; value: boolean; onChange: (v: boolean) => void }) {
  return (
    <div style={styles.settingRow}>
      <span style={styles.settingLabel}>{label}</span>
      <button
        onClick={() => onChange(!value)}
        style={{
          ...styles.toggle,
          background: value ? 'var(--green-primary)' : 'var(--bg-tertiary)',
        }}
      >
        <div style={{
          ...styles.toggleKnob,
          transform: value ? 'translateX(16px)' : 'translateX(0)',
        }} />
      </button>
    </div>
  );
}

export { DEFAULT_SETTINGS };

const styles: Record<string, React.CSSProperties> = {
  overlay: {
    position: 'fixed', inset: 0,
    background: 'rgba(0,0,0,0.6)',
    display: 'flex', alignItems: 'center', justifyContent: 'center',
    zIndex: 11000,
  },
  modal: {
    width: '700px', maxHeight: '550px',
    background: 'var(--bg-secondary)',
    border: '1px solid var(--border)',
    borderRadius: '8px',
    display: 'flex', flexDirection: 'column',
  },
  header: {
    display: 'flex', justifyContent: 'space-between', alignItems: 'center',
    padding: '16px 20px',
    borderBottom: '1px solid var(--border)',
  },
  title: { fontSize: '18px', fontWeight: 700, color: 'var(--text-primary)' },
  closeBtn: {
    background: 'transparent', border: 'none', color: 'var(--text-secondary)',
    fontSize: '18px', cursor: 'pointer', padding: '4px 8px',
  },
  body: { display: 'flex', flex: 1, overflow: 'hidden' },
  sidebar: {
    width: '160px', borderRight: '1px solid var(--border)',
    padding: '8px 0', display: 'flex', flexDirection: 'column',
  },
  sectionBtn: {
    background: 'transparent', border: 'none',
    padding: '10px 16px', textAlign: 'left',
    fontSize: '14px', fontFamily: 'var(--font-ui)',
    cursor: 'pointer',
  },
  content: { flex: 1, padding: '16px 20px', overflowY: 'auto' },
  settingRow: {
    display: 'flex', justifyContent: 'space-between', alignItems: 'center',
    padding: '10px 0', borderBottom: '1px solid rgba(31,41,55,0.3)',
  },
  settingLabel: { fontSize: '14px', color: 'var(--text-primary)' },
  settingValue: { fontSize: '13px', color: 'var(--text-secondary)', fontFamily: 'var(--font-mono)' },
  toggle: {
    width: '36px', height: '20px', borderRadius: '10px',
    border: 'none', cursor: 'pointer', position: 'relative', padding: 0,
    transition: 'background 150ms',
  },
  toggleKnob: {
    width: '16px', height: '16px', borderRadius: '50%',
    background: '#FFF', position: 'absolute', top: '2px', left: '2px',
    transition: 'transform 150ms',
  },
  input: {
    width: '80px', height: '28px',
    background: 'var(--bg-input)', color: 'var(--text-primary)',
    border: '1px solid var(--border)', borderRadius: '4px',
    padding: '0 8px', fontFamily: 'var(--font-mono)', fontSize: '13px',
  },
  footer: {
    display: 'flex', justifyContent: 'space-between',
    padding: '12px 20px', borderTop: '1px solid var(--border)',
  },
  footerBtn: {
    background: 'transparent', border: 'none',
    fontSize: '14px', cursor: 'pointer', fontFamily: 'var(--font-ui)',
  },
};
