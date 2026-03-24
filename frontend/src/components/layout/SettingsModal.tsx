import { useState } from 'react';

export interface KeyBindings {
  pauseResume: string;
  speed1: string;
  speed2: string;
  speed3: string;
  speed4: string;
  tabDashboard: string;
  tabPortfolio: string;
  tabMarket: string;
  tabOrders: string;
  tabNews: string;
  tabAnalytics: string;
  quickSave: string;
  help: string;
}

export const DEFAULT_KEYBINDINGS: KeyBindings = {
  pauseResume: 'Space',
  speed1: '1', speed2: '2', speed3: '3', speed4: '4',
  tabDashboard: 'D', tabPortfolio: 'P', tabMarket: 'M',
  tabOrders: 'O', tabNews: 'N', tabAnalytics: 'A',
  quickSave: 'Ctrl+S', help: '?',
};

export interface GameSettings {
  // General
  autosave: boolean;
  confirmOrders: boolean;
  autoPauseOnNews: boolean;
  autoPauseOnAlert: boolean;
  autoPauseOnMarketOpen: boolean;
  skipWeekends: boolean;
  // Audio
  masterVolume: number;
  sfxVolume: number;
  musicVolume: number;
  marketBellSound: boolean;
  tradeSound: boolean;
  newsAlertSound: boolean;
  // Graphics
  windowMode: 'windowed' | 'fullscreen' | 'borderless';
  resolution: string;
  uiScale: number;
  reducedAnimations: boolean;
  showSparklines: boolean;
  newsTickerSpeed: number;
  // Controls
  keyBindings: KeyBindings;
}

export const DEFAULT_SETTINGS: GameSettings = {
  autosave: true,
  confirmOrders: true,
  autoPauseOnNews: true,
  autoPauseOnAlert: true,
  autoPauseOnMarketOpen: false,
  skipWeekends: false,
  masterVolume: 80,
  sfxVolume: 70,
  musicVolume: 50,
  marketBellSound: true,
  tradeSound: true,
  newsAlertSound: true,
  windowMode: 'windowed',
  resolution: 'native',
  uiScale: 100,
  reducedAnimations: false,
  showSparklines: true,
  newsTickerSpeed: 60,
  keyBindings: { ...DEFAULT_KEYBINDINGS },
};

interface SettingsModalProps {
  isOpen: boolean;
  onClose: () => void;
  settings: GameSettings;
  onSettingsChange: (settings: GameSettings) => void;
}

const SECTIONS = [
  { id: 'general', label: 'General' },
  { id: 'graphics', label: 'Graphics' },
  { id: 'audio', label: 'Audio' },
  { id: 'controls', label: 'Controls' },
] as const;

