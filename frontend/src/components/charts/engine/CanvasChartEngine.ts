/**
 * Custom Canvas-2D chart engine — the in-house replacement for the TradingView
 * Lightweight Charts library (see design/WORLD_SIM_VISION.md E4/§8). Framework- and
 * window-agnostic on purpose: it owns its own <canvas>, render loop and input handling so
 * the same engine can later drive detached panels on separate monitors. React only feeds it data.
 *
 * Renders: candle/line/area price pane with volume overlaid at its base + indicator line
 * overlays (SMA/EMA/Bollinger/VWAP), an RSI sub-pane (0–100 with 70/30 guides), a shared
 * time axis, auto Y-scale over the visible window, drag-pan, wheel-zoom and a crosshair.
 * The view is preserved across live ticks (only recentred on symbol/timeframe change).
 *
 * Colours are NOT hardcoded: the engine resolves them from the app's CSS design tokens
 * (globals.css :root — --bg-primary, --green-primary, --chart-*, …) at draw time, so it
 * follows the app theme automatically (incl. the colourblind palettes) and will inherit any
 * future frontend design overhaul without touching this file. A <canvas> can't use var(),
 * so we read the computed custom properties ourselves and derive alpha variants in JS.
 */

export interface Bar {
  time: number; // Unix seconds
  open: number;
  high: number;
  low: number;
  close: number;
  volume: number;
}

export type ChartKind = 'candle' | 'line' | 'area';

export interface IndicatorPoint {
  time: number;
  value: number;
}

export interface Indicators {
  sma20?: IndicatorPoint[];
  sma50?: IndicatorPoint[];
  sma200?: IndicatorPoint[];
  ema12?: IndicatorPoint[];
  bollingerUpper?: IndicatorPoint[];
  bollingerMiddle?: IndicatorPoint[];
  bollingerLower?: IndicatorPoint[];
  vwap?: IndicatorPoint[];
}

/** Resolved colour set for one render pass, pulled from the CSS design tokens. */
interface Theme {
  bg: string;
  grid: string;
  axis: string;
  text: string;
  crosshair: string;
  labelBg: string;
  labelText: string;
  up: string;
  down: string;
  volUp: string;
  volDown: string;
  line: string;
  areaTop: string;
  areaBottom: string;
  rsi: string;
  rsi70: string;
  rsi30: string;
  font: string;
  sma20: string;
  sma50: string;
  sma200: string;
  ema12: string;
  bollinger: string;
  vwap: string;
}

const OVERLAY_SPECS: { key: keyof Indicators; themeKey: keyof Theme; width: number }[] = [
  { key: 'sma20', themeKey: 'sma20', width: 1 },
  { key: 'sma50', themeKey: 'sma50', width: 1 },
  { key: 'sma200', themeKey: 'sma200', width: 2 },
  { key: 'ema12', themeKey: 'ema12', width: 1 },
  { key: 'bollingerUpper', themeKey: 'bollinger', width: 1 },
  { key: 'bollingerMiddle', themeKey: 'bollinger', width: 1 },
  { key: 'bollingerLower', themeKey: 'bollinger', width: 1 },
  { key: 'vwap', themeKey: 'vwap', width: 1 },
];

const AXIS_W = 60; // right-hand price-axis gutter (px)
const TIME_H = 22; // bottom time-axis gutter (px)
const RSI_FRACTION = 0.22; // share of the plot height given to the RSI sub-pane
const PANE_GAP = 10; // gap between price pane and RSI pane (px)
const VOLUME_FRACTION = 0.2; // share of the price pane height used by the volume overlay
const DEFAULT_WINDOW = 90; // bars shown on a fresh load, matching the old LW view
const MIN_VISIBLE = 10; // never zoom in tighter than this many bars
const BODY_FRACTION = 0.7; // candle body width relative to the per-bar slot
const PRICE_PAD = 0.08; // vertical padding above/below the visible price range
const RSI_PERIOD = 14;

interface Layout {
  plotW: number;
  priceTop: number;
  priceH: number;
  rsiTop: number;
  rsiH: number;
  hasRSI: boolean;
}

