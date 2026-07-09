import { useState, useEffect } from 'react';
import { useMarketStore } from '@/stores/marketStore';

/**
 * Scrolling ticker at the bottom of the screen.
 * Two modes: NEWS (headlines) and PRICES (scrolling stock prices like Bloomberg TV).
 * Spec 3.6 & 13.1.
 */
export function NewsTicker() {
  const newsItems = useMarketStore((s) => s.newsItems);
  const stockList = useMarketStore((s) => s.stockList);
  const selectStock = useMarketStore((s) => s.selectStock);
  const priceFlash = useMarketStore((s) => s.priceFlash);
  const economicData = useMarketStore((s) => s.economicData);
  const [mode, setMode] = useState<'news' | 'prices'>('news');

  // Snapshot the headlines periodically instead of reacting to every incoming item — otherwise
  // each new news item changed the list/duration and restarted the scroll animation (banner reset).
  const [tickerNews, setTickerNews] = useState(() => newsItems.slice(0, 20));
  useEffect(() => {
    const snap = () => setTickerNews(useMarketStore.getState().newsItems.slice(0, 20));
    snap();
    const id = setInterval(snap, 25_000);
    return () => clearInterval(id);
  }, []);

  return (
    <footer style={styles.ticker}>
      {/* Mode Toggle */}
      <button
        onClick={() => setMode(m => m === 'news' ? 'prices' : 'news')}
        style={{
          ...styles.badge,
          background: mode === 'news' ? 'var(--bg-tertiary)' : 'color-mix(in srgb, var(--text-accent) 15%, transparent)',
          color: mode === 'news' ? 'var(--text-secondary)' : 'var(--text-accent)',
          cursor: 'pointer', border: 'none',
        }}
      >
        {mode === 'news' ? 'NEWS' : 'TAPE'}
      </button>

      <div style={styles.scrollArea}>
        {mode === 'news' ? (
          /* News Mode */
          tickerNews.length > 0 ? (
            <div style={{
              ...styles.scrollContent,
              animationDuration: `${Math.max(30, tickerNews.length * 8)}s`,
            }}>
              {tickerNews.map((item) => {
                const isRumor = item.type === 'Rumor';
                const color = isRumor
                  ? 'var(--text-accent)'
                  : item.sentiment > 0.1
                    ? 'var(--green-primary)'
                    : item.sentiment < -0.1
                      ? 'var(--red-primary)'
                      : 'var(--text-secondary)';
                const symbol = item.affectedSymbols[0];
                return (
                  <span key={item.id} className="news-slide-in" style={styles.tickerItem}>
                    {isRumor ? (
                      <span style={styles.rumorBadge}>RUMOR</span>
                    ) : item.tier && item.tier >= 3 ? (
                      <span style={{
                        fontSize: '8px', fontWeight: 800, padding: '0 4px', borderRadius: '2px',
                        background: item.tier >= 4 ? 'color-mix(in srgb, var(--red-primary) 30%, transparent)' : 'color-mix(in srgb, var(--warning) 30%, transparent)',
                        color: item.tier >= 4 ? '#FF4444' : 'var(--warning)',
                        marginRight: '3px', letterSpacing: '0.5px',
                      }}>{item.tier >= 4 ? 'BLACK SWAN' : 'CRISIS'}</span>
                    ) : (
                      <span style={{
                        ...styles.severityDot,
                        background: item.severity === 'Major' ? 'var(--red-primary)' :
                                    item.severity === 'Moderate' ? 'var(--warning)' : 'var(--text-disabled)',
                      }} />
                    )}
                    <span
                      className={item.severity === 'Major' && !isRumor ? 'pulse' : ''}
                      style={{
                        ...styles.headline, color, cursor: symbol ? 'pointer' : 'default',
                        textShadow: isRumor
                          ? '0 0 6px var(--accent-glow)'
                          : item.severity === 'Major'
                            ? `0 0 8px ${item.sentiment > 0 ? 'var(--green-glow)' : 'var(--red-glow)'}`
                            : 'none',
                      }}
                      onClick={() => symbol && selectStock(symbol)}
                    >
                      {item.headline}
                    </span>
                    <span style={styles.separator}>|</span>
                  </span>
                );
              })}
            </div>
          ) : (
            <span style={styles.emptyText}>Waiting for market events...</span>
          )
        ) : (
          /* Price Ticker Mode — Bloomberg TV style */
          <div style={styles.scrollContent}>
            {/* Commodity prices first */}
            {economicData?.indicators && (
              <>
                {[
                  { label: 'GOLD', value: economicData.indicators.goldPrice, fmt: '$' + economicData.indicators.goldPrice.toFixed(0), color: 'var(--warning)' },
                  { label: 'OIL', value: economicData.indicators.oilPrice, fmt: '$' + economicData.indicators.oilPrice.toFixed(2), color: 'var(--text-primary)' },
                  { label: 'RATE', value: economicData.indicators.interestRate, fmt: economicData.indicators.interestRate.toFixed(2) + '%', color: 'var(--chart-cyan)' },
                  { label: 'F&G', value: economicData.fearGreedIndex, fmt: economicData.fearGreedIndex.toFixed(0), color: economicData.fearGreedIndex < 30 ? 'var(--red-primary)' : economicData.fearGreedIndex > 70 ? 'var(--green-primary)' : 'var(--text-primary)' },
                ].map(c => (
                  <span key={c.label} style={styles.tickerItem}>
                    <span className="mono" style={{ fontSize: '10px', fontWeight: 600, color: 'var(--text-disabled)' }}>{c.label}</span>
                    <span className="mono" style={{ fontSize: '12px', fontWeight: 700, color: c.color }}>{c.fmt}</span>
                    <span style={styles.separator}>|</span>
                  </span>
                ))}
              </>
            )}
            {stockList
              .filter(s => s.volume > 100_000)
              .sort((a, b) => Math.abs(b.changePercent) - Math.abs(a.changePercent))
              .slice(0, 40)
              .map((s, i) => {
                const flash = priceFlash.get(s.symbol);
                return (
                  <span key={s.symbol} style={styles.tickerItem}>
                    <span
                      className="mono"
                      style={{ fontSize: '12px', fontWeight: 700, color: 'var(--text-primary)', cursor: 'pointer' }}
                      onClick={() => selectStock(s.symbol)}
                    >
                      {s.symbol}
                    </span>
                    <span className={`mono ${flash === 'up' ? 'price-up' : flash === 'down' ? 'price-down' : ''}`}
                      style={{ fontSize: '12px', color: 'var(--text-primary)' }}>
                      ${s.price.toFixed(2)}
                    </span>
                    <span className="mono" style={{
                      fontSize: '11px', fontWeight: 600,
                      color: s.changePercent >= 0 ? 'var(--green-primary)' : 'var(--red-primary)',
                    }}>
                      {s.changePercent >= 0 ? '+' : ''}{s.changePercent.toFixed(2)}%
                    </span>
                    {i < 39 && <span style={styles.separator}>|</span>}
                  </span>
                );
              })}
          </div>
        )}
      </div>
    </footer>
  );
}

