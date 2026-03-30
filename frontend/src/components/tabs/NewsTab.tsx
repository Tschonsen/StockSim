import { useState } from 'react';
import { useMarketStore } from '@/stores/marketStore';
import { styles } from '@/styles/centralStyles';

export function NewsTab() {
  const newsItems = useMarketStore((s) => s.newsItems);
  const activeArcs = useMarketStore((s) => s.activeArcs);
  const selectStock = useMarketStore((s) => s.selectStock);

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
              background: 'rgba(239,68,68,0.08)', border: '1px solid rgba(239,68,68,0.2)',
              borderRadius: '6px', padding: '6px 10px', fontSize: '11px',
            }}>
              <span style={{ color: 'var(--red-primary)', fontWeight: 700, fontSize: '9px' }}>DEVELOPING</span>
              <span style={{ color: 'var(--text-primary)', fontWeight: 600 }}>{arc.name}</span>
              {arc.sector && <span style={{ color: 'var(--text-disabled)', fontSize: '10px' }}>{arc.sector}</span>}
              {arc.path && <span style={{
                fontSize: '9px', padding: '1px 4px', borderRadius: '3px',
                background: arc.path === 'A' ? 'rgba(16,185,129,0.15)' : 'rgba(239,68,68,0.15)',
                color: arc.path === 'A' ? 'var(--green-primary)' : 'var(--red-primary)',
                fontWeight: 700,
              }}>Path {arc.path}</span>}
              <span className="mono" style={{ color: 'var(--text-disabled)', fontSize: '9px' }}>Phase {arc.phase + 1}</span>
            </div>
          ))}
        </div>
      )}
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '12px' }}>
        <h2 style={{ ...styles.heading, marginBottom: 0 }}>News Feed ({newsItems.length})</h2>
        <div style={{ display: 'flex', gap: '4px' }}>
          {['all', 'Macro', 'Sector', 'Company', 'Rumor'].map(f => (
            <button key={f} onClick={() => setNewsFilter(f)} style={{
              padding: '4px 10px', border: 'none', borderRadius: '4px', cursor: 'pointer',
              fontSize: '11px', fontWeight: 600, fontFamily: 'var(--font-ui)',
              background: newsFilter === f ? 'var(--text-accent)' : 'var(--bg-tertiary)',
              color: newsFilter === f ? 'var(--text-primary)' : 'var(--text-secondary)',
            }}>{f === 'all' ? 'All' : f}</button>
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
          return item.type === newsFilter;
        }).sort((a, b) => (b.timestamp ?? '').localeCompare(a.timestamp ?? '')); // Newest first
        return filtered.length > 0 ? (
          <div style={{ display: 'flex', flexDirection: 'column', gap: '6px' }}>
            {filtered.map((item) => {
              const isRumor = item.type === 'Rumor';
              const color = isRumor ? 'var(--text-accent)' :
                            item.sentiment > 0.1 ? 'var(--green-primary)' :
                            item.sentiment < -0.1 ? 'var(--red-primary)' : 'var(--text-secondary)';
              return (
                <div key={item.id} style={{
                  background: isRumor ? 'rgba(96,165,250,0.05)' : 'var(--bg-secondary)',
                  border: `1px solid ${isRumor ? 'rgba(96,165,250,0.2)' : 'var(--border)'}`,
                  borderRadius: '6px', padding: '10px 14px',
                  cursor: item.affectedSymbols[0] ? 'pointer' : 'default',
                  borderLeft: `3px solid ${color}`,
                }} onClick={() => setExpandedNewsId(expandedNewsId === item.id ? null : item.id)}>
                  <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '3px' }}>
                    <div style={{ display: 'flex', gap: '6px', alignItems: 'center' }}>
                      <span style={{
                        fontSize: '9px', fontWeight: 600, padding: '1px 5px', borderRadius: '3px',
                        background: isRumor ? 'rgba(96,165,250,0.15)' :
                                    item.severity === 'Major' ? 'var(--red-dim)' : item.severity === 'Moderate' ? 'rgba(245,158,11,0.2)' : 'var(--bg-tertiary)',
                        color: isRumor ? 'var(--text-accent)' :
                               item.severity === 'Major' ? 'var(--red-primary)' : item.severity === 'Moderate' ? 'var(--warning)' : 'var(--text-disabled)',
                        border: isRumor ? '1px solid rgba(96,165,250,0.3)' : 'none',
                      }}>{isRumor ? '\u{1F4AC} Rumor' : item.type}</span>
                      {item.tier && item.tier >= 3 && (
                        <span style={{
                          fontSize: '8px', fontWeight: 800, padding: '1px 4px', borderRadius: '3px',
                          background: item.tier >= 4 ? 'rgba(239,68,68,0.2)' : 'rgba(245,158,11,0.15)',
                          color: item.tier >= 4 ? 'var(--red-primary)' : 'var(--warning)',
                          letterSpacing: '0.5px',
                        }}>{item.tier >= 4 ? 'BLACK SWAN' : 'CRISIS'}</span>
                      )}
                      {item.affectedSymbols.length > 0 && item.affectedSymbols.map(sym => (
                        <span key={sym} className="mono" style={{
                          fontSize: '10px', fontWeight: 700, color: 'var(--text-accent)',
                          background: 'rgba(96,165,250,0.1)', padding: '1px 5px', borderRadius: '3px',
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
                      {/* Impact + Tier + Tags row */}
                      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', flexWrap: 'wrap', gap: '6px' }}>
                        <div style={{ display: 'flex', alignItems: 'center', gap: '8px', fontSize: '11px', color: 'var(--text-secondary)' }}>
                          <span>Impact: <span className="mono" style={{ color: item.priceEffect >= 0 ? 'var(--green-primary)' : 'var(--red-primary)', fontWeight: 700 }}>
                            {item.priceEffect >= 0 ? '+' : ''}{(item.priceEffect * 100).toFixed(1)}%
                          </span></span>
                          {item.tier && item.tier > 1 && (
                            <span style={{
                              fontSize: '9px', fontWeight: 700, padding: '1px 5px', borderRadius: '3px',
                              background: item.tier >= 3 ? 'rgba(239,68,68,0.15)' : 'rgba(245,158,11,0.15)',
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