export class CanvasChartEngine {
  private canvas: HTMLCanvasElement;
  private ctx: CanvasRenderingContext2D;
  private ro: ResizeObserver;
  private rootStyle: CSSStyleDeclaration; // live computed style → design tokens
  private colors!: Theme; // resolved once per draw()

  private bars: Bar[] = [];
  private kind: ChartKind = 'candle';
  private indicators: Indicators | undefined;
  private rsi: IndicatorPoint[] = [];
  private timeIndex = new Map<number, number>(); // bar time → bar index (for aligning overlays)

  private from = 0; // first visible bar index (fractional for smooth pan/zoom)
  private to = 0; // one past the last visible bar index
  private prevLen = 0; // bar count at the previous setData (for right-edge follow)

  private cssW = 0;
  private cssH = 0;
  private dpr = 1;
  private mouse: { x: number; y: number } | null = null;
  private dragging = false;
  private dragStart = { x: 0, from: 0, to: 0 };
  private raf = 0;

  // Bound listeners kept for clean teardown.
  private readonly onMouseDown = (e: MouseEvent) => this.handleDown(e);
  private readonly onMouseMove = (e: MouseEvent) => this.handleMove(e);
  private readonly onMouseUp = () => this.handleUp();
  private readonly onMouseLeave = () => this.handleLeave();
  private readonly onWheel = (e: WheelEvent) => this.handleWheel(e);

  constructor(private container: HTMLElement) {
    this.canvas = document.createElement('canvas');
    this.canvas.style.display = 'block';
    this.canvas.style.width = '100%';
    this.canvas.style.height = '100%';
    this.canvas.style.cursor = 'crosshair';
    container.appendChild(this.canvas);

    const ctx = this.canvas.getContext('2d');
    if (!ctx) throw new Error('CanvasChartEngine: 2D context unavailable');
    this.ctx = ctx;
    this.rootStyle = getComputedStyle(container); // custom props inherit from :root

    this.canvas.addEventListener('mousedown', this.onMouseDown);
    this.canvas.addEventListener('mousemove', this.onMouseMove);
    window.addEventListener('mouseup', this.onMouseUp);
    this.canvas.addEventListener('mouseleave', this.onMouseLeave);
    this.canvas.addEventListener('wheel', this.onWheel, { passive: false });

    this.ro = new ResizeObserver(() => this.resize());
    this.ro.observe(container);
    this.resize();
  }

  /**
   * Replace the bar set. `resetView` recentres on the most recent DEFAULT_WINDOW bars
   * (symbol/timeframe change). Otherwise the current window width is preserved; if the view
   * was pinned to the latest bar it follows new bars, else the user's scroll position is kept.
   */
  setData(bars: Bar[], kind: ChartKind, indicators: Indicators | undefined, resetView: boolean): void {
    this.bars = bars;
    this.kind = kind;
    this.indicators = indicators;
    this.rsi = computeRSI(bars, RSI_PERIOD);
    this.timeIndex = new Map(bars.map((b, i) => [b.time, i]));

    if (resetView || this.to === 0) {
      this.to = bars.length;
      this.from = Math.max(0, bars.length - DEFAULT_WINDOW);
    } else {
      const width = this.to - this.from;
      const wasAtRightEdge = this.to >= this.prevLen - 0.5;
      if (wasAtRightEdge) {
        this.to = bars.length;
        this.from = Math.max(0, this.to - width);
      }
      this.clampView();
    }
    this.prevLen = bars.length;
    this.scheduleDraw();
  }

  resize(): void {
    const rect = this.container.getBoundingClientRect();
    this.cssW = rect.width;
    this.cssH = rect.height;
    this.dpr = window.devicePixelRatio || 1;
    this.canvas.width = Math.max(1, Math.round(this.cssW * this.dpr));
    this.canvas.height = Math.max(1, Math.round(this.cssH * this.dpr));
    this.scheduleDraw();
  }

