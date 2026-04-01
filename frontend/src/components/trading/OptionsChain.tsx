import { useState, useEffect } from 'react';
import { useMarketStore } from '@/stores/marketStore';
import { WebSocketClient } from '@/services/websocket';
import { ArrowLeft, TrendingUp, TrendingDown } from 'lucide-react';

interface OptionData {
  id: number;
  type: string;
  strike: number;
  expiry: string;
  theo: number;
  bid: number;
  ask: number;
  last: number;
  iv: number;
  delta: number;
  gamma: number;
  theta: number;
  vega: number;
  volume: number;
  openInterest: number;
  itm: boolean;
}

interface SliceData {
  expirationDate: string;
  daysToExpiry: number;
  strikes: number[];
  calls: OptionData[];
  puts: OptionData[];
}

interface PositionData {
  contractId: number;
  type: string;
  strike: number;
  expiry: string;
  quantity: number;
  avgCost: number;
}

interface ChainData {
  symbol: string;
  expirations: string[];
  slices: SliceData[];
  positions?: PositionData[];
  noChain?: boolean;
}

interface Props {
  wsClient: WebSocketClient;
}

export function OptionsChain({ wsClient }: Props) {
  const selectedSymbol = useMarketStore(s => s.selectedSymbol);
  const stocks = useMarketStore(s => s.stocks);
  const [viewSymbol, setViewSymbol] = useState<string | null>(null);
  const [chain, setChain] = useState<ChainData | null>(null);
  const [selectedExpiry, setSelectedExpiry] = useState(0);
  const [orderMsg, setOrderMsg] = useState<{ text: string; ok: boolean } | null>(null);
  const [orderQty, setOrderQty] = useState(1);

  // Use viewSymbol (from chip click) or selectedSymbol (from watchlist)
  const activeSymbol = viewSymbol || selectedSymbol;

  useEffect(() => {
    if (!activeSymbol) return;
    setChain(null);
    wsClient.send('GetOptionsChain', { Symbol: activeSymbol });

    const unsub = wsClient.on('OptionsChain', (payload) => {
      const data = payload as ChainData;
      if (data.symbol === activeSymbol) {
        setChain(data);
        setSelectedExpiry(0);
      }
    });

    const unsub2 = wsClient.on('OptionOrderResult', (payload) => {
      const r = payload as { success: boolean; message: string };
      setOrderMsg({ text: r.message, ok: r.success });
      setTimeout(() => setOrderMsg(null), 4000);
      wsClient.send('GetOptionsChain', { Symbol: activeSymbol });
    });

    return () => { unsub(); unsub2(); };
  }, [activeSymbol, wsClient]);

  // Optionable stocks
  const optionableStocks = Array.from(stocks.values())
    .filter(s => !s.traits?.includes('ETF') && s.marketCap > 1_000_000_000)
    .sort((a, b) => b.marketCap - a.marketCap)
    .slice(0, 20);

  // === STOCK PICKER VIEW ===
  if (!activeSymbol || (chain?.noChain && !viewSymbol)) {
    return (
      <div style={S.pickerWrap}>
        <div style={S.pickerTitle}>Options Trading</div>
        <p style={S.pickerDesc}>
          Trade call and put options on large-cap stocks. Options let you profit from price moves
          with limited risk — or hedge your existing positions.
        </p>
        {optionableStocks.length > 0 ? (
          <>
            <p style={S.sectionLabel}>Available for options trading ({optionableStocks.length})</p>
            <div style={S.stockGrid}>
              {optionableStocks.map(s => {
                const up = s.changePercent >= 0;
                return (
                  <button key={s.symbol} style={S.stockCard} onClick={() => setViewSymbol(s.symbol)}>
                    <div style={S.stockCardTop}>
                      <span style={S.stockSymbol}>{s.symbol}</span>
                      <span style={{ ...S.stockChange, color: up ? 'var(--green-primary)' : 'var(--red-primary)' }}>
                        {up ? '+' : ''}{s.changePercent.toFixed(2)}%
                      </span>
                    </div>
                    <div style={S.stockCardBottom}>
                      <span style={S.stockPrice}>${s.price.toFixed(2)}</span>
                      <span style={S.stockMcap}>{s.marketCap > 1e9 ? (s.marketCap/1e9).toFixed(1) + 'B' : (s.marketCap/1e6).toFixed(0) + 'M'}</span>
                    </div>
                    <div style={S.stockName}>{s.name}</div>
                  </button>
                );
              })}
            </div>
          </>
        ) : (
          <p style={S.pickerDesc}>No stocks eligible for options yet. Stocks need Market Cap {'>'} $1B.</p>
        )}
      </div>
    );
  }

  // === NO CHAIN FOR THIS STOCK ===
  if (chain?.noChain) {
    const stock = stocks.get(activeSymbol);
    const mcap = stock?.marketCap ?? 0;
    return (
      <div style={S.pickerWrap}>
        <button style={S.backBtn} onClick={() => setViewSymbol(null)}>
          <ArrowLeft size={14} /> Back to stock picker
        </button>
        <div style={{ ...S.pickerTitle, marginTop: 12 }}>No Options for {activeSymbol}</div>
        <p style={S.pickerDesc}>
          Market cap ${mcap > 1e9 ? (mcap/1e9).toFixed(1) + 'B' : (mcap/1e6).toFixed(0) + 'M'} is
          below the $1B threshold. Try a larger stock.
        </p>
        <p style={S.sectionLabel}>Available stocks:</p>
        <div style={S.stockGrid}>
          {optionableStocks.slice(0, 8).map(s => (
            <button key={s.symbol} style={S.stockCard} onClick={() => setViewSymbol(s.symbol)}>
              <div style={S.stockCardTop}>
                <span style={S.stockSymbol}>{s.symbol}</span>
                <span style={S.stockPrice}>${s.price.toFixed(2)}</span>
              </div>
            </button>
          ))}
        </div>
      </div>
    );
  }

  // === LOADING ===
  if (!chain) {
    return (
      <div style={S.pickerWrap}>
        {viewSymbol && (
          <button style={S.backBtn} onClick={() => setViewSymbol(null)}>
            <ArrowLeft size={14} /> Back
          </button>
        )}
        <div style={S.loading}>Loading options for {activeSymbol}...</div>
      </div>
    );
  }

  // === CHAIN VIEW ===
  const slice = chain.slices[selectedExpiry];
  if (!slice) return <div style={S.loading}>Loading...</div>;

  const stock = stocks.get(activeSymbol);
  const stockPrice = stock?.price ?? 0;
  const stockChange = stock?.changePercent ?? 0;
  const up = stockChange >= 0;

  const callMap = new Map(slice.calls.map(c => [c.strike, c]));
  const putMap = new Map(slice.puts.map(p => [p.strike, p]));

  // Map positions by contractId for highlighting
  const positionMap = new Map<number, PositionData>();
  chain.positions?.forEach(p => positionMap.set(p.contractId, p));
  const hasPositions = (chain.positions?.length ?? 0) > 0;

  const handleBuy = (contractId: number) => {
    wsClient.send('BuyOption', { ContractId: contractId, Symbol: activeSymbol, Quantity: orderQty });
  };
  const handleSell = (contractId: number) => {
    wsClient.send('SellOption', { ContractId: contractId, Symbol: activeSymbol, Quantity: orderQty });
  };

  const fmtNum = (n: number | undefined, d: number) => n != null ? n.toFixed(d) : '-';

  return (
    <div style={S.container}>
      {/* Header */}
      <div style={S.header}>
        <button style={S.backBtn} onClick={() => setViewSymbol(null)}>
          <ArrowLeft size={14} />
        </button>
        <div style={S.headerInfo}>
          <span style={S.headerSymbol}>{activeSymbol}</span>
          <span style={S.headerPrice}>${stockPrice.toFixed(2)}</span>
          <span style={{ ...S.headerChange, color: up ? 'var(--green-primary)' : 'var(--red-primary)' }}>
            {up ? <TrendingUp size={12} /> : <TrendingDown size={12} />}
            {up ? '+' : ''}{stockChange.toFixed(2)}%
          </span>
        </div>
        <div style={S.qtyWrap}>
          <label style={S.qtyLabel}>Qty</label>
          <input
            type="number" min={1} max={100} value={orderQty}
            onChange={e => setOrderQty(Math.max(1, parseInt(e.target.value) || 1))}
            style={S.qtyInput}
          />
        </div>
      </div>

      {/* Expiration selector */}
      <div style={S.expiryRow}>
        {chain.slices.map((s, i) => (
          <button
            key={s.expirationDate}
            onClick={() => setSelectedExpiry(i)}
            style={{ ...S.expiryBtn, ...(i === selectedExpiry ? S.expiryBtnActive : {}) }}
          >
            {new Date(s.expirationDate).toLocaleDateString('en-US', { month: 'short', day: 'numeric' })}
            <span style={S.dteLabel}>{s.daysToExpiry}d</span>
          </button>
        ))}
      </div>

      {/* Order feedback */}
      {orderMsg && (
        <div style={{ ...S.toast, borderColor: orderMsg.ok ? 'var(--green-primary)' : 'var(--red-primary)', color: orderMsg.ok ? '#6ee7b7' : '#fca5a5' }}>
          {orderMsg.text}
        </div>
      )}

      {/* Positions summary */}
      {hasPositions && (
        <div style={S.positionsBar}>
          <span style={S.positionsLabel}>Your Positions:</span>
          {chain.positions!.map(p => {
            const contract = p.type === 'Call' ? callMap.get(p.strike) : putMap.get(p.strike);
            const currentPrice = contract ? (contract.bid + contract.ask) / 2 : 0;
            const pnl = (currentPrice - p.avgCost) * 100 * p.quantity;
            const pnlColor = pnl >= 0 ? 'var(--green-primary)' : 'var(--red-primary)';
            return (
              <span key={p.contractId} style={S.positionChip}>
                <span style={{ color: p.type === 'Call' ? 'var(--green-primary)' : 'var(--red-primary)', fontWeight: 700 }}>
                  {p.quantity}x {p.type}
                </span>
                {' '}${p.strike} @ ${p.avgCost.toFixed(2)}
                <span style={{ color: pnlColor, fontWeight: 600 }}> {pnl >= 0 ? '+' : ''}{pnl.toFixed(0)}</span>
              </span>
            );
          })}
        </div>
      )}

      {/* Chain table */}
      <div style={S.tableWrap}>
        <table style={S.table}>
          <thead>
            <tr>
              <th colSpan={8} style={{ ...S.th, ...S.callHeader }}>CALLS</th>
              <th style={{ ...S.th, ...S.strikeHeader }}>STRIKE</th>
              <th colSpan={8} style={{ ...S.th, ...S.putHeader }}>PUTS</th>
            </tr>
            <tr>
              <th style={S.thAction}></th>
              <th style={S.th}>Bid</th>
              <th style={S.th}>Ask</th>
              <th style={S.th}>Last</th>
              <th style={S.th}>IV%</th>
              <th style={S.th}>Delta</th>
              <th style={S.th}>OI</th>
              <th style={S.th}>Vol</th>
              <th style={{ ...S.th, ...S.strikeCol }}></th>
              <th style={S.th}>Vol</th>
              <th style={S.th}>OI</th>
              <th style={S.th}>Delta</th>
              <th style={S.th}>IV%</th>
              <th style={S.th}>Last</th>
              <th style={S.th}>Ask</th>
              <th style={S.th}>Bid</th>
              <th style={S.thAction}></th>
            </tr>
          </thead>
          <tbody>
            {slice.strikes.map(strike => {
              const call = callMap.get(strike);
              const put = putMap.get(strike);
              const isAtm = stockPrice > 0 && Math.abs(strike - stockPrice) / stockPrice < 0.02;
              const rowStyle = isAtm ? S.atmRow : call?.itm ? S.itmCallRow : put?.itm ? S.itmPutRow : undefined;
              return (
                <tr key={strike} style={rowStyle}>
                  {/* Call Buy/Sell */}
                  <td style={S.tdAction}>
                    {call && (
                      <div style={S.actionBtns}>
                        <button style={S.buyBtn} onClick={() => handleBuy(call.id)} title={`Buy ${orderQty} Call @ $${fmtNum(call.ask, 2)}`}>Buy</button>
                        <button style={S.sellBtn} onClick={() => handleSell(call.id)} title={`Sell ${orderQty} Call @ $${fmtNum(call.bid, 2)}`}>Sell</button>
                      </div>
                    )}
                  </td>
                  <td style={S.td} className="mono">{fmtNum(call?.bid, 2)}</td>
                  <td style={S.td} className="mono">{fmtNum(call?.ask, 2)}</td>
                  <td style={S.td} className="mono">{fmtNum(call?.last, 2)}</td>
                  <td style={{ ...S.td, color: (call?.iv ?? 0) > 50 ? '#fbbf24' : 'var(--text-secondary)' }} className="mono">{fmtNum(call?.iv, 1)}</td>
                  <td style={{ ...S.td, color: (call?.delta ?? 0) > 0.5 ? 'var(--green-primary)' : 'var(--text-secondary)' }} className="mono">{fmtNum(call?.delta, 2)}</td>
                  <td style={S.tdDim} className="mono">{call?.openInterest?.toLocaleString() ?? '-'}</td>
                  <td style={S.td} className="mono">{call?.volume ?? '-'}</td>
                  {/* Strike */}
                  <td style={{ ...S.strikeCell, ...(isAtm ? { borderLeft: '2px solid var(--text-accent)', borderRight: '2px solid var(--text-accent)' } : {}) }} className="mono">
                    ${strike.toFixed(strike < 10 ? 2 : 0)}
                  </td>
                  {/* Put side (mirrored) */}
                  <td style={S.td} className="mono">{put?.volume ?? '-'}</td>
                  <td style={S.tdDim} className="mono">{put?.openInterest?.toLocaleString() ?? '-'}</td>
                  <td style={{ ...S.td, color: Math.abs(put?.delta ?? 0) > 0.5 ? 'var(--red-primary)' : 'var(--text-secondary)' }} className="mono">{fmtNum(put?.delta, 2)}</td>
                  <td style={{ ...S.td, color: (put?.iv ?? 0) > 50 ? '#fbbf24' : 'var(--text-secondary)' }} className="mono">{fmtNum(put?.iv, 1)}</td>
                  <td style={S.td} className="mono">{fmtNum(put?.last, 2)}</td>
                  <td style={S.td} className="mono">{fmtNum(put?.ask, 2)}</td>
                  <td style={S.td} className="mono">{fmtNum(put?.bid, 2)}</td>
                  <td style={S.tdAction}>
                    {put && (
                      <div style={S.actionBtns}>
                        <button style={S.buyBtn} onClick={() => handleBuy(put.id)} title={`Buy ${orderQty} Put @ $${fmtNum(put.ask, 2)}`}>Buy</button>
                        <button style={S.sellBtn} onClick={() => handleSell(put.id)} title={`Sell ${orderQty} Put @ $${fmtNum(put.bid, 2)}`}>Sell</button>
                      </div>
                    )}
                  </td>
                </tr>
              );
            })}
          </tbody>
        </table>
      </div>
    </div>
  );
}