export function SettingsModal({ isOpen, onClose, settings, onSettingsChange }: SettingsModalProps) {
  const [activeSection, setActiveSection] = useState('general');

  if (!isOpen) return null;

  const update = <K extends keyof GameSettings>(key: K, value: GameSettings[K]) => {
    onSettingsChange({ ...settings, [key]: value });
  };

  return (
    <div style={styles.overlay} onClick={onClose}>
      <div style={styles.modal} onClick={e => e.stopPropagation()}>
        <div style={styles.header}>
          <span style={styles.title}>Settings</span>
          <button style={styles.closeBtn} onClick={onClose}>x</button>
        </div>

        <div style={styles.body}>
          <div style={styles.sidebar}>
            {SECTIONS.map(s => (
              <button key={s.id} onClick={() => setActiveSection(s.id)}
                style={{ ...styles.sectionBtn, ...(activeSection === s.id ? styles.sectionBtnActive : {}) }}>
                {s.label}
              </button>
            ))}
          </div>

          <div style={styles.content}>
            {activeSection === 'general' && (
              <>
                <SectionHeader label="Game" />
                <Toggle label="Autosave" desc="Save automatically every few minutes" value={settings.autosave} onChange={v => update('autosave', v)} />
                <Toggle label="Confirm Orders" desc="Show confirmation before placing trades" value={settings.confirmOrders} onChange={v => update('confirmOrders', v)} />
                <Toggle label="Skip Weekends" desc="Fast-forward through non-trading days" value={settings.skipWeekends} onChange={v => update('skipWeekends', v)} />
                <SectionHeader label="Auto-Pause" />
                <Toggle label="Breaking News" desc="Pause on major market events" value={settings.autoPauseOnNews} onChange={v => update('autoPauseOnNews', v)} />
                <Toggle label="Price Alerts" desc="Pause when an alert triggers" value={settings.autoPauseOnAlert} onChange={v => update('autoPauseOnAlert', v)} />
                <Toggle label="Market Open" desc="Pause at 9:30 AM each day" value={settings.autoPauseOnMarketOpen} onChange={v => update('autoPauseOnMarketOpen', v)} />
              </>
            )}

            {activeSection === 'graphics' && (
              <>
                <SectionHeader label="Window" />
                <SelectRow label="Window Mode" value={settings.windowMode} options={[
                  { value: 'windowed', label: 'Windowed' },
                  { value: 'fullscreen', label: 'Fullscreen' },
                  { value: 'borderless', label: 'Borderless Fullscreen' },
                ]} onChange={v => update('windowMode', v as GameSettings['windowMode'])} />
                <SelectRow label="Resolution" value={settings.resolution} options={[
                  { value: 'native', label: 'Native' },
                  { value: '3840x2160', label: '3840 x 2160 (4K)' },
                  { value: '3440x1440', label: '3440 x 1440 (UW QHD)' },
                  { value: '2560x1440', label: '2560 x 1440 (QHD)' },
                  { value: '2560x1080', label: '2560 x 1080 (UW FHD)' },
                  { value: '1920x1200', label: '1920 x 1200 (WUXGA)' },
                  { value: '1920x1080', label: '1920 x 1080 (FHD)' },
                  { value: '1680x1050', label: '1680 x 1050' },
                  { value: '1600x900', label: '1600 x 900' },
                  { value: '1440x900', label: '1440 x 900' },
                  { value: '1366x768', label: '1366 x 768' },
                  { value: '1280x720', label: '1280 x 720 (HD)' },
                ]} onChange={v => update('resolution', v)} />
                <SectionHeader label="Interface" />
                <SliderRow label="UI Scale" value={settings.uiScale} min={80} max={150} step={10}
                  display={`${settings.uiScale}%`} onChange={v => update('uiScale', v)} />
                <SliderRow label="Ticker Speed" value={settings.newsTickerSpeed} min={20} max={120} step={5}
                  display={`${settings.newsTickerSpeed} px/s`} onChange={v => update('newsTickerSpeed', v)} />
                <Toggle label="Show Sparklines" desc="Mini-charts in watchlist rows" value={settings.showSparklines} onChange={v => update('showSparklines', v)} />
                <Toggle label="Reduced Animations" desc="Disable glow and flash effects" value={settings.reducedAnimations} onChange={v => update('reducedAnimations', v)} />
              </>
            )}

            {activeSection === 'audio' && (
              <>
                <SectionHeader label="Volume" />
                <SliderRow label="Master Volume" value={settings.masterVolume} min={0} max={100} step={1}
                  display={`${settings.masterVolume}%`} onChange={v => update('masterVolume', v)} />
                <SliderRow label="Sound Effects" value={settings.sfxVolume} min={0} max={100} step={1}
                  display={`${settings.sfxVolume}%`} onChange={v => update('sfxVolume', v)} />
                <SliderRow label="Music" value={settings.musicVolume} min={0} max={100} step={1}
                  display={`${settings.musicVolume}%`} onChange={v => update('musicVolume', v)} />
                <SectionHeader label="Sounds" />
                <Toggle label="Market Bell" desc="Bell at market open and close" value={settings.marketBellSound} onChange={v => update('marketBellSound', v)} />
                <Toggle label="Trade Execution" desc="Sound when orders fill" value={settings.tradeSound} onChange={v => update('tradeSound', v)} />
                <Toggle label="News Alerts" desc="Sound on breaking news" value={settings.newsAlertSound} onChange={v => update('newsAlertSound', v)} />
              </>
            )}

            {activeSection === 'controls' && (
              <>
                <SectionHeader label="Time Control" />
                <KeyBind label="Pause / Resume" bindKey="pauseResume" bindings={settings.keyBindings}
                  onChange={b => update('keyBindings', { ...settings.keyBindings, ...b })} />
                <KeyBind label="Speed 1x" bindKey="speed1" bindings={settings.keyBindings}
                  onChange={b => update('keyBindings', { ...settings.keyBindings, ...b })} />
                <KeyBind label="Speed 2x" bindKey="speed2" bindings={settings.keyBindings}
                  onChange={b => update('keyBindings', { ...settings.keyBindings, ...b })} />
                <KeyBind label="Speed 5x" bindKey="speed3" bindings={settings.keyBindings}
                  onChange={b => update('keyBindings', { ...settings.keyBindings, ...b })} />
                <KeyBind label="Speed 10x" bindKey="speed4" bindings={settings.keyBindings}
                  onChange={b => update('keyBindings', { ...settings.keyBindings, ...b })} />
                <SectionHeader label="Navigation" />
                <KeyBind label="Dashboard" bindKey="tabDashboard" bindings={settings.keyBindings}
                  onChange={b => update('keyBindings', { ...settings.keyBindings, ...b })} />
                <KeyBind label="Portfolio" bindKey="tabPortfolio" bindings={settings.keyBindings}
                  onChange={b => update('keyBindings', { ...settings.keyBindings, ...b })} />
                <KeyBind label="Market" bindKey="tabMarket" bindings={settings.keyBindings}
                  onChange={b => update('keyBindings', { ...settings.keyBindings, ...b })} />
                <KeyBind label="Orders" bindKey="tabOrders" bindings={settings.keyBindings}
                  onChange={b => update('keyBindings', { ...settings.keyBindings, ...b })} />
                <KeyBind label="News" bindKey="tabNews" bindings={settings.keyBindings}
                  onChange={b => update('keyBindings', { ...settings.keyBindings, ...b })} />
                <KeyBind label="Analytics" bindKey="tabAnalytics" bindings={settings.keyBindings}
                  onChange={b => update('keyBindings', { ...settings.keyBindings, ...b })} />
                <SectionHeader label="System" />
                <KeyBind label="Quick Save" bindKey="quickSave" bindings={settings.keyBindings}
                  onChange={b => update('keyBindings', { ...settings.keyBindings, ...b })} />
                <KeyBind label="Help" bindKey="help" bindings={settings.keyBindings}
                  onChange={b => update('keyBindings', { ...settings.keyBindings, ...b })} />
                <div style={{ marginTop: '8px' }}>
                  <button onClick={() => update('keyBindings', { ...DEFAULT_KEYBINDINGS })}
                    style={{ background: 'transparent', border: '1px solid var(--border)', borderRadius: '4px', color: 'var(--text-disabled)', padding: '4px 12px', fontSize: '11px', cursor: 'pointer', fontFamily: 'var(--font-ui)' }}>
                    Reset Keybindings
                  </button>
                </div>
              </>
            )}
          </div>
        </div>

        <div style={styles.footer}>
          <button style={{ ...styles.footerBtn, color: 'var(--text-disabled)' }}
            onClick={() => onSettingsChange(DEFAULT_SETTINGS)}>
            Reset to Defaults
          </button>
          <button style={{ ...styles.footerBtn, color: 'var(--text-accent)' }} onClick={onClose}>
            Done
          </button>
        </div>
      </div>
    </div>
  );
}