  destroy(): void {
    this.ro.disconnect();
    cancelAnimationFrame(this.raf);
    this.canvas.removeEventListener('mousedown', this.onMouseDown);
    this.canvas.removeEventListener('mousemove', this.onMouseMove);
    window.removeEventListener('mouseup', this.onMouseUp);
    this.canvas.removeEventListener('mouseleave', this.onMouseLeave);
    this.canvas.removeEventListener('wheel', this.onWheel);
    this.canvas.remove();
  }

  // ---- Design tokens -------------------------------------------------------

  /** Read the current CSS custom property, trimmed, with a fallback if unset. */
  private cssVar(name: string, fallback: string): string {
    const v = this.rootStyle.getPropertyValue(name).trim();
    return v || fallback;
  }

  /** Resolve the full colour set from CSS tokens (live → follows theme switches). */
  private readTheme(): Theme {
    const border = this.cssVar('--border', '#1F2937');
    const text = this.cssVar('--text-secondary', '#9CA3AF');
    const up = this.cssVar('--green-primary', '#10B981');
    const down = this.cssVar('--red-primary', '#EF4444');
    const line = this.cssVar('--text-accent', '#60A5FA');
    return {
      bg: this.cssVar('--bg-primary', '#0A0E17'),
      grid: hexToRgba(border, 0.5, 'rgba(31, 41, 55, 0.5)'),
      axis: border,
      text,
      crosshair: hexToRgba(text, 0.5, 'rgba(156, 163, 175, 0.5)'),
      labelBg: this.cssVar('--bg-tertiary', '#1F2937'),
      labelText: this.cssVar('--text-primary', '#E5E7EB'),
      up,
      down,
      volUp: this.cssVar('--green-glow', 'rgba(16, 185, 129, 0.4)'),
      volDown: this.cssVar('--red-glow', 'rgba(239, 68, 68, 0.4)'),
      line,
      areaTop: hexToRgba(line, 0.4, 'rgba(96, 165, 250, 0.4)'),
      areaBottom: hexToRgba(line, 0.02, 'rgba(96, 165, 250, 0.02)'),
      rsi: this.cssVar('--chart-purple', '#8B5CF6'),
      rsi70: hexToRgba(down, 0.3, 'rgba(239, 68, 68, 0.3)'),
      rsi30: hexToRgba(up, 0.3, 'rgba(16, 185, 129, 0.3)'),
      font: `11px ${this.cssVar('--font-mono', "'JetBrains Mono', monospace")}`,
      sma20: this.cssVar('--warning', '#F59E0B'),
      sma50: this.cssVar('--chart-purple', '#8B5CF6'),
      sma200: this.cssVar('--chart-pink', '#EC4899'),
      ema12: this.cssVar('--chart-cyan', '#06B6D4'),
      bollinger: this.cssVar('--chart-blue', '#60A5FA'),
      vwap: this.cssVar('--chart-cyan', '#06B6D4'),
    };
  }

  // ---- Input ---------------------------------------------------------------

  private handleDown(e: MouseEvent): void {
    this.dragging = true;
    this.dragStart = { x: e.offsetX, from: this.from, to: this.to };
  }

  private handleMove(e: MouseEvent): void {
    this.mouse = { x: e.offsetX, y: e.offsetY };
    if (this.dragging) {
      const plotW = this.cssW - AXIS_W;
      const barW = plotW / (this.dragStart.to - this.dragStart.from);
      const deltaBars = (e.offsetX - this.dragStart.x) / barW;
      this.from = this.dragStart.from - deltaBars;
      this.to = this.dragStart.to - deltaBars;
      this.clampView();
    }
    this.scheduleDraw();
  }

  private handleUp(): void {
    this.dragging = false;
  }

  private handleLeave(): void {
    this.mouse = null;
    this.dragging = false;
    this.scheduleDraw();
  }

  private handleWheel(e: WheelEvent): void {
    e.preventDefault();
    const plotW = this.cssW - AXIS_W;
    if (plotW <= 0) return;
    const width = this.to - this.from;
    const factor = e.deltaY > 0 ? 1.1 : 1 / 1.1; // wheel down = zoom out
    const newWidth = clamp(width * factor, MIN_VISIBLE, Math.max(MIN_VISIBLE, this.bars.length));
    const anchor = this.from + (e.offsetX / plotW) * width; // bar index under the cursor
    this.from = anchor - (e.offsetX / plotW) * newWidth;
    this.to = this.from + newWidth;
    this.clampView();
    this.scheduleDraw();
  }

