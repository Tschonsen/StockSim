import { useState } from 'react';
import { useMarketStore } from '@/stores/marketStore';
import { styles } from '@/styles/centralStyles';

export function NewsTab() {
  const newsItems = useMarketStore((s) => s.newsItems);
  const activeArcs = useMarketStore((s) => s.activeArcs);
  const selectStock = useMarketStore((s) => s.selectStock);
  const portfolio = useMarketStore((s) => s.portfolio);

  const heldSymbols = new Set(portfolio?.positions?.map(p => p.symbol) ?? []);

  const [newsFilter, setNewsFilter] = useState<string>('all');
  const [expandedNewsId, setExpandedNewsId] = useState<number | null>(null);

  return (
    <div style={styles.content}>
      {/* Active Story Arcs Banner */}
      {activeArcs.length > 0 && (
        <div style={{
          display: 'flex', gap: '8px', marginBottom: '10px', flexWrap: 'wrap',
        }}>
          {activeArcs.map((arc) => (
            <div key={arc.id} style={{
              display: 'flex', alignItems: 'center', gap: '6px',
              background: 'color-mix(in srgb, var(--red-primary) 8%, transparent)', border: '1px solid color-mix(in srgb, var(--red-primary) 20%, transparent)',
              borderRadius: '6px', padding: '6px 10px', fontSize: '11px',
            }}>
              <span style={{ color: 'var(--red-primary)', fontWeight: 700, fontSize: '9px' }}>DEVELOPING</span>
              <span style={{ color: 'var(--text-primary)', fontWeight: 600 }}>{arc.name}</span>
              {arc.sector && <span style={{ color: 'var(--text-disabled)', fontSize: '10px' }}>{arc.sector}</span>}
              {arc.path && <span style={{
                fontSize: '9px', padding: '1px 4px', borderRadius: '3px',
                background: arc.path === 'A' ? 'color-mix(in srgb, var(--green-primary) 15%, transparent)' : 'color-mix(in srgb, var(--red-primary) 15%, transparent)',
                color: arc.path === 'A' ? 'var(--green-primary)' : 'var(--red-primary)',
                fontWeight: 700,
              }}>Path {arc.path}</span>}
              <span className="mono" style={{ color: 'var(--text-disabled)', fontSize: '9px' }}>Phase {arc.phase + 1}</span>
            </div>
          ))}
        </div>
      )}
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '12px' }}>
        <h2 style={{ ...styles.heading, marginBottom: 0 }}>
          News Feed ({newsItems.length})
          {newsItems.length > 0 && (
            <span style={{ fontSize: '10px', color: 'var(--text-disabled)', fontWeight: 400, marginLeft: '8px' }}>
              {newsItems.filter(n => n.severity === 'Major').length > 0 && <span style={{ color: 'var(--red-primary)' }}>{newsItems.filter(n => n.severity === 'Major').length} major </span>}
              {newsItems.filter(n => n.type === 'Rumor').length > 0 && <span style={{ color: 'var(--text-accent)' }}>{newsItems.filter(n => n.type === 'Rumor').length} rumors</span>}
            </span>
          )}
        </h2>
        <div style={{ display: 'flex', gap: '4px' }}>
          {['all', 'Macro', 'Sector', 'Company', 'Rumor', 'portfolio'].map(f => (
            <button key={f} onClick={() => setNewsFilter(f)} style={{
              padding: '4px 10px', border: 'none', borderRadius: '4px', cursor: 'pointer',
              fontSize: '11px', fontWeight: 600, fontFamily: 'var(--font-ui)',
              background: newsFilter === f ? (f === 'portfolio' ? 'var(--warning)' : 'var(--text-accent)') : 'var(--bg-tertiary)',
              color: newsFilter === f ? 'var(--text-primary)' : (f === 'portfolio' ? 'var(--warning)' : 'var(--text-secondary)'),
              opacity: f === 'portfolio' && heldSymbols.size === 0 ? 0.4 : 1,
            }}>{f === 'all' ? 'All' : f === 'portfolio' ? 'My Portfolio' : f}</button>
          ))}
          <button onClick={() => setNewsFilter(newsFilter === 'bullish' ? 'all' : 'bullish')} style={{
            padding: '4px 10px', border: 'none', borderRadius: '4px', cursor: 'pointer',
            fontSize: '11px', fontWeight: 600,
            background: newsFilter === 'bullish' ? 'var(--green-primary)' : 'var(--bg-tertiary)',
            color: newsFilter === 'bullish' ? 'var(--text-primary)' : 'var(--green-primary)',
          }}>Bullish</button>
          <button onClick={() => setNewsFilter(newsFilter === 'bearish' ? 'all' : 'bearish')} style={{
            padding: '4px 10px', border: 'none', borderRadius: '4px', cursor: 'pointer',
            fontSize: '11px', fontWeight: 600,
            background: newsFilter === 'bearish' ? 'var(--red-primary)' : 'var(--bg-tertiary)',
            color: newsFilter === 'bearish' ? 'var(--text-primary)' : 'var(--red-primary)',
          }}>Bearish</button>
        </div>
      </div>
      {(() => {
        const filtered = newsItems.filter(item => {
          if (newsFilter === 'all') return true;
          if (newsFilter === 'bullish') return item.sentiment > 0.1;
          if (newsFilter === 'bearish') return item.sentiment < -0.1;
          if (newsFilter === 'portfolio') return item.affectedSymbols.some(s => heldSymbols.has(s));
          return item.type === newsFilter;
        }).sort((a, b) => (b.timestamp ?? '').localeCompare(a.timestamp ?? '')); // Newest first
        return filtered.length > 0 ? (
          <div style={{ display: 'flex', flexDirection: 'column', gap: '6px' }}>
            {filtered.map((item) => {
              const isRumor = item.type === 'Rumor';
              const affectsPortfolio = item.affectedSymbols.some(s => heldSymbols.has(s));
              const color = isRumor ? 'var(--text-accent)' :
                            item.sentiment > 0.1 ? 'var(--green-primary)' :
                            item.sentiment < -0.1 ? 'var(--red-primary)' : 'var(--text-secondary)';
              return (
                <div key={item.id} style={{
                  background: affectsPortfolio ? 'color-mix(in srgb, var(--text-accent) 4%, transparent)' : isRumor ? 'color-mix(in srgb, var(--text-accent) 5%, transparent)' : 'var(--bg-secondary)',
                  border: `1px solid ${isRumor ? 'color-mix(in srgb, var(--text-accent) 20%, transparent)' : 'var(--border)'}`,
                  borderRadius: '6px', padding: '10px 14px',
                  cursor: item.affectedSymbols[0] ? 'pointer' : 'default',
                  borderLeft: `3px solid ${color}`,
                }} onClick={() => setExpandedNewsId(expandedNewsId === item.id ? null : item.id)}>
                  <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '3px' }}>
                    <div style={{ display: 'flex', gap: '6px', alignItems: 'center' }}>
                      <span style={{
                        fontSize: '9px', fontWeight: 600, padding: '1px 5px', borderRadius: '3px',
                        background: isRumor ? 'color-mix(in srgb, var(--text-accent) 15%, transparent)' :
                                    item.severity === 'Major' ? 'var(--red-dim)' : item.severity === 'Moderate' ? 'color-mix(in srgb, var(--warning) 20%, transparent)' : 'var(--bg-tertiary)',
                        color: isRumor ? 'var(--text-accent)' :
                               item.severity === 'Major' ? 'var(--red-primary)' : item.severity === 'Moderate' ? 'var(--warning)' : 'var(--text-disabled)',
                        border: isRumor ? '1px solid var(--accent-glow)' : 'none',
                      }}>{isRumor ? '\u{1F4AC} Rumor' : item.type}</span>
                      {item.tags?.includes('supply_chain') && (
                        <span style={{
                          fontSize: '8px', fontWeight: 700, padding: '1px 4px', borderRadius: '3px',
                          background: 'color-mix(in srgb, var(--chart-orange) 15%, transparent)', color: 'var(--chart-orange)',
                          letterSpacing: '0.3px',
                        }}>SUPPLY CHAIN</span>
                      )}
                      {item.tags?.includes('seasonal') && (
                        <span style={{
                          fontSize: '8px', fontWeight: 700, padding: '1px 4px', borderRadius: '3px',
                          background: 'color-mix(in srgb, var(--chart-cyan) 12%, transparent)', color: 'var(--chart-cyan)',
                          letterSpacing: '0.3px',
                        }}>SEASONAL</span>
                      )}
                      {item.tags?.includes('insider') && (
                        <span style={{
                          fontSize: '8px', fontWeight: 700, padding: '1px 4px', borderRadius: '3px',
                          background: 'color-mix(in srgb, var(--chart-purple) 15%, transparent)', color: 'var(--chart-purple)',
                          letterSpacing: '0.3px',
                        }}>SEC FILING</span>
                      )}
                      {item.tier && item.tier >= 3 && (
                        <span style={{
                          fontSize: '8px', fontWeight: 800, padding: '1px 4px', borderRadius: '3px',
                          background: item.tier >= 4 ? 'color-mix(in srgb, var(--red-primary) 20%, transparent)' : 'color-mix(in srgb, var(--warning) 15%, transparent)',
                          color: item.tier >= 4 ? 'var(--red-primary)' : 'var(--warning)',
                          letterSpacing: '0.5px',
                        }}>{item.tier >= 4 ? 'BLACK SWAN' : 'CRISIS'}</span>
                      )}
                      {affectsPortfolio && (
                        <span style={{
                          fontSize: '8px', fontWeight: 700, padding: '1px 4px', borderRadius: '3px',
                          background: 'color-mix(in srgb, var(--text-accent) 15%, transparent)', color: 'var(--text-accent)',
                        }}>MY STOCK</span>
                      )}
                      {item.affectedSymbols.length > 0 && item.affectedSymbols.map(sym => (
                        <span key={sym} className="mono" style={{
                          fontSize: '10px', fontWeight: 700, color: 'var(--text-accent)',
                          background: 'color-mix(in srgb, var(--text-accent) 10%, transparent)', padding: '1px 5px', borderRadius: '3px',
                          cursor: 'pointer',
                        }} onClick={e => { e.stopPropagation(); selectStock(sym); }}>{sym}</span>
                      ))}
                    </div>
                    <span className="mono" style={{ fontSize: '10px', color: 'var(--text-disabled)' }}>
                      {item.timestamp ? new Date(item.timestamp).toLocaleTimeString() : ''}
                    </span>
                  </div>
                  <div style={{ fontSize: '13px', color, fontFamily: 'var(--font-ui)', lineHeight: 1.4 }}>
                    {item.headline}
                  </div>
                  {/* Expandable detail section (Bloomberg-Style, Phase 1E) */}
                  {expandedNewsId === item.id && (
                    <div style={{ marginTop: '8px', paddingTop: '8px', borderTop: '1px solid var(--border)' }}>
                      {/* Summary */}
                      {item.summary && (
                        <div style={{ fontSize: '12px', color: 'var(--text-primary)', lineHeight: 1.5, marginBottom: '8px' }}>
                          {item.summary}
                        </div>
                      )}
                      {/* Analyst Quote */}
                      {item.analystQuote && item.analystName && (
                        <div style={{
                          fontSize: '11px', color: 'var(--text-secondary)', fontStyle: 'italic',
                          borderLeft: '2px solid var(--text-accent)', paddingLeft: '8px', marginBottom: '8px',
                        }}>
                          "{item.analystQuote}"
                          <div style={{ fontStyle: 'normal', fontSize: '10px', color: 'var(--text-disabled)', marginTop: '2px' }}>
                            — {item.analystName}{item.analystFirm ? `, ${item.analystFirm}` : ''}
                          </div>
                        </div>
                      )}
                      {/* Detailed Impacts — which companies and why */}
                      {item.detailedImpacts && item.detailedImpacts.length > 0 && (
                        <div style={{ marginBottom: '8px' }}>
                          <div style={{ fontSize: '10px', fontWeight: 600, color: 'var(--text-disabled)', textTransform: 'uppercase', letterSpacing: '0.5px', marginBottom: '4px' }}>
                            Affected Companies
                          </div>
                          <div style={{ display: 'flex', flexDirection: 'column', gap: '3px' }}>
                            {item.detailedImpacts.map((d) => (
                              <div key={d.symbol} style={{
                                display: 'flex', alignItems: 'center', gap: '8px', fontSize: '11px',
                                padding: '4px 8px', background: 'var(--bg-tertiary)', borderRadius: '4px',
                              }}>
                                <span className="mono" style={{
                                  fontWeight: 700, color: 'var(--text-accent)', cursor: 'pointer', minWidth: '40px',
                                }} onClick={e => { e.stopPropagation(); selectStock(d.symbol); }}>{d.symbol}</span>
                                <span style={{
                                  fontSize: '9px', fontWeight: 600, padding: '1px 5px', borderRadius: '3px',
                                  background: d.role === 'primary' ? 'color-mix(in srgb, var(--red-primary) 15%, transparent)' : d.role === 'beneficiary' ? 'color-mix(in srgb, var(--green-primary) 15%, transparent)' : 'color-mix(in srgb, var(--text-accent) 10%, transparent)',
                                  color: d.role === 'primary' ? 'var(--red-primary)' : d.role === 'beneficiary' ? 'var(--green-primary)' : 'var(--text-accent)',
                                }}>{d.role}</span>
                                <span className="mono" style={{
                                  fontWeight: 600, fontSize: '11px',
                                  color: d.priceEffect >= 0 ? 'var(--green-primary)' : 'var(--red-primary)',
                                }}>{d.priceEffect >= 0 ? '+' : ''}{(d.priceEffect * 100).toFixed(1)}%</span>
                                <span style={{ color: 'var(--text-muted)', flex: 1 }}>{d.reason}</span>
                              </div>
                            ))}
                          </div>
                        </div>
                      )}
                      {/* Historical Parallel */}
                      {item.historicalParallel && (
                        <div style={{
                          fontSize: '11px', color: 'var(--text-secondary)', marginBottom: '8px',
                          padding: '6px 10px', background: 'color-mix(in srgb, var(--text-accent) 5%, transparent)', borderRadius: '4px',
                          border: '1px solid color-mix(in srgb, var(--text-accent) 10%, transparent)',
                        }}>
                          <span style={{ fontWeight: 600, color: 'var(--text-accent)', fontSize: '10px' }}>HISTORICAL PARALLEL </span>
                          {item.historicalParallel}
                        </div>
                      )}
                      {/* What to Watch */}
                      {item.whatToWatch && item.whatToWatch.length > 0 && (
                        <div style={{ marginBottom: '8px' }}>
                          <div style={{ fontSize: '10px', fontWeight: 600, color: 'var(--text-disabled)', textTransform: 'uppercase', letterSpacing: '0.5px', marginBottom: '3px' }}>
                            What to Watch
                          </div>
                          <ul style={{ margin: 0, paddingLeft: '16px', fontSize: '11px', color: 'var(--text-secondary)', lineHeight: 1.6 }}>
                            {item.whatToWatch.map((w, i) => <li key={i}>{w}</li>)}
                          </ul>
                        </div>
                      )}
                      {/* Impact + Duration + Tier row */}
                      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', flexWrap: 'wrap', gap: '6px' }}>
                        <div style={{ display: 'flex', alignItems: 'center', gap: '8px', fontSize: '11px', color: 'var(--text-secondary)' }}>
                          <span>Impact: <span className="mono" style={{ color: item.priceEffect >= 0 ? 'var(--green-primary)' : 'var(--red-primary)', fontWeight: 700 }}>
                            {item.priceEffect >= 0 ? '+' : ''}{(item.priceEffect * 100).toFixed(1)}%
                          </span></span>
                          {item.durationMinutes && (
                            <span>Duration: <span className="mono" style={{ fontWeight: 600 }}>
                              {item.durationMinutes >= 60 ? `${(item.durationMinutes / 60).toFixed(1)}h` : `${item.durationMinutes}m`}
                            </span></span>
                          )}
                          {item.tier && item.tier > 1 && (
                            <span style={{
                              fontSize: '9px', fontWeight: 700, padding: '1px 5px', borderRadius: '3px',
                              background: item.tier >= 3 ? 'color-mix(in srgb, var(--red-primary) 15%, transparent)' : 'color-mix(in srgb, var(--warning) 15%, transparent)',
                              color: item.tier >= 3 ? 'var(--red-primary)' : 'var(--warning)',
                            }}>TIER {item.tier}</span>
                          )}
                          {item.affectedSectors.length > 0 && (
                            <span>Sectors: {item.affectedSectors.join(', ')}</span>
                          )}
                        </div>
                        {item.affectedSymbols[0] && (
                          <button onClick={e => { e.stopPropagation(); selectStock(item.affectedSymbols[0]); }} style={{
                            padding: '4px 12px', borderRadius: '4px', border: 'none', cursor: 'pointer',
                            fontSize: '11px', fontWeight: 700, fontFamily: 'var(--font-ui)',
                            background: 'var(--text-accent)', color: 'var(--text-primary)',
                          }}>Trade {item.affectedSymbols[0]}</button>
                        )}
                      </div>
                      {/* Tags */}
                      {item.tags && item.tags.length > 0 && (
                        <div style={{ display: 'flex', gap: '4px', flexWrap: 'wrap', marginTop: '6px' }}>
                          {item.tags.map((tag: string) => (
                            <span key={tag} style={{
                              fontSize: '9px', padding: '1px 6px', borderRadius: '3px',
                              background: 'var(--bg-tertiary)', color: 'var(--text-disabled)',
                              fontFamily: 'var(--font-mono)',
                            }}>{tag}</span>
                          ))}
                        </div>
                      )}
                    </div>
                  )}
                </div>
              );
            })}
          </div>
        ) : (
          <div style={{ color: 'var(--text-disabled)', fontSize: '14px' }}>
            {newsItems.length > 0 ? `No ${newsFilter} news.` : 'No news yet. Start the simulation to see market events.'}
          </div>
        );
      })()}
    </div>
  );
}
