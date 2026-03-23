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
          <p style={styles.versionLine}>v0.1.0 — Early Access</p>
        </div>

        <div style={styles.menu}>
          {hasSaves && <MenuButton label="Continue" onClick={onContinue} highlight />}
          <MenuButton label="New Game" onClick={onNewGame} />
          <MenuButton label="Load Game" onClick={onLoadGame} disabled={!hasSaves} />
          <MenuButton label="Settings" onClick={onSettings} />
          {onQuit && <MenuButton label="Quit" onClick={onQuit} subtle />}
        </div>

        <div style={styles.versionBottom}>
          <span className="mono">StockSim v0.1.0 — Closed Alpha</span>
        </div>
      </div>

      {/* Right side: What's New panel */}
      <div style={styles.rightPanel}>
        <div style={styles.panelHeader}>
          <div style={styles.panelBadge}>NEW</div>
          <h2 style={styles.panelTitle}>Patch Notes — v0.1.0</h2>
        </div>

        <div style={styles.panelScroll}>
          <ChangelogSection icon="📊" title="Trading" items={[
            'Market, Limit, Stop, Stop-Limit & Trailing Stop orders',
            'Short Selling and Cover positions',
            'Order confirmation with cost preview',
            'Realistic slippage on large orders',
          ]} />
          <ChangelogSection icon="🌍" title="Living Market" items={[
            '250+ stocks across 12 sectors, procedurally generated',
            '1 year of historical price data from day one',
            '50+ dynamic event templates drive price action',
            'IPOs bring new companies, delistings remove failing ones',
            'Flash Crashes and Circuit Breakers for dramatic moments',
            'Economic cycles rotate sector strength over time',
          ]} />
          <ChangelogSection icon="🤖" title="AI Traders" items={[
            'Market Makers maintain liquidity and spreads',
            'Retail traders chase momentum and panic sell',
            'Institutions buy dips on blue chips',
            'Algorithms amplify short-term trends',
          ]} />
          <ChangelogSection icon="📈" title="Analysis Tools" items={[
            'Candlestick charts with TradingView engine',
            'SMA, EMA, RSI, MACD, Bollinger Bands overlays',
            'Real-time orderbook with 10-level depth',
            'Stock Screener: find stocks by criteria',
          ]} />
          <ChangelogSection icon="💼" title="Your Portfolio" items={[
            'Live P&L tracking with allocation visualization',
            'Quarterly dividends with ex-date mechanics',
            'Price alerts that auto-pause the game',
            'Daily market summary at close',
          ]} />
          <ChangelogSection icon="🎮" title="Experience" items={[
            'Bloomberg Terminal dark aesthetic with neon glows',
            'Prices flash green/red on every tick',
            'Breaking news pulses in the ticker',
            'Synthesized audio for trades and events',
            'Full keyboard shortcut support',
          ]} />
        </div>

        <div style={styles.panelFooter}>
          <span style={{ fontSize: '11px', color: 'var(--text-disabled)' }}>
            Coming soon: Margin Trading, Analyst Ratings, Achievements, Scenarios
          </span>
        </div>
      </div>
    </div>
  );
}

function ChangelogSection({ icon, title, items }: { icon: string; title: string; items: string[] }) {
  return (
    <div style={{ marginBottom: '20px' }}>
      <h3 style={{ fontSize: '12px', fontWeight: 700, color: 'var(--text-accent)', marginBottom: '8px', letterSpacing: '1px', display: 'flex', alignItems: 'center', gap: '6px' }}>
        <span style={{ fontSize: '14px' }}>{icon}</span>
        {title.toUpperCase()}
      </h3>
      {items.map((item, i) => (
        <div key={i} style={{
          fontSize: '11px', color: 'var(--text-secondary)', padding: '3px 0 3px 14px',
          borderLeft: '1px solid rgba(96, 165, 250, 0.15)',
          lineHeight: '1.5',
        }}>
          {item}
        </div>
      ))}
    </div>
  );
}

function MenuButton({ label, onClick, highlight, disabled, subtle }: {
  label: string; onClick: () => void; highlight?: boolean; disabled?: boolean; subtle?: boolean;
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
      }}
    >
      {label}
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
  versionLine: {
    fontFamily: 'var(--font-mono)', fontSize: '11px', color: 'var(--text-disabled)',
    marginTop: '6px', letterSpacing: '1px',
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
    fontSize: '10px', color: 'var(--text-disabled)',
    fontFamily: 'var(--font-mono)', letterSpacing: '0.5px',
  },
  // Right panel — What's New
  rightPanel: {
    width: '340px', position: 'relative', zIndex: 1,
    background: 'rgba(17, 24, 39, 0.85)',
    borderLeft: '1px solid var(--border)',
    display: 'flex', flexDirection: 'column',
    backdropFilter: 'blur(8px)',
  },
  panelHeader: {
    padding: '24px 20px 16px',
    borderBottom: '1px solid var(--border)',
    display: 'flex', alignItems: 'center', gap: '10px',
  },
  panelBadge: {
    fontSize: '9px', fontWeight: 800, color: '#FFF',
    background: 'var(--green-primary)', padding: '2px 8px',
    borderRadius: '4px', letterSpacing: '1px',
  },
  panelTitle: {
    fontSize: '14px', fontWeight: 700, color: 'var(--text-primary)',
    fontFamily: 'var(--font-ui)', margin: 0,
  },
  panelScroll: {
    flex: 1, overflowY: 'auto', padding: '16px 20px',
  },
  panelFooter: {
    padding: '12px 20px', borderTop: '1px solid var(--border)',
    textAlign: 'center',
  },
};
