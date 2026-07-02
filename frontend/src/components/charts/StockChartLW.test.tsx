// @vitest-environment happy-dom
import { describe, it, expect, vi, afterEach } from 'vitest';
import { render, cleanup } from '@testing-library/react';
import type { OHLCVData } from './StockChart';

// Record how the component drives the Lightweight-Charts API (mocked — no canvas needed).
const h = vi.hoisted(() => ({ series: [] as { def?: string; pane?: number; data?: unknown[] }[] }));

vi.mock('lightweight-charts', () => ({
  createChart: () => ({
    addSeries: (def: { __tag?: string }, _opts: unknown, pane?: number) => {
      const rec: { def?: string; pane?: number; data?: unknown[] } = { def: def?.__tag, pane };
      h.series.push(rec);
      return {
        setData: (d: unknown[]) => { rec.data = d; },
        update: () => {},
        priceScale: () => ({ applyOptions: () => {} }),
        seriesType: () => 'fn',
      };
    },
    removeSeries: () => {},
    timeScale: () => ({ fitContent: () => {}, setVisibleLogicalRange: () => {} }),
    remove: () => {},
  }),
  CandlestickSeries: { __tag: 'Candlestick' },
  HistogramSeries: { __tag: 'Histogram' },
  LineSeries: { __tag: 'Line' },
  AreaSeries: { __tag: 'Area' },
  ColorType: { Solid: 'solid' },
  CrosshairMode: { Normal: 0 },
  LineStyle: { Dashed: 2 },
}));

import { StockChartLW } from './StockChartLW';

afterEach(() => { cleanup(); h.series.length = 0; });

function sample(n = 60): OHLCVData[] {
  const out: OHLCVData[] = [];
  let p = 100;
  for (let i = 0; i < n; i++) {
    const open = p, close = p + ((i % 5) - 2);
    out.push({ time: 1_700_000_000 + i * 86_400, open, high: Math.max(open, close) + 1, low: Math.min(open, close) - 1, close, volume: 1000 + i });
    p = close;
  }
  return out;
}

describe('StockChartLW wiring', () => {
  it('creates candle + volume + overlays + RSI pane and feeds mapped data', () => {
    const data = sample(60);
    render(
      <StockChartLW symbol="TEST" data={data} chartType="candle"
        indicators={{ sma20: data.map(d => ({ time: d.time, value: d.close })) }} />,
    );

    const defs = h.series.map(s => s.def);
    expect(defs).toContain('Candlestick');
    expect(defs).toContain('Histogram');
    expect(defs.filter(d => d === 'Line').length).toBeGreaterThanOrEqual(9); // 8 overlays + rsi + 2 ref lines
    expect(h.series.some(s => s.pane === 1)).toBe(true); // RSI sub-pane

    expect(h.series.find(s => s.def === 'Candlestick')?.data).toHaveLength(60);
    expect(h.series.find(s => s.def === 'Histogram')?.data).toHaveLength(60);
  });

  it('uses a Line price series for the line chart type', () => {
    render(<StockChartLW symbol="T" data={sample(30)} chartType="line" />);
    expect(h.series.some(s => s.def === 'Candlestick')).toBe(false);
    expect(h.series.filter(s => s.def === 'Line').length).toBeGreaterThanOrEqual(1);
  });
});
