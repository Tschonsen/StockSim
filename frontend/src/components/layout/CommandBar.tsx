import { useState, useEffect, useRef, useMemo, useCallback } from 'react';
import { useMarketStore } from '@/stores/marketStore';
import { WebSocketClient } from '@/services/websocket';
// Types used via store
import { Search, TrendingUp, BarChart3, Briefcase, FileText, Newspaper, PieChart, BookOpen, ArrowRight } from 'lucide-react';

interface CommandBarProps {
  wsClient: WebSocketClient;
  isOpen: boolean;
  onClose: () => void;
}

interface CommandResult {
  id: string;
  type: 'stock' | 'nav' | 'action' | 'etf';
  label: string;
  sublabel: string;
  icon: React.ReactNode;
  action: () => void;
}

export function CommandBar({ wsClient, isOpen, onClose }: CommandBarProps) {
  const [query, setQuery] = useState('');
  const [selectedIndex, setSelectedIndex] = useState(0);
  const inputRef = useRef<HTMLInputElement>(null);

  const stockList = useMarketStore((s) => s.stockList);
  const selectStock = useMarketStore((s) => s.selectStock);
  const setActiveTab = useMarketStore((s) => s.setActiveTab);

  // Focus input when opened
  useEffect(() => {
    if (isOpen) {
      setQuery('');
      setSelectedIndex(0);
      setTimeout(() => inputRef.current?.focus(), 50);
    }
  }, [isOpen]);

  // Navigation commands
  const navCommands: CommandResult[] = useMemo(() => [
    { id: 'nav-dashboard', type: 'nav', label: 'Dashboard', sublabel: 'Market overview, heatmap, movers', icon: <BarChart3 size={14} />, action: () => { setActiveTab('dashboard'); onClose(); } },
    { id: 'nav-portfolio', type: 'nav', label: 'Portfolio', sublabel: 'Positions, P&L, allocation', icon: <Briefcase size={14} />, action: () => { setActiveTab('portfolio'); onClose(); } },
    { id: 'nav-market', type: 'nav', label: 'Market', sublabel: 'All stocks, screener, search', icon: <TrendingUp size={14} />, action: () => { setActiveTab('market'); onClose(); } },
    { id: 'nav-orders', type: 'nav', label: 'Orders', sublabel: 'Open and filled orders', icon: <FileText size={14} />, action: () => { setActiveTab('orders'); onClose(); } },
    { id: 'nav-news', type: 'nav', label: 'News', sublabel: 'Market events and headlines', icon: <Newspaper size={14} />, action: () => { setActiveTab('news'); onClose(); } },
    { id: 'nav-analytics', type: 'nav', label: 'Analytics', sublabel: 'Performance, stats, achievements', icon: <PieChart size={14} />, action: () => { setActiveTab('analytics'); onClose(); } },
    { id: 'nav-journal', type: 'nav', label: 'Journal', sublabel: 'Trading history and P&L', icon: <BookOpen size={14} />, action: () => { setActiveTab('journal'); onClose(); } },
  ], [setActiveTab, onClose]);

  // Action commands
  const actionCommands: CommandResult[] = useMemo(() => [
    { id: 'act-pause', type: 'action', label: 'Pause', sublabel: 'Pause simulation', icon: <span style={{ fontSize: '14px' }}>||</span>, action: () => { wsClient.send('SetSpeed', { speed: 0 }); onClose(); } },
    { id: 'act-play', type: 'action', label: 'Play 1x', sublabel: 'Normal speed', icon: <ArrowRight size={14} />, action: () => { wsClient.send('SetSpeed', { speed: 1 }); onClose(); } },
    { id: 'act-fast', type: 'action', label: 'Fast 5x', sublabel: 'Very fast speed', icon: <ArrowRight size={14} />, action: () => { wsClient.send('SetSpeed', { speed: 5 }); onClose(); } },
    { id: 'act-max', type: 'action', label: 'Max 10x', sublabel: 'Maximum speed', icon: <ArrowRight size={14} />, action: () => { wsClient.send('SetSpeed', { speed: 10 }); onClose(); } },
    { id: 'act-save', type: 'action', label: 'Save Game', sublabel: 'Quick save', icon: <span style={{ fontSize: '14px' }}>S</span>, action: () => { wsClient.send('SaveGame', {}); onClose(); } },
  ], [wsClient, onClose]);

  // Parse potential buy/sell commands
  const parseTradeCommand = useCallback((q: string): CommandResult | null => {
    const match = q.match(/^(buy|sell|short|cover)\s+(\d+)\s+(\w+)$/i);
    if (!match) return null;
    const [, side, qty, symbol] = match;
    const stock = stockList.find(s => s.symbol.toLowerCase() === symbol.toLowerCase());
    if (!stock) return null;

    const orderSide = side.charAt(0).toUpperCase() + side.slice(1).toLowerCase();
    return {
      id: `trade-${side}-${qty}-${symbol}`,
      type: 'action',
      label: `${orderSide} ${qty} ${stock.symbol}`,
      sublabel: `@ $${stock.price.toFixed(2)} | Est. $${(stock.price * parseInt(qty)).toFixed(0)}`,
      icon: <TrendingUp size={14} />,
      action: () => {
        wsClient.send('PlaceOrder', {
          symbol: stock.symbol,
          side: orderSide,
          type: 'Market',
          quantity: parseInt(qty),
        });
        onClose();
      },
    };
  }, [stockList, wsClient, onClose]);

  // Build results
  const results = useMemo(() => {
    const q = query.toLowerCase().trim();
    if (!q) {
      // Show recent/popular navigation when empty
      return [...navCommands.slice(0, 4), ...actionCommands.slice(0, 2)];
    }

    const matches: CommandResult[] = [];

    // Check for trade command first
    const tradeCmd = parseTradeCommand(q);
    if (tradeCmd) matches.push(tradeCmd);

    // Search stocks
    const stockMatches = stockList
      .filter(s =>
        s.symbol.toLowerCase().includes(q) ||
        s.name.toLowerCase().includes(q) ||
        s.sector.toLowerCase().includes(q)
      )
      .slice(0, 8)
      .map(s => ({
        id: `stock-${s.symbol}`,
        type: (s.traits.includes('ETF') ? 'etf' : 'stock') as CommandResult['type'],
        label: s.symbol,
        sublabel: `${s.name} | $${s.price.toFixed(2)} ${s.changePercent >= 0 ? '+' : ''}${s.changePercent.toFixed(2)}%`,
        icon: <TrendingUp size={14} />,
        action: () => { selectStock(s.symbol); onClose(); },
      }));
    matches.push(...stockMatches);

    // Search nav commands
    const navMatches = navCommands.filter(c =>
      c.label.toLowerCase().includes(q) || c.sublabel.toLowerCase().includes(q)
    );
    matches.push(...navMatches);

    // Search action commands
    const actMatches = actionCommands.filter(c =>
      c.label.toLowerCase().includes(q) || c.sublabel.toLowerCase().includes(q)
    );
    matches.push(...actMatches);

    return matches.slice(0, 12);
  }, [query, stockList, navCommands, actionCommands, selectStock, onClose, parseTradeCommand]);

  // Keyboard navigation
  const handleKeyDown = (e: React.KeyboardEvent) => {
    if (e.key === 'Escape') {
      onClose();
    } else if (e.key === 'ArrowDown') {
      e.preventDefault();
      setSelectedIndex(i => Math.min(i + 1, results.length - 1));
    } else if (e.key === 'ArrowUp') {
      e.preventDefault();
      setSelectedIndex(i => Math.max(i - 1, 0));
    } else if (e.key === 'Enter') {
      e.preventDefault();
      if (results[selectedIndex]) {
        results[selectedIndex].action();
      }
    }
  };

  // Reset selection when results change
  useEffect(() => {
    setSelectedIndex(0);
  }, [query]);

  if (!isOpen) return null;

  return (
    <div style={S.overlay} onClick={onClose}>
      <div style={S.container} onClick={e => e.stopPropagation()}>
        {/* Search Input */}
        <div style={S.inputRow}>
          <Search size={18} style={{ color: 'var(--text-disabled)', flexShrink: 0 }} />
          <input
            ref={inputRef}
            type="text"
            value={query}
            onChange={e => setQuery(e.target.value)}
            onKeyDown={handleKeyDown}
            placeholder='Search stocks, navigate, or "buy 100 AAPL"...'
            style={S.input}
          />
          <kbd style={S.kbd}>ESC</kbd>
        </div>

        {/* Results */}
        {results.length > 0 && (
          <div style={S.results}>
            {results.map((r, i) => (
              <button
                key={r.id}
                onClick={r.action}
                onMouseEnter={() => setSelectedIndex(i)}
                style={{
                  ...S.resultItem,
                  background: i === selectedIndex ? 'var(--bg-tertiary)' : 'transparent',
                  borderLeft: i === selectedIndex ? '2px solid var(--text-accent)' : '2px solid transparent',
                }}
              >
                <span style={{
                  ...S.resultIcon,
                  color: r.type === 'stock' ? 'var(--green-primary)' :
                         r.type === 'etf' ? '#8B5CF6' :
                         r.type === 'nav' ? 'var(--text-accent)' : '#F59E0B',
                }}>{r.icon}</span>
                <div style={S.resultText}>
                  <span style={S.resultLabel}>{r.label}</span>
                  <span style={S.resultSub}>{r.sublabel}</span>
                </div>
                <span style={S.resultType}>
                  {r.type === 'stock' ? 'STOCK' : r.type === 'etf' ? 'ETF' : r.type === 'nav' ? 'NAV' : 'CMD'}
                </span>
              </button>
            ))}
          </div>
        )}

        {results.length === 0 && query && (
          <div style={S.empty}>No results for "{query}"</div>
        )}

        {/* Footer hints */}
        <div style={S.footer}>
          <span style={S.hint}><kbd style={S.kbdSmall}>↑↓</kbd> Navigate</span>
          <span style={S.hint}><kbd style={S.kbdSmall}>Enter</kbd> Select</span>
          <span style={S.hint}><kbd style={S.kbdSmall}>Esc</kbd> Close</span>
          <span style={S.hint}>Try: <span className="mono" style={{ color: 'var(--text-accent)' }}>buy 50 SIMX</span></span>
        </div>
      </div>
    </div>
  );
}

