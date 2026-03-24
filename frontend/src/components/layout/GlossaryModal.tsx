import { useState, useMemo } from 'react';
import { Search, X } from 'lucide-react';

interface GlossaryEntry {
  term: string;
  category: string;
  definition: string;
}

const GLOSSARY: GlossaryEntry[] = [
  // Orders
  { term: 'Market Order', category: 'Orders', definition: 'An order to buy or sell immediately at the current market price. Guaranteed execution, but price may vary slightly (slippage).' },
  { term: 'Limit Order', category: 'Orders', definition: 'An order to buy or sell at a specific price or better. Only executes if the market reaches your price. May never fill.' },
  { term: 'Stop Order', category: 'Orders', definition: 'An order that triggers a market order when the price reaches a specified level. Used to limit losses (stop-loss) or enter positions.' },
  { term: 'Stop-Limit Order', category: 'Orders', definition: 'Combines a stop and a limit order. When the stop price is hit, a limit order is placed instead of a market order.' },
  { term: 'Trailing Stop', category: 'Orders', definition: 'A stop order that automatically adjusts as the price moves in your favor. Locks in profits while giving room for growth.' },
  { term: 'Bracket Order (OCO)', category: 'Orders', definition: 'A take-profit and stop-loss pair. One Cancels Other — when one fills, the other is automatically cancelled.' },
  { term: 'GTC (Good Till Cancelled)', category: 'Orders', definition: 'Order stays active until you cancel it or it fills. No expiration date.' },
  { term: 'Day Order', category: 'Orders', definition: 'Order expires at market close if not filled. Automatically cancelled at 4:00 PM.' },
  { term: 'Slippage', category: 'Orders', definition: 'The difference between the expected price and the actual fill price. More common with large orders or low-liquidity stocks.' },

  // Trading
  { term: 'Short Selling', category: 'Trading', definition: 'Selling borrowed shares to profit from a price decline. You sell first, buy back later. Unlimited loss potential.' },
  { term: 'Cover', category: 'Trading', definition: 'Buying back shares to close a short position. This is how you exit a short trade.' },
  { term: 'Margin Trading', category: 'Trading', definition: 'Borrowing money from the broker to trade. 2:1 leverage means you can buy $2 of stock for every $1 of cash.' },
  { term: 'Margin Call', category: 'Trading', definition: 'When your equity falls below the maintenance requirement (25%), the broker forcefully sells your positions to recover the loan.' },
  { term: 'Commission', category: 'Trading', definition: 'A fee charged per trade. Reduces your profit on every buy and sell.' },
  { term: 'Buying Power', category: 'Trading', definition: 'Total amount you can spend. With margin: Cash + available margin. Without margin: just Cash.' },

  // Analysis
  { term: 'P/E Ratio', category: 'Analysis', definition: 'Price to Earnings ratio. Stock price divided by annual earnings per share. Lower = potentially undervalued. Negative = company losing money.' },
  { term: 'Market Cap', category: 'Analysis', definition: 'Total market value of a company. Price x Shares Outstanding. Large cap >$10B, Mid cap $2-10B, Small cap <$2B.' },
  { term: 'Dividend Yield', category: 'Analysis', definition: 'Annual dividend payment as a percentage of stock price. 3% yield means $3 per year for every $100 invested.' },
  { term: 'SMA (Simple Moving Average)', category: 'Analysis', definition: 'Average closing price over N periods. SMA50 = 50-day average. Price above SMA = bullish, below = bearish.' },
  { term: 'RSI (Relative Strength Index)', category: 'Analysis', definition: 'Momentum indicator (0-100). Above 70 = overbought (may fall). Below 30 = oversold (may rise).' },
  { term: 'MACD', category: 'Analysis', definition: 'Trend-following momentum indicator. Signal line crossovers indicate buy/sell opportunities.' },
  { term: 'Bollinger Bands', category: 'Analysis', definition: 'Volatility bands around a moving average. Price touching upper band = overbought, lower band = oversold.' },
  { term: 'Fair Value', category: 'Analysis', definition: 'Estimated "true" price of a stock based on fundamentals. If price < fair value, stock may be undervalued.' },

  // Market
  { term: 'Bull Market', category: 'Market', definition: 'A market condition where prices are rising or expected to rise. Characterized by optimism and strong buying.' },
  { term: 'Bear Market', category: 'Market', definition: 'A market condition where prices are falling 20%+ from recent highs. Characterized by pessimism and selling.' },
  { term: 'Flash Crash', category: 'Market', definition: 'An extremely rapid market decline, usually caused by cascading sell orders. Can drop 5-10% in minutes.' },
  { term: 'Circuit Breaker', category: 'Market', definition: 'Automatic trading halt triggered by extreme market moves. Level 1: -7%, Level 2: -13%, Level 3: -20% (market closes).' },
  { term: 'IPO (Initial Public Offering)', category: 'Market', definition: 'When a private company first sells shares to the public. Often volatile in early trading.' },
  { term: 'Stock Split', category: 'Market', definition: 'Dividing existing shares into more shares at a lower price. 2:1 split = 2x shares at half the price. Total value unchanged.' },
  { term: 'Yield Curve', category: 'Market', definition: 'Graph of bond yields by maturity. Normal = upward slope. Inverted = short-term rates above long-term (recession signal).' },
  { term: 'Fear & Greed Index', category: 'Market', definition: 'Sentiment indicator (0-100). Below 25 = Extreme Fear (potential buying opportunity). Above 75 = Extreme Greed (potential sell signal).' },

  // Portfolio
  { term: 'Unrealized P&L', category: 'Portfolio', definition: 'Profit or loss on positions you still hold. Not "real" until you sell — the price could change.' },
  { term: 'Realized P&L', category: 'Portfolio', definition: 'Profit or loss on closed trades. This is your actual locked-in gain or loss.' },
  { term: 'Drawdown', category: 'Portfolio', definition: 'Peak-to-trough decline in portfolio value. Max drawdown measures your worst losing streak.' },
  { term: 'Sharpe Ratio', category: 'Portfolio', definition: 'Risk-adjusted return. Higher = better. Above 1.0 = good, above 2.0 = excellent. Measures return per unit of risk.' },
  { term: 'Win Rate', category: 'Portfolio', definition: 'Percentage of trades that were profitable. 50%+ is decent, but depends on average win vs average loss size.' },
  { term: 'Profit Factor', category: 'Portfolio', definition: 'Total gains divided by total losses. Above 1.0 = profitable. Above 1.5 = good. Above 2.0 = excellent.' },
  { term: 'VaR (Value at Risk)', category: 'Portfolio', definition: 'Estimated maximum loss over a time period at a confidence level. "95% VaR of $5,000" means 95% chance you won\'t lose more than $5,000 today.' },
  { term: 'Tax Loss Harvesting', category: 'Portfolio', definition: 'Selling losing positions to offset capital gains taxes. Short-term losses offset short-term gains first.' },

  // Economy
  { term: 'Interest Rate (Fed Rate)', category: 'Economy', definition: 'The rate set by the central bank. Higher rates = borrowing more expensive, hurts growth stocks and real estate.' },
  { term: 'Inflation', category: 'Economy', definition: 'Rate at which prices increase. Moderate inflation (2%) is healthy. High inflation erodes purchasing power and increases rates.' },
  { term: 'GDP Growth', category: 'Economy', definition: 'Gross Domestic Product growth rate. Positive = economy expanding. Negative for 2 quarters = recession.' },
  { term: 'PMI (Purchasing Managers Index)', category: 'Economy', definition: 'Manufacturing activity indicator. Above 50 = expansion. Below 50 = contraction.' },
];

