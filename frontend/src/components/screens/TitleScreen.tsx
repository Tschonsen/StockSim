import { useEffect, useRef, useState } from 'react';

interface TitleScreenProps {
  onNewGame: () => void;
  onContinue: () => void;
  onLoadGame: () => void;
  onSettings: () => void;
  onQuit?: () => void;
  hasSaves: boolean;
}

/**
 * Full-screen title screen. Bible 3.0.3.
 * Animated background with scrolling fake stock tickers.
 */
export function TitleScreen({ onNewGame, onContinue, onLoadGame, onSettings, onQuit, hasSaves }: TitleScreenProps) {
  return (
    <div style={styles.container}>
      <TickerBackground />

      {/* Left side: Logo + Menu */}
      <div style={styles.leftSide}>
        <div style={styles.logoSection}>
          <h1 style={styles.logo} className="pulse">STOCKSIM</h1>
          <p style={styles.tagline}>Trade. Speculate. Dominate.</p>
        </div>

        <div style={styles.menu}>
          <MenuButton label="Online" onClick={() => {}} disabled>
            <span style={styles.comingSoon}>Coming Soon</span>
          </MenuButton>
          {hasSaves && <MenuButton label="Continue" onClick={onContinue} highlight />}
          <MenuButton label="New Game" onClick={onNewGame} />
          <MenuButton label="Load Game" onClick={onLoadGame} disabled={!hasSaves} />
          <MenuButton label="Settings" onClick={onSettings} />
          {onQuit && <MenuButton label="Quit" onClick={onQuit} subtle />}
        </div>

        <div style={styles.versionBottom}>
          <span className="mono">v0.1.0 — <span style={styles.versionName}>Diamond Hands</span></span>
        </div>
      </div>

      {/* Right side: Patch Notes */}
      <div style={styles.rightPadding}>
        <div style={styles.rightPanel}>
          <div style={styles.panelHeader}>
            <span style={styles.panelLabel}>PATCH NOTES</span>
            <span style={styles.panelVersion}>Diamond Hands</span>
          </div>

          <div style={styles.panelScroll}>
            <PatchSection title="Trading" items={[
              'Market, Limit, Stop, Stop-Limit, Trailing Stop',
              'Short Selling / Cover',
              'Order confirmation dialog',
              'Slippage model',
            ]} />
            <PatchSection title="Simulation" items={[
              '250+ stocks, 12 sectors',
              '1 year historical data',
              '50+ event templates',
              'IPO / Delisting',
              'Flash Crash / Circuit Breaker',
              'Economic cycle with sector rotation',
              'Gap Up/Down at open',
            ]} />
            <PatchSection title="AI" items={[
              'Market Maker, Retail, Institutional, Algorithmic',
            ]} />
            <PatchSection title="Analysis" items={[
              'Candlestick charts',
              'SMA, EMA, RSI, MACD, Bollinger',
              'Orderbook depth (10 levels)',
              'Stock Screener (8 presets)',
            ]} />
            <PatchSection title="Portfolio" items={[
              'P&L tracking, allocation bar',
              'Dividends with ex-date',
              'Price alerts, day summary',
            ]} />
          </div>

          <div style={styles.panelFooter}>
            <span>Next: Margin Trading, Analyst Ratings, Achievements</span>
          </div>
        </div>
      </div>
    </div>
  );
}

function PatchSection({ title, items }: { title: string; items: string[] }) {
  return (
    <div style={{ marginBottom: '14px' }}>
      <div style={{ fontSize: '10px', fontWeight: 700, color: 'var(--text-accent)', letterSpacing: '1.5px', marginBottom: '4px' }}>
        {title.toUpperCase()}
      </div>
      {items.map((item, i) => (
        <div key={i} style={{ fontSize: '11px', color: 'var(--text-disabled)', padding: '1px 0 1px 8px', lineHeight: '1.6' }}>
          <span style={{ color: 'var(--text-secondary)', marginRight: '4px' }}>·</span>{item}
        </div>
      ))}
    </div>
  );
}

