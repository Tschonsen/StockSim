import { useState } from 'react';
import { useFocusTrap } from '@/hooks/useFocusTrap';

// --- Keybindings ---

export interface KeyBindings {
  pauseResume: string;
  speed1: string; speed2: string; speed3: string; speed4: string;
  tabDashboard: string; tabPortfolio: string; tabMarket: string;
  tabOrders: string; tabNews: string; tabAnalytics: string;
  quickSave: string; help: string;
}

export const DEFAULT_KEYBINDINGS: KeyBindings = {
  pauseResume: 'Space',
  speed1: '1', speed2: '2', speed3: '3', speed4: '4',
  tabDashboard: 'D', tabPortfolio: 'P', tabMarket: 'M',
  tabOrders: 'O', tabNews: 'N', tabAnalytics: 'A',
  quickSave: 'Ctrl+S', help: '?',
};

// --- Settings ---

export interface GameSettings {
  // Gameplay
  autosave: boolean;
  autosaveInterval: number;
  showAutosaveNotification: boolean;
  showTooltips: boolean;
  confirmOrders: boolean;
  autoPauseOnNews: boolean;
  autoPauseOnAlert: boolean;
  autoPauseOnMarketOpen: boolean;
  autoPauseOnMarginCall: boolean;
  autoPauseOnShortSqueeze: boolean;
  autoPauseOnOrderExecution: boolean;
  skipWeekends: boolean;
  language: string;
  // Simulation (Spec 16.3)
  tradingCommission: boolean;
  commissionAmount: number;
  marginInterest: boolean;
  shortBorrowFees: boolean;
  enableTaxes: boolean;
  taxRateMode: 'realistic' | 'flat' | 'off';
  smaEnforcement: boolean;
  smaStrictness: 'lenient' | 'normal' | 'strict';
  // Display (Spec 16.5)
  defaultChartTimeframe: string;
  defaultChartType: 'candle' | 'line' | 'area';
  numberFormat: 'us' | 'eu';
  showSparklines: boolean;
  // Video
  windowMode: 'windowed' | 'fullscreen' | 'borderless';
  resolution: string;
  vsync: boolean;
  fpsLimit: number;
  showFps: boolean;
  uiScale: number;
  // Audio
  masterVolume: number;
  sfxVolume: number;
  musicVolume: number;
  marketBellSound: boolean;
  tradeSound: boolean;
  newsAlertSound: boolean;
  // Accessibility
  reducedAnimations: boolean;
  highContrast: boolean;
  colorblindMode: 'off' | 'deuteranopia' | 'protanopia' | 'tritanopia';
  largeText: boolean;
  newsTickerSpeed: number;
  // Controls
  keyBindings: KeyBindings;
}

export const DEFAULT_SETTINGS: GameSettings = {
  autosave: true,
  autosaveInterval: 5,
  showAutosaveNotification: true,
  showTooltips: true,
  confirmOrders: true,
  autoPauseOnNews: false,
  autoPauseOnAlert: false,
  autoPauseOnMarketOpen: false,
  autoPauseOnMarginCall: true,
  autoPauseOnShortSqueeze: false,
  autoPauseOnOrderExecution: false,
  skipWeekends: false,
  language: 'en',
  tradingCommission: true,
  commissionAmount: 4.95,
  marginInterest: true,
  shortBorrowFees: true,
  enableTaxes: true,
  taxRateMode: 'realistic',
  smaEnforcement: true,
  smaStrictness: 'normal',
  defaultChartTimeframe: '1D',
  defaultChartType: 'candle',
  numberFormat: 'us',
  showSparklines: true,
  windowMode: 'windowed',
  resolution: 'native',
  vsync: true,
  fpsLimit: 60,
  showFps: false,
  uiScale: 100,
  masterVolume: 80,
  sfxVolume: 70,
  musicVolume: 50,
  marketBellSound: true,
  tradeSound: true,
  newsAlertSound: true,
  reducedAnimations: false,
  highContrast: false,
  colorblindMode: 'off',
  largeText: false,
  newsTickerSpeed: 60,
  keyBindings: { ...DEFAULT_KEYBINDINGS },
};

const APP_VERSION = '0.2.0';

// --- Component ---

interface SettingsModalProps {
  isOpen: boolean;
  onClose: () => void;
  settings: GameSettings;
  onSettingsChange: (settings: GameSettings) => void;
}

