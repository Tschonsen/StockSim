import { OrderSide, OrderType } from '@/types/market';

interface ConfirmOrderDialogProps {
  isOpen: boolean;
  symbol: string;
  side: OrderSide;
  type: OrderType;
  quantity: number;
  estimatedPrice: number;
  commission: number;
  onConfirm: () => void;
  onCancel: () => void;
}

/**
 * Order confirmation dialog. Bible 3.7.
 */
export function ConfirmOrderDialog({
  isOpen, symbol, side, type, quantity, estimatedPrice, commission, onConfirm, onCancel,
}: ConfirmOrderDialogProps) {
  if (!isOpen) return null;

  const total = quantity * estimatedPrice + (side === 'Buy' || side === 'Cover' ? commission : -commission);
  const sideColor = side === 'Buy' ? 'var(--green-primary)' :
                    side === 'Sell' ? 'var(--red-primary)' :
                    side === 'Short' ? '#F59E0B' : '#60A5FA';

  return (
    <div style={styles.overlay} onClick={onCancel}>
      <div style={styles.modal} onClick={e => e.stopPropagation()}>
        <h3 style={styles.title}>
          Confirm {side} Order
        </h3>

        <div style={styles.details}>
          <Row label="Stock" value={symbol} mono />
          <Row label="Order Type" value={type} />
          <Row label="Quantity" value={`${quantity} shares`} mono />
          <Row label="Est. Price" value={`$${estimatedPrice.toFixed(2)}`} mono />
          <Row label="Commission" value={`$${commission.toFixed(2)}`} mono />
          <div style={styles.divider} />
          <Row label="Total" value={`$${Math.abs(total).toFixed(2)}`} mono bold />
        </div>

        {type === 'Market' && (
          <div style={styles.warning}>
            Market orders execute at the best available price. Actual price may differ slightly.
          </div>
        )}

        <div style={styles.buttons}>
          <button style={styles.cancelBtn} onClick={onCancel}>Cancel</button>
          <button
            style={{ ...styles.confirmBtn, background: sideColor }}
            onClick={onConfirm}
          >
            Confirm {side}
          </button>
        </div>
      </div>
    </div>
  );
}

function Row({ label, value, mono, bold }: { label: string; value: string; mono?: boolean; bold?: boolean }) {
  return (
    <div style={styles.row}>
      <span style={styles.label}>{label}</span>
      <span className={mono ? 'mono' : ''} style={{
        fontSize: bold ? '16px' : '14px', fontWeight: bold ? 700 : 400,
        color: 'var(--text-primary)',
      }}>{value}</span>
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
    padding: '8px 24px', borderRadius: '6px', color: '#FFF', border: 'none',
    cursor: 'pointer', fontSize: '14px', fontWeight: 700, fontFamily: 'var(--font-ui)',
  },
};
