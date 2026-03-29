import { useState, useMemo } from 'react';
import { Search, X } from 'lucide-react';
import { GLOSSARY } from '@/data/glossary';

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
