/**
 * Keyboard shortcuts overlay. Shown with ? key. Bible 18.
 */
interface ShortcutsHelpProps {
  isOpen: boolean;
  onClose: () => void;
}

const SHORTCUTS = [
  { category: 'Time Control', items: [
    { key: 'Space', action: 'Pause / Resume' },
    { key: '1', action: '1x Speed' },
    { key: '2', action: '2x Speed' },
    { key: '3', action: '5x Speed' },
    { key: '4', action: '10x Speed' },
    { key: '+ / →', action: 'Faster' },
    { key: '- / ←', action: 'Slower' },
  ]},
  { category: 'Navigation', items: [
    { key: 'D', action: 'Dashboard' },
    { key: 'P', action: 'Portfolio' },
    { key: 'M', action: 'Market' },
    { key: 'O', action: 'Orders' },
    { key: 'X', action: 'Options' },
    { key: 'N', action: 'News' },
    { key: 'A', action: 'Analytics' },
    { key: 'J', action: 'Journal' },
  ]},
  { category: 'Trading', items: [
    { key: 'B', action: 'Quick Buy' },
    { key: 'S', action: 'Quick Sell' },
    { key: 'H', action: 'Quick Short' },
    { key: 'C', action: 'Quick Cover' },
    { key: 'Escape', action: 'Menu / Close' },
  ]},
  { category: 'System', items: [
    { key: 'Ctrl+S', action: 'Quick Save' },
    { key: 'Ctrl+K', action: 'Search (Command Bar)' },
    { key: 'Ctrl+W', action: 'Open Wiki' },
    { key: '?', action: 'This help' },
  ]},
];

export function ShortcutsHelp({ isOpen, onClose }: ShortcutsHelpProps) {
  if (!isOpen) return null;

  return (
    <div style={styles.overlay} onClick={onClose}>
      <div style={styles.modal} onClick={e => e.stopPropagation()}>
        <div style={styles.header}>
          <span style={styles.title}>Keyboard Shortcuts</span>
          <button style={styles.closeBtn} onClick={onClose} aria-label="Close shortcuts help">x</button>
        </div>
        <div style={styles.body}>
          {SHORTCUTS.map(cat => (
            <div key={cat.category} style={styles.category}>
              <h4 style={styles.catTitle}>{cat.category}</h4>
              {cat.items.map(item => (
                <div key={item.key} style={styles.row}>
                  <kbd style={styles.key}>{item.key}</kbd>
                  <span style={styles.action}>{item.action}</span>
                </div>
              ))}
            </div>
          ))}
        </div>
      </div>
    </div>
  );
}

const styles: Record<string, React.CSSProperties> = {
  overlay: {
    position: 'fixed', inset: 0, background: 'rgba(0,0,0,0.6)',
    display: 'flex', alignItems: 'center', justifyContent: 'center', zIndex: 5000,
  },
  modal: {
    width: '500px', maxHeight: '500px', background: 'var(--bg-secondary)',
    border: '1px solid var(--border)', borderRadius: '10px', overflow: 'hidden',
  },
  header: {
    display: 'flex', justifyContent: 'space-between', alignItems: 'center',
    padding: '16px 20px', borderBottom: '1px solid var(--border)',
  },
  title: { fontSize: '16px', fontWeight: 700, color: 'var(--text-primary)' },
  closeBtn: { background: 'transparent', border: 'none', color: 'var(--text-secondary)', fontSize: '18px', cursor: 'pointer' },
  body: { padding: '16px 20px', display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '16px', overflowY: 'auto', maxHeight: '400px' },
  category: {},
  catTitle: { fontSize: '11px', fontWeight: 700, color: 'var(--text-accent)', textTransform: 'uppercase' as const, marginBottom: '6px' },
  row: { display: 'flex', justifyContent: 'space-between', alignItems: 'center', padding: '3px 0' },
  key: {
    background: 'var(--bg-tertiary)', color: 'var(--text-primary)',
    padding: '2px 8px', borderRadius: '4px', fontSize: '12px',
    fontFamily: 'var(--font-mono)', border: '1px solid var(--border)',
    minWidth: '40px', textAlign: 'center' as const,
  },
  action: { fontSize: '13px', color: 'var(--text-secondary)' },
};