const TABS = [
  { id: 'gameplay', label: 'Gameplay' },
  { id: 'simulation', label: 'Simulation' },
  { id: 'video', label: 'Video' },
  { id: 'audio', label: 'Audio' },
  { id: 'controls', label: 'Controls' },
  { id: 'accessibility', label: 'Accessibility' },
] as const;

export function SettingsModal({ isOpen, onClose, settings, onSettingsChange }: SettingsModalProps) {
  const [tab, setTab] = useState('gameplay');
  const trapRef = useFocusTrap<HTMLDivElement>();
  if (!isOpen) return null;

  const set = <K extends keyof GameSettings>(key: K, value: GameSettings[K]) => {
    onSettingsChange({ ...settings, [key]: value });
  };

  return (
    <div style={S.overlay} onClick={onClose}>
      <div ref={trapRef} style={S.modal} onClick={e => e.stopPropagation()}>
        {/* Header */}
        <div style={S.header}>
          <span style={S.title}>Settings</span>
          <span role="button" tabIndex={0} style={S.closeBtn} onClick={onClose} onKeyDown={e => e.key === 'Enter' && onClose()} aria-label="Close settings">x</span>
        </div>

        <div style={S.body}>
          {/* Sidebar */}
          <div style={S.sidebar}>
            {TABS.map(t => (
              <button key={t.id} onClick={() => setTab(t.id)}
                style={{ ...S.tab, ...(tab === t.id ? S.tabActive : {}) }}>
                {t.label}
              </button>
            ))}
          </div>

          {/* Content */}
          <div style={S.content}>

            {tab === 'gameplay' && <>
              <H>General</H>
              <Toggle k="autosave" label="Autosave" v={settings.autosave} set={set} />
              {settings.autosave && (
                <Select k="autosaveInterval" label="Autosave Interval" v={String(settings.autosaveInterval)}
                  opts={[['1','1 min'],['5','5 min'],['10','10 min'],['15','15 min'],['30','30 min']]}
                  set={(_, val) => set('autosaveInterval', Number(val))} />
              )}
              {settings.autosave && (
                <Toggle k="showAutosaveNotification" label="Show Autosave Notification" v={settings.showAutosaveNotification} set={set} />
              )}
              <Toggle k="showTooltips" label="Show Tooltips" v={settings.showTooltips} set={set} />
              <Toggle k="confirmOrders" label="Confirm Orders" v={settings.confirmOrders} set={set} />
              <Toggle k="skipWeekends" label="Skip Weekends" v={settings.skipWeekends} set={set} />
              <Select k="language" label="Language" v={settings.language}
                opts={[['en','English'],['de','Deutsch']]} set={set} />
              <H>Auto-Pause</H>
              <Toggle k="autoPauseOnNews" label="On Breaking News" v={settings.autoPauseOnNews} set={set} />
              <Toggle k="autoPauseOnAlert" label="On Price Alert" v={settings.autoPauseOnAlert} set={set} />
              <Toggle k="autoPauseOnMarginCall" label="On Margin Call" v={settings.autoPauseOnMarginCall} set={set} />
              <Toggle k="autoPauseOnShortSqueeze" label="On Short Squeeze" v={settings.autoPauseOnShortSqueeze} set={set} />
              <Toggle k="autoPauseOnOrderExecution" label="On Order Execution" v={settings.autoPauseOnOrderExecution} set={set} />
              <Toggle k="autoPauseOnMarketOpen" label="On Market Open" v={settings.autoPauseOnMarketOpen} set={set} />
            </>}

            {tab === 'simulation' && <>
              <H>Trading Costs</H>
              <Toggle k="tradingCommission" label="Trading Commission" v={settings.tradingCommission} set={set} />
              {settings.tradingCommission && (
                <div style={S.row}>
                  <span style={S.label}>Commission Amount</span>
                  <div style={{ display: 'flex', alignItems: 'center', gap: '4px' }}>
                    <span style={{ fontSize: '12px', color: 'var(--text-secondary)' }}>$</span>
                    <input type="number" min={0} max={50} step={0.05}
                      value={settings.commissionAmount}
                      onChange={e => set('commissionAmount', parseFloat(e.target.value) || 0)}
                      style={{ ...S.select, width: '80px', textAlign: 'right' }} />
                  </div>
                </div>
              )}
              <Toggle k="marginInterest" label="Margin Interest" v={settings.marginInterest} set={set} />
              <Toggle k="shortBorrowFees" label="Short Borrow Fees" v={settings.shortBorrowFees} set={set} />
              <H>Taxes</H>
              <Toggle k="enableTaxes" label="Enable Taxes" v={settings.enableTaxes} set={set} />
              {settings.enableTaxes && (
                <Select k="taxRateMode" label="Tax Rate Mode" v={settings.taxRateMode}
                  opts={[['realistic','Realistic (15-35%)'],['flat','Flat (20%)'],['off','Off']]} set={set} />
              )}
              <H>Regulation</H>
              <Toggle k="smaEnforcement" label="SMA Enforcement" v={settings.smaEnforcement} set={set} />
              {settings.smaEnforcement && (
                <Select k="smaStrictness" label="SMA Strictness" v={settings.smaStrictness}
                  opts={[['lenient','Lenient'],['normal','Normal'],['strict','Strict']]} set={set} />
              )}
            </>}

            {tab === 'video' && <>
              <H>Interface</H>
              <Slider k="uiScale" label="UI Scale" v={settings.uiScale} min={80} max={150} step={10}
                fmt={v => `${v}%`} set={set} />
              <Select k="defaultChartTimeframe" label="Default Chart Timeframe" v={settings.defaultChartTimeframe}
                opts={[['1D','1 Day'],['1W','1 Week'],['1M','1 Month'],['ALL','All']]} set={set} />
              <Select k="defaultChartType" label="Default Chart Type" v={settings.defaultChartType}
                opts={[['candle','Candlestick'],['line','Line'],['area','Area']]} set={set} />
              <Select k="numberFormat" label="Number Format" v={settings.numberFormat}
                opts={[['us','1,234.56 (US)'],['eu','1.234,56 (EU)']]} set={set} />
              <Toggle k="showSparklines" label="Show Sparklines in Watchlist" v={settings.showSparklines} set={set} />
            </>}

            {tab === 'audio' && <>
              <H>Volume</H>
              <Slider k="masterVolume" label="Master" v={settings.masterVolume} min={0} max={100} step={5}
                fmt={v => `${v}%`} set={set} />
              <Slider k="musicVolume" label="Music" v={settings.musicVolume} min={0} max={100} step={5}
                fmt={v => `${v}%`} set={set} />
              <Slider k="sfxVolume" label="Sound Effects" v={settings.sfxVolume} min={0} max={100} step={5}
                fmt={v => `${v}%`} set={set} />
              <H>Notifications</H>
              <Toggle k="marketBellSound" label="Market Bell" v={settings.marketBellSound} set={set} />
              <Toggle k="tradeSound" label="Trade Execution" v={settings.tradeSound} set={set} />
              <Toggle k="newsAlertSound" label="News Alerts" v={settings.newsAlertSound} set={set} />
            </>}

            {tab === 'controls' && <>
              <H>Time</H>
              <KB label="Pause / Resume" bk="pauseResume" b={settings.keyBindings}
                onChange={b => set('keyBindings', { ...settings.keyBindings, ...b })} />
              <KB label="1x Speed" bk="speed1" b={settings.keyBindings}
                onChange={b => set('keyBindings', { ...settings.keyBindings, ...b })} />
              <KB label="2x Speed" bk="speed2" b={settings.keyBindings}
                onChange={b => set('keyBindings', { ...settings.keyBindings, ...b })} />
              <KB label="5x Speed" bk="speed3" b={settings.keyBindings}
                onChange={b => set('keyBindings', { ...settings.keyBindings, ...b })} />
              <KB label="10x Speed" bk="speed4" b={settings.keyBindings}
                onChange={b => set('keyBindings', { ...settings.keyBindings, ...b })} />
              <H>Navigation</H>
              <KB label="Dashboard" bk="tabDashboard" b={settings.keyBindings}
                onChange={b => set('keyBindings', { ...settings.keyBindings, ...b })} />
              <KB label="Portfolio" bk="tabPortfolio" b={settings.keyBindings}
                onChange={b => set('keyBindings', { ...settings.keyBindings, ...b })} />
              <KB label="Market" bk="tabMarket" b={settings.keyBindings}
                onChange={b => set('keyBindings', { ...settings.keyBindings, ...b })} />
              <KB label="Orders" bk="tabOrders" b={settings.keyBindings}
                onChange={b => set('keyBindings', { ...settings.keyBindings, ...b })} />
              <KB label="News" bk="tabNews" b={settings.keyBindings}
                onChange={b => set('keyBindings', { ...settings.keyBindings, ...b })} />
              <KB label="Analytics" bk="tabAnalytics" b={settings.keyBindings}
                onChange={b => set('keyBindings', { ...settings.keyBindings, ...b })} />
              <H>System</H>
              <KB label="Quick Save" bk="quickSave" b={settings.keyBindings}
                onChange={b => set('keyBindings', { ...settings.keyBindings, ...b })} />
              <KB label="Help" bk="help" b={settings.keyBindings}
                onChange={b => set('keyBindings', { ...settings.keyBindings, ...b })} />
              <div style={{ marginTop: '8px' }}>
                <button onClick={() => set('keyBindings', { ...DEFAULT_KEYBINDINGS })}
                  style={S.resetBtn}>Reset to Defaults</button>
              </div>
            </>}

            {tab === 'accessibility' && <>
              <H>Visual</H>
              <Toggle k="reducedAnimations" label="Reduced Animations" v={settings.reducedAnimations} set={set} />
              <Toggle k="highContrast" label="High Contrast" v={settings.highContrast} set={set} />
              <Toggle k="largeText" label="Large Text" v={settings.largeText} set={set} />
              <Select k="colorblindMode" label="Colorblind Mode" v={settings.colorblindMode}
                opts={[['off','Off'],['deuteranopia','Deuteranopia (Red-Green)'],['protanopia','Protanopia (Red-Green)'],['tritanopia','Tritanopia (Blue-Yellow)']]} set={set} />
              <H>Reading</H>
              <Slider k="newsTickerSpeed" label="Ticker Speed" v={settings.newsTickerSpeed} min={20} max={120} step={5}
                fmt={v => `${v}`} set={set} />
            </>}

          </div>
        </div>

        {/* Footer */}
        <div style={S.footer}>
          <button style={{ ...S.footerBtn, color: 'var(--text-disabled)' }}
            onClick={() => onSettingsChange(DEFAULT_SETTINGS)}>Reset All</button>
          <span style={{ fontSize: '11px', color: 'var(--text-disabled)', fontFamily: 'var(--font-mono)', alignSelf: 'center' }}>v{APP_VERSION}</span>
          <button style={{ ...S.footerBtn, color: 'var(--text-accent)' }} onClick={onClose}>Done</button>
        </div>
      </div>
    </div>
  );
}