const styles: Record<string, React.CSSProperties> = {
  ticker: {
    height: 'var(--ticker-height)',
    background: 'var(--bg-darkest)',
    borderTop: '1px solid var(--border)',
    display: 'flex',
    alignItems: 'center',
    padding: '0 var(--space-3)',
    gap: 'var(--space-2)',
    flexShrink: 0,
  },
  badge: {
    fontSize: '10px',
    fontWeight: 600,
    padding: '2px 6px',
    borderRadius: '3px',
    flexShrink: 0,
    letterSpacing: '0.5px',
    fontFamily: 'var(--font-mono)',
  },
  scrollArea: {
    flex: 1,
    overflow: 'hidden',
  },
  scrollContent: {
    display: 'inline-flex',
    alignItems: 'center',
    gap: '0',
    whiteSpace: 'nowrap' as const,
    animation: 'tickerScroll var(--ticker-speed, 60s) linear infinite',
    paddingLeft: '100%',
  },
  tickerItem: {
    display: 'inline-flex',
    alignItems: 'center',
    gap: '6px',
    flexShrink: 0,
  },
  severityDot: {
    width: '6px',
    height: '6px',
    borderRadius: '50%',
    flexShrink: 0,
  },
  headline: {
    fontFamily: 'var(--font-mono)',
    fontSize: '12px',
    whiteSpace: 'nowrap' as const,
  },
  separator: {
    color: 'var(--text-disabled)',
    margin: '0 12px',
    fontSize: '12px',
  },
  emptyText: {
    fontFamily: 'var(--font-mono)',
    fontSize: '12px',
    color: 'var(--text-disabled)',
    whiteSpace: 'nowrap' as const,
  },
  rumorBadge: {
    fontSize: '9px',
    fontWeight: 700,
    padding: '1px 4px',
    borderRadius: '3px',
    background: 'color-mix(in srgb, var(--text-accent) 15%, transparent)',
    color: 'var(--text-accent)',
    border: '1px solid var(--accent-glow)',
    flexShrink: 0,
    letterSpacing: '0.5px',
    fontFamily: 'var(--font-mono)',
  },
};