  private clampView(): void {
    const len = this.bars.length;
    let width = this.to - this.from;
    if (width < MIN_VISIBLE) width = MIN_VISIBLE;
    if (width > len) width = len;
    if (this.from < 0) this.from = 0;
    this.to = this.from + width;
    if (this.to > len) {
      this.to = len;
      this.from = Math.max(0, this.to - width);
    }
  }

  // ---- Rendering -----------------------------------------------------------

  private scheduleDraw(): void {
    cancelAnimationFrame(this.raf);
    this.raf = requestAnimationFrame(() => this.draw());
  }

  private layout(): Layout {
    const plotW = this.cssW - AXIS_W;
    const availH = this.cssH - TIME_H;
    const hasRSI = this.rsi.length > 0 && availH > 120;
    if (!hasRSI) {
      return { plotW, priceTop: 0, priceH: availH, rsiTop: 0, rsiH: 0, hasRSI };
    }
    const rsiH = availH * RSI_FRACTION;
    const priceH = availH - rsiH - PANE_GAP;
    return { plotW, priceTop: 0, priceH, rsiTop: priceH + PANE_GAP, rsiH, hasRSI };
  }

  private draw(): void {
    const ctx = this.ctx;
    this.colors = this.readTheme();
    ctx.setTransform(this.dpr, 0, 0, this.dpr, 0, 0);
    ctx.clearRect(0, 0, this.cssW, this.cssH);
    ctx.fillStyle = this.colors.bg;
    ctx.fillRect(0, 0, this.cssW, this.cssH);

    const L = this.layout();
    if (L.plotW <= 0 || L.priceH <= 0 || this.to <= this.from || this.bars.length === 0) return;

    const barW = L.plotW / (this.to - this.from);
    const xCenter = (i: number) => (i - this.from + 0.5) * barW;
    const lo = Math.max(0, Math.floor(this.from));
    const hi = Math.min(this.bars.length, Math.ceil(this.to));

    const { min, max } = this.priceRange(lo, hi);
    const y = (p: number) => L.priceTop + L.priceH * (1 - (p - min) / (max - min));

    this.drawPriceGrid(L, min, max, y);
    this.drawTimeAxis(L, xCenter, lo, hi);
    this.drawVolume(L, barW, xCenter, lo, hi);
    if (this.kind === 'candle') this.drawCandles(barW, xCenter, y, lo, hi);
    else this.drawLineOrArea(L, xCenter, y, lo, hi);
    this.drawOverlays(xCenter, y);
    if (L.hasRSI) this.drawRSIPane(L, xCenter);
    this.drawCrosshair(L, barW, min, max);
  }

  private priceRange(lo: number, hi: number): { min: number; max: number } {
    let min = Infinity;
    let max = -Infinity;
    for (let i = lo; i < hi; i++) {
      const b = this.bars[i];
      if (b.low < min) min = b.low;
      if (b.high > max) max = b.high;
    }
    if (!isFinite(min) || !isFinite(max)) return { min: 0, max: 1 };
    if (min === max) return { min: min - 1, max: max + 1 };
    const pad = (max - min) * PRICE_PAD;
    return { min: min - pad, max: max + pad };
  }

  private drawPriceGrid(L: Layout, min: number, max: number, y: (p: number) => number): void {
    const ctx = this.ctx;
    const c = this.colors;
    ctx.font = c.font;
    ctx.textBaseline = 'middle';
    ctx.textAlign = 'left';
    for (const price of niceTicks(min, max, 5)) {
      const py = y(price);
      if (py < L.priceTop || py > L.priceTop + L.priceH) continue;
      ctx.strokeStyle = c.grid;
      ctx.beginPath();
      ctx.moveTo(0, Math.round(py) + 0.5);
      ctx.lineTo(L.plotW, Math.round(py) + 0.5);
      ctx.stroke();
      ctx.fillStyle = c.text;
      ctx.fillText(fmtPrice(price), L.plotW + 6, py);
    }
    ctx.strokeStyle = c.axis;
    ctx.beginPath();
    ctx.moveTo(L.plotW + 0.5, 0);
    ctx.lineTo(L.plotW + 0.5, L.priceTop + L.priceH);
    ctx.stroke();
  }

