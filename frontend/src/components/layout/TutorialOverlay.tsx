import { useState } from 'react';

interface TutorialStep {
  title: string;
  description: string;
  highlight?: string; // CSS selector or area name
}

/**
 * Tutorial overlay. Bible 14: 7-step onboarding.
 */
const STEPS: TutorialStep[] = [
  {
    title: 'Welcome to StockSim!',
    description: 'This is a realistic stock market simulator. You start with $50,000 in cash and your goal is to grow your portfolio through smart trading. Let\'s learn the basics.',
  },
  {
    title: 'The Market Table',
    description: 'Click the "Market" tab (or press M) to see all available stocks. You can sort by any column and filter by name, symbol, or sector. Click any stock to view its chart.',
  },
  {
    title: 'Reading Charts',
    description: 'The candlestick chart shows price history. Green candles = price went up, red = price went down. The chart now shows technical indicators like SMA and Bollinger Bands to help with analysis.',
  },
  {
    title: 'Placing Your First Order',
    description: 'Select a stock and look at the Order Panel on the right. Choose BUY, set a quantity, and click "Place Buy Order". Market orders execute instantly at the current ask price.',
  },
  {
    title: 'Order Types',
    description: 'Market orders execute immediately. Limit orders wait for your target price. Stop orders protect against losses. Trailing Stops follow the price up and sell when it drops.',
  },
  {
    title: 'Watching the News',
    description: 'The news ticker at the bottom shows live market events. Breaking news can move prices significantly. Use the News tab for the full event history. Green = positive, Red = negative.',
  },
  {
    title: 'Time Controls',
    description: 'Use Space to pause/resume. Keys 1-4 set speed (1x, 2x, 5x, 10x). Pause to analyze, speed up to fast-forward. The game auto-pauses on important events like price alerts. Good luck!',
  },
];

interface TutorialOverlayProps {
  isOpen: boolean;
  onClose: () => void;
}

export function TutorialOverlay({ isOpen, onClose }: TutorialOverlayProps) {
  const [step, setStep] = useState(0);

  if (!isOpen) return null;

  const current = STEPS[step];
  const isLast = step === STEPS.length - 1;

  return (
    <div style={styles.overlay}>
      <div style={styles.card}>
        {/* Step indicator */}
        <div style={styles.stepIndicator}>
          {STEPS.map((_, i) => (
            <div key={i} style={{
              ...styles.dot,
              background: i === step ? 'var(--text-accent)' : i < step ? 'var(--green-primary)' : 'var(--bg-tertiary)',
            }} />
          ))}
        </div>

        <div style={styles.stepCount}>Step {step + 1} of {STEPS.length}</div>

        <h2 style={styles.title}>{current.title}</h2>
        <p style={styles.description}>{current.description}</p>

        <div style={styles.buttons}>
          {step > 0 && (
            <button style={styles.backBtn} onClick={() => setStep(s => s - 1)}>
              Back
            </button>
          )}
          <button style={styles.skipBtn} onClick={onClose}>
            Skip Tutorial
          </button>
          <button
            style={styles.nextBtn}
            onClick={() => isLast ? onClose() : setStep(s => s + 1)}
          >
            {isLast ? 'Start Trading!' : 'Next'}
          </button>
        </div>
      </div>
    </div>
  );
}

const styles: Record<string, React.CSSProperties> = {
  overlay: {
    position: 'fixed', inset: 0,
    background: 'rgba(0,0,0,0.7)',
    display: 'flex', alignItems: 'center', justifyContent: 'center',
    zIndex: 2000,
  },
  card: {
    width: '520px',
    background: 'var(--bg-secondary)',
    border: '1px solid var(--border)',
    borderRadius: '12px',
    padding: '32px',
    textAlign: 'center',
  },
  stepIndicator: {
    display: 'flex', justifyContent: 'center', gap: '8px', marginBottom: '16px',
  },
  dot: {
    width: '8px', height: '8px', borderRadius: '50%',
  },
  stepCount: {
    fontSize: '12px', color: 'var(--text-disabled)', marginBottom: '8px',
  },
  title: {
    fontSize: '22px', fontWeight: 700, color: 'var(--text-primary)',
    marginBottom: '12px', fontFamily: 'var(--font-ui)',
  },
  description: {
    fontSize: '15px', color: 'var(--text-secondary)', lineHeight: '1.6',
    marginBottom: '24px', fontFamily: 'var(--font-ui)',
  },
  buttons: {
    display: 'flex', justifyContent: 'center', gap: '12px',
  },
  backBtn: {
    padding: '10px 20px', borderRadius: '6px',
    background: 'var(--bg-tertiary)', color: 'var(--text-secondary)',
    border: 'none', cursor: 'pointer', fontSize: '14px', fontFamily: 'var(--font-ui)',
  },
  skipBtn: {
    padding: '10px 20px', borderRadius: '6px',
    background: 'transparent', color: 'var(--text-disabled)',
    border: '1px solid var(--border)', cursor: 'pointer', fontSize: '14px', fontFamily: 'var(--font-ui)',
  },
  nextBtn: {
    padding: '10px 24px', borderRadius: '6px',
    background: 'var(--text-accent)', color: '#FFFFFF',
    border: 'none', cursor: 'pointer', fontSize: '14px', fontWeight: 700, fontFamily: 'var(--font-ui)',
  },
};