function SectionHeader({ label }: { label: string }) {
  return (
    <div style={{ fontSize: '10px', fontWeight: 700, color: 'var(--text-accent)', letterSpacing: '1.5px', padding: '12px 0 6px', marginTop: '4px' }}>
      {label.toUpperCase()}
    </div>
  );
}

function Toggle({ label, desc, value, onChange }: { label: string; desc: string; value: boolean; onChange: (v: boolean) => void }) {
  return (
    <div style={styles.settingRow}>
      <div>
        <div style={styles.settingLabel}>{label}</div>
        <div style={styles.settingDesc}>{desc}</div>
      </div>
      <button onClick={() => onChange(!value)}
        style={{ ...styles.toggle, background: value ? 'var(--green-primary)' : 'var(--bg-tertiary)' }}>
        <div style={{ ...styles.toggleKnob, transform: value ? 'translateX(16px)' : 'translateX(0)' }} />
      </button>
    </div>
  );
}

function SliderRow({ label, value, min, max, step, display, onChange }: {
  label: string; value: number; min: number; max: number; step: number; display: string; onChange: (v: number) => void;
}) {
  return (
    <div style={styles.settingRow}>
      <span style={styles.settingLabel}>{label}</span>
      <div style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
        <input type="range" min={min} max={max} step={step} value={value}
          onChange={e => onChange(parseFloat(e.target.value))}
          style={{ width: '100px', accentColor: 'var(--text-accent)' }} />
        <span className="mono" style={{ fontSize: '12px', color: 'var(--text-primary)', width: '50px', textAlign: 'right' }}>{display}</span>
      </div>
    </div>
  );
}

function SelectRow({ label, value, options, onChange }: {
  label: string; value: string; options: { value: string; label: string }[]; onChange: (v: string) => void;
}) {
  return (
    <div style={styles.settingRow}>
      <span style={styles.settingLabel}>{label}</span>
      <select value={value} onChange={e => onChange(e.target.value)}
        style={{
          background: 'var(--bg-input)', color: 'var(--text-primary)',
          border: '1px solid var(--border)', borderRadius: '4px',
          padding: '4px 8px', fontSize: '12px', fontFamily: 'var(--font-ui)',
          cursor: 'pointer', width: '170px',
        }}>
        {options.map(o => <option key={o.value} value={o.value}>{o.label}</option>)}
      </select>
    </div>
  );
}