const S: Record<string, React.CSSProperties> = {
  // Picker
  pickerWrap: { padding: '24px 24px', overflow: 'auto', height: '100%' },
  pickerTitle: { fontSize: 20, fontWeight: 700, color: 'var(--text-primary)', marginBottom: 4 },
  pickerDesc: { fontSize: 13, color: 'var(--text-secondary)', lineHeight: 1.7, margin: '0 0 20px', maxWidth: 520 },
  sectionLabel: { fontSize: 11, fontWeight: 700, color: 'var(--text-disabled)', margin: '0 0 10px', textTransform: 'uppercase' as const, letterSpacing: '0.08em' },
  stockGrid: { display: 'grid', gridTemplateColumns: 'repeat(auto-fill, minmax(150px, 1fr))', gap: 8 },
  stockCard: {
    display: 'flex', flexDirection: 'column' as const, gap: 3, padding: '10px 12px', borderRadius: 6,
    border: '1px solid var(--border-color)', background: 'var(--bg-tertiary)',
    cursor: 'pointer', textAlign: 'left' as const, transition: 'border-color 150ms, background 150ms',
  },
  stockCardTop: { display: 'flex', justifyContent: 'space-between', alignItems: 'center' },
  stockCardBottom: { display: 'flex', justifyContent: 'space-between', alignItems: 'center' },
  stockSymbol: { fontWeight: 800, fontSize: 13, color: 'var(--text-primary)', fontFamily: 'var(--font-mono)' },
  stockPrice: { fontSize: 12, color: 'var(--text-secondary)', fontFamily: 'var(--font-mono)' },
  stockChange: { fontSize: 11, fontWeight: 600, fontFamily: 'var(--font-mono)' },
  stockMcap: { fontSize: 10, color: 'var(--text-disabled)' },
  stockName: { fontSize: 10, color: 'var(--text-disabled)', overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' as const },

  // Chain
  container: { display: 'flex', flexDirection: 'column', height: '100%', gap: 6 },
  loading: { padding: 40, textAlign: 'center', color: 'var(--text-disabled)', fontSize: 14 },
  header: { display: 'flex', alignItems: 'center', gap: 10, padding: '4px 0' },
  backBtn: {
    display: 'flex', alignItems: 'center', gap: 4, padding: '5px 10px', borderRadius: 4,
    border: '1px solid var(--border-color)', background: 'var(--bg-tertiary)',
    color: 'var(--text-secondary)', fontSize: 12, cursor: 'pointer',
  },
  headerInfo: { display: 'flex', alignItems: 'center', gap: 8, flex: 1 },
  headerSymbol: { fontSize: 16, fontWeight: 800, color: 'var(--text-primary)', fontFamily: 'var(--font-mono)' },
  headerPrice: { fontSize: 15, fontWeight: 600, color: 'var(--text-primary)', fontFamily: 'var(--font-mono)' },
  headerChange: { display: 'flex', alignItems: 'center', gap: 3, fontSize: 12, fontWeight: 600, fontFamily: 'var(--font-mono)' },
  qtyWrap: { display: 'flex', alignItems: 'center', gap: 4 },
  qtyLabel: { fontSize: 11, color: 'var(--text-disabled)', fontWeight: 600 },
  qtyInput: {
    width: 48, padding: '4px 6px', borderRadius: 4, border: '1px solid var(--border-color)',
    background: 'var(--bg-tertiary)', color: 'var(--text-primary)', fontSize: 12,
    fontFamily: 'var(--font-mono)', textAlign: 'center' as const, outline: 'none',
  },
  expiryRow: { display: 'flex', gap: 4, flexWrap: 'wrap' as const },
  expiryBtn: {
    padding: '5px 12px', borderRadius: 5, border: '1px solid var(--border-color)',
    background: 'var(--bg-tertiary)', color: 'var(--text-secondary)',
    fontSize: 12, fontWeight: 600, cursor: 'pointer', display: 'flex', alignItems: 'center', gap: 5,
    fontFamily: 'var(--font-mono)', transition: 'all 150ms',
  },
  expiryBtnActive: {
    background: 'rgba(96,165,250,0.15)', borderColor: 'var(--text-accent)', color: '#93c5fd',
  },
  dteLabel: { fontSize: 9, color: 'var(--text-disabled)', fontWeight: 400 },
  toast: {
    padding: '6px 12px', borderRadius: 4, border: '1px solid',
    fontSize: 12, fontWeight: 600, background: 'var(--bg-secondary)',
  },
  tableWrap: { flex: 1, overflow: 'auto' },
  table: { width: '100%', borderCollapse: 'collapse' as const, fontSize: 11 },
  th: {
    padding: '5px 6px', textAlign: 'right' as const, fontSize: 9, fontWeight: 700,
    color: 'var(--text-disabled)', textTransform: 'uppercase' as const, letterSpacing: '0.05em',
    borderBottom: '1px solid var(--border-color)', whiteSpace: 'nowrap' as const,
  },
  thAction: { width: 72, padding: 2, borderBottom: '1px solid var(--border-color)' },
  callHeader: { textAlign: 'center' as const, color: 'var(--green-primary)', fontSize: 10, letterSpacing: '0.12em' },
  putHeader: { textAlign: 'center' as const, color: 'var(--red-primary)', fontSize: 10, letterSpacing: '0.12em' },
  strikeHeader: { textAlign: 'center' as const, background: 'var(--bg-tertiary)' },
  strikeCol: { textAlign: 'center' as const },
  td: {
    padding: '4px 6px', textAlign: 'right' as const, fontSize: 11,
    color: 'var(--text-secondary)', borderBottom: '1px solid rgba(31,41,55,0.3)',
  },
  tdDim: {
    padding: '4px 6px', textAlign: 'right' as const, fontSize: 10,
    color: 'var(--text-disabled)', borderBottom: '1px solid rgba(31,41,55,0.3)',
  },
  tdAction: {
    padding: '2px 4px', borderBottom: '1px solid rgba(31,41,55,0.3)', textAlign: 'center' as const,
  },
  actionBtns: { display: 'flex', gap: 3, justifyContent: 'center' },
  strikeCell: {
    padding: '4px 10px', textAlign: 'center' as const, fontWeight: 700,
    color: 'var(--text-primary)', background: 'var(--bg-tertiary)',
    borderBottom: '1px solid rgba(31,41,55,0.3)', fontSize: 12,
  },
  atmRow: { background: 'rgba(96,165,250,0.08)' },
  itmCallRow: { background: 'rgba(16,185,129,0.04)' },
  itmPutRow: { background: 'rgba(239,68,68,0.04)' },
  positionsBar: {
    display: 'flex', alignItems: 'center', gap: 8, padding: '6px 10px',
    background: 'rgba(96,165,250,0.08)', borderRadius: 4, border: '1px solid rgba(96,165,250,0.2)',
    flexWrap: 'wrap' as const,
  },
  positionsLabel: { fontSize: 11, fontWeight: 700, color: 'var(--text-accent)' },
  positionChip: {
    fontSize: 11, fontFamily: 'var(--font-mono)', color: 'var(--text-secondary)',
    padding: '2px 8px', background: 'var(--bg-tertiary)', borderRadius: 3,
  },
  buyBtn: {
    padding: '3px 8px', borderRadius: 3, border: '1px solid var(--green-primary)',
    background: 'rgba(16,185,129,0.1)', color: 'var(--green-primary)',
    fontSize: 10, fontWeight: 700, cursor: 'pointer', letterSpacing: '0.02em',
  },
  sellBtn: {
    padding: '3px 8px', borderRadius: 3, border: '1px solid var(--red-primary)',
    background: 'rgba(239,68,68,0.1)', color: 'var(--red-primary)',
    fontSize: 10, fontWeight: 700, cursor: 'pointer', letterSpacing: '0.02em',
  },
};
