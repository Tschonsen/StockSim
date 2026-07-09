import { useEffect, useRef, useState } from 'react';

interface TitleScreenProps {
  onNewGame: () => void;
  onContinue: () => void;
  onLoadGame: () => void;
  onSettings: () => void;
  onQuit?: () => void;
  hasSaves: boolean;
}

export function TitleScreen({ onNewGame, onContinue, onLoadGame, onSettings, onQuit, hasSaves }: TitleScreenProps) {
  const [visible, setVisible] = useState(false);
  useEffect(() => { setTimeout(() => setVisible(true), 100); }, []);

  return (
    <div style={S.screen}>
      <TickerBg />

      {/* Left column: logo + menu */}
      <div style={S.left}>
        <div style={{ ...S.fadeIn, opacity: visible ? 1 : 0, transitionDelay: '0ms' }}>
          <h1 style={S.logo}>STOCKSIM</h1>
          <p style={S.tagline}>Trade. Speculate. Dominate.</p>
        </div>

        <nav style={S.nav}>
          {/* Primary group */}
          <div style={S.group}>
            <Btn label="New Game" onClick={onNewGame} primary delay={visible ? 120 : 0} />
            {hasSaves && <Btn label="Continue" onClick={onContinue} delay={visible ? 180 : 0} />}
            <Btn label="Load Game" onClick={onLoadGame} disabled={!hasSaves} delay={visible ? 240 : 0} />
          </div>

          {/* Online — coming soon */}
          <OnlineBtn delay={visible ? 300 : 0} />

          {/* Secondary group */}
          <div style={{ ...S.group, marginTop: '14px' }}>
            <Btn label="Settings" onClick={onSettings} small delay={visible ? 380 : 0} />
            {onQuit && <Btn label="Quit" onClick={onQuit} small muted delay={visible ? 440 : 0} />}
          </div>
        </nav>

        <div style={S.version}>v0.3.0 — <em>Deep Markets</em></div>
      </div>

      {/* Right: Patch Notes */}
      <div style={S.rightWrap}>
        <div style={{ ...S.panel, ...S.fadeIn, opacity: visible ? 1 : 0, transitionDelay: '500ms' }}>
          <div style={S.panelHead}>
            <span style={S.panelLabel}>PATCH NOTES</span>
            <span style={S.panelVer}>v0.2.0 — Wall Street</span>
          </div>
          <div style={S.panelBody}>
            <Sec t="v0.3.0 Highlights" items={[
              'Supply Chain: events cascade through supplier/customer networks',
              'History Mode: 9 playable historical crises (2008, COVID, GameStop...)',
              'Dynamic Fundamentals: CEO archetype drives revenue, margins, employees',
              'Seasonality: January Effect, Sell in May, Triple Witching, Holiday Rally',
              'Elections, FOMC Meetings, Shareholder Votes, CEO Firing',
              'Commodity ETFs: GLD, SLV, USO tracking gold/silver/oil',
              '39 scenarios, 53 achievements, 2,400+ news headlines',
              'RSI sub-chart, price alerts, stock comparison, sector rotation',
            ]} />
            <Sec t="Core Features" items={['Education Wiki — 57 articles','Options Trading — Black-Scholes, Greeks, IV','Supply Chain + Whisper Network — no other game has this']} />
            <Sec t="Realism" items={['Wash Sale Rule (30-day cost basis adjustment)','Player Reputation (Market Influence + SEC Scrutiny)','Dynamic Fundamentals (Revenue/Earnings drift daily)','Overnight gaps, margin interest, short borrow fees']} />
            <Sec t="Content" items={['20 scenarios (incl. Big Short, Pandemic, Squeeze)','16 Decision Cases (interactive learning)','500 event templates, 226 analysts, 80 glossary entries']} />
            <Sec t="Polish" items={['Achievement animations, career rank promotion','VIX gauge, Fear & Greed, Market Phase display','Auto-pause on news/alerts/margin calls','Settings fully wired (audio toggles, keybindings)','584 automated tests, Playwright E2E setup']} />
          </div>
          <div style={S.panelFoot}>Next: Steam Early Access — August 2026</div>
        </div>
      </div>
    </div>
  );
}

