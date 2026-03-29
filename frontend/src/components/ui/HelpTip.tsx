import { useState, useRef, useEffect } from 'react';
import { HelpCircle } from 'lucide-react';
import { GLOSSARY } from '@/data/glossary';

interface HelpTipProps {
  /** Glossary term to look up (e.g. "P/E Ratio", "Short Selling") */
  term: string;
  /** Optional: override the displayed label (defaults to term) */
  label?: string;
  /** Show as inline icon (default) or underline the label text */
  mode?: 'icon' | 'underline';
  /** Icon size in pixels */
  size?: number;
}

/**
 * Context-sensitive glossary tooltip.
 * Hover over the icon/text to see the definition from the glossary.
 * Usage: <HelpTip term="P/E Ratio" /> or <HelpTip term="Short Selling" mode="underline" label="Short" />
 */
export function HelpTip({ term, label, mode = 'icon', size = 12 }: HelpTipProps) {
  const [show, setShow] = useState(false);
  const [tooltipStyle, setTooltipStyle] = useState<React.CSSProperties>({});
  const ref = useRef<HTMLSpanElement>(null);

  const entry = GLOSSARY.find(g =>
    g.term.toLowerCase() === term.toLowerCase() ||
    g.term.toLowerCase().startsWith(term.toLowerCase())
  );

  useEffect(() => {
    if (show && ref.current) {
      const rect = ref.current.getBoundingClientRect();
      const spaceBelow = window.innerHeight - rect.bottom;
      const spaceAbove = rect.top;
      const above = spaceAbove > spaceBelow && spaceAbove > 120;
      // Center horizontally, clamp to viewport
      let left = rect.left + rect.width / 2 - 140; // 140 = half of 280px width
      left = Math.max(10, Math.min(left, window.innerWidth - 290));
      setTooltipStyle({
        left,
        ...(above ? { bottom: window.innerHeight - rect.top + 6 } : { top: rect.bottom + 6 }),
      });
    }
  }, [show]);

  if (!entry) return label ? <span>{label}</span> : null;

  return (
    <span
      ref={ref}
      style={S.wrapper}
      onMouseEnter={() => setShow(true)}
      onMouseLeave={() => setShow(false)}
    >
      {mode === 'icon' ? (
        <HelpCircle size={size} style={S.icon} />
      ) : (
        <span style={S.underline}>{label || entry.term}</span>
      )}

      {show && (
        <div style={{ ...S.tooltip, ...tooltipStyle }}>
          <div style={S.tooltipHeader}>
            <span style={S.tooltipTerm}>{entry.term}</span>
            <span style={S.tooltipCategory}>{entry.category}</span>
          </div>
          <div style={S.tooltipBody}>{entry.definition}</div>
        </div>
      )}
    </span>
  );
}

const S: Record<string, React.CSSProperties> = {
  wrapper: {
    position: 'relative',
    display: 'inline-flex',
    alignItems: 'center',
    cursor: 'help',
  },
  icon: {
    color: '#4B5563',
    opacity: 0.6,
    transition: 'opacity 0.15s',
  },
  underline: {
    borderBottom: '1px dotted #4B5563',
    cursor: 'help',
  },
  tooltip: {
    position: 'fixed',
    width: 280,
    maxWidth: 'calc(100vw - 20px)',
    padding: '8px 10px',
    background: '#1e293b',
    border: '1px solid #334155',
    borderRadius: 6,
    boxShadow: '0 8px 24px rgba(0,0,0,0.5)',
    zIndex: 9999,
    pointerEvents: 'none' as const,
  },
  tooltipHeader: {
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'center',
    marginBottom: 4,
  },
  tooltipTerm: {
    fontWeight: 700,
    fontSize: '12px',
    color: '#F3F4F6',
    letterSpacing: '0.01em',
  },
  tooltipCategory: {
    fontSize: '9px',
    fontWeight: 600,
    color: '#60A5FA',
    background: '#1e3a5f',
    padding: '1px 5px',
    borderRadius: 3,
    letterSpacing: '0.05em',
    textTransform: 'uppercase' as const,
  },
  tooltipBody: {
    fontSize: '11px',
    color: '#9CA3AF',
    lineHeight: '1.5',
  },
};