// --- Sub-Components ---

function H({ children }: { children: string }) {
  return <div style={{ fontSize: '10px', fontWeight: 700, color: 'var(--text-accent)', letterSpacing: '1.5px', padding: '14px 0 4px' }}>{children.toUpperCase()}</div>;
}

function Toggle<K extends keyof GameSettings>({ label, k, v, set }: { label: string; k: K; v: boolean; set: (k: K, v: GameSettings[K]) => void }) {
  return (
    <div style={S.row}>
      <span style={S.label}>{label}</span>
      <button onClick={() => set(k, !v as GameSettings[K])}
        style={{ ...S.toggle, background: v ? 'var(--green-primary)' : 'var(--bg-tertiary)' }}>
        <div style={{ ...S.knob, transform: v ? 'translateX(16px)' : 'translateX(0)' }} />
      </button>
    </div>
  );
}

function Slider<K extends keyof GameSettings>({ label, k, v, min, max, step, fmt, set }: {
  label: string; k: K; v: number; min: number; max: number; step: number; fmt: (v: number) => string;
  set: (k: K, v: GameSettings[K]) => void;
}) {
  return (
    <div style={S.row}>
      <span style={S.label}>{label}</span>
      <div style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
        <input type="range" min={min} max={max} step={step} value={v}
          onChange={e => set(k, parseFloat(e.target.value) as GameSettings[K])}
          style={{ width: '100px', accentColor: 'var(--text-accent)' }} />
        <span className="mono" style={{ fontSize: '12px', color: 'var(--text-primary)', width: '44px', textAlign: 'right' }}>{fmt(v)}</span>
      </div>
    </div>
  );
}