interface Props {
  isOpen: boolean;
  onClose: () => void;
}

export function GlossaryModal({ isOpen, onClose }: Props) {
  const [search, setSearch] = useState('');
  const [category, setCategory] = useState<string>('all');

  const categories = useMemo(() => {
    const cats = [...new Set(GLOSSARY.map(g => g.category))];
    return ['all', ...cats];
  }, []);

  const filtered = useMemo(() => {
    return GLOSSARY.filter(g => {
      const matchSearch = !search || g.term.toLowerCase().includes(search.toLowerCase()) || g.definition.toLowerCase().includes(search.toLowerCase());
      const matchCat = category === 'all' || g.category === category;
      return matchSearch && matchCat;
    });
  }, [search, category]);

  if (!isOpen) return null;

  return (
    <div style={S.overlay} onClick={onClose}>
      <div style={S.modal} onClick={e => e.stopPropagation()}>
        <div style={S.header}>
          <h2 style={S.title}>Glossary</h2>
          <button style={S.closeBtn} onClick={onClose}><X size={18} /></button>
        </div>

        {/* Search + Categories */}
        <div style={S.controls}>
          <div style={S.searchBox}>
            <Search size={14} style={{ color: 'var(--text-disabled)', flexShrink: 0 }} />
            <input type="text" value={search} onChange={e => setSearch(e.target.value)}
              placeholder="Search terms..." style={S.searchInput} autoFocus />
          </div>
          <div style={S.categories}>
            {categories.map(c => (
              <button key={c} onClick={() => setCategory(c)} style={{
                ...S.catBtn,
                background: category === c ? 'var(--text-accent)' : 'var(--bg-tertiary)',
                color: category === c ? '#FFF' : 'var(--text-secondary)',
              }}>{c === 'all' ? 'All' : c}</button>
            ))}
          </div>
        </div>

        {/* Entries */}
        <div style={S.list}>
          {filtered.map(entry => (
            <div key={entry.term} style={S.entry}>
              <div style={S.entryHeader}>
                <span style={S.term}>{entry.term}</span>
                <span style={S.cat}>{entry.category}</span>
              </div>
              <p style={S.definition}>{entry.definition}</p>
            </div>
          ))}
          {filtered.length === 0 && (
            <div style={{ padding: '24px', textAlign: 'center', color: 'var(--text-disabled)' }}>
              No matches for "{search}"
            </div>
          )}
        </div>

        <div style={S.footer}>
          <span style={{ fontSize: '11px', color: 'var(--text-disabled)' }}>{GLOSSARY.length} terms | Press ? to open</span>
        </div>
      </div>
    </div>
  );
}

