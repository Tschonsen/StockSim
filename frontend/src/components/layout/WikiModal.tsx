import { useState, useMemo, useEffect, useCallback, useRef } from 'react';
import { Search, X, ChevronRight, ChevronDown, BookOpen, ExternalLink } from 'lucide-react';
import { WIKI_ARTICLES, WIKI_CATEGORIES, WikiArticle } from '@/data/wiki';
import { useFocusTrap } from '@/hooks/useFocusTrap';

interface Props {
  isOpen: boolean;
  onClose: () => void;
  initialArticle?: string;
}

export function WikiModal({ isOpen, onClose, initialArticle }: Props) {
  const [search, setSearch] = useState('');
  const [selectedArticleId, setSelectedArticleId] = useState<string | null>(initialArticle ?? null);
  const [expandedCategories, setExpandedCategories] = useState<Set<string>>(new Set(WIKI_CATEGORIES.map(c => c.id)));
  const trapRef = useFocusTrap<HTMLDivElement>();
  const searchRef = useRef<HTMLInputElement>(null);

  // Reset state when opening with an initial article
  useEffect(() => {
    if (isOpen && initialArticle) {
      setSelectedArticleId(initialArticle);
      setSearch('');
    }
  }, [isOpen, initialArticle]);

  // Keyboard: Escape closes, Ctrl+F focuses search
  useEffect(() => {
    if (!isOpen) return;
    const handler = (e: KeyboardEvent) => {
      if (e.key === 'Escape') {
        e.preventDefault();
        onClose();
      }
      if ((e.ctrlKey || e.metaKey) && e.key === 'f') {
        e.preventDefault();
        searchRef.current?.focus();
      }
    };
    window.addEventListener('keydown', handler);
    return () => window.removeEventListener('keydown', handler);
  }, [isOpen, onClose]);

  const toggleCategory = useCallback((catId: string) => {
    setExpandedCategories(prev => {
      const next = new Set(prev);
      if (next.has(catId)) next.delete(catId);
      else next.add(catId);
      return next;
    });
  }, []);

  // Filter articles by search
  const filteredArticles = useMemo(() => {
    if (!search.trim()) return WIKI_ARTICLES;
    const q = search.toLowerCase();
    return WIKI_ARTICLES.filter(a =>
      a.title.toLowerCase().includes(q) ||
      a.summary.toLowerCase().includes(q) ||
      a.content.toLowerCase().includes(q)
    );
  }, [search]);

  // Group articles by category
  const articlesByCategory = useMemo(() => {
    const map = new Map<string, WikiArticle[]>();
    for (const cat of WIKI_CATEGORIES) {
      map.set(cat.id, []);
    }
    for (const article of filteredArticles) {
      const list = map.get(article.category);
      if (list) list.push(article);
    }
    return map;
  }, [filteredArticles]);

  const selectedArticle = useMemo(() =>
    WIKI_ARTICLES.find(a => a.id === selectedArticleId) ?? null,
    [selectedArticleId]
  );

  const relatedArticles = useMemo(() => {
    if (!selectedArticle?.relatedIds) return [];
    return selectedArticle.relatedIds
      .map(id => WIKI_ARTICLES.find(a => a.id === id))
      .filter((a): a is WikiArticle => !!a);
  }, [selectedArticle]);

  if (!isOpen) return null;

  const totalArticles = WIKI_ARTICLES.length;
  const matchCount = filteredArticles.length;

  return (
    <div style={S.overlay} onClick={onClose}>
      <div ref={trapRef} style={S.modal} onClick={e => e.stopPropagation()}>
        {/* Header */}
        <div style={S.header}>
          <div style={S.headerLeft}>
            <BookOpen size={18} style={{ color: 'var(--text-accent)' }} />
            <span style={S.title}>StockSim Wiki</span>
            <span style={S.articleCount}>{totalArticles} articles</span>
          </div>
          <div style={S.headerRight}>
            <div style={S.searchBox}>
              <Search size={14} style={{ color: 'var(--text-disabled)', flexShrink: 0 }} />
              <input
                ref={searchRef}
                type="text"
                value={search}
                onChange={e => setSearch(e.target.value)}
                placeholder="Search articles... (Ctrl+F)"
                style={S.searchInput}
              />
              {search && (
                <span style={S.matchBadge}>{matchCount} found</span>
              )}
            </div>
            <button style={S.closeBtn} onClick={onClose} aria-label="Close wiki">
              <X size={18} />
            </button>
          </div>
        </div>

        {/* Body: Sidebar + Content */}
        <div style={S.body}>
          {/* Left Sidebar */}
          <div style={S.sidebar}>
            <div style={S.sidebarScroll}>
              <div style={S.sidebarSection}>CATEGORIES</div>
              {WIKI_CATEGORIES.map(cat => {
                const articles = articlesByCategory.get(cat.id) ?? [];
                const isExpanded = expandedCategories.has(cat.id);
                const hasArticles = articles.length > 0;

                return (
                  <div key={cat.id}>
                    <button
                      style={S.categoryBtn}
                      onClick={() => toggleCategory(cat.id)}
                    >
                      {isExpanded
                        ? <ChevronDown size={12} style={{ flexShrink: 0 }} />
                        : <ChevronRight size={12} style={{ flexShrink: 0 }} />
                      }
                      <span style={S.categoryIcon}>{cat.icon}</span>
                      <span style={S.categoryLabel}>{cat.name}</span>
                      {hasArticles && (
                        <span style={S.categoryCount}>{articles.length}</span>
                      )}
                    </button>
                    {isExpanded && articles.map(article => (
                      <button
                        key={article.id}
                        style={{
                          ...S.articleBtn,
                          ...(selectedArticleId === article.id ? S.articleBtnActive : {}),
                        }}
                        onClick={() => setSelectedArticleId(article.id)}
                      >
                        {article.title}
                      </button>
                    ))}
                    {isExpanded && !hasArticles && (
                      <div style={S.emptyCategory}>No articles yet</div>
                    )}
                  </div>
                );
              })}

              {/* Glossary link */}
              <div style={S.sidebarDivider} />
              <div style={S.sidebarSection}>GLOSSARY</div>
              <div style={S.glossaryHint}>
                Press <kbd style={S.kbd}>?</kbd> to open the Glossary with 80+ terms
              </div>
            </div>
          </div>

          {/* Right Content */}
          <div style={S.content}>
            {selectedArticle ? (
              <div style={S.articleContainer}>
                <div style={S.articleHeader}>
                  <span style={S.articleCategory}>
                    {WIKI_CATEGORIES.find(c => c.id === selectedArticle.category)?.icon}{' '}
                    {WIKI_CATEGORIES.find(c => c.id === selectedArticle.category)?.name}
                  </span>
                  <h1 style={S.articleTitle}>{selectedArticle.title}</h1>
                  <p style={S.articleSummary}>{selectedArticle.summary}</p>
                </div>
                <div style={S.articleDivider} />
                <div style={S.articleBody}>
                  <ArticleContent content={selectedArticle.content} />
                </div>

                {/* Glossary terms */}
                {selectedArticle.glossaryTerms && selectedArticle.glossaryTerms.length > 0 && (
                  <div style={S.glossaryTerms}>
                    <span style={S.glossaryTermsLabel}>Key Terms:</span>
                    {selectedArticle.glossaryTerms.map(term => (
                      <span key={term} style={S.glossaryTerm}>{term}</span>
                    ))}
                  </div>
                )}

                {/* Related articles */}
                {relatedArticles.length > 0 && (
                  <div style={S.relatedSection}>
                    <div style={S.relatedLabel}>Related Articles</div>
                    <div style={S.relatedLinks}>
                      {relatedArticles.map(ra => (
                        <button
                          key={ra.id}
                          style={S.relatedLink}
                          onClick={() => setSelectedArticleId(ra.id)}
                        >
                          <ExternalLink size={12} />
                          {ra.title}
                        </button>
                      ))}
                    </div>
                  </div>
                )}
              </div>
            ) : (
              <div style={S.emptyState}>
                <BookOpen size={48} style={{ color: 'var(--border)', marginBottom: '16px' }} />
                <h2 style={S.emptyTitle}>
                  {totalArticles === 0
                    ? 'Wiki Coming Soon'
                    : 'Select an Article'
                  }
                </h2>
                <p style={S.emptyText}>
                  {totalArticles === 0
                    ? 'Educational articles are being written. Check back soon!'
                    : 'Choose a topic from the sidebar to start learning.'
                  }
                </p>
              </div>
            )}
          </div>
        </div>

        {/* Footer */}
        <div style={S.footer}>
          <span style={{ fontSize: '11px', color: 'var(--text-disabled)' }}>
            Ctrl+W to toggle | Ctrl+F to search | Esc to close
          </span>
        </div>
      </div>
    </div>
  );
}