function Select<K extends keyof GameSettings>({ label, k, v, opts, set }: {
  label: string; k: K; v: string; opts: string[][]; set: (k: K, v: GameSettings[K]) => void;
}) {
  return (
    <div style={S.row}>
      <span style={S.label}>{label}</span>
      <select value={v} onChange={e => set(k, e.target.value as GameSettings[K])} style={S.select}>
        {opts.map(([val, lbl]) => <option key={val} value={val}>{lbl}</option>)}
      </select>
    </div>
  );
}

function KB({ label, bk, b, onChange }: { label: string; bk: keyof KeyBindings; b: KeyBindings; onChange: (p: Partial<KeyBindings>) => void }) {
  const [listening, setListening] = useState(false);
  const listen = () => {
    setListening(true);
    const h = (e: KeyboardEvent) => {
      e.preventDefault(); e.stopPropagation();
      let key = e.key === ' ' ? 'Space' : e.key.length === 1 ? e.key.toUpperCase() : e.key;
      if (e.ctrlKey && key !== 'Control') key = 'Ctrl+' + key;
      if (e.altKey && key !== 'Alt') key = 'Alt+' + key;
      onChange({ [bk]: key });
      setListening(false);
      window.removeEventListener('keydown', h, true);
    };
    window.addEventListener('keydown', h, true);
  };
  return (
    <div style={S.row}>
      <span style={S.label}>{label}</span>
      <button onClick={listen} style={{
        ...S.kbd, cursor: 'pointer',
        ...(listening ? { background: 'var(--text-accent)', color: 'var(--text-primary)', borderColor: 'var(--text-accent)' } : {}),
      }}>{listening ? '...' : b[bk]}</button>
    </div>
  );
}

