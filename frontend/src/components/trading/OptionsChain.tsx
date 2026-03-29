import { useState, useEffect } from 'react';
import { useMarketStore } from '@/stores/marketStore';
import { WebSocketClient } from '@/services/websocket';
import { HelpTip } from '@/components/ui/HelpTip';

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

interface ChainData {
  symbol: string;
  expirations: string[];
  slices: SliceData[];
  noChain?: boolean;
}

interface Props {
  wsClient: WebSocketClient;
}

export function OptionsChain({ wsClient }: Props) {
  const selectedSymbol = useMarketStore(s => s.selectedSymbol);
  const [chain, setChain] = useState<ChainData | null>(null);
  const [selectedExpiry, setSelectedExpiry] = useState(0);
  const [orderMsg, setOrderMsg] = useState<{ text: string; ok: boolean } | null>(null);

  // Fetch chain when symbol changes
  useEffect(() => {
    if (!selectedSymbol) return;
    wsClient.send('GetOptionsChain', { Symbol: selectedSymbol });

    const unsub = wsClient.on('OptionsChain', (payload) => {
      const data = payload as ChainData;
      if (data.symbol === selectedSymbol) {
        setChain(data);
        setSelectedExpiry(0);
      }
    });

    const unsub2 = wsClient.on('OptionOrderResult', (payload) => {
      const r = payload as { success: boolean; message: string };
      setOrderMsg({ text: r.message, ok: r.success });
      setTimeout(() => setOrderMsg(null), 3000);
      // Refresh chain after order
      wsClient.send('GetOptionsChain', { Symbol: selectedSymbol });
    });

    return () => { unsub(); unsub2(); };
  }, [selectedSymbol, wsClient]);

  if (!selectedSymbol) {
    return <div style={S.empty}>Select a stock to view options chain</div>;
  }

  if (!chain || chain.noChain) {
    return <div style={S.empty}>No options available for {selectedSymbol} (requires Market Cap {'>'} $1B)</div>;
  }

  const slice = chain.slices[selectedExpiry];
  if (!slice) return <div style={S.empty}>Loading...</div>;

  const callMap = new Map(slice.calls.map(c => [c.strike, c]));
  const putMap = new Map(slice.puts.map(p => [p.strike, p]));

  const handleBuy = (contractId: number) => {
    wsClient.send('BuyOption', { ContractId: contractId, Symbol: selectedSymbol, Quantity: 1 });
  };
  const handleSell = (contractId: number) => {
    wsClient.send('SellOption', { ContractId: contractId, Symbol: selectedSymbol, Quantity: 1 });
  };

  return (
    <div style={S.container}>
      {/* Header */}
      <div style={S.header}>
        <span style={S.title}>{selectedSymbol} Options Chain</span>
        <HelpTip term="Fair Value" size={12} />
      </div>

      {/* Expiration selector */}
      <div style={S.expiryRow}>
        {chain.slices.map((s, i) => (
          <button
            key={s.expirationDate}
            onClick={() => setSelectedExpiry(i)}
            style={{
              ...S.expiryBtn,
              ...(i === selectedExpiry ? S.expiryBtnActive : {}),
            }}
          >
            {new Date(s.expirationDate).toLocaleDateString('en-US', { month: 'short', day: 'numeric' })}
            <span style={S.dteLabel}>{s.daysToExpiry}d</span>
          </button>
        ))}
      </div>

      {/* Order feedback */}
      {orderMsg && (
        <div style={{ ...S.toast, borderColor: orderMsg.ok ? '#10B981' : '#EF4444', color: orderMsg.ok ? '#6ee7b7' : '#fca5a5' }}>
          {orderMsg.text}
        </div>
      )}

      {/* Chain table */}
      <div style={S.tableWrap}>
        <table style={S.table}>
          <thead>
            <tr>
              <th colSpan={7} style={{ ...S.th, ...S.callHeader }}>CALLS</th>
              <th style={{ ...S.th, ...S.strikeHeader }}>STRIKE</th>
              <th colSpan={7} style={{ ...S.th, ...S.putHeader }}>PUTS</th>
            </tr>
            <tr>
              {/* Call columns */}
              <th style={S.th}>Bid</th>
              <th style={S.th}>Ask</th>
              <th style={S.th}>Last</th>
              <th style={S.th}>IV%</th>
              <th style={S.th}>Delta</th>
              <th style={S.th}>Vol</th>
              <th style={S.thAction}></th>
              {/* Strike */}
              <th style={{ ...S.th, ...S.strikeCol }}></th>
              {/* Put columns */}
              <th style={S.thAction}></th>
              <th style={S.th}>Bid</th>
              <th style={S.th}>Ask</th>
              <th style={S.th}>Last</th>
              <th style={S.th}>IV%</th>
              <th style={S.th}>Delta</th>
              <th style={S.th}>Vol</th>
            </tr>
          </thead>
          <tbody>
            {slice.strikes.map(strike => {
              const call = callMap.get(strike);
              const put = putMap.get(strike);
              return (
                <tr key={strike} style={call?.itm || put?.itm ? S.itmRow : undefined}>
                  {/* Call side */}
                  <td style={S.td} className="mono">{call?.bid.toFixed(2) ?? '-'}</td>
                  <td style={S.td} className="mono">{call?.ask.toFixed(2) ?? '-'}</td>
                  <td style={S.td} className="mono">{call?.last.toFixed(2) ?? '-'}</td>
                  <td style={S.td} className="mono">{call?.iv.toFixed(1) ?? '-'}</td>
                  <td style={{ ...S.td, color: (call?.delta ?? 0) > 0.5 ? '#10B981' : '#9CA3AF' }} className="mono">
                    {call?.delta.toFixed(2) ?? '-'}
                  </td>
                  <td style={S.td} className="mono">{call?.volume ?? '-'}</td>
                  <td style={S.tdAction}>
                    {call && <>
                      <button style={S.buyBtn} onClick={() => handleBuy(call.id)}>B</button>
                      <button style={S.sellBtn} onClick={() => handleSell(call.id)}>S</button>
                    </>}
                  </td>
                  {/* Strike */}
                  <td style={S.strikeCell} className="mono">${strike.toFixed(0)}</td>
                  {/* Put side */}
                  <td style={S.tdAction}>
                    {put && <>
                      <button style={S.buyBtn} onClick={() => handleBuy(put.id)}>B</button>
                      <button style={S.sellBtn} onClick={() => handleSell(put.id)}>S</button>
                    </>}
                  </td>
                  <td style={S.td} className="mono">{put?.bid.toFixed(2) ?? '-'}</td>
                  <td style={S.td} className="mono">{put?.ask.toFixed(2) ?? '-'}</td>
                  <td style={S.td} className="mono">{put?.last.toFixed(2) ?? '-'}</td>
                  <td style={S.td} className="mono">{put?.iv.toFixed(1) ?? '-'}</td>
                  <td style={{ ...S.td, color: Math.abs(put?.delta ?? 0) > 0.5 ? '#EF4444' : '#9CA3AF' }} className="mono">
                    {put?.delta.toFixed(2) ?? '-'}
                  </td>
                  <td style={S.td} className="mono">{put?.volume ?? '-'}</td>
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
  container: { display: 'flex', flexDirection: 'column', height: '100%', gap: 8 },
  empty: { padding: 40, textAlign: 'center', color: 'var(--text-disabled)', fontSize: 14 },
  header: { display: 'flex', alignItems: 'center', gap: 8 },
  title: { fontSize: 16, fontWeight: 700, color: 'var(--text-primary)' },
  expiryRow: { display: 'flex', gap: 4, flexWrap: 'wrap' },
  expiryBtn: {
    padding: '4px 10px', borderRadius: 4, border: '1px solid var(--border)',
    background: 'var(--bg-tertiary)', color: 'var(--text-secondary)',
    fontSize: 11, fontWeight: 600, cursor: 'pointer', display: 'flex', alignItems: 'center', gap: 4,
    fontFamily: 'var(--font-mono)',
  },
  expiryBtnActive: {
    background: 'rgba(96,165,250,0.15)', borderColor: '#60A5FA', color: '#93c5fd',
  },
  dteLabel: { fontSize: 9, color: 'var(--text-disabled)' },
  toast: {
    padding: '4px 10px', borderRadius: 4, border: '1px solid',
    fontSize: 11, fontWeight: 600, background: 'var(--bg-secondary)',
  },
  tableWrap: { flex: 1, overflow: 'auto' },
  table: { width: '100%', borderCollapse: 'collapse', fontSize: 11 },
  th: {
    padding: '4px 6px', textAlign: 'right', fontSize: 9, fontWeight: 700,
    color: 'var(--text-disabled)', textTransform: 'uppercase', letterSpacing: '0.05em',
    borderBottom: '1px solid var(--border)', whiteSpace: 'nowrap',
  },
  thAction: { width: 44, padding: 2, borderBottom: '1px solid var(--border)' },
  callHeader: { textAlign: 'center', color: '#10B981', fontSize: 10, letterSpacing: '0.1em' },
  putHeader: { textAlign: 'center', color: '#EF4444', fontSize: 10, letterSpacing: '0.1em' },
  strikeHeader: { textAlign: 'center', background: 'var(--bg-tertiary)' },
  strikeCol: { textAlign: 'center' },
  td: {
    padding: '3px 6px', textAlign: 'right', fontSize: 11,
    color: 'var(--text-secondary)', borderBottom: '1px solid rgba(31,41,55,0.3)',
  },
  tdAction: {
    padding: '2px 2px', borderBottom: '1px solid rgba(31,41,55,0.3)', textAlign: 'center',
    whiteSpace: 'nowrap' as const,
  },
  strikeCell: {
    padding: '3px 8px', textAlign: 'center', fontWeight: 700,
    color: 'var(--text-primary)', background: 'var(--bg-tertiary)',
    borderBottom: '1px solid rgba(31,41,55,0.3)', fontSize: 12,
  },
  itmRow: { background: 'rgba(96,165,250,0.03)' },
  buyBtn: {
    padding: '3px 6px', borderRadius: 2, border: '1px solid #10B981',
    background: 'rgba(16,185,129,0.1)', color: '#10B981',
    fontSize: 10, fontWeight: 800, cursor: 'pointer', minHeight: 20, minWidth: 18,
  },
  sellBtn: {
    padding: '3px 6px', borderRadius: 2, border: '1px solid #EF4444',
    background: 'rgba(239,68,68,0.1)', color: '#EF4444',
    fontSize: 10, fontWeight: 800, cursor: 'pointer', minHeight: 20, minWidth: 18, marginLeft: 2,
  },
};