/**
 * Renders article content with simple markup support:
 * - Lines starting with ## → h2 heading
 * - Lines starting with ### → h3 heading
 * - Lines starting with - → bullet list items
 * - Lines starting with > → tip callout boxes
 * - Empty lines → paragraph break
 * - Inline: **bold**, `code`
 */
function ArticleContent({ content }: { content: string }) {
  if (!content) return null;

  const lines = content.split('\n');
  const elements: React.ReactNode[] = [];
  let i = 0;

  while (i < lines.length) {
    const line = lines[i];

    // Heading ##
    if (line.startsWith('### ')) {
      elements.push(
        <h3 key={i} style={S.h3}>{renderInline(line.slice(4))}</h3>
      );
      i++;
      continue;
    }
    if (line.startsWith('## ')) {
      elements.push(
        <h2 key={i} style={S.h2}>{renderInline(line.slice(3))}</h2>
      );
      i++;
      continue;
    }

    // Bullet list
    if (line.startsWith('- ')) {
      const items: string[] = [];
      while (i < lines.length && lines[i].startsWith('- ')) {
        items.push(lines[i].slice(2));
        i++;
      }
      elements.push(
        <ul key={`ul-${i}`} style={S.ul}>
          {items.map((item, idx) => (
            <li key={idx} style={S.li}>{renderInline(item)}</li>
          ))}
        </ul>
      );
      continue;
    }

    // Tip callout
    if (line.startsWith('> ')) {
      const tipLines: string[] = [];
      while (i < lines.length && lines[i].startsWith('> ')) {
        tipLines.push(lines[i].slice(2));
        i++;
      }
      elements.push(
        <div key={`tip-${i}`} style={S.tip}>
          <span style={S.tipIcon}>💡</span>
          <div>{tipLines.map((tl, idx) => (
            <span key={idx}>{renderInline(tl)}{idx < tipLines.length - 1 ? <br /> : null}</span>
          ))}</div>
        </div>
      );
      continue;
    }

    // Empty line
    if (line.trim() === '') {
      i++;
      continue;
    }

    // Paragraph: collect consecutive non-special lines
    const paraLines: string[] = [];
    while (i < lines.length && lines[i].trim() !== '' && !lines[i].startsWith('## ') && !lines[i].startsWith('### ') && !lines[i].startsWith('- ') && !lines[i].startsWith('> ')) {
      paraLines.push(lines[i]);
      i++;
    }
    elements.push(
      <p key={`p-${i}`} style={S.paragraph}>{renderInline(paraLines.join(' '))}</p>
    );
  }

  return <>{elements}</>;
}