function MenuButton({ label, onClick, highlight, disabled, subtle, children }: {
  label: string; onClick: () => void; highlight?: boolean; disabled?: boolean; subtle?: boolean; children?: React.ReactNode;
}) {
  const [hovered, setHovered] = useState(false);

  return (
    <button
      onClick={onClick}
      disabled={disabled}
      onMouseEnter={() => setHovered(true)}
      onMouseLeave={() => setHovered(false)}
      style={{
        ...styles.button,
        ...(subtle ? styles.buttonSubtle : {}),
        ...(highlight ? styles.buttonHighlight : {}),
        ...(hovered && !disabled ? styles.buttonHover : {}),
        ...(disabled ? styles.buttonDisabled : {}),
        ...(children ? { display: 'flex', flexDirection: 'column', alignItems: 'center', justifyContent: 'center', height: '56px' } : {}),
      }}
    >
      {label}
      {children}
    </button>
  );
}

function TickerBackground() {
  const canvasRef = useRef<HTMLCanvasElement>(null);

  useEffect(() => {
    const canvas = canvasRef.current;
    if (!canvas) return;
    const ctx = canvas.getContext('2d');
    if (!ctx) return;

    canvas.width = window.innerWidth;
    canvas.height = window.innerHeight;

    const symbols = [
      'AAPL', 'GOOG', 'MSFT', 'AMZN', 'TSLA', 'META', 'NVDA', 'JPM',
      'BAC', 'XOM', 'CVX', 'PFE', 'JNJ', 'WMT', 'DIS', 'NFLX',
      'AMD', 'INTC', 'CSCO', 'ORCL', 'CRM', 'ADBE', 'PYPL', 'SQ',
    ];

    interface Ticker { x: number; y: number; symbol: string; price: number; speed: number; flash: number; flashDir: 'up' | 'down'; }
    const tickers: Ticker[] = Array.from({ length: 50 }, () => ({
      x: Math.random() * canvas.width, y: Math.random() * canvas.height,
      symbol: symbols[Math.floor(Math.random() * symbols.length)],
      price: 10 + Math.random() * 400, speed: 0.15 + Math.random() * 0.4,
      flash: 0, flashDir: Math.random() > 0.5 ? 'up' : 'down',
    }));

    let animId: number;
    const draw = () => {
      ctx.clearRect(0, 0, canvas.width, canvas.height);
      ctx.font = '11px "JetBrains Mono", monospace';

      for (const t of tickers) {
        t.x -= t.speed;
        t.y -= t.speed * 0.2;
        if (t.x < -160) { t.x = canvas.width + 60; t.y = Math.random() * canvas.height; }
        if (t.y < -20) t.y = canvas.height + 20;

        if (Math.random() < 0.003) {
          t.flash = 1;
          t.flashDir = Math.random() > 0.5 ? 'up' : 'down';
          t.price += (t.flashDir === 'up' ? 1 : -1) * Math.random() * 2;
          t.price = Math.max(1, t.price);
        }

        let alpha = 0.06;
        let color = '#4B5563';
        if (t.flash > 0) {
          alpha = 0.06 + t.flash * 0.2;
          color = t.flashDir === 'up' ? '#10B981' : '#EF4444';
          t.flash -= 0.015;
        }

        ctx.fillStyle = color;
        ctx.globalAlpha = alpha;
        ctx.fillText(`${t.symbol} $${t.price.toFixed(2)}`, t.x, t.y);
      }
      ctx.globalAlpha = 1;
      animId = requestAnimationFrame(draw);
    };
    draw();

    const handleResize = () => { canvas.width = window.innerWidth; canvas.height = window.innerHeight; };
    window.addEventListener('resize', handleResize);
    return () => { cancelAnimationFrame(animId); window.removeEventListener('resize', handleResize); };
  }, []);

  return <canvas ref={canvasRef} style={styles.canvas} />;
}