  private drawTimeAxis(L: Layout, xCenter: (i: number) => number, lo: number, hi: number): void {
    const ctx = this.ctx;
    const c = this.colors;
    const visible = hi - lo;
    const stride = Math.max(1, Math.ceil(visible / 6));
    const yBottom = this.cssH - TIME_H;
    ctx.fillStyle = c.text;
    ctx.font = c.font;
    ctx.textBaseline = 'top';
    ctx.textAlign = 'center';
    for (let i = lo; i < hi; i += stride) {
      const bar = this.bars[i];
      if (!bar) continue;
      const px = xCenter(i);
      if (px < 0 || px > L.plotW) continue;
      ctx.strokeStyle = c.grid;
      ctx.beginPath();
      ctx.moveTo(Math.round(px) + 0.5, 0);
      ctx.lineTo(Math.round(px) + 0.5, yBottom);
      ctx.stroke();
      ctx.fillText(fmtDate(bar.time), px, yBottom + 5);
    }
  }

  private drawVolume(L: Layout, barW: number, xCenter: (i: number) => number, lo: number, hi: number): void {
    let maxVol = 0;
    for (let i = lo; i < hi; i++) if (this.bars[i].volume > maxVol) maxVol = this.bars[i].volume;
    if (maxVol <= 0) return;
    const ctx = this.ctx;
    const c = this.colors;
    const volH = L.priceH * VOLUME_FRACTION;
    const base = L.priceTop + L.priceH;
    const bodyW = Math.max(1, barW * BODY_FRACTION);
    for (let i = lo; i < hi; i++) {
      const b = this.bars[i];
      const h = (b.volume / maxVol) * volH;
      ctx.fillStyle = b.close >= b.open ? c.volUp : c.volDown;
      ctx.fillRect(xCenter(i) - bodyW / 2, base - h, bodyW, h);
    }
  }

  private drawCandles(barW: number, xCenter: (i: number) => number, y: (p: number) => number, lo: number, hi: number): void {
    const ctx = this.ctx;
    const c = this.colors;
    const bodyW = Math.max(1, barW * BODY_FRACTION);
    for (let i = lo; i < hi; i++) {
      const b = this.bars[i];
      const color = b.close >= b.open ? c.up : c.down;
      const cx = xCenter(i);
      ctx.strokeStyle = color;
      ctx.fillStyle = color;
      ctx.beginPath();
      ctx.moveTo(Math.round(cx) + 0.5, y(b.high));
      ctx.lineTo(Math.round(cx) + 0.5, y(b.low));
      ctx.stroke();
      const top = Math.min(y(b.open), y(b.close));
      const h = Math.max(1, Math.abs(y(b.close) - y(b.open)));
      ctx.fillRect(cx - bodyW / 2, top, bodyW, h);
    }
  }

  private drawLineOrArea(L: Layout, xCenter: (i: number) => number, y: (p: number) => number, lo: number, hi: number): void {
    const ctx = this.ctx;
    const c = this.colors;
    const trace = () => {
      ctx.beginPath();
      for (let i = lo; i < hi; i++) {
        const px = xCenter(i);
        const py = y(this.bars[i].close);
        if (i === lo) ctx.moveTo(px, py);
        else ctx.lineTo(px, py);
      }
    };
    if (this.kind === 'area') {
      trace();
      const grad = ctx.createLinearGradient(0, L.priceTop, 0, L.priceTop + L.priceH);
      grad.addColorStop(0, c.areaTop);
      grad.addColorStop(1, c.areaBottom);
      ctx.lineTo(xCenter(hi - 1), L.priceTop + L.priceH);
      ctx.lineTo(xCenter(lo), L.priceTop + L.priceH);
      ctx.closePath();
      ctx.fillStyle = grad;
      ctx.fill();
    }
    trace();
    ctx.strokeStyle = c.line;
    ctx.lineWidth = 2;
    ctx.stroke();
    ctx.lineWidth = 1;
  }

