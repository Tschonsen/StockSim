import { useState, useCallback, useEffect } from 'react';

/**
 * Bible 14.2: Interactive tutorial with spotlight effect.
 * 8 steps guiding the player through the UI with dimmed overlay + highlighted region.
 * Tutorial box positioned next to the spotlighted area.
 */

interface SpotlightRegion {
  top: string;
  left: string;
  width: string;
  height: string;
}

interface TutorialStep {
  title: string;
  description: string;
  tips?: string[];
  spotlight?: SpotlightRegion;
  cardPosition: 'center' | 'right' | 'left' | 'bottom';
  /** If set, Next button is disabled until this event fires on window */
  requiredAction?: string;
  actionHint?: string;
}

const STEPS: TutorialStep[] = [
  {
    title: 'Welcome to StockSim!',
    description: 'This is your trading desk. Let\'s get familiar with the layout.',
    tips: [
      '1. Watchlist (left) — stocks you\'re tracking',
      '2. Central Area — charts, portfolio, market data',
      '3. Trading Panel (right) — buy and sell stocks',
      '4. News Ticker (bottom) — live market events',
      '5. Time Controls (top) — control simulation speed',
    ],
    cardPosition: 'center',
  },
  {
    title: 'Your Watchlist',
    description: 'Your Watchlist shows stocks you\'re tracking. Click on any stock to see its chart and details. You can add stocks from the Market tab.',
    spotlight: { top: 'var(--topbar-height)', left: '0', width: 'var(--sidebar-left-width)', height: 'calc(100vh - var(--topbar-height) - var(--ticker-height))' },
    cardPosition: 'right',
    requiredAction: 'stockSelected',
    actionHint: 'Click a stock in the watchlist to continue',
  },
  {
    title: 'Reading Charts',
    description: 'The candlestick chart shows price history. Green candles mean the price went up, red means it went down. The thin lines (wicks) show the high and low prices.',
    tips: ['Try changing the timeframe using the buttons above the chart.'],
    spotlight: { top: 'var(--topbar-height)', left: 'var(--sidebar-left-width)', width: 'calc(100vw - var(--sidebar-left-width) - var(--sidebar-right-width))', height: 'calc(50vh - var(--topbar-height))' },
    cardPosition: 'bottom',
  },
  {
    title: 'Your First Trade',
    description: 'Let\'s make your first trade! Select a stock from the watchlist, then use the Order Panel on the right. Choose BUY, enter a quantity (try 10 shares), and click "Place Buy Order".',
    spotlight: { top: 'var(--topbar-height)', left: 'calc(100vw - var(--sidebar-right-width))', width: 'var(--sidebar-right-width)', height: 'calc(100vh - var(--topbar-height) - var(--ticker-height))' },
    cardPosition: 'left',
    requiredAction: 'orderPlaced',
    actionHint: 'Place a buy order to continue',
  },
  {
    title: 'Order Types',
    description: 'Market orders execute instantly at current price. Limit orders wait for your target price. Stop orders protect against losses. Trailing Stops follow the price up automatically.',
    tips: [
      'Market — instant execution at best price',
      'Limit — only at your price or better',
      'Stop — triggers when price hits threshold',
      'Trailing Stop — dynamic stop that follows gains',
    ],
    cardPosition: 'center',
  },
  {
    title: 'Your Portfolio',
    description: 'Click the Portfolio tab (or press P) to see your positions, P&L, and performance charts. Green means profit, red means loss. Track your unrealized P&L in real-time.',
    spotlight: { top: '0', left: '0', width: '100vw', height: 'var(--topbar-height)' },
    cardPosition: 'center',
  },
  {
    title: 'Time Controls',
    description: 'You control time! Press Space to pause/resume. Use keys 1-4 to set speed (1x, 2x, 5x, 10x). The game auto-pauses on important events like price alerts and breaking news.',
    tips: [
      'Space — Pause / Resume',
      '1 — Normal speed (1 min/sec)',
      '2 — Fast (2 min/sec)',
      '3 — Very Fast (5 min/sec)',
      '4 — Maximum (10 min/sec)',
    ],
    spotlight: { top: '0', left: 'calc(100vw - 300px)', width: '300px', height: 'var(--topbar-height)' },
    cardPosition: 'left',
    requiredAction: 'speedChanged',
    actionHint: 'Change speed or pause to continue',
  },
  {
    title: 'Tutorial Complete!',
    description: 'You now know the basics. Here are a few tips to get started:',
    tips: [
      'Watch the news ticker for market-moving events',
      'Use Limit Orders to buy at a specific price',
      'Press Ctrl+S to save your game anytime',
      'Press Ctrl+K to search for any stock',
      'Check the Analytics tab for portfolio metrics',
    ],
    cardPosition: 'center',
  },
];

interface TutorialOverlayProps {
  isOpen: boolean;
  onClose: () => void;
}