const styles: Record<string, React.CSSProperties> = {
  container: {
    position: 'fixed', inset: 0, background: 'var(--bg-primary)',
    display: 'flex', alignItems: 'stretch',
    zIndex: 9000,
  },
  canvas: { position: 'absolute', inset: 0, width: '100%', height: '100%', pointerEvents: 'none' },
  leftSide: {
    flex: 1, position: 'relative', zIndex: 1,
    display: 'flex', flexDirection: 'column', alignItems: 'center', justifyContent: 'center',
  },
  logoSection: { textAlign: 'center', marginBottom: '48px' },
  logo: {
    fontFamily: 'var(--font-mono)', fontSize: '56px', fontWeight: 700,
    color: 'var(--text-primary)', letterSpacing: '8px', margin: 0,
    textShadow: '0 0 30px rgba(96, 165, 250, 0.2), 0 0 60px rgba(96, 165, 250, 0.1)',
  },
  tagline: {
    fontFamily: 'var(--font-ui)', fontSize: '16px', color: 'var(--text-secondary)',
    marginTop: '8px', letterSpacing: '3px',
  },
  menu: { display: 'flex', flexDirection: 'column', gap: '8px', alignItems: 'center' },
  button: {
    width: '300px', height: '48px', background: 'var(--bg-tertiary)',
    border: '1px solid var(--border)', borderRadius: '8px',
    color: 'var(--text-primary)', fontFamily: 'var(--font-ui)',
    fontSize: '16px', fontWeight: 600, cursor: 'pointer',
    transition: 'all 150ms ease', letterSpacing: '1px',
  },
  buttonSubtle: {
    background: 'transparent', border: '1px solid rgba(31, 41, 55, 0.5)',
    color: 'var(--text-secondary)', fontSize: '14px', height: '40px',
  },
  buttonHighlight: {
    borderLeft: '3px solid var(--text-accent)', background: 'rgba(96, 165, 250, 0.08)',
  },
  buttonHover: {
    background: 'rgba(96, 165, 250, 0.12)', transform: 'scale(1.02)',
    boxShadow: '0 0 20px rgba(96, 165, 250, 0.1)',
  },
  buttonDisabled: { opacity: 0.35, cursor: 'not-allowed' },
  versionBottom: {
    position: 'absolute', bottom: '24px',
    display: 'flex', flexDirection: 'column', alignItems: 'center', gap: '2px',
    fontSize: '10px', color: 'var(--text-disabled)',
    fontFamily: 'var(--font-mono)', letterSpacing: '0.5px',
  },
  versionName: {
    color: 'var(--text-secondary)',
    fontStyle: 'italic',
  },
  comingSoon: {
    fontSize: '10px', color: 'var(--text-disabled)',
    fontWeight: 400, letterSpacing: '0.5px', marginTop: '2px',
  },
  // Right panel — Patch Notes
  rightPadding: {
    position: 'relative', zIndex: 1,
    display: 'flex', alignItems: 'center',
    paddingRight: '24px',
  },
  rightPanel: {
    width: '260px',
    maxHeight: '480px',
    background: 'rgba(17, 24, 39, 0.9)',
    border: '1px solid rgba(31, 41, 55, 0.6)',
    borderRadius: '8px',
    display: 'flex', flexDirection: 'column',
    backdropFilter: 'blur(12px)',
  },
  panelHeader: {
    padding: '14px 16px 10px',
    borderBottom: '1px solid rgba(31, 41, 55, 0.6)',
    display: 'flex', justifyContent: 'space-between', alignItems: 'baseline',
  },
  panelLabel: {
    fontSize: '9px', fontWeight: 700, color: 'var(--text-disabled)',
    letterSpacing: '2px',
  },
  panelVersion: {
    fontSize: '11px', fontWeight: 600, color: 'var(--text-accent)',
    fontFamily: 'var(--font-mono)',
  },
  panelScroll: {
    flex: 1, overflowY: 'auto', padding: '12px 16px',
  },
  panelFooter: {
    padding: '10px 16px', borderTop: '1px solid rgba(31, 41, 55, 0.6)',
    fontSize: '10px', color: 'var(--text-disabled)',
  },
};
