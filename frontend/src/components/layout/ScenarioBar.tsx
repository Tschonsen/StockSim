import { useState } from 'react';
import { useMarketStore } from '@/stores/marketStore';
import { Target, Clock, ChevronDown, ChevronUp, Trophy, AlertTriangle } from 'lucide-react';

/**
 * Bloomberg-style Scenario Progress Bar.
 * Shows active scenario objective, progress, time remaining, and key stats.
 * Sits between TopBar and CentralArea. Collapsible.
 */

// Difficulty badge colors
const DIFF_COLORS: Record<string, { bg: string; text: string; border: string }> = {
  easy:   { bg: '#064e3b', text: '#6ee7b7', border: 'var(--green-primary)' },
  normal: { bg: '#1e3a5f', text: '#93c5fd', border: 'var(--chart-blue)' },
  hard:   { bg: '#78350f', text: '#fcd34d', border: 'var(--warning)' },
  brutal: { bg: '#7f1d1d', text: '#fca5a5', border: 'var(--red-primary)' },
};

export function ScenarioBar() {
  const scenarioProgress = useMarketStore((s) => s.scenarioProgress);
  const portfolio = useMarketStore((s) => s.portfolio);
  const [collapsed, setCollapsed] = useState(false);

  if (!scenarioProgress) return null;

  const {
    scenarioName, difficulty, targetValue, targetDescription,
    daysRemaining,
    currentEquity, startingCash, winCondition, loseCondition,
  } = scenarioProgress;

  const diff = DIFF_COLORS[difficulty] || DIFF_COLORS.normal;
  const progressPct = targetValue && targetValue > startingCash
    ? Math.min(100, Math.max(0, ((currentEquity - startingCash) / (targetValue - startingCash)) * 100))
    : 0;
  const totalReturn = startingCash > 0 ? ((currentEquity - startingCash) / startingCash) * 100 : 0;
  const isPositive = totalReturn >= 0;
  const isUrgent = daysRemaining !== null && daysRemaining <= 5;

  // Stats from portfolio
  const trades = portfolio?.tradeCount ?? 0;

  return (
    <div style={S.wrapper}>
      {/* Collapsed: single-line summary */}
      <div style={S.headerRow} onClick={() => setCollapsed(!collapsed)}>
        {/* Left: scenario name + badge */}
        <div style={S.leftGroup}>
          <Trophy size={13} style={{ color: diff.text, flexShrink: 0 }} />
          <span style={{ ...S.scenarioName, color: diff.text }}>{scenarioName}</span>
          <span style={{ ...S.badge, background: diff.bg, color: diff.text, borderColor: diff.border }}>
            {difficulty.toUpperCase()}
          </span>
        </div>

        {/* Center: mini progress */}
        <div style={S.centerGroup}>
          <span style={S.labelDim}>Target</span>
          <span style={S.monoValue}>
            ${targetValue ? targetValue.toLocaleString(undefined, { maximumFractionDigits: 0 }) : '—'}
          </span>
          <div style={S.miniProgressTrack}>
            <div style={{
              ...S.miniProgressFill,
              width: `${Math.max(0, Math.min(100, progressPct))}%`,
              background: progressPct >= 100 ? 'var(--green-primary)' : progressPct > 60 ? 'var(--chart-blue)' : 'var(--warning)',
            }} />
          </div>
          <span style={{ ...S.monoValue, color: isPositive ? 'var(--green-primary)' : 'var(--red-primary)' }}>
            {isPositive ? '+' : ''}{totalReturn.toFixed(1)}%
          </span>
        </div>

        {/* Right: time + collapse */}
        <div style={S.rightGroup}>
          {daysRemaining !== null && (
            <>
              <Clock size={12} style={{ color: isUrgent ? 'var(--red-primary)' : 'var(--text-disabled)' }} />
              <span style={{
                ...S.monoValue,
                color: isUrgent ? 'var(--red-primary)' : 'var(--text-secondary)',
                fontWeight: isUrgent ? 700 : 500,
              }}>
                {daysRemaining}d
              </span>
            </>
          )}
          {collapsed ? <ChevronDown size={14} style={{ color: 'var(--text-disabled)' }} /> : <ChevronUp size={14} style={{ color: 'var(--text-disabled)' }} />}
        </div>
      </div>

      {/* Expanded: full details */}
      {!collapsed && (
        <div style={S.expandedRow}>
          {/* Progress bar full width */}
          <div style={S.progressSection}>
            <div style={S.progressLabels}>
              <span style={S.labelDim}>
                <Target size={11} style={{ marginRight: 4, verticalAlign: -1 }} />
                {targetDescription || winCondition}
              </span>
              <span style={S.monoSmall}>
                ${currentEquity.toLocaleString(undefined, { maximumFractionDigits: 0 })}
                {targetValue ? ` / $${targetValue.toLocaleString(undefined, { maximumFractionDigits: 0 })}` : ''}
              </span>
            </div>
            <div style={S.progressTrack}>
              <div style={{
                ...S.progressFill,
                width: `${Math.max(1, Math.min(100, progressPct))}%`,
                background: progressPct >= 100
                  ? 'linear-gradient(90deg, #059669, var(--green-primary))'
                  : progressPct > 60
                    ? 'linear-gradient(90deg, #2563EB, var(--chart-blue))'
                    : 'linear-gradient(90deg, #D97706, var(--warning))',
              }} />
              {/* Starting point marker */}
              <div style={S.startMarker} />
            </div>
          </div>

          {/* Stats row */}
          <div style={S.statsRow}>
            <StatCell label="EQUITY" value={`$${currentEquity.toLocaleString(undefined, { maximumFractionDigits: 0 })}`} color={isPositive ? 'var(--green-primary)' : 'var(--red-primary)'} />
            <StatCell label="RETURN" value={`${isPositive ? '+' : ''}${totalReturn.toFixed(2)}%`} color={isPositive ? 'var(--green-primary)' : 'var(--red-primary)'} />
            <StatCell label="TRADES" value={`${trades}`} />
            {daysRemaining !== null && (
              <StatCell
                label="DAYS LEFT"
                value={`${daysRemaining}`}
                color={isUrgent ? 'var(--red-primary)' : undefined}
                icon={isUrgent ? <AlertTriangle size={10} style={{ color: 'var(--red-primary)' }} /> : undefined}
              />
            )}
            {loseCondition && (
              <StatCell label="FAIL IF" value={loseCondition} color="var(--text-disabled)" small />
            )}
          </div>
        </div>
      )}
    </div>
  );
}