export function TutorialOverlay({ isOpen, onClose }: TutorialOverlayProps) {
  const [step, setStep] = useState(0);
  const [actionCompleted, setActionCompleted] = useState(false);

  const handleClose = useCallback(() => {
    setStep(0);
    setActionCompleted(false);
    onClose();
  }, [onClose]);

  // Listen for tutorial action completion events
  useEffect(() => {
    if (!isOpen) return;
    const current = STEPS[step];
    if (!current?.requiredAction) {
      setActionCompleted(true);
      return;
    }
    setActionCompleted(false);

    const handler = () => setActionCompleted(true);
    window.addEventListener(`tutorial:${current.requiredAction}`, handler);
    return () => window.removeEventListener(`tutorial:${current.requiredAction}`, handler);
  }, [step, isOpen]);

  if (!isOpen) return null;

  const current = STEPS[step];
  const isLast = step === STEPS.length - 1;
  const isFirst = step === 0;
  const spot = current.spotlight;

  // Card positioning based on spotlight location
  const cardStyle = getCardStyle(current.cardPosition, spot);

  return (
    <div style={S.overlay}>
      {/* Spotlight cutout using box-shadow trick */}
      {spot && (
        <div style={{
          position: 'fixed',
          top: spot.top,
          left: spot.left,
          width: spot.width,
          height: spot.height,
          zIndex: 2001,
          border: '2px solid var(--text-accent)',
          borderRadius: '8px',
          boxShadow: '0 0 0 9999px rgba(0, 0, 0, 0.65)',
          pointerEvents: 'none',
        }} />
      )}

      {/* Tutorial card */}
      <div style={{ ...S.card, ...cardStyle }}>
        {/* Step indicator */}
        <div style={S.stepRow}>
          <div style={S.dots}>
            {STEPS.map((_, i) => (
              <div key={i} style={{
                ...S.dot,
                background: i === step ? 'var(--text-accent)' : i < step ? 'var(--green-primary)' : 'var(--bg-tertiary)',
              }} />
            ))}
          </div>
          <span style={S.stepCount}>Step {step + 1} of {STEPS.length}</span>
        </div>

        <h2 style={S.title}>{current.title}</h2>
        <p style={S.description}>{current.description}</p>

        {current.tips && (
          <ul style={S.tipList}>
            {current.tips.map((tip, i) => (
              <li key={i} style={S.tipItem}>{tip}</li>
            ))}
          </ul>
        )}

        <div style={S.buttons}>
          <button style={S.skipBtn} onClick={handleClose}>
            {isLast ? '' : 'Skip Tutorial'}
          </button>
          <div style={{ display: 'flex', gap: '8px' }}>
            {!isFirst && (
              <button style={S.backBtn} onClick={() => setStep(s => s - 1)}>
                Back
              </button>
            )}
            <button
              style={{
                ...S.nextBtn,
                ...(current.requiredAction && !actionCompleted ? { opacity: 0.4, cursor: 'not-allowed' } : {}),
              }}
              disabled={!!current.requiredAction && !actionCompleted}
              onClick={() => {
                setActionCompleted(false);
                isLast ? handleClose() : setStep(s => s + 1);
              }}
            >
              {isLast ? 'Start Trading!' : current.requiredAction && !actionCompleted ? (current.actionHint || 'Complete action...') : 'Next'}
            </button>
          </div>
        </div>
      </div>
    </div>
  );
}

function getCardStyle(position: TutorialStep['cardPosition'], spot?: SpotlightRegion): React.CSSProperties {
  const base: React.CSSProperties = { position: 'fixed', zIndex: 2002 };

  if (!spot || position === 'center') {
    return { ...base, top: '50%', left: '50%', transform: 'translate(-50%, -50%)' };
  }

  switch (position) {
    case 'right':
      return { ...base, top: '15%', left: 'calc(var(--sidebar-left-width) + 20px)' };
    case 'left':
      return { ...base, top: '15%', right: 'calc(var(--sidebar-right-width) + 20px)' };
    case 'bottom':
      return { ...base, top: '55%', left: '50%', transform: 'translateX(-50%)' };
    default:
      return { ...base, top: '50%', left: '50%', transform: 'translate(-50%, -50%)' };
  }
}

const S: Record<string, React.CSSProperties> = {
  overlay: {
    position: 'fixed', inset: 0,
    background: 'rgba(0,0,0,0.55)',
    zIndex: 2000,
  },
  card: {
    width: '400px',
    background: 'var(--bg-secondary)',
    border: '2px solid var(--text-accent)',
    borderRadius: '12px',
    padding: '24px',
    boxShadow: '0 8px 32px rgba(0,0,0,0.5)',
  },
  stepRow: {
    display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '16px',
  },
  dots: {
    display: 'flex', gap: '6px',
  },
  dot: {
    width: '8px', height: '8px', borderRadius: '50%',
  },
  stepCount: {
    fontSize: '11px', color: 'var(--text-disabled)', fontFamily: 'var(--font-mono)',
  },
  title: {
    fontSize: '18px', fontWeight: 700, color: 'var(--text-primary)',
    marginBottom: '8px', fontFamily: 'var(--font-ui)', margin: '0 0 8px 0',
  },
  description: {
    fontSize: '14px', color: 'var(--text-secondary)', lineHeight: '1.6',
    marginBottom: '12px', fontFamily: 'var(--font-ui)', margin: '0 0 12px 0',
  },
  tipList: {
    margin: '0 0 16px 0', padding: '0 0 0 18px',
    fontSize: '13px', color: 'var(--text-secondary)', lineHeight: '1.8',
    fontFamily: 'var(--font-ui)',
  },
  tipItem: {
    marginBottom: '2px',
  },
  buttons: {
    display: 'flex', justifyContent: 'space-between', alignItems: 'center',
  },
  backBtn: {
    padding: '8px 16px', borderRadius: '6px',
    background: 'var(--bg-tertiary)', color: 'var(--text-secondary)',
    border: 'none', cursor: 'pointer', fontSize: '13px', fontFamily: 'var(--font-ui)',
  },
  skipBtn: {
    padding: '8px 16px', borderRadius: '6px',
    background: 'transparent', color: 'var(--text-disabled)',
    border: 'none', cursor: 'pointer', fontSize: '13px', fontFamily: 'var(--font-ui)',
    minWidth: '100px',
  },
  nextBtn: {
    padding: '8px 20px', borderRadius: '6px',
    background: 'var(--text-accent)', color: '#FFFFFF',
    border: 'none', cursor: 'pointer', fontSize: '13px', fontWeight: 700, fontFamily: 'var(--font-ui)',
  },
};