function OnlineBtn({ delay }: { delay: number }) {
  const [h, setH] = useState(false);
  return (
    <div style={{ ...S.fadeIn, opacity: delay ? 1 : 0, transitionDelay: `${delay}ms` }}>
      <button
        disabled
        onMouseEnter={() => setH(true)}
        onMouseLeave={() => setH(false)}
        style={{ ...S.btn, ...S.btnDisabled, cursor: 'default' }}
      >
        <span style={{ transition: 'opacity 200ms', opacity: h ? 0 : 1 }}>Online</span>
        <span style={{ transition: 'opacity 200ms', opacity: h ? 1 : 0, position: 'absolute', left: '20px', fontSize: '13px', color: 'var(--text-disabled)', fontStyle: 'italic', fontWeight: 400 }}>Coming Soon</span>
      </button>
    </div>
  );
}

function Btn({ label, onClick, primary, small, muted, disabled, delay }: {
  label: string; onClick: () => void; primary?: boolean; small?: boolean; muted?: boolean; disabled?: boolean; delay?: number;
}) {
  const [h, setH] = useState(false);
  return (
    <div style={{ ...S.fadeIn, opacity: delay ? 1 : 0, transitionDelay: `${delay}ms` }}>
      <button onClick={onClick} disabled={disabled}
        onMouseEnter={() => setH(true)} onMouseLeave={() => setH(false)}
        style={{
          ...S.btn,
          ...(primary ? S.btnPrimary : {}),
          ...(small ? S.btnSmall : {}),
          ...(muted ? S.btnMuted : {}),
          ...(disabled ? S.btnDisabled : {}),
          ...(h && !disabled ? S.btnHover : {}),
        }}>
        {label}
      </button>
    </div>
  );
}

function Sec({ t, items }: { t: string; items: string[] }) {
  return (
    <div style={{ marginBottom: '12px' }}>
      <div style={{ fontSize: '9px', fontWeight: 700, color: 'var(--text-accent)', letterSpacing: '1.5px', marginBottom: '3px' }}>{t.toUpperCase()}</div>
      {items.map((x, i) => <div key={i} style={{ fontSize: '11px', color: 'var(--text-disabled)', padding: '1px 0 1px 8px', lineHeight: 1.5 }}><span style={{ color: 'var(--text-secondary)', marginRight: '4px' }}>·</span>{x}</div>)}
    </div>
  );
}

function TickerBg() {
  const ref = useRef<HTMLCanvasElement>(null);
  useEffect(() => {
    const c = ref.current; if (!c) return;
    const ctx = c.getContext('2d'); if (!ctx) return;
    c.width = window.innerWidth; c.height = window.innerHeight;
    const syms = ['AAPL','GOOG','MSFT','AMZN','TSLA','META','NVDA','JPM','XOM','PFE','WMT','DIS','AMD','INTC','CRM','NFLX'];
    type T = { x: number; y: number; s: string; p: number; sp: number; f: number; d: 'up'|'down' };
    const ts: T[] = Array.from({ length: 45 }, () => ({
      x: Math.random()*c.width, y: Math.random()*c.height,
      s: syms[Math.floor(Math.random()*syms.length)],
      p: 10+Math.random()*400, sp: 0.12+Math.random()*0.35,
      f: 0, d: Math.random()>0.5?'up':'down',
    }));
    let id: number;
    const draw = () => {
      ctx.clearRect(0,0,c.width,c.height);
      ctx.font = '11px "JetBrains Mono",monospace';
      for (const t of ts) {
        t.x -= t.sp; t.y -= t.sp*0.15;
        if (t.x < -150) { t.x = c.width+50; t.y = Math.random()*c.height; }
        if (t.y < -20) t.y = c.height+20;
        if (Math.random()<0.003) { t.f=1; t.d=Math.random()>0.5?'up':'down'; t.p+=t.d==='up'?Math.random()*2:-Math.random()*2; t.p=Math.max(1,t.p); }
        ctx.fillStyle = t.f>0?(t.d==='up'?'#10B981':'#EF4444'):'#4B5563';
        ctx.globalAlpha = 0.05+(t.f>0?t.f*0.15:0);
        ctx.fillText(`${t.s} $${t.p.toFixed(2)}`,t.x,t.y);
        if (t.f>0) t.f-=0.012;
      }
      ctx.globalAlpha=1;
      id=requestAnimationFrame(draw);
    };
    draw();
    const r=()=>{c.width=window.innerWidth;c.height=window.innerHeight;};
    window.addEventListener('resize',r);
    return ()=>{cancelAnimationFrame(id);window.removeEventListener('resize',r);};
  },[]);
  return <canvas ref={ref} style={{ position:'absolute',inset:0,width:'100%',height:'100%',pointerEvents:'none' }} />;
}