// --- Styles ---

const S: Record<string, React.CSSProperties> = {
  overlay: { position: 'fixed', inset: 0, background: 'rgba(0,0,0,0.6)', display: 'flex', alignItems: 'center', justifyContent: 'center', zIndex: 11000 },
  modal: { width: '620px', height: '460px', background: 'var(--bg-secondary)', border: '1px solid var(--border)', borderRadius: '8px', display: 'flex', flexDirection: 'column' },
  header: { display: 'flex', justifyContent: 'space-between', alignItems: 'center', padding: '12px 18px', borderBottom: '1px solid var(--border)' },
  title: { fontSize: '15px', fontWeight: 700, color: 'var(--text-primary)' },
  closeBtn: { color: 'var(--text-secondary)', fontSize: '16px', cursor: 'pointer', padding: '2px 6px' },
  body: { display: 'flex', flex: 1, overflow: 'hidden' },
  sidebar: { width: '130px', borderRight: '1px solid var(--border)', padding: '4px 0', display: 'flex', flexDirection: 'column' },
  tab: { background: 'transparent', border: 'none', borderLeft: '2px solid transparent', padding: '7px 12px', textAlign: 'left', fontSize: '13px', fontFamily: 'var(--font-ui)', cursor: 'pointer', color: 'var(--text-secondary)', transition: 'all 100ms' },
  tabActive: { color: 'var(--text-accent)', borderLeftColor: 'var(--text-accent)', background: 'rgba(96,165,250,0.06)' },
  content: { flex: 1, padding: '2px 18px 12px', overflowY: 'auto' },
  row: { display: 'flex', justifyContent: 'space-between', alignItems: 'center', padding: '6px 0', borderBottom: '1px solid rgba(31,41,55,0.15)', minHeight: '32px' },
  label: { fontSize: '13px', color: 'var(--text-primary)' },
  toggle: { width: '36px', height: '20px', borderRadius: '10px', border: 'none', cursor: 'pointer', position: 'relative', padding: 0, transition: 'background 150ms', flexShrink: 0 },
  knob: { width: '16px', height: '16px', borderRadius: '50%', background: 'var(--text-primary)', position: 'absolute', top: '2px', left: '2px', transition: 'transform 150ms' },
  select: { background: 'var(--bg-input)', color: 'var(--text-primary)', border: '1px solid var(--border)', borderRadius: '4px', padding: '3px 6px', fontSize: '12px', fontFamily: 'var(--font-ui)', cursor: 'pointer', width: '170px' },
  kbd: { background: 'var(--bg-tertiary)', color: 'var(--text-primary)', padding: '2px 10px', borderRadius: '4px', fontSize: '11px', fontFamily: 'var(--font-mono)', border: '1px solid var(--border)', minWidth: '50px', textAlign: 'center' },
  resetBtn: { background: 'transparent', border: '1px solid var(--border)', borderRadius: '4px', color: 'var(--text-disabled)', padding: '3px 10px', fontSize: '11px', cursor: 'pointer', fontFamily: 'var(--font-ui)' },
  footer: { display: 'flex', justifyContent: 'space-between', padding: '10px 18px', borderTop: '1px solid var(--border)' },
  footerBtn: { background: 'transparent', border: 'none', fontSize: '13px', cursor: 'pointer', fontFamily: 'var(--font-ui)' },
};