  private drawOverlays(xCenter: (i: number) => number, y: (p: number) => number): void {
    if (!this.indicators) return;
    const ctx = this.ctx;
    for (const spec of OVERLAY_SPECS) {
      const line = this.indicators[spec.key];
      if (!line || line.length === 0) continue;
      ctx.strokeStyle = this.colors[spec.themeKey];
      ctx.lineWidth = spec.width;
      ctx.beginPath();
      let started = false;
      for (const pt of line) {
        const idx = this.timeIndex.get(pt.time);
        if (idx === undefined || idx < this.from - 1 || idx > this.to + 1) { started = false; continue; }
        const px = xCenter(idx);
        const py = y(pt.value);
        if (!started) { ctx.moveTo(px, py); started = true; }
        else ctx.lineTo(px, py);
      }
      ctx.stroke();
    }
    ctx.lineWidth = 1;
  }

  private drawRSIPane(L: Layout, xCenter: (i: number) => number): void {
    const ctx = this.ctx;
    const c = this.colors;
    const yRsi = (v: number) => L.rsiTop + L.rsiH * (1 - v / 100);
    // 70/30 dashed guide lines.
    ctx.setLineDash([3, 3]);
    for (const [level, color] of [[70, c.rsi70], [30, c.rsi30]] as const) {
      ctx.strokeStyle = color;
      ctx.beginPath();
      ctx.moveTo(0, Math.round(yRsi(level)) + 0.5);
      ctx.lineTo(L.plotW, Math.round(yRsi(level)) + 0.5);
      ctx.stroke();
    }
    ctx.setLineDash([]);
    // RSI line.
    ctx.strokeStyle = c.rsi;
    ctx.lineWidth = 1;
    ctx.beginPath();
    let started = false;
    for (const pt of this.rsi) {
      const idx = this.timeIndex.get(pt.time);
      if (idx === undefined || idx < this.from - 1 || idx > this.to + 1) { started = false; continue; }
      const px = xCenter(idx);
      const py = yRsi(pt.value);
      if (!started) { ctx.moveTo(px, py); started = true; }
      else ctx.lineTo(px, py);
    }
    ctx.stroke();
    // "RSI" tag.
    ctx.fillStyle = c.text;
    ctx.font = c.font;
    ctx.textAlign = 'left';
    ctx.textBaseline = 'top';
    ctx.fillText('RSI 14', 4, L.rsiTop + 2);
  }

  private drawCrosshair(L: Layout, barW: number, min: number, max: number): void {
    const m = this.mouse;
    if (!m || m.x < 0 || m.x > L.plotW) return;
    const ctx = this.ctx;
    const c = this.colors;
    const nearest = clamp(Math.round(this.from + m.x / barW - 0.5), 0, this.bars.length - 1);
    const cx = (nearest - this.from + 0.5) * barW;

    ctx.save();
    ctx.strokeStyle = c.crosshair;
    ctx.setLineDash([4, 4]);
    ctx.beginPath();
    ctx.moveTo(Math.round(cx) + 0.5, 0);
    ctx.lineTo(Math.round(cx) + 0.5, this.cssH - TIME_H);
    ctx.moveTo(0, Math.round(m.y) + 0.5);
    ctx.lineTo(L.plotW, Math.round(m.y) + 0.5);
    ctx.stroke();
    ctx.setLineDash([]);

    // Price label on the right axis (only inside the price pane).
    if (m.y >= L.priceTop && m.y <= L.priceTop + L.priceH) {
      const price = min + (1 - (m.y - L.priceTop) / L.priceH) * (max - min);
      this.axisLabel(fmtPrice(price), L.plotW + 1, m.y, 'left');
    }
    // Date label on the time axis.
    const bar = this.bars[nearest];
    if (bar) this.timeLabel(fmtDate(bar.time), cx, this.cssH - TIME_H + 1);
    ctx.restore();
  }