function StatCell({ label, value, color, icon, small }: {
  label: string; value: string; color?: string; icon?: React.ReactNode; small?: boolean;
}) {
  return (
    <div style={S.statCell}>
      <span style={S.statLabel}>{label}</span>
      <div style={{ display: 'flex', alignItems: 'center', gap: 3 }}>
        {icon}
        <span style={{
          fontFamily: "'JetBrains Mono', monospace",
          fontSize: small ? '10px' : '12px',
          fontWeight: 600,
          color: color || 'var(--text-primary)',
          letterSpacing: '-0.02em',
        }}>{value}</span>
      </div>
    </div>
  );
}

// --- Styles ---
const S: Record<string, React.CSSProperties> = {
  wrapper: {
    background: 'linear-gradient(180deg, var(--bg-darkest) 0%, var(--bg-secondary) 100%)',
    borderBottom: '1px solid var(--border-dark)',
    padding: '0 16px',
    userSelect: 'none',
    fontSize: '12px',
  },
  headerRow: {
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'space-between',
    height: 32,
    cursor: 'pointer',
    gap: 16,
  },
  leftGroup: {
    display: 'flex',
    alignItems: 'center',
    gap: 8,
    minWidth: 0,
    flexShrink: 0,
  },
  centerGroup: {
    display: 'flex',
    alignItems: 'center',
    gap: 10,
    flex: 1,
    justifyContent: 'center',
    minWidth: 0,
  },
  rightGroup: {
    display: 'flex',
    alignItems: 'center',
    gap: 6,
    flexShrink: 0,
  },
  scenarioName: {
    fontWeight: 700,
    fontSize: '12px',
    letterSpacing: '0.02em',
    whiteSpace: 'nowrap' as const,
  },
  badge: {
    fontSize: '9px',
    fontWeight: 800,
    padding: '1px 6px',
    borderRadius: 3,
    border: '1px solid',
    letterSpacing: '0.08em',
    lineHeight: '16px',
  },
  labelDim: {
    color: 'var(--text-disabled)',
    fontSize: '11px',
    whiteSpace: 'nowrap' as const,
  },
  monoValue: {
    fontFamily: "'JetBrains Mono', monospace",
    fontSize: '12px',
    fontWeight: 600,
    color: 'var(--text-primary)',
    letterSpacing: '-0.02em',
  },
  monoSmall: {
    fontFamily: "'JetBrains Mono', monospace",
    fontSize: '11px',
    fontWeight: 500,
    color: 'var(--text-secondary)',
  },
  miniProgressTrack: {
    width: 80,
    height: 4,
    background: 'var(--border-dark)',
    borderRadius: 2,
    overflow: 'hidden' as const,
    flexShrink: 0,
  },
  miniProgressFill: {
    height: '100%',
    borderRadius: 2,
    transition: 'width 0.5s ease',
  },
  expandedRow: {
    paddingBottom: 8,
    display: 'flex',
    flexDirection: 'column' as const,
    gap: 6,
  },
  progressSection: {
    display: 'flex',
    flexDirection: 'column' as const,
    gap: 3,
  },
  progressLabels: {
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'center',
  },
  progressTrack: {
    width: '100%',
    height: 6,
    background: 'var(--border-dark)',
    borderRadius: 3,
    overflow: 'hidden' as const,
    position: 'relative' as const,
  },
  progressFill: {
    height: '100%',
    borderRadius: 3,
    transition: 'width 0.8s ease',
  },
  startMarker: {
    position: 'absolute' as const,
    left: 0,
    top: -1,
    width: 2,
    height: 8,
    background: 'var(--border-hover)',
    borderRadius: 1,
  },
  statsRow: {
    display: 'flex',
    gap: 2,
    flexWrap: 'wrap' as const,
  },
  statCell: {
    display: 'flex',
    flexDirection: 'column' as const,
    padding: '3px 10px',
    background: 'var(--bg-primary)',
    borderRadius: 3,
    border: '1px solid var(--border-dark)',
    minWidth: 70,
  },
  statLabel: {
    fontSize: '9px',
    fontWeight: 700,
    color: 'var(--border-hover)',
    letterSpacing: '0.1em',
    lineHeight: '14px',
  },
};