const S: Record<string, React.CSSProperties> = {
  overlay: {
    position: 'fixed', inset: 0, zIndex: 8000,
    background: 'rgba(0,0,0,0.6)', backdropFilter: 'blur(2px)',
    display: 'flex', alignItems: 'center', justifyContent: 'center',
  },
  modal: {
    width: '640px', maxHeight: '80vh', background: 'var(--bg-secondary)',
    border: '1px solid var(--border)', borderRadius: '12px',
    display: 'flex', flexDirection: 'column' as const, overflow: 'hidden',
  },
  header: {
    display: 'flex', justifyContent: 'space-between', alignItems: 'center',
    padding: '16px 20px', borderBottom: '1px solid var(--border)',
  },
  title: { fontSize: '18px', fontWeight: 700, color: 'var(--text-primary)', margin: 0 },
  closeBtn: {
    background: 'transparent', border: 'none', color: 'var(--text-secondary)',
    cursor: 'pointer', padding: '4px',
  },
  controls: { padding: '12px 20px', borderBottom: '1px solid var(--border)' },
  searchBox: {
    display: 'flex', alignItems: 'center', gap: '8px',
    background: 'var(--bg-input)', border: '1px solid var(--border)',
    borderRadius: '6px', padding: '8px 12px', marginBottom: '8px',
  },
  searchInput: {
    flex: 1, background: 'transparent', border: 'none',
    color: 'var(--text-primary)', fontSize: '14px',
    fontFamily: 'var(--font-ui)', outline: 'none',
  },
  categories: { display: 'flex', gap: '4px', flexWrap: 'wrap' as const },
  catBtn: {
    padding: '3px 10px', border: 'none', borderRadius: '4px', cursor: 'pointer',
    fontSize: '11px', fontWeight: 600, fontFamily: 'var(--font-ui)',
  },
  list: { flex: 1, overflowY: 'auto' as const, padding: '8px 20px' },
  entry: {
    padding: '12px 0', borderBottom: '1px solid rgba(31,41,55,0.3)',
  },
  entryHeader: { display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '4px' },
  term: { fontSize: '14px', fontWeight: 700, color: 'var(--text-accent)' },
  cat: {
    fontSize: '10px', fontWeight: 600, color: 'var(--text-disabled)',
    background: 'var(--bg-tertiary)', padding: '2px 6px', borderRadius: '3px',
  },
  definition: {
    fontSize: '13px', color: 'var(--text-secondary)', lineHeight: 1.5, margin: 0,
  },
  footer: { padding: '10px 20px', borderTop: '1px solid var(--border)', textAlign: 'center' as const },
};