const S: Record<string, React.CSSProperties> = {
  screen: { position:'fixed',inset:0,background:'var(--bg-primary)',display:'flex',zIndex:9000 },
  left: { position:'relative',zIndex:1,display:'flex',flexDirection:'column',justifyContent:'center',paddingLeft:'100px',width:'55%' },
  logo: { fontFamily:'var(--font-mono)',fontSize:'52px',fontWeight:700,color:'var(--text-primary)',letterSpacing:'6px',margin:0,textShadow:'0 0 30px color-mix(in srgb, var(--text-accent) 20%, transparent),0 0 60px color-mix(in srgb, var(--text-accent) 8%, transparent)' },
  tagline: { fontFamily:'var(--font-ui)',fontSize:'15px',color:'var(--text-secondary)',marginTop:'6px',letterSpacing:'2px' },
  nav: { marginTop:'40px',display:'flex',flexDirection:'column',gap:'6px' },
  group: { display:'flex',flexDirection:'column',gap:'6px' },
  btn: { width:'280px',height:'44px',background:'var(--bg-tertiary)',border:'1px solid var(--border)',borderRadius:'6px',color:'var(--text-primary)',fontFamily:'var(--font-ui)',fontSize:'15px',fontWeight:500,cursor:'pointer',textAlign:'left',paddingLeft:'20px',transition:'all 150ms',letterSpacing:'0.5px' },
  btnPrimary: { borderLeft:'3px solid var(--green-primary)',color:'var(--text-primary)',fontWeight:600 },
  btnSmall: { height:'38px',fontSize:'13px',color:'var(--text-secondary)',fontWeight:400 },
  btnMuted: { background:'transparent',borderColor:'color-mix(in srgb, var(--border) 30%, transparent)',color:'var(--text-disabled)' },
  btnDisabled: { opacity:0.4,cursor:'default' },
  btnHover: { background:'color-mix(in srgb, var(--text-accent) 6%, transparent)',boxShadow:'inset 0 0 0 1px color-mix(in srgb, var(--text-accent) 20%, transparent)',transform:'translateX(3px)' },
  version: { position:'absolute',bottom:'24px',left:'100px',fontSize:'11px',color:'var(--text-disabled)',fontFamily:'var(--font-mono)' },
  fadeIn: { transition:'opacity 400ms ease' },
  // Right panel
  rightWrap: { position:'relative',zIndex:1,display:'flex',alignItems:'center',paddingRight:'60px',flex:1,justifyContent:'flex-end' },
  panel: { width:'250px',maxHeight:'460px',background:'rgba(17,24,39,0.9)',border:'1px solid color-mix(in srgb, var(--border) 40%, transparent)',borderRadius:'8px',display:'flex',flexDirection:'column',backdropFilter:'blur(12px)' },
  panelHead: { padding:'12px 14px 8px',borderBottom:'1px solid color-mix(in srgb, var(--border) 50%, transparent)',display:'flex',justifyContent:'space-between',alignItems:'baseline' },
  panelLabel: { fontSize:'9px',fontWeight:700,color:'var(--text-disabled)',letterSpacing:'2px' },
  panelVer: { fontSize:'10px',fontWeight:600,color:'var(--text-accent)',fontFamily:'var(--font-mono)' },
  panelBody: { flex:1,overflowY:'auto',padding:'10px 14px' },
  panelFoot: { padding:'8px 14px',borderTop:'1px solid color-mix(in srgb, var(--border) 50%, transparent)',fontSize:'10px',color:'var(--text-disabled)' },
};