function KeyBind({ label, bindKey, bindings, onChange }: {
  label: string; bindKey: keyof KeyBindings; bindings: KeyBindings;
  onChange: (partial: Partial<KeyBindings>) => void;
}) {
  const [listening, setListening] = useState(false);

  const handleClick = () => {
    setListening(true);
    const handler = (e: KeyboardEvent) => {
      e.preventDefault();
      e.stopPropagation();
      let key = e.key === ' ' ? 'Space' : e.key.length === 1 ? e.key.toUpperCase() : e.key;
      if (e.ctrlKey && key !== 'Control') key = 'Ctrl+' + key;
      if (e.altKey && key !== 'Alt') key = 'Alt+' + key;
      onChange({ [bindKey]: key });
      setListening(false);
      window.removeEventListener('keydown', handler, true);
    };
    window.addEventListener('keydown', handler, true);
  };

  return (
    <div style={styles.settingRow}>
      <span style={styles.settingLabel}>{label}</span>
      <button onClick={handleClick} style={{
        ...styles.kbd,
        ...(listening ? { background: 'var(--text-accent)', color: '#FFF', borderColor: 'var(--text-accent)' } : {}),
        cursor: 'pointer', minWidth: '60px',
      }}>
        {listening ? 'Press a key...' : bindings[bindKey]}
      </button>
    </div>
  );
}

const styles: Record<string, React.CSSProperties> = {
  overlay: {
    position: 'fixed', inset: 0, background: 'rgba(0,0,0,0.6)',
    display: 'flex', alignItems: 'center', justifyContent: 'center', zIndex: 11000,
  },
  modal: {
    width: '640px', height: '480px',
    background: 'var(--bg-secondary)', border: '1px solid var(--border)',
    borderRadius: '8px', display: 'flex', flexDirection: 'column',
  },
  header: {
    display: 'flex', justifyContent: 'space-between', alignItems: 'center',
    padding: '14px 20px', borderBottom: '1px solid var(--border)',
  },
  title: { fontSize: '16px', fontWeight: 700, color: 'var(--text-primary)', fontFamily: 'var(--font-ui)' },
  closeBtn: { background: 'transparent', border: 'none', color: 'var(--text-secondary)', fontSize: '16px', cursor: 'pointer' },
  body: { display: 'flex', flex: 1, overflow: 'hidden' },
  sidebar: {
    width: '140px', borderRight: '1px solid var(--border)',
    padding: '6px 0', display: 'flex', flexDirection: 'column',
  },
  sectionBtn: {
    background: 'transparent', border: 'none', borderLeft: '2px solid transparent',
    padding: '8px 14px', textAlign: 'left', fontSize: '13px',
    fontFamily: 'var(--font-ui)', cursor: 'pointer', color: 'var(--text-secondary)',
    transition: 'all 100ms',
  },
  sectionBtnActive: {
    color: 'var(--text-accent)', borderLeftColor: 'var(--text-accent)',
    background: 'rgba(96, 165, 250, 0.06)',
  },
  content: { flex: 1, padding: '4px 20px 16px', overflowY: 'auto' },
  settingRow: {
    display: 'flex', justifyContent: 'space-between', alignItems: 'center',
    padding: '8px 0', borderBottom: '1px solid rgba(31,41,55,0.2)',
    minHeight: '36px',
  },
  settingLabel: { fontSize: '13px', color: 'var(--text-primary)' },
  settingDesc: { fontSize: '10px', color: 'var(--text-disabled)', marginTop: '1px' },
  toggle: {
    width: '36px', height: '20px', borderRadius: '10px', flexShrink: 0,
    border: 'none', cursor: 'pointer', position: 'relative', padding: 0,
    transition: 'background 150ms',
  },
  toggleKnob: {
    width: '16px', height: '16px', borderRadius: '50%',
    background: '#FFF', position: 'absolute', top: '2px', left: '2px',
    transition: 'transform 150ms',
  },
  kbd: {
    background: 'var(--bg-tertiary)', color: 'var(--text-primary)',
    padding: '2px 8px', borderRadius: '4px', fontSize: '11px',
    fontFamily: 'var(--font-mono)', border: '1px solid var(--border)',
  },
  footer: {
    display: 'flex', justifyContent: 'space-between',
    padding: '10px 20px', borderTop: '1px solid var(--border)',
  },
  footerBtn: {
    background: 'transparent', border: 'none',
    fontSize: '13px', cursor: 'pointer', fontFamily: 'var(--font-ui)',
  },
};
