import { useState } from 'react';
import { HelpTip } from '@/components/ui/HelpTip';
import { Target, BookOpen, CheckCircle, XCircle, ChevronRight } from 'lucide-react';
import type { DecisionPoint } from '@/data/decisionCases';

interface Props {
  decision: DecisionPoint;
  caseName: string;
  onClose: () => void;
}

/**
 * Decision Case popup — presents a market situation and asks the player to choose.
 * Shows explanation after choice is made.
 */
export function DecisionCaseModal({ decision, caseName, onClose }: Props) {
  const [selected, setSelected] = useState<string | null>(null);
  const chosen = decision.choices.find(c => c.id === selected);

  return (
    <div style={S.overlay} onClick={onClose}>
      <div style={S.modal} onClick={e => e.stopPropagation()}>
        {/* Header */}
        <div style={S.header}>
          <div style={S.headerLeft}>
            <BookOpen size={16} style={{ color: '#F59E0B' }} />
            <span style={S.caseLabel}>{caseName}</span>
          </div>
          <span style={S.title}>{decision.title}</span>
        </div>

        {/* Situation */}
        <div style={S.situation}>
          <p style={S.situationText}>{decision.situation}</p>
          {decision.glossaryTerms.length > 0 && (
            <div style={S.glossaryRow}>
              {decision.glossaryTerms.map(term => (
                <HelpTip key={term} term={term} mode="underline" label={term} size={11} />
              ))}
            </div>
          )}
        </div>

        {/* Choices */}
        {!selected && (
          <div style={S.choicesGrid}>
            <div style={S.choicesLabel}>
              <Target size={12} style={{ color: '#60A5FA' }} />
              What do you do?
            </div>
            {decision.choices.map(choice => (
              <button
                key={choice.id}
                style={S.choiceBtn}
                onClick={() => setSelected(choice.id)}
                onMouseEnter={e => {
                  (e.target as HTMLElement).style.borderColor = '#60A5FA';
                  (e.target as HTMLElement).style.background = 'rgba(96,165,250,0.08)';
                }}
                onMouseLeave={e => {
                  (e.target as HTMLElement).style.borderColor = '#1e293b';
                  (e.target as HTMLElement).style.background = '#0d1321';
                }}
              >
                <span style={S.choiceLabel}>{choice.label}</span>
                <span style={S.choiceDesc}>{choice.description}</span>
                <ChevronRight size={14} style={{ color: '#4B5563', flexShrink: 0 }} />
              </button>
            ))}
          </div>
        )}

        {/* Explanation (after choosing) */}
        {selected && chosen && (
          <div style={S.resultSection}>
            <div style={{
              ...S.resultBadge,
              background: chosen.isRecommended ? 'rgba(16,185,129,0.1)' : 'rgba(245,158,11,0.1)',
              borderColor: chosen.isRecommended ? '#10B981' : '#F59E0B',
              color: chosen.isRecommended ? '#6ee7b7' : '#fcd34d',
            }}>
              {chosen.isRecommended
                ? <><CheckCircle size={14} /> Great choice!</>
                : <><XCircle size={14} /> Not ideal — here's why:</>
              }
            </div>
            <div style={S.yourChoice}>
              <span style={S.yourChoiceLabel}>You chose:</span> {chosen.label}
            </div>
            <p style={S.explanation}>{chosen.explanation}</p>

            {/* Show recommended if player didn't pick it */}
            {!chosen.isRecommended && (
              <div style={S.recommended}>
                <span style={{ fontWeight: 700, color: '#10B981' }}>Recommended:</span>{' '}
                {decision.choices.find(c => c.isRecommended)?.label} — {decision.choices.find(c => c.isRecommended)?.explanation}
              </div>
            )}

            <button style={S.continueBtn} onClick={onClose}>
              Continue Trading
              <ChevronRight size={14} />
            </button>
          </div>
        )}
      </div>
    </div>
  );
}

const S: Record<string, React.CSSProperties> = {
  overlay: {
    position: 'fixed',
    inset: 0,
    background: 'rgba(0,0,0,0.7)',
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'center',
    zIndex: 10000,
  },
  modal: {
    width: 540,
    maxHeight: '85vh',
    overflow: 'auto',
    background: '#111827',
    border: '1px solid #1e293b',
    borderRadius: 10,
    boxShadow: '0 20px 60px rgba(0,0,0,0.6)',
  },
  header: {
    padding: '16px 20px 12px',
    borderBottom: '1px solid #1e293b',
    display: 'flex',
    flexDirection: 'column' as const,
    gap: 4,
  },
  headerLeft: {
    display: 'flex',
    alignItems: 'center',
    gap: 6,
  },
  caseLabel: {
    fontSize: '11px',
    fontWeight: 600,
    color: '#F59E0B',
    textTransform: 'uppercase' as const,
    letterSpacing: '0.06em',
  },
  title: {
    fontSize: '18px',
    fontWeight: 700,
    color: '#F3F4F6',
    letterSpacing: '-0.01em',
  },
  situation: {
    padding: '16px 20px',
    borderBottom: '1px solid #1e293b',
  },
  situationText: {
    fontSize: '13px',
    color: '#D1D5DB',
    lineHeight: '1.6',
    margin: 0,
  },
  glossaryRow: {
    display: 'flex',
    gap: 12,
    marginTop: 10,
    fontSize: '11px',
    color: '#6B7280',
  },
  choicesGrid: {
    padding: '16px 20px',
    display: 'flex',
    flexDirection: 'column' as const,
    gap: 8,
  },
  choicesLabel: {
    fontSize: '12px',
    fontWeight: 700,
    color: '#60A5FA',
    display: 'flex',
    alignItems: 'center',
    gap: 6,
    marginBottom: 4,
  },
  choiceBtn: {
    display: 'flex',
    alignItems: 'center',
    gap: 12,
    padding: '12px 14px',
    background: '#0d1321',
    border: '1px solid #1e293b',
    borderRadius: 6,
    cursor: 'pointer',
    textAlign: 'left' as const,
    transition: 'all 0.15s',
    width: '100%',
  },
  choiceLabel: {
    fontSize: '13px',
    fontWeight: 600,
    color: '#E5E7EB',
    minWidth: 0,
  },
  choiceDesc: {
    fontSize: '11px',
    color: '#6B7280',
    flex: 1,
  },
  resultSection: {
    padding: '16px 20px 20px',
  },
  resultBadge: {
    display: 'inline-flex',
    alignItems: 'center',
    gap: 6,
    padding: '6px 12px',
    borderRadius: 6,
    border: '1px solid',
    fontSize: '13px',
    fontWeight: 700,
    marginBottom: 12,
  },
  yourChoice: {
    fontSize: '12px',
    color: '#9CA3AF',
    marginBottom: 8,
  },
  yourChoiceLabel: {
    fontWeight: 700,
    color: '#D1D5DB',
  },
  explanation: {
    fontSize: '13px',
    color: '#D1D5DB',
    lineHeight: '1.6',
    margin: '0 0 12px',
  },
  recommended: {
    fontSize: '12px',
    color: '#9CA3AF',
    lineHeight: '1.6',
    padding: '10px 12px',
    background: 'rgba(16,185,129,0.05)',
    border: '1px solid rgba(16,185,129,0.15)',
    borderRadius: 6,
    marginBottom: 16,
  },
  continueBtn: {
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'center',
    gap: 6,
    width: '100%',
    padding: '10px',
    background: '#1e3a5f',
    color: '#93c5fd',
    border: '1px solid #2563EB',
    borderRadius: 6,
    fontSize: '13px',
    fontWeight: 700,
    cursor: 'pointer',
  },
};
