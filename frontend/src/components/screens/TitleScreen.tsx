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

        <div style={S.version}>v0.1.0 — <em>Diamond Hands</em></div>
      </div>

      {/* Right: Patch Notes */}
      <div style={S.rightWrap}>
        <div style={{ ...S.panel, ...S.fadeIn, opacity: visible ? 1 : 0, transitionDelay: '500ms' }}>
          <div style={S.panelHead}>
            <span style={S.panelLabel}>PATCH NOTES</span>
            <span style={S.panelVer}>Diamond Hands</span>
          </div>
          <div style={S.panelBody}>
            <Sec t="Trading" items={['Market, Limit, Stop, Stop-Limit, Trailing Stop','Short Selling / Cover','Order confirmation, slippage model']} />
            <Sec t="Simulation" items={['250+ stocks, 12 sectors','1 year historical data','50+ events, IPO, delisting, flash crash','Circuit breaker, economic cycle, gap up/down']} />
            <Sec t="AI" items={['Market Maker, Retail, Institutional, Algorithmic']} />
            <Sec t="Analysis" items={['Candlestick charts, 5 indicators','Orderbook depth, stock screener']} />
            <Sec t="Portfolio" items={['P&L tracking, allocation bar','Dividends, price alerts, day summary']} />
          </div>
          <div style={S.panelFoot}>Next: Margin Trading, Analyst Ratings, Achievements</div>
        </div>
      </div>
    </div>
  );
}

function OnlineBtn({ delay }: { delay: number }) {
  const [h, setH] = useState(false);
  return (
    <div style={{ ...S.fadeIn, opacity: delay ? 1 : 0, transitionDelay: `${delay}ms` }}>
      <div
        onMouseEnter={() => setH(true)}
        onMouseLeave={() => setH(false)}
        style={{ ...S.btn, ...S.btnDisabled, cursor: 'default', opacity: 0.4, position: 'relative', overflow: 'hidden' }}
      >
        <span style={{ transition: 'opacity 200ms', opacity: h ? 0 : 1 }}>Online</span>
        <span style={{ transition: 'opacity 200ms', opacity: h ? 1 : 0, position: 'absolute', left: '20px', top: '50%', transform: 'translateY(-50%)', fontSize: '13px', color: 'var(--text-disabled)', fontStyle: 'italic', fontWeight: 400 }}>Coming Soon</span>
      </div>
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
  logo: { fontFamily:'var(--font-mono)',fontSize:'52px',fontWeight:700,color:'var(--text-primary)',letterSpacing:'6px',margin:0,textShadow:'0 0 30px rgba(96,165,250,0.2),0 0 60px rgba(96,165,250,0.08)' },
  tagline: { fontFamily:'var(--font-ui)',fontSize:'15px',color:'var(--text-secondary)',marginTop:'6px',letterSpacing:'2px' },
  nav: { marginTop:'40px',display:'flex',flexDirection:'column',gap:'6px' },
  group: { display:'flex',flexDirection:'column',gap:'6px' },
  btn: { width:'280px',height:'44px',background:'var(--bg-tertiary)',border:'1px solid var(--border)',borderRadius:'6px',color:'var(--text-primary)',fontFamily:'var(--font-ui)',fontSize:'15px',fontWeight:500,cursor:'pointer',textAlign:'left',paddingLeft:'20px',transition:'all 150ms',letterSpacing:'0.5px' },
  btnPrimary: { borderLeft:'3px solid #10B981',color:'var(--text-primary)',fontWeight:600 },
  btnSmall: { height:'38px',fontSize:'13px',color:'var(--text-secondary)',fontWeight:400 },
  btnMuted: { background:'transparent',borderColor:'rgba(31,41,55,0.3)',color:'var(--text-disabled)' },
  btnDisabled: { opacity:0.4,cursor:'default' },
  btnHover: { background:'rgba(96,165,250,0.06)',borderColor:'rgba(96,165,250,0.25)',transform:'translateX(3px)' },
  version: { position:'absolute',bottom:'24px',left:'100px',fontSize:'11px',color:'var(--text-disabled)',fontFamily:'var(--font-mono)' },
  fadeIn: { transition:'opacity 400ms ease' },
  // Right panel
  rightWrap: { position:'relative',zIndex:1,display:'flex',alignItems:'center',paddingRight:'60px',flex:1,justifyContent:'flex-end' },
  panel: { width:'250px',maxHeight:'460px',background:'rgba(17,24,39,0.9)',border:'1px solid rgba(31,41,55,0.4)',borderRadius:'8px',display:'flex',flexDirection:'column',backdropFilter:'blur(12px)' },
  panelHead: { padding:'12px 14px 8px',borderBottom:'1px solid rgba(31,41,55,0.5)',display:'flex',justifyContent:'space-between',alignItems:'baseline' },
  panelLabel: { fontSize:'9px',fontWeight:700,color:'var(--text-disabled)',letterSpacing:'2px' },
  panelVer: { fontSize:'10px',fontWeight:600,color:'var(--text-accent)',fontFamily:'var(--font-mono)' },
  panelBody: { flex:1,overflowY:'auto',padding:'10px 14px' },
  panelFoot: { padding:'8px 14px',borderTop:'1px solid rgba(31,41,55,0.5)',fontSize:'10px',color:'var(--text-disabled)' },
};
