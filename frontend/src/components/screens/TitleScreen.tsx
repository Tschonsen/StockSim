import { useEffect, useState, useRef } from 'react';

interface TitleScreenProps {
  onNewGame: () => void;
  onContinue: () => void;
  onLoadGame: () => void;
  onSettings: () => void;
  hasSaves: boolean;
}

/**
 * Full-screen title screen. Bible 3.0.3.
 * Animated background with scrolling fake stock tickers.
 */
export function TitleScreen({ onNewGame, onContinue, onLoadGame, onSettings, hasSaves }: TitleScreenProps) {
  return (
    <div style={styles.container}>
      {/* Animated background — scrolling fake tickers */}
      <TickerBackground />

      {/* Content overlay */}
      <div style={styles.content}>
        {/* Logo */}
        <div style={styles.logoSection}>
          <h1 style={styles.logo} className="pulse">STOCKSIM</h1>
          <p style={styles.tagline}>Trade. Speculate. Dominate.</p>
        </div>

        {/* Menu buttons */}
        <div style={styles.menu}>
          {hasSaves && (
            <MenuButton label="Continue" onClick={onContinue} highlight />
          )}
          <MenuButton label="New Game" onClick={onNewGame} />
          <MenuButton label="Load Game" onClick={onLoadGame} disabled={!hasSaves} />
          <MenuButton label="Settings" onClick={onSettings} />
        </div>

        {/* Version */}
        <div style={styles.version}>
          <span className="mono">v0.1.0</span>
        </div>
      </div>
    </div>
  );
}

function MenuButton({ label, onClick, highlight, disabled }: {
  label: string; onClick: () => void; highlight?: boolean; disabled?: boolean;
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
        ...(highlight ? styles.buttonHighlight : {}),
        ...(hovered && !disabled ? styles.buttonHover : {}),
        ...(disabled ? styles.buttonDisabled : {}),
      }}
    >
      {label}
    </button>
  );
}

/** Fake scrolling stock ticker background for immersion */
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

    interface Ticker {
      x: number; y: number; symbol: string; price: number;
      speed: number; flash: number; flashDir: 'up' | 'down';
    }

    const tickers: Ticker[] = Array.from({ length: 40 }, () => ({
      x: Math.random() * canvas.width,
      y: Math.random() * canvas.height,
      symbol: symbols[Math.floor(Math.random() * symbols.length)],
      price: 10 + Math.random() * 400,
      speed: 0.2 + Math.random() * 0.5,
      flash: 0,
      flashDir: Math.random() > 0.5 ? 'up' : 'down',
    }));

    let animId: number;
    const draw = () => {
      ctx.clearRect(0, 0, canvas.width, canvas.height);
      ctx.font = '12px "JetBrains Mono", monospace';

      for (const t of tickers) {
        // Move
        t.x -= t.speed;
        t.y -= t.speed * 0.3;
        if (t.x < -150) { t.x = canvas.width + 50; t.y = Math.random() * canvas.height; }
        if (t.y < -20) t.y = canvas.height + 20;

        // Random price tick
        if (Math.random() < 0.005) {
          t.flash = 1;
          t.flashDir = Math.random() > 0.5 ? 'up' : 'down';
          t.price += (t.flashDir === 'up' ? 1 : -1) * (Math.random() * 2);
          t.price = Math.max(1, t.price);
        }

        // Color
        let alpha = 0.08;
        let color = '#4B5563';
        if (t.flash > 0) {
          alpha = 0.08 + t.flash * 0.15;
          color = t.flashDir === 'up' ? '#10B981' : '#EF4444';
          t.flash -= 0.02;
        }

        ctx.fillStyle = color;
        ctx.globalAlpha = alpha;
        ctx.fillText(`${t.symbol} $${t.price.toFixed(2)}`, t.x, t.y);
      }
      ctx.globalAlpha = 1;
      animId = requestAnimationFrame(draw);
    };
    draw();

    const handleResize = () => {
      canvas.width = window.innerWidth;
      canvas.height = window.innerHeight;
    };
    window.addEventListener('resize', handleResize);

    return () => {
      cancelAnimationFrame(animId);
      window.removeEventListener('resize', handleResize);
    };
  }, []);

  return <canvas ref={canvasRef} style={styles.canvas} />;
}

const styles: Record<string, React.CSSProperties> = {
  container: {
    position: 'fixed', inset: 0,
    background: 'var(--bg-primary)',
    display: 'flex', alignItems: 'center', justifyContent: 'center',
    zIndex: 10000,
  },
  canvas: {
    position: 'absolute', inset: 0,
    width: '100%', height: '100%',
    pointerEvents: 'none',
  },
  content: {
    position: 'relative', zIndex: 1,
    display: 'flex', flexDirection: 'column', alignItems: 'center',
  },
  logoSection: {
    textAlign: 'center', marginBottom: '48px',
  },
  logo: {
    fontFamily: 'var(--font-mono)',
    fontSize: '56px',
    fontWeight: 700,
    color: 'var(--text-primary)',
    letterSpacing: '8px',
    textShadow: '0 0 30px rgba(96, 165, 250, 0.2), 0 0 60px rgba(96, 165, 250, 0.1)',
    margin: 0,
  },
  tagline: {
    fontFamily: 'var(--font-ui)',
    fontSize: '16px',
    color: 'var(--text-secondary)',
    marginTop: '8px',
    letterSpacing: '3px',
  },
  menu: {
    display: 'flex', flexDirection: 'column', gap: '8px', alignItems: 'center',
  },
  button: {
    width: '300px', height: '48px',
    background: 'var(--bg-tertiary)',
    border: '1px solid var(--border)',
    borderRadius: '8px',
    color: 'var(--text-primary)',
    fontFamily: 'var(--font-ui)',
    fontSize: '16px',
    fontWeight: 600,
    cursor: 'pointer',
    transition: 'all 150ms ease',
    letterSpacing: '1px',
  },
  buttonHighlight: {
    borderLeft: '3px solid var(--text-accent)',
    background: 'rgba(96, 165, 250, 0.08)',
  },
  buttonHover: {
    background: 'rgba(96, 165, 250, 0.12)',
    transform: 'scale(1.02)',
    boxShadow: '0 0 20px rgba(96, 165, 250, 0.1)',
  },
  buttonDisabled: {
    opacity: 0.35,
    cursor: 'not-allowed',
  },
  version: {
    position: 'absolute',
    bottom: '-120px',
    right: '-200px',
    fontSize: '11px',
    color: 'var(--text-disabled)',
  },
};
