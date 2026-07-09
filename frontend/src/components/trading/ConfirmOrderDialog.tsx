import { useState } from 'react';
import { OrderSide, OrderType } from '@/types/market';
import { useFocusTrap } from '@/hooks/useFocusTrap';

interface ConfirmOrderDialogProps {
  isOpen: boolean;
  symbol: string;
  side: OrderSide;
  type: OrderType;
  quantity: number;
  estimatedPrice: number;
  commission: number;
  cash?: number;
  totalEquity?: number;
  onConfirm: () => void;
  onCancel: () => void;
}

/**
 * Order confirmation dialog. Spec 3.7.
 */
export function ConfirmOrderDialog({
  isOpen, symbol, side, type, quantity, estimatedPrice, commission, cash, totalEquity, onConfirm, onCancel,
}: ConfirmOrderDialogProps) {
  const trapRef = useFocusTrap<HTMLDivElement>();
  if (!isOpen) return null;

  const isBuy = side === 'Buy' || side === 'Cover';
  const total = quantity * estimatedPrice + (isBuy ? commission : -commission);
  const cashAfter = cash !== undefined ? (isBuy ? cash - total : cash + total) : undefined;
  const sideColor = side === 'Buy' ? 'var(--green-primary)' :
                    side === 'Sell' ? 'var(--red-primary)' :
                    side === 'Short' ? 'var(--warning)' : 'var(--text-accent)';

  return (
    <div style={styles.overlay} onClick={onCancel}>
      <div ref={trapRef} style={styles.modal} onClick={e => e.stopPropagation()}>
        <h3 style={styles.title}>
          Confirm {side} Order
        </h3>

        <div style={styles.details}>
          <Row label="Stock" value={symbol} mono />
          <Row label="Side" value={side} color={sideColor} />
          <Row label="Order Type" value={type} />
          <Row label="Quantity" value={`${quantity} shares`} mono />
          <Row label="Est. Price" value={`$${estimatedPrice.toFixed(2)}`} mono />
          <Row label="Commission" value={`$${commission.toFixed(2)}`} mono />
          <div style={styles.divider} />
          <Row label={isBuy ? 'Total Debit' : 'Total Credit'} value={`$${Math.abs(total).toFixed(2)}`} mono bold />
        </div>

        {/* Buying Power Impact */}
        {cashAfter !== undefined && (
          <div style={{
            display: 'flex', justifyContent: 'space-between', padding: '8px 12px',
            background: 'var(--bg-tertiary)', borderRadius: '4px', marginBottom: '12px',
            fontSize: '12px',
          }}>
            <div style={{ color: 'var(--text-secondary)' }}>
              <div>Cash Before: <span className="mono" style={{ color: 'var(--text-primary)' }}>${cash!.toFixed(2)}</span></div>
              <div>Cash After: <span className="mono" style={{ color: cashAfter < 0 ? 'var(--red-primary)' : 'var(--text-primary)', fontWeight: 600 }}>${cashAfter.toFixed(2)}</span></div>
            </div>
            {totalEquity !== undefined && (
              <div style={{ color: 'var(--text-secondary)', textAlign: 'right' }}>
                <div>Portfolio: <span className="mono" style={{ color: 'var(--text-primary)' }}>${totalEquity.toFixed(0)}</span></div>
                <div>Trade Size: <span className="mono" style={{ color: 'var(--text-primary)' }}>{((total / totalEquity) * 100).toFixed(1)}%</span></div>
              </div>
            )}
          </div>
        )}

        {type === 'Market' && (
          <div style={styles.warning}>
            Market orders execute at the best available price. Actual fill price may differ due to slippage.
          </div>
        )}

        {cashAfter !== undefined && cashAfter < 0 && (
          <div style={{ ...styles.warning, color: 'var(--red-primary)', background: 'color-mix(in srgb, var(--red-primary) 10%, transparent)', border: '1px solid color-mix(in srgb, var(--red-primary) 20%, transparent)' }}>
            This order exceeds available cash. It may be rejected or trigger margin.
          </div>
        )}

        <ConfirmButtons side={side} sideColor={sideColor} onConfirm={onConfirm} onCancel={onCancel} />
      </div>
    </div>
  );
}

function Row({ label, value, mono, bold, color }: { label: string; value: string; mono?: boolean; bold?: boolean; color?: string }) {
  return (
    <div style={styles.row}>
      <span style={styles.label}>{label}</span>
      <span className={mono ? 'mono' : ''} style={{
        fontSize: bold ? '16px' : '14px', fontWeight: bold ? 700 : 400,
        color: color || 'var(--text-primary)',
      }}>{value}</span>
    </div>
  );
}

function ConfirmButtons({ side, sideColor, onConfirm, onCancel }: {
  side: string; sideColor: string; onConfirm: () => void; onCancel: () => void;
}) {
  const [submitted, setSubmitted] = useState(false);
  return (
    <div style={styles.buttons}>
      <button style={styles.cancelBtn} onClick={onCancel} disabled={submitted}>Cancel</button>
      <button
        style={{ ...styles.confirmBtn, background: sideColor, ...(submitted ? { opacity: 0.5 } : {}) }}
        disabled={submitted}
        onClick={() => { setSubmitted(true); onConfirm(); }}
      >
        {submitted ? 'Submitting...' : `Confirm ${side}`}
      </button>
    </div>
  );
}

const styles: Record<string, React.CSSProperties> = {
  overlay: {
    position: 'fixed', inset: 0, background: 'rgba(0,0,0,0.5)',
    display: 'flex', alignItems: 'center', justifyContent: 'center', zIndex: 4000,
  },
  modal: {
    width: '400px', background: 'var(--bg-secondary)',
    border: '1px solid var(--border)', borderRadius: '8px', padding: '24px',
  },
  title: {
    fontSize: '16px', fontWeight: 600, color: 'var(--text-primary)',
    textAlign: 'center', marginBottom: '16px',
  },
  details: { marginBottom: '16px' },
  row: {
    display: 'flex', justifyContent: 'space-between', alignItems: 'center',
    padding: '6px 0',
  },
  label: { fontSize: '13px', color: 'var(--text-secondary)' },
  divider: { borderTop: '1px solid var(--border)', margin: '8px 0' },
  warning: {
    fontSize: '11px', color: 'var(--text-disabled)', padding: '8px 12px',
    background: 'var(--bg-tertiary)', borderRadius: '4px', marginBottom: '16px',
  },
  buttons: { display: 'flex', gap: '12px', justifyContent: 'flex-end' },
  cancelBtn: {
    padding: '8px 20px', borderRadius: '6px', background: 'var(--bg-tertiary)',
    color: 'var(--text-secondary)', border: 'none', cursor: 'pointer',
    fontSize: '14px', fontFamily: 'var(--font-ui)',
  },
  confirmBtn: {
    padding: '8px 24px', borderRadius: '6px', color: 'var(--text-primary)', border: 'none',
    cursor: 'pointer', fontSize: '14px', fontWeight: 700, fontFamily: 'var(--font-ui)',
  },
};
