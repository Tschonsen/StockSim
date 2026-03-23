import { useEffect, useState, useRef } from 'react';

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
  const [showChangelog, setShowChangelog] = useState(false);

  return (
    <div style={styles.container}>
      <TickerBackground />

      <div style={styles.content}>
        {/* Logo */}
        <div style={styles.logoSection}>
          <h1 style={styles.logo} className="pulse">STOCKSIM</h1>
          <p style={styles.tagline}>Trade. Speculate. Dominate.</p>
          <p style={styles.versionLine}>v0.1.0 — Early Access</p>
        </div>

        {/* Menu buttons */}
        <div style={styles.menu}>
          {hasSaves && <MenuButton label="Continue" onClick={onContinue} highlight />}
          <MenuButton label="New Game" onClick={onNewGame} />
          <MenuButton label="Load Game" onClick={onLoadGame} disabled={!hasSaves} />
          <MenuButton label="Settings" onClick={onSettings} />
          <MenuButton label="What's New" onClick={() => setShowChangelog(true)} subtle />
          {onQuit && <MenuButton label="Quit" onClick={onQuit} subtle />}
        </div>
      </div>

      {/* Version bottom-right */}
      <div style={styles.versionCorner}>
        <span className="mono" style={{ fontSize: '10px', color: 'var(--text-disabled)' }}>
          StockSim v0.1.0 | Phase 2 | 254 Tests
        </span>
      </div>

      {/* What's New / Changelog overlay */}
      {showChangelog && (
        <div style={styles.changelogOverlay} onClick={() => setShowChangelog(false)}>
          <div style={styles.changelogCard} onClick={e => e.stopPropagation()}>
            <h2 style={styles.changelogTitle}>What's New in v0.1.0</h2>

            <div style={styles.changelogScroll}>
              <ChangelogSection title="Trading" items={[
                'Market, Limit, Stop, Stop-Limit, Trailing Stop orders',
                'Short Selling and Cover orders',
                'Order confirmation dialog before every trade',
                'Slippage model for large orders',
              ]} />
              <ChangelogSection title="Market Simulation" items={[
                '250+ procedurally generated stocks across 12 sectors',
                '252 days of historical price data at game start',
                '50+ event templates (macro, sector, company)',
                'IPO and Delisting events',
                'Flash Crash with automatic recovery',
                'Circuit Breaker system (Level 1/2/3)',
                'Gap Up/Down at market open',
                'Economic cycle with sector rotation',
              ]} />
              <ChangelogSection title="AI Traders" items={[
                'Market Maker (spread/liquidity management)',
                'Retail Trader (FOMO/panic sentiment)',
                'Institutional (contrarian, value-based)',
                'Algorithmic (momentum micro-trading)',
              ]} />
              <ChangelogSection title="Charts & Analysis" items={[
                'TradingView candlestick charts with volume',
                'Technical indicators: SMA, EMA, RSI, MACD, Bollinger Bands',
                'Orderbook visualization (10 bid/ask levels)',
                'Stock Screener with 8 presets',
              ]} />
              <ChangelogSection title="Portfolio" items={[
                'Real-time P&L tracking (realized + unrealized)',
                'Portfolio allocation visualization',
                'Dividends with ex-date and 15% tax',
                'Analytics tab with performance metrics',
                'Day Summary popup at market close',
              ]} />
              <ChangelogSection title="UI & Experience" items={[
                'Bloomberg Terminal aesthetic with glow effects',
                'Live price flash animations',
                'Dashboard sector heatmap',
                'Keyboard shortcuts (press ? for help)',
                '7-step interactive tutorial',
                'Audio feedback for trades and events',
                'Save/Load with multiple slots + autosave',
              ]} />
            </div>

            <button style={styles.changelogClose} onClick={() => setShowChangelog(false)}>
              Close
            </button>
          </div>
        </div>
      )}
    </div>
  );
}

function ChangelogSection({ title, items }: { title: string; items: string[] }) {
  return (
    <div style={{ marginBottom: '16px' }}>
      <h3 style={{ fontSize: '13px', fontWeight: 700, color: 'var(--text-accent)', marginBottom: '6px', textTransform: 'uppercase', letterSpacing: '1px' }}>{title}</h3>
      {items.map((item, i) => (
        <div key={i} style={{ fontSize: '12px', color: 'var(--text-secondary)', padding: '2px 0 2px 12px', borderLeft: '2px solid var(--bg-tertiary)' }}>
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
    display: 'flex', alignItems: 'center', justifyContent: 'center',
    zIndex: 9000,
  },
  canvas: { position: 'absolute', inset: 0, width: '100%', height: '100%', pointerEvents: 'none' },
  content: { position: 'relative', zIndex: 1, display: 'flex', flexDirection: 'column', alignItems: 'center' },
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
  versionCorner: {
    position: 'absolute', bottom: '16px', right: '20px', zIndex: 2,
  },
  changelogOverlay: {
    position: 'fixed', inset: 0, background: 'rgba(0,0,0,0.7)',
    display: 'flex', alignItems: 'center', justifyContent: 'center', zIndex: 10000,
  },
  changelogCard: {
    width: '560px', maxHeight: '70vh', background: 'var(--bg-secondary)',
    border: '1px solid var(--border)', borderRadius: '12px', padding: '28px',
    display: 'flex', flexDirection: 'column',
  },
  changelogTitle: {
    fontSize: '20px', fontWeight: 700, color: 'var(--text-primary)',
    marginBottom: '20px', textAlign: 'center',
    fontFamily: 'var(--font-ui)',
  },
  changelogScroll: { flex: 1, overflowY: 'auto', marginBottom: '16px' },
  changelogClose: {
    width: '100%', height: '40px', borderRadius: '6px',
    background: 'var(--bg-tertiary)', color: 'var(--text-secondary)',
    border: '1px solid var(--border)', cursor: 'pointer',
    fontSize: '14px', fontFamily: 'var(--font-ui)',
  },
};