/** Renders **bold** and `code` inline formatting */
function renderInline(text: string): React.ReactNode {
  // Split by **bold** and `code` patterns
  const parts: React.ReactNode[] = [];
  let remaining = text;
  let key = 0;

  while (remaining.length > 0) {
    // Find the next special pattern
    const boldIdx = remaining.indexOf('**');
    const codeIdx = remaining.indexOf('`');

    // No more patterns
    if (boldIdx === -1 && codeIdx === -1) {
      parts.push(remaining);
      break;
    }

    // Determine which pattern comes first
    const nextIdx = boldIdx === -1 ? codeIdx : codeIdx === -1 ? boldIdx : Math.min(boldIdx, codeIdx);

    // Add text before the pattern
    if (nextIdx > 0) {
      parts.push(remaining.slice(0, nextIdx));
      remaining = remaining.slice(nextIdx);
    }

    // Bold pattern
    if (remaining.startsWith('**')) {
      const endIdx = remaining.indexOf('**', 2);
      if (endIdx === -1) {
        parts.push(remaining);
        break;
      }
      parts.push(
        <strong key={`b-${key++}`} style={{ color: 'var(--text-primary)', fontWeight: 700 }}>
          {remaining.slice(2, endIdx)}
        </strong>
      );
      remaining = remaining.slice(endIdx + 2);
      continue;
    }

    // Code pattern
    if (remaining.startsWith('`')) {
      const endIdx = remaining.indexOf('`', 1);
      if (endIdx === -1) {
        parts.push(remaining);
        break;
      }
      parts.push(
        <code key={`c-${key++}`} style={S.inlineCode}>
          {remaining.slice(1, endIdx)}
        </code>
      );
      remaining = remaining.slice(endIdx + 1);
      continue;
    }

    // Shouldn't reach here, but safety
    parts.push(remaining[0]);
    remaining = remaining.slice(1);
  }

  return parts.length === 1 ? parts[0] : <>{parts}</>;
}

