import { useMemo, useState } from 'react';
import { StockData } from '@/types/market';

interface ScreenerPreset {
  name: string;
  description: string;
  filter: (s: StockData) => boolean;
  category: 'price' | 'fundamental' | 'technical' | 'type';
}

const PRESETS: ScreenerPreset[] = [
  // Price Action
  { name: 'Top Gainers', description: 'Up > 2% today', category: 'price',
    filter: s => s.changePercent > 2 },
  { name: 'Top Losers', description: 'Down > 2% today', category: 'price',
    filter: s => s.changePercent < -2 },
  { name: 'New Highs', description: 'Near day high', category: 'price',
    filter: s => s.dayHigh ? s.price >= s.dayHigh * 0.99 : false },
  { name: 'New Lows', description: 'Near day low', category: 'price',
    filter: s => s.dayLow ? s.price <= s.dayLow * 1.01 : false },

  // Fundamentals
  { name: 'Value (Low P/E)', description: 'P/E < 15', category: 'fundamental',
    filter: s => (s.peRatio ?? 0) > 0 && (s.peRatio ?? 999) < 15 },
  { name: 'High Dividend', description: 'Yield > 3%', category: 'fundamental',
    filter: s => (s.dividendYield ?? 0) > 0.03 },
  { name: 'Blue Chips', description: 'Market cap > $50B', category: 'fundamental',
    filter: s => s.marketCap > 50_000_000_000 },
  { name: 'Penny Stocks', description: 'Price under $5', category: 'fundamental',
    filter: s => s.price < 5 && !s.traits?.includes('ETF') },

  // Technical / Volume
  { name: 'High Volume', description: 'Volume > 5M', category: 'technical',
    filter: s => s.volume > 5_000_000 },
  { name: 'Volatile', description: 'High volatility', category: 'technical',
    filter: s => s.traits?.includes('Volatile') || s.traits?.includes('Speculative') },
  { name: 'Growth', description: 'Growth stocks', category: 'technical',
    filter: s => s.traits?.includes('Growth Stock') || s.traits?.includes('Fast Grower') },
  { name: 'Defensive', description: 'Low volatility', category: 'technical',
    filter: s => s.traits?.includes('Defensive') || s.traits?.includes('Blue Chip') },

  // Type
  { name: 'ETFs', description: 'Exchange-Traded Funds', category: 'type',
    filter: s => s.traits?.includes('ETF') },
  { name: 'Dividend Payers', description: 'Stocks with dividends', category: 'type',
    filter: s => (s.dividendYield ?? 0) > 0 },
  { name: 'Debt Heavy', description: 'High leverage', category: 'type',
    filter: s => s.traits?.includes('Debt Heavy') },
  { name: 'Speculative', description: 'High risk stocks', category: 'type',
    filter: s => s.traits?.includes('Speculative') || s.traits?.includes('Penny Stock') },
];

interface StockScreenerProps {
  stocks: StockData[];
  onApplyFilter: (filterText: string) => void;
  onSelectStock: (symbol: string) => void;
}

export function StockScreener({ stocks, onSelectStock }: StockScreenerProps) {
  const [activeCategory, setActiveCategory] = useState<string | null>(null);

  const filteredPresets = useMemo(() => {
    const presets = activeCategory
      ? PRESETS.filter(p => p.category === activeCategory)
      : PRESETS;
    return presets.map(p => ({
      ...p,
      count: stocks.filter(p.filter).length,
      topStocks: stocks.filter(p.filter)
        .sort((a, b) => Math.abs(b.changePercent) - Math.abs(a.changePercent))
        .slice(0, 3),
    }));
  }, [stocks, activeCategory]);

  const categories = [
    { id: 'price', label: 'Price' },
    { id: 'fundamental', label: 'Fundamental' },
    { id: 'technical', label: 'Technical' },
    { id: 'type', label: 'Type' },
  ];

  return (
    <div style={styles.container}>
      <div style={styles.header}>
        <span style={styles.title}>Stock Screener</span>
        <div style={{ display: 'flex', gap: '4px' }}>
          <button
            onClick={() => setActiveCategory(null)}
            style={{
              ...styles.catBtn,
              background: !activeCategory ? 'var(--text-accent)' : 'var(--bg-tertiary)',
              color: !activeCategory ? 'var(--text-primary)' : 'var(--text-secondary)',
            }}
          >All</button>
          {categories.map(c => (
            <button
              key={c.id}
              onClick={() => setActiveCategory(activeCategory === c.id ? null : c.id)}
              style={{
                ...styles.catBtn,
                background: activeCategory === c.id ? 'var(--text-accent)' : 'var(--bg-tertiary)',
                color: activeCategory === c.id ? 'var(--text-primary)' : 'var(--text-secondary)',
              }}
            >{c.label}</button>
          ))}
        </div>
      </div>
      <div style={styles.grid}>
        {filteredPresets.map(preset => (
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
  header: { display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '8px' },
  title: { fontSize: '14px', fontWeight: 600, color: 'var(--text-primary)' },
  catBtn: {
    padding: '3px 10px', border: 'none', borderRadius: '4px', cursor: 'pointer',
    fontSize: '10px', fontWeight: 600, fontFamily: 'var(--font-ui)', letterSpacing: '0.5px',
  },
  grid: { display: 'grid', gridTemplateColumns: 'repeat(4, 1fr)', gap: '8px' },
  card: {
    background: 'var(--bg-secondary)', border: '1px solid var(--border)',
    borderRadius: '6px', padding: '10px', cursor: 'default',
  },
  cardHeader: { display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '2px' },
  presetName: { fontSize: '12px', fontWeight: 700, color: 'var(--text-primary)' },
  count: { fontSize: '11px', fontWeight: 700, color: 'var(--text-accent)', background: 'color-mix(in srgb, var(--text-accent) 10%, transparent)', padding: '1px 6px', borderRadius: '8px' },
  description: { fontSize: '10px', color: 'var(--text-disabled)', display: 'block', marginBottom: '6px' },
  preview: { display: 'flex', gap: '4px', flexWrap: 'wrap' as const },
  previewSymbol: {
    fontSize: '10px', fontWeight: 600, color: 'var(--text-accent)',
    background: 'var(--bg-tertiary)', padding: '1px 5px', borderRadius: '3px',
    cursor: 'pointer',
  },
  more: { fontSize: '10px', color: 'var(--text-disabled)' },
};
