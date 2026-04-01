import { useState, useMemo } from 'react';
import { useMarketStore } from '@/stores/marketStore';
import { StockScreener } from '@/components/trading/StockScreener';
import { StockData } from '@/types/market';
import { WebSocketClient } from '@/services/websocket';
import { Plus, ChevronUp, ChevronDown } from 'lucide-react';
import { styles } from '@/styles/centralStyles';

interface MarketTabProps {
  wsClient: WebSocketClient;
}

export function MarketTab({ wsClient: _wsClient }: MarketTabProps) {
  const stockList = useMarketStore((s) => s.stockList);
  const selectStock = useMarketStore((s) => s.selectStock);
  const addToWatchlist = useMarketStore((s) => s.addToWatchlist);
  const watchlist = useMarketStore((s) => s.watchlist);
  const priceFlash = useMarketStore((s) => s.priceFlash);

  const [sortField, setSortField] = useState<keyof StockData>('symbol');
  const [sortDir, setSortDir] = useState<'asc' | 'desc'>('asc');
  const [filterText, setFilterText] = useState('');
  const [marketRowsVisible, setMarketRowsVisible] = useState(50);

  // Sector data for sector detail banner
  const sectorData = useMemo(() => {
    const sectors = new Map<string, { count: number; totalChange: number; totalCap: number }>();
    stockList.forEach(s => {
      const existing = sectors.get(s.sector) || { count: 0, totalChange: 0, totalCap: 0 };
      existing.count++;
      existing.totalChange += s.changePercent;
      existing.totalCap += s.marketCap;
      sectors.set(s.sector, existing);
    });
    return Array.from(sectors.entries()).map(([name, data]) => ({
      name,
      avgChange: data.count > 0 ? data.totalChange / data.count : 0,
      count: data.count,
      totalCap: data.totalCap,
    })).sort((a, b) => b.avgChange - a.avgChange);
  }, [stockList]);

  const sortedStocks = useMemo(() => {
    let list = [...stockList];
    if (filterText) {
      const q = filterText.toLowerCase();
      list = list.filter(s =>
        s.symbol.toLowerCase().includes(q) ||
        s.name.toLowerCase().includes(q) ||
        s.sector.toLowerCase().includes(q)
      );
    }
    list.sort((a, b) => {
      const aVal = a[sortField];
      const bVal = b[sortField];
      if (typeof aVal === 'string' && typeof bVal === 'string') {
        return sortDir === 'asc' ? aVal.localeCompare(bVal) : bVal.localeCompare(aVal);
      }
      if (typeof aVal === 'number' && typeof bVal === 'number') {
        return sortDir === 'asc' ? aVal - bVal : bVal - aVal;
      }
      return 0;
    });
    return list;
  }, [stockList, sortField, sortDir, filterText]);

  const handleSort = (field: keyof StockData) => {
    if (sortField === field) {
      setSortDir(d => d === 'asc' ? 'desc' : 'asc');
    } else {
      setSortField(field);
      setSortDir(field === 'symbol' || field === 'name' ? 'asc' : 'desc');
    }
  };

  const SortIcon = ({ field }: { field: keyof StockData }) => {
    if (sortField !== field) return null;
    return sortDir === 'asc'
      ? <ChevronUp size={12} style={{ marginLeft: '2px' }} />
      : <ChevronDown size={12} style={{ marginLeft: '2px' }} />;
  };

  return (
    <div style={styles.content}>
      {/* Stock Screener */}
      <StockScreener
        stocks={stockList}
        onApplyFilter={setFilterText}
        onSelectStock={selectStock}
      />

      {/* Sector Detail Banner (when filtered by sector) */}
      {filterText && sectorData.find(s => s.name.toLowerCase() === filterText.toLowerCase()) && (() => {
        const sector = sectorData.find(s => s.name.toLowerCase() === filterText.toLowerCase())!;
        const sectorStocks = stockList.filter(s => s.sector.toLowerCase() === filterText.toLowerCase());
        const bestStock = [...sectorStocks].sort((a, b) => b.changePercent - a.changePercent)[0];
        const worstStock = [...sectorStocks].sort((a, b) => a.changePercent - b.changePercent)[0];
        const avgPE = sectorStocks.filter(s => (s.peRatio ?? 0) > 0).reduce((s, st) => s + (st.peRatio ?? 0), 0) / Math.max(1, sectorStocks.filter(s => (s.peRatio ?? 0) > 0).length);
        const totalMcap = sectorStocks.reduce((s, st) => s + st.marketCap, 0);
        // Find sector ETF
        const etfMap: Record<string, string> = { Technology: 'STEC', Energy: 'SENG', Financials: 'SFIN', Healthcare: 'SHLT', 'Consumer Goods': 'SCON', Industrials: 'SIND', Materials: 'SMAT', 'Real Estate': 'SREL', Telecommunications: 'STEL', Utilities: 'SUTL', 'Luxury Goods': 'SLUX', Transportation: 'STRN' };
        const etfSymbol = etfMap[sector.name];
        return (
          <div style={{
            background: 'var(--bg-secondary)', border: '1px solid var(--border)',
            borderRadius: '8px', padding: '12px 16px', marginBottom: '12px',
          }}>
            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '8px' }}>
              <h3 style={{ fontSize: '16px', fontWeight: 700, color: 'var(--text-primary)', margin: 0 }}>{sector.name}</h3>
              <div style={{ display: 'flex', gap: '8px' }}>
                <span className="mono" style={{
                  fontSize: '14px', fontWeight: 700,
                  color: sector.avgChange >= 0 ? 'var(--green-primary)' : 'var(--red-primary)',
                }}>{sector.avgChange >= 0 ? '+' : ''}{sector.avgChange.toFixed(2)}%</span>
                {etfSymbol && (
                  <button onClick={() => selectStock(etfSymbol)} style={{
                    background: 'rgba(96,165,250,0.1)', border: '1px solid rgba(96,165,250,0.3)',
                    borderRadius: '4px', padding: '2px 8px', cursor: 'pointer',
                    fontSize: '11px', fontWeight: 700, color: 'var(--text-accent)', fontFamily: 'var(--font-mono)',
                  }}>{etfSymbol}</button>
                )}
              </div>
            </div>
            <div style={{ display: 'flex', gap: '16px', fontSize: '11px', color: 'var(--text-secondary)' }}>
              <span>{sector.count} stocks</span>
              <span>Mkt Cap: ${totalMcap >= 1e12 ? `${(totalMcap / 1e12).toFixed(1)}T` : `${(totalMcap / 1e9).toFixed(0)}B`}</span>
              <span>Avg P/E: {avgPE > 0 ? avgPE.toFixed(1) : 'N/A'}</span>
              {bestStock && <span>Best: <span className="mono" style={{ color: 'var(--green-primary)' }}>{bestStock.symbol} +{bestStock.changePercent.toFixed(1)}%</span></span>}
              {worstStock && <span>Worst: <span className="mono" style={{ color: 'var(--red-primary)' }}>{worstStock.symbol} {worstStock.changePercent.toFixed(1)}%</span></span>}
            </div>
          </div>
        );
      })()}

      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 'var(--space-3)' }}>
        <h2 style={{ ...styles.heading, marginBottom: 0 }}>
          Market ({filterText ? `${sortedStocks.length} / ` : ''}{stockList.length} stocks)
        </h2>
        <input
          type="text"
          value={filterText}
          onChange={(e) => setFilterText(e.target.value)}
          placeholder="Filter by symbol, name, sector..."
          style={{
            width: '280px',
            height: '32px',
            background: 'var(--bg-input)',
            color: 'var(--text-primary)',
            border: '1px solid var(--border)',
            borderRadius: '4px',
            padding: '0 10px',
            fontFamily: 'var(--font-ui)',
            fontSize: '13px',
          }}
        />
      </div>
      <div style={styles.tableContainer}>
        <table style={styles.table}>
          <thead>
            <tr>
              <th style={{ ...styles.th, ...styles.sortableTh }} onClick={() => handleSort('symbol')}>Symbol<SortIcon field="symbol" /></th>
              <th style={{ ...styles.th, ...styles.sortableTh }} onClick={() => handleSort('name')}>Name<SortIcon field="name" /></th>
              <th style={{ ...styles.th, ...styles.sortableTh }} onClick={() => handleSort('sector')}>Sector<SortIcon field="sector" /></th>
              <th style={{ ...styles.th, ...styles.sortableTh, textAlign: 'right' }} onClick={() => handleSort('price')}>Price<SortIcon field="price" /></th>
              <th style={{ ...styles.th, ...styles.sortableTh, textAlign: 'right' }} onClick={() => handleSort('changePercent')}>Change<SortIcon field="changePercent" /></th>
              <th style={{ ...styles.th, ...styles.sortableTh, textAlign: 'right' }} onClick={() => handleSort('volume')}>Volume<SortIcon field="volume" /></th>
              <th style={{ ...styles.th, textAlign: 'center', width: '40px' }}></th>
            </tr>
          </thead>
          <tbody>
            {sortedStocks.length === 0 && (
              <tr><td colSpan={7} style={{ padding: '20px', textAlign: 'center', color: 'var(--text-disabled)' }}>
                No stocks match your filter.
              </td></tr>
            )}
            {sortedStocks.slice(0, marketRowsVisible).map((s) => (
              <tr
                key={s.symbol}
                style={styles.tr}
                onClick={() => selectStock(s.symbol)}
                onMouseEnter={(e) => (e.currentTarget.style.background = 'var(--bg-tertiary)')}
                onMouseLeave={(e) => (e.currentTarget.style.background = 'transparent')}
              >
                <td className="mono" style={{ ...styles.td, fontWeight: 700 }}>
                  {s.symbol}
                  {s.traits?.includes('ETF') && (
                    <span style={{
                      fontSize: '8px', fontWeight: 700, marginLeft: '4px', padding: '0 3px',
                      borderRadius: '2px', background: 'rgba(139,92,246,0.15)', color: 'var(--chart-purple)',
                      verticalAlign: 'super',
                    }}>{s.traits.includes('Commodity ETF') ? 'CMDTY' : 'ETF'}</span>
                  )}
                  {s.isSSR && <span style={{
                    fontSize: '8px', fontWeight: 700, marginLeft: '4px', padding: '0 3px',
                    borderRadius: '2px', background: 'rgba(245,158,11,0.15)', color: 'var(--warning)',
                    verticalAlign: 'super',
                  }}>SSR</span>}
                </td>
                <td style={styles.td}>{s.name}</td>
                <td style={{ ...styles.td, color: 'var(--text-secondary)', fontSize: '12px' }}>{s.sector}</td>
                <td key={`${s.symbol}-${s.price}`}
                  className={`mono ${priceFlash.get(s.symbol) === 'up' ? 'price-up' : priceFlash.get(s.symbol) === 'down' ? 'price-down' : ''}`}
                  style={{ ...styles.td, textAlign: 'right' }}>${s.price.toFixed(2)}</td>
                <td className={`mono ${s.changePercent >= 0 ? 'positive' : 'negative'}`} style={{
                  ...styles.td, textAlign: 'right',
                }}>
                  {s.changePercent >= 0 ? '+' : ''}{s.changePercent.toFixed(2)}%
                </td>
                <td className="mono" style={{ ...styles.td, textAlign: 'right', color: 'var(--text-secondary)' }}>
                  {s.volume >= 1_000_000 ? `${(s.volume / 1_000_000).toFixed(1)}M` :
                   s.volume >= 1_000 ? `${(s.volume / 1_000).toFixed(1)}K` :
                   s.volume.toString()}
                </td>
                <td style={{ ...styles.td, textAlign: 'center' }}>
                  {!watchlist.includes(s.symbol) && (
                    <button
                      style={styles.addBtn}
                      onClick={(e) => { e.stopPropagation(); addToWatchlist(s.symbol); }}
                      title="Add to Watchlist"
                    >
                      <Plus size={14} />
                    </button>
                  )}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
        {sortedStocks.length > marketRowsVisible && (
          <div style={{ textAlign: 'center', padding: '12px' }}>
            <button
              onClick={() => setMarketRowsVisible(v => Math.min(v + 50, sortedStocks.length))}
              style={{
                padding: '8px 24px', border: '1px solid var(--border)', borderRadius: '4px',
                background: 'var(--bg-tertiary)', color: 'var(--text-secondary)',
                cursor: 'pointer', fontSize: '12px', fontFamily: 'var(--font-ui)',
              }}
            >
              Show More ({sortedStocks.length - marketRowsVisible} remaining)
            </button>
          </div>
        )}
      </div>
    </div>
  );
}