// --- Styles ---

const S: Record<string, React.CSSProperties> = {
  overlay: {
    position: 'fixed', inset: 0, zIndex: 9000,
    background: 'rgba(0,0,0,0.7)', backdropFilter: 'blur(3px)',
    display: 'flex', alignItems: 'center', justifyContent: 'center',
  },
  modal: {
    width: '90vw', maxWidth: '1100px', height: '85vh',
    background: 'var(--bg-secondary)',
    border: '1px solid var(--border)', borderRadius: '12px',
    display: 'flex', flexDirection: 'column' as const, overflow: 'hidden',
    boxShadow: '0 16px 64px rgba(0,0,0,0.5)',
  },

  // Header
  header: {
    display: 'flex', justifyContent: 'space-between', alignItems: 'center',
    padding: '12px 20px', borderBottom: '1px solid var(--border)',
    background: 'var(--bg-tertiary)',
  },
  headerLeft: {
    display: 'flex', alignItems: 'center', gap: '10px',
  },
  headerRight: {
    display: 'flex', alignItems: 'center', gap: '12px',
  },
  title: {
    fontSize: '16px', fontWeight: 700, color: 'var(--text-primary)',
    fontFamily: 'var(--font-mono)', letterSpacing: '0.5px',
  },
  articleCount: {
    fontSize: '11px', color: 'var(--text-disabled)',
    background: 'var(--bg-primary)', padding: '2px 8px', borderRadius: '4px',
    fontFamily: 'var(--font-mono)',
  },
  searchBox: {
    display: 'flex', alignItems: 'center', gap: '8px',
    background: 'var(--bg-input)', border: '1px solid var(--border)',
    borderRadius: '6px', padding: '6px 12px', width: '280px',
  },
  searchInput: {
    flex: 1, background: 'transparent', border: 'none',
    color: 'var(--text-primary)', fontSize: '13px',
    fontFamily: 'var(--font-ui)', outline: 'none',
  },
  matchBadge: {
    fontSize: '10px', color: 'var(--text-accent)',
    background: 'color-mix(in srgb, var(--text-accent) 10%, transparent)', padding: '1px 6px',
    borderRadius: '3px', fontFamily: 'var(--font-mono)', whiteSpace: 'nowrap' as const,
  },
  closeBtn: {
    background: 'transparent', border: 'none', color: 'var(--text-secondary)',
    cursor: 'pointer', padding: '4px', borderRadius: '4px',
  },

  // Body
  body: {
    display: 'flex', flex: 1, overflow: 'hidden',
  },

  // Sidebar
  sidebar: {
    width: '240px', borderRight: '1px solid var(--border)',
    background: 'var(--bg-primary)', display: 'flex', flexDirection: 'column' as const,
  },
  sidebarScroll: {
    flex: 1, overflowY: 'auto' as const, padding: '8px 0',
  },
  sidebarSection: {
    fontSize: '10px', fontWeight: 700, color: 'var(--text-disabled)',
    letterSpacing: '1.5px', padding: '12px 16px 4px',
  },
  sidebarDivider: {
    height: '1px', background: 'var(--border)', margin: '8px 16px',
  },
  categoryBtn: {
    display: 'flex', alignItems: 'center', gap: '6px', width: '100%',
    background: 'transparent', border: 'none', padding: '7px 16px',
    color: 'var(--text-secondary)', cursor: 'pointer', fontSize: '13px',
    fontFamily: 'var(--font-ui)', fontWeight: 600, textAlign: 'left' as const,
    transition: 'color 100ms',
  },
  categoryIcon: {
    fontSize: '14px', width: '20px', textAlign: 'center' as const,
  },
  categoryLabel: {
    flex: 1,
  },
  categoryCount: {
    fontSize: '10px', color: 'var(--text-disabled)', fontFamily: 'var(--font-mono)',
    background: 'var(--bg-tertiary)', padding: '1px 5px', borderRadius: '3px',
  },
  articleBtn: {
    display: 'block', width: '100%', background: 'transparent', border: 'none',
    padding: '5px 16px 5px 48px', color: 'var(--text-secondary)', cursor: 'pointer',
    fontSize: '12px', fontFamily: 'var(--font-ui)', textAlign: 'left' as const,
    transition: 'all 100ms', whiteSpace: 'nowrap' as const, overflow: 'hidden',
    textOverflow: 'ellipsis',
  },
  articleBtnActive: {
    color: 'var(--text-accent)', background: 'color-mix(in srgb, var(--text-accent) 8%, transparent)',
    borderLeft: '2px solid var(--text-accent)', paddingLeft: '46px',
  },
  emptyCategory: {
    padding: '4px 16px 4px 48px', fontSize: '11px', color: 'var(--text-disabled)',
    fontStyle: 'italic',
  },
  glossaryHint: {
    padding: '8px 16px', fontSize: '12px', color: 'var(--text-disabled)',
    lineHeight: 1.5,
  },
  kbd: {
    background: 'var(--bg-tertiary)', padding: '1px 5px', borderRadius: '3px',
    fontSize: '11px', fontFamily: 'var(--font-mono)', border: '1px solid var(--border)',
    color: 'var(--text-primary)',
  },

  // Content area
  content: {
    flex: 1, overflowY: 'auto' as const, background: 'var(--bg-primary)',
  },
  articleContainer: {
    maxWidth: '720px', margin: '0 auto', padding: '32px 40px',
  },
  articleHeader: {
    marginBottom: '8px',
  },
  articleCategory: {
    fontSize: '11px', fontWeight: 600, color: 'var(--text-accent)',
    letterSpacing: '0.5px',
  },
  articleTitle: {
    fontSize: '24px', fontWeight: 700, color: 'var(--text-primary)',
    margin: '8px 0 12px', lineHeight: 1.3,
  },
  articleSummary: {
    fontSize: '14px', color: 'var(--text-secondary)', lineHeight: 1.6,
    margin: 0, fontStyle: 'italic',
  },
  articleDivider: {
    height: '1px', background: 'var(--border)', margin: '20px 0',
  },
  articleBody: {
    fontSize: '14px', color: 'var(--text-secondary)', lineHeight: 1.7,
  },

  // Article content elements
  h2: {
    fontSize: '18px', fontWeight: 700, color: 'var(--text-primary)',
    margin: '28px 0 12px', paddingBottom: '6px',
    borderBottom: '1px solid var(--border)',
  },
  h3: {
    fontSize: '15px', fontWeight: 700, color: 'var(--text-primary)',
    margin: '20px 0 8px',
  },
  paragraph: {
    fontSize: '14px', color: 'var(--text-secondary)', lineHeight: 1.7,
    margin: '0 0 14px',
  },
  ul: {
    margin: '0 0 14px', paddingLeft: '20px',
  },
  li: {
    fontSize: '14px', color: 'var(--text-secondary)', lineHeight: 1.7,
    marginBottom: '4px',
  },
  tip: {
    display: 'flex', gap: '10px',
    background: 'color-mix(in srgb, var(--text-accent) 6%, transparent)', border: '1px solid color-mix(in srgb, var(--text-accent) 15%, transparent)',
    borderRadius: '6px', padding: '12px 16px', margin: '14px 0',
    fontSize: '13px', color: 'var(--text-secondary)', lineHeight: 1.6,
  },
  tipIcon: {
    fontSize: '16px', flexShrink: 0, marginTop: '1px',
  },
  inlineCode: {
    fontFamily: 'var(--font-mono)', fontSize: '12px',
    background: 'var(--bg-tertiary)', border: '1px solid var(--border)',
    borderRadius: '3px', padding: '1px 5px',
    color: 'var(--text-accent)',
  },

  // Glossary terms
  glossaryTerms: {
    display: 'flex', flexWrap: 'wrap' as const, gap: '6px', alignItems: 'center',
    padding: '12px 0', marginTop: '16px', borderTop: '1px solid var(--border)',
  },
  glossaryTermsLabel: {
    fontSize: '11px', fontWeight: 700, color: 'var(--text-disabled)',
    letterSpacing: '0.5px', marginRight: '4px',
  },
  glossaryTerm: {
    fontSize: '11px', fontWeight: 600, color: 'var(--text-accent)',
    background: 'color-mix(in srgb, var(--text-accent) 8%, transparent)', border: '1px solid color-mix(in srgb, var(--text-accent) 15%, transparent)',
    padding: '2px 8px', borderRadius: '4px',
  },

  // Related articles
  relatedSection: {
    marginTop: '24px', padding: '16px 0', borderTop: '1px solid var(--border)',
  },
  relatedLabel: {
    fontSize: '11px', fontWeight: 700, color: 'var(--text-disabled)',
    letterSpacing: '1px', marginBottom: '8px',
  },
  relatedLinks: {
    display: 'flex', flexWrap: 'wrap' as const, gap: '8px',
  },
  relatedLink: {
    display: 'flex', alignItems: 'center', gap: '4px',
    background: 'var(--bg-tertiary)', border: '1px solid var(--border)',
    borderRadius: '6px', padding: '6px 12px', cursor: 'pointer',
    color: 'var(--text-accent)', fontSize: '12px', fontWeight: 600,
    fontFamily: 'var(--font-ui)', transition: 'all 100ms',
  },

  // Empty state
  emptyState: {
    display: 'flex', flexDirection: 'column' as const,
    alignItems: 'center', justifyContent: 'center',
    height: '100%', padding: '40px',
  },
  emptyTitle: {
    fontSize: '18px', fontWeight: 700, color: 'var(--text-primary)',
    margin: '0 0 8px',
  },
  emptyText: {
    fontSize: '14px', color: 'var(--text-disabled)', margin: 0,
    textAlign: 'center' as const,
  },

  // Footer
  footer: {
    padding: '8px 20px', borderTop: '1px solid var(--border)',
    textAlign: 'center' as const, background: 'var(--bg-tertiary)',
  },
};