  private axisLabel(text: string, x: number, y: number, align: CanvasTextAlign): void {
    const ctx = this.ctx;
    const c = this.colors;
    ctx.font = c.font;
    const w = ctx.measureText(text).width + 8;
    ctx.fillStyle = c.labelBg;
    ctx.fillRect(x, y - 8, align === 'left' ? w : -w, 16);
    ctx.fillStyle = c.labelText;
    ctx.textAlign = 'left';
    ctx.textBaseline = 'middle';
    ctx.fillText(text, x + 4, y);
  }

  private timeLabel(text: string, cx: number, top: number): void {
    const ctx = this.ctx;
    const c = this.colors;
    ctx.font = c.font;
    const w = ctx.measureText(text).width + 8;
    ctx.fillStyle = c.labelBg;
    ctx.fillRect(cx - w / 2, top, w, 16);
    ctx.fillStyle = c.labelText;
    ctx.textAlign = 'center';
    ctx.textBaseline = 'top';
    ctx.fillText(text, cx, top + 3);
  }
}

/** Wilder's RSI over close prices, returned as points aligned to bar times. */
function computeRSI(bars: Bar[], period: number): IndicatorPoint[] {
  if (bars.length <= period) return [];
  const out: IndicatorPoint[] = [];
  let avgGain = 0;
  let avgLoss = 0;
  for (let i = 1; i <= period; i++) {
    const diff = bars[i].close - bars[i - 1].close;
    if (diff > 0) avgGain += diff;
    else avgLoss -= diff;
  }
  avgGain /= period;
  avgLoss /= period;
  out.push({ time: bars[period].time, value: avgLoss === 0 ? 100 : 100 - 100 / (1 + avgGain / avgLoss) });
  for (let i = period + 1; i < bars.length; i++) {
    const diff = bars[i].close - bars[i - 1].close;
    avgGain = (avgGain * (period - 1) + (diff > 0 ? diff : 0)) / period;
    avgLoss = (avgLoss * (period - 1) + (diff < 0 ? -diff : 0)) / period;
    out.push({ time: bars[i].time, value: avgLoss === 0 ? 100 : 100 - 100 / (1 + avgGain / avgLoss) });
  }
  return out;
}

/** ~`count` evenly spaced "round" price levels spanning [min, max]. */
function niceTicks(min: number, max: number, count: number): number[] {
  const range = max - min;
  if (range <= 0) return [min];
  const rawStep = range / count;
  const mag = Math.pow(10, Math.floor(Math.log10(rawStep)));
  const norm = rawStep / mag;
  const step = (norm >= 5 ? 5 : norm >= 2 ? 2 : 1) * mag;
  const start = Math.ceil(min / step) * step;
  const ticks: number[] = [];
  for (let v = start; v <= max + step * 1e-6; v += step) ticks.push(v);
  return ticks;
}

function clamp(v: number, lo: number, hi: number): number {
  return v < lo ? lo : v > hi ? hi : v;
}

/** Convert a #RRGGBB / #RGB token to an rgba() string at the given alpha, or return fallback. */
function hexToRgba(hex: string, alpha: number, fallback: string): string {
  const h = hex.trim().replace(/^#/, '');
  let r: number;
  let g: number;
  let b: number;
  if (/^[0-9a-f]{6}$/i.test(h)) {
    const int = parseInt(h, 16);
    r = (int >> 16) & 255;
    g = (int >> 8) & 255;
    b = int & 255;
  } else if (/^[0-9a-f]{3}$/i.test(h)) {
    r = parseInt(h[0] + h[0], 16);
    g = parseInt(h[1] + h[1], 16);
    b = parseInt(h[2] + h[2], 16);
  } else {
    return fallback;
  }
  return `rgba(${r}, ${g}, ${b}, ${alpha})`;
}

function fmtPrice(v: number): string {
  const abs = Math.abs(v);
  if (abs >= 1000) return v.toFixed(0);
  if (abs >= 1) return v.toFixed(2);
  return v.toFixed(3);
}

function fmtDate(ts: number): string {
  const d = new Date(ts * 1000);
  const mm = (d.getMonth() + 1).toString().padStart(2, '0');
  const dd = d.getDate().toString().padStart(2, '0');
  return `${mm}/${dd}`;
}