const S: Record<string, React.CSSProperties> = {
  overlay: {
    position: 'fixed', inset: 0, zIndex: 10000,
    background: 'rgba(0,0,0,0.6)', backdropFilter: 'blur(2px)',
    display: 'flex', justifyContent: 'center', paddingTop: '15vh',
  },
  container: {
    width: '580px', maxHeight: '480px',
    background: 'var(--bg-secondary)',
    border: '1px solid var(--border)',
    borderRadius: '12px',
    boxShadow: '0 20px 60px rgba(0,0,0,0.5), 0 0 40px rgba(96,165,250,0.05)',
    display: 'flex', flexDirection: 'column' as const,
    overflow: 'hidden',
  },
  inputRow: {
    display: 'flex', alignItems: 'center', gap: '12px',
    padding: '16px 20px',
    borderBottom: '1px solid var(--border)',
  },
  input: {
    flex: 1, background: 'transparent', border: 'none',
    color: 'var(--text-primary)', fontSize: '15px',
    fontFamily: 'var(--font-ui)', outline: 'none',
  },
  kbd: {
    padding: '2px 8px', borderRadius: '4px', fontSize: '11px',
    background: 'var(--bg-tertiary)', color: 'var(--text-disabled)',
    border: '1px solid var(--border)', fontFamily: 'var(--font-mono)',
  },
  results: {
    flex: 1, overflowY: 'auto' as const, padding: '4px 0',
  },
  resultItem: {
    width: '100%', display: 'flex', alignItems: 'center', gap: '12px',
    padding: '10px 20px', border: 'none', cursor: 'pointer',
    fontFamily: 'var(--font-ui)', textAlign: 'left' as const,
    transition: 'background 100ms',
  },
  resultIcon: {
    width: '28px', height: '28px', borderRadius: '6px',
    background: 'var(--bg-tertiary)', display: 'flex',
    alignItems: 'center', justifyContent: 'center', flexShrink: 0,
  },
  resultText: {
    flex: 1, display: 'flex', flexDirection: 'column' as const, minWidth: 0,
  },
  resultLabel: {
    fontSize: '14px', fontWeight: 600, color: 'var(--text-primary)',
  },
  resultSub: {
    fontSize: '12px', color: 'var(--text-secondary)',
    overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' as const,
  },
  resultType: {
    fontSize: '10px', fontWeight: 600, color: 'var(--text-disabled)',
    letterSpacing: '1px', fontFamily: 'var(--font-mono)',
  },
  empty: {
    padding: '32px', textAlign: 'center' as const,
    color: 'var(--text-disabled)', fontSize: '14px',
  },
  footer: {
    display: 'flex', gap: '16px', padding: '10px 20px',
    borderTop: '1px solid var(--border)', alignItems: 'center',
  },
  hint: {
    fontSize: '11px', color: 'var(--text-disabled)',
    display: 'flex', alignItems: 'center', gap: '4px',
  },
  kbdSmall: {
    padding: '1px 4px', borderRadius: '3px', fontSize: '10px',
    background: 'var(--bg-tertiary)', color: 'var(--text-disabled)',
    border: '1px solid var(--border)', fontFamily: 'var(--font-mono)',
  },
};
