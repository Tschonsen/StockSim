import { useMemo } from 'react';
import { StockData } from '@/types/market';

interface ScreenerPreset {
  name: string;
  description: string;
  filter: (s: StockData) => boolean;
}

const PRESETS: ScreenerPreset[] = [
  { name: 'Top Gainers', description: 'Stocks up > 2% today',
    filter: s => s.changePercent > 2 },
  { name: 'Top Losers', description: 'Stocks down > 2% today',
    filter: s => s.changePercent < -2 },
  { name: 'High Volume', description: 'Volume > 5M today',
    filter: s => s.volume > 5_000_000 },
  { name: 'Penny Stocks', description: 'Price under $5',
    filter: s => s.price < 5 },
  { name: 'Blue Chips', description: 'Market cap > $50B',
    filter: s => s.marketCap > 50_000_000_000 },
  { name: 'Dividend Stocks', description: 'Stocks with dividends',
    filter: s => s.traits?.includes('Dividend Aristocrat') || s.traits?.includes('Slow Grower') },
  { name: 'Growth Stocks', description: 'Growth & Fast Grower traits',
    filter: s => s.traits?.includes('Growth Stock') || s.traits?.includes('Fast Grower') },
  { name: 'Volatile', description: 'High volatility stocks',
    filter: s => s.traits?.includes('Volatile') || s.traits?.includes('Speculative') },
];

interface StockScreenerProps {
  stocks: StockData[];
  onApplyFilter: (filterText: string) => void;
  onSelectStock: (symbol: string) => void;
}

/**
 * Stock screener with preset filters. Bible 3.4.4.
 * New idea: quick-filter buttons that help discover stocks.
 */
export function StockScreener({ stocks, onSelectStock }: StockScreenerProps) {
  const presetResults = useMemo(() =>
    PRESETS.map(p => ({
      ...p,
      count: stocks.filter(p.filter).length,
      topStocks: stocks.filter(p.filter).slice(0, 3),
    })),
    [stocks]
  );

  return (
    <div style={styles.container}>
      <div style={styles.header}>
        <span style={styles.title}>Stock Screener</span>
        <span style={styles.subtitle}>{stocks.length} stocks</span>
      </div>
      <div style={styles.grid}>
        {presetResults.map(preset => (
          <div key={preset.name} style={styles.card}>
            <div style={styles.cardHeader}>
              <span style={styles.presetName}>{preset.name}</span>
              <span style={styles.count}>{preset.count}</span>
            </div>
            <span style={styles.description}>{preset.description}</span>
            {preset.topStocks.length > 0 && (
              <div style={styles.preview}>
                {preset.topStocks.map(s => (
                  <span
                    key={s.symbol}
                    className="mono"
                    style={styles.previewSymbol}
                    onClick={() => onSelectStock(s.symbol)}
                  >
                    {s.symbol}
                  </span>
                ))}
                {preset.count > 3 && (
                  <span style={styles.more}>+{preset.count - 3}</span>
                )}
              </div>
            )}
          </div>
        ))}
      </div>
    </div>
  );
}

const styles: Record<string, React.CSSProperties> = {
  container: { marginBottom: '20px' },
  header: { display: 'flex', justifyContent: 'space-between', alignItems: 'baseline', marginBottom: '8px' },
  title: { fontSize: '14px', fontWeight: 600, color: 'var(--text-primary)' },
  subtitle: { fontSize: '11px', color: 'var(--text-disabled)' },
  grid: { display: 'grid', gridTemplateColumns: 'repeat(4, 1fr)', gap: '8px' },
  card: {
    background: 'var(--bg-secondary)', border: '1px solid var(--border)',
    borderRadius: '6px', padding: '10px', cursor: 'default',
  },
  cardHeader: { display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '2px' },
  presetName: { fontSize: '12px', fontWeight: 700, color: 'var(--text-primary)' },
  count: { fontSize: '11px', fontWeight: 700, color: 'var(--text-accent)', background: 'rgba(96,165,250,0.1)', padding: '1px 6px', borderRadius: '8px' },
  description: { fontSize: '10px', color: 'var(--text-disabled)', display: 'block', marginBottom: '6px' },
  preview: { display: 'flex', gap: '4px', flexWrap: 'wrap' },
  previewSymbol: {
    fontSize: '10px', fontWeight: 600, color: 'var(--text-accent)',
    background: 'var(--bg-tertiary)', padding: '1px 5px', borderRadius: '3px',
    cursor: 'pointer',
  },
  more: { fontSize: '10px', color: 'var(--text-disabled)' },
};
