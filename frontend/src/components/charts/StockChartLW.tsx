import { useEffect, useRef } from 'react';
import {
  createChart,
  CandlestickSeries,
  HistogramSeries,
  LineSeries,
  AreaSeries,
  ColorType,
  CrosshairMode,
  LineStyle,
  type IChartApi,
  type ISeriesApi,
  type SeriesType,
  type UTCTimestamp,
} from 'lightweight-charts';
import { createLogger } from '@/services/logger';
import type { OHLCVData, IndicatorData, ChartType, IndicatorLine } from './StockChart';

const log = createLogger('StockChartLW');

interface StockChartLWProps {
  symbol: string;
  data: OHLCVData[];
  indicators?: IndicatorData;
  chartType?: ChartType;
  width?: number;
  height?: number;
}

const INDICATOR_COLORS = {
  sma20: '#F59E0B',
  sma50: '#8B5CF6',
  sma200: '#EC4899',
  ema12: '#06B6D4',
  bollinger: '#60A5FA',
  vwap: '#06B6D4',
} as const;

const t = (ts: number) => ts as UTCTimestamp;

/**
 * TradingView Lightweight Charts (v5) replacement for the ECharts StockChart.
 * Candlestick/line/area on the price pane + volume overlay + SMA/EMA/Bollinger/VWAP overlays
 * + an RSI sub-pane. Compare overlays are not yet ported. Consumes the same props as StockChart.
 */
export function StockChartLW({ symbol, data, indicators, chartType = 'candle', width, height }: StockChartLWProps) {
  const containerRef = useRef<HTMLDivElement>(null);
  const chartRef = useRef<IChartApi | null>(null);
  const seriesRef = useRef<{
    price?: ISeriesApi<SeriesType>;
    volume?: ISeriesApi<'Histogram'>;
    rsi?: ISeriesApi<'Line'>;
    rsi70?: ISeriesApi<'Line'>;
    rsi30?: ISeriesApi<'Line'>;
    overlays: Record<string, ISeriesApi<'Line'>>;
  }>({ overlays: {} });
  const appliedLenRef = useRef(0);   // how many bars are currently on the chart
  const dataSymbolRef = useRef('');  // symbol the current bars belong to

  // Create the chart once.
  useEffect(() => {
    if (!containerRef.current) return;
    const chart = createChart(containerRef.current, {
      autoSize: true,
      layout: {
        background: { type: ColorType.Solid, color: '#0A0E17' },
        textColor: '#9CA3AF',
        fontFamily: "'JetBrains Mono', monospace",
        fontSize: 11,
        panes: { separatorColor: '#1F2937', separatorHoverColor: 'rgba(96,165,250,0.2)' },
      },
      grid: {
        vertLines: { color: 'rgba(31, 41, 55, 0.5)' },
        horzLines: { color: 'rgba(31, 41, 55, 0.5)' },
      },
      crosshair: { mode: CrosshairMode.Normal },
      rightPriceScale: { borderColor: '#1F2937' },
      timeScale: { borderColor: '#1F2937', timeVisible: false, rightOffset: 0 },
    });
    chartRef.current = chart;
    log.info('LW chart created', { symbol });
    return () => {
      chart.remove();
      chartRef.current = null;
      seriesRef.current = { overlays: {} };
    };
  }, []);

  // (Re)build the series set when the symbol or chart type changes.
  useEffect(() => {
    const chart = chartRef.current;
    if (!chart) return;

    // Tear down any existing series.
    const s = seriesRef.current;
    Object.values(s).forEach((v) => {
      if (v && typeof (v as ISeriesApi<SeriesType>).seriesType === 'function') chart.removeSeries(v as ISeriesApi<SeriesType>);
    });
    Object.values(s.overlays).forEach((v) => chart.removeSeries(v));
    seriesRef.current = { overlays: {} };

    // Price series (pane 0).
    let price: ISeriesApi<SeriesType>;
    if (chartType === 'candle') {
      price = chart.addSeries(CandlestickSeries, {
        upColor: '#10B981', downColor: '#EF4444',
        borderUpColor: '#10B981', borderDownColor: '#EF4444',
        wickUpColor: '#10B981', wickDownColor: '#EF4444',
      }, 0);
    } else if (chartType === 'area') {
      price = chart.addSeries(AreaSeries, {
        lineColor: '#60A5FA', topColor: 'rgba(96,165,250,0.4)', bottomColor: 'rgba(96,165,250,0.02)', lineWidth: 2,
      }, 0);
    } else {
      price = chart.addSeries(LineSeries, { color: '#60A5FA', lineWidth: 2 }, 0);
    }
    seriesRef.current.price = price;

    // Volume histogram overlaid on the bottom of the price pane.
    const volume = chart.addSeries(HistogramSeries, {
      priceFormat: { type: 'volume' },
      priceScaleId: 'volume',
      lastValueVisible: false,
      priceLineVisible: false,
    }, 0);
    volume.priceScale().applyOptions({ scaleMargins: { top: 0.8, bottom: 0 } });
    seriesRef.current.volume = volume;

    // Overlay line series (created up front; fed data only when present).
    const overlay = (key: string, color: string, lineWidth: 1 | 2 = 1) => {
      seriesRef.current.overlays[key] = chart.addSeries(LineSeries, {
        color, lineWidth, lastValueVisible: false, priceLineVisible: false, crosshairMarkerVisible: false,
      }, 0);
    };
    overlay('sma20', INDICATOR_COLORS.sma20);
    overlay('sma50', INDICATOR_COLORS.sma50);
    overlay('sma200', INDICATOR_COLORS.sma200, 2);
    overlay('ema12', INDICATOR_COLORS.ema12);
    overlay('bollingerUpper', INDICATOR_COLORS.bollinger);
    overlay('bollingerMiddle', INDICATOR_COLORS.bollinger);
    overlay('bollingerLower', INDICATOR_COLORS.bollinger);
    overlay('vwap', INDICATOR_COLORS.vwap);

    // RSI sub-pane (pane 1) with 70/30 reference lines.
    const rsi = chart.addSeries(LineSeries, {
      color: '#A78BFA', lineWidth: 1, lastValueVisible: false, priceLineVisible: false,
    }, 1);
    const refLine = (color: string) => chart.addSeries(LineSeries, {
      color, lineWidth: 1, lineStyle: LineStyle.Dashed, lastValueVisible: false,
      priceLineVisible: false, crosshairMarkerVisible: false,
    }, 1);
    seriesRef.current.rsi = rsi;
    seriesRef.current.rsi70 = refLine('rgba(239,68,68,0.3)');
    seriesRef.current.rsi30 = refLine('rgba(16,185,129,0.3)');
    appliedLenRef.current = 0; // fresh series → next data pass does a full setData
  }, [symbol, chartType]);

  // Feed data into the series whenever data or indicators change.
  // Live updates use series.update() on just the changed tail (preserving the user's zoom/scroll);
  // a full setData + fitContent only runs on first load or a symbol change.
  useEffect(() => {
    const s = seriesRef.current;
    if (!s.price || data.length === 0) return;

    const reset = dataSymbolRef.current !== symbol || appliedLenRef.current === 0 || data.length < appliedLenRef.current;
    const from = reset ? 0 : Math.max(0, appliedLenRef.current - 1); // also refresh the last (possibly intrabar) bar

    if (chartType === 'candle') {
      const bar = (d: OHLCVData) => ({ time: t(d.time), open: d.open, high: d.high, low: d.low, close: d.close });
      if (reset) s.price.setData(data.map(bar));
      else try { for (let i = from; i < data.length; i++) s.price.update(bar(data[i])); }
        catch { s.price.setData(data.map(bar)); }
    } else {
      const pt = (d: OHLCVData) => ({ time: t(d.time), value: d.close });
      if (reset) s.price.setData(data.map(pt));
      else try { for (let i = from; i < data.length; i++) s.price.update(pt(data[i])); }
        catch { s.price.setData(data.map(pt)); }
    }

    const vol = (d: OHLCVData) => ({
      time: t(d.time), value: d.volume,
      color: d.close >= d.open ? 'rgba(16, 185, 129, 0.5)' : 'rgba(239, 68, 68, 0.5)',
    });
    if (reset) s.volume?.setData(data.map(vol));
    else try { for (let i = from; i < data.length; i++) s.volume?.update(vol(data[i])); }
      catch { s.volume?.setData(data.map(vol)); }

    const setOverlay = (key: string, line?: IndicatorLine[]) => {
      const series = s.overlays[key];
      if (!series) return;
      series.setData((line ?? []).map((d) => ({ time: t(d.time), value: d.value })));
    };
    setOverlay('sma20', indicators?.sma20);
    setOverlay('sma50', indicators?.sma50);
    setOverlay('sma200', indicators?.sma200);
    setOverlay('ema12', indicators?.ema12);
    setOverlay('bollingerUpper', indicators?.bollingerUpper);
    setOverlay('bollingerMiddle', indicators?.bollingerMiddle);
    setOverlay('bollingerLower', indicators?.bollingerLower);
    setOverlay('vwap', indicators?.vwap);

    const rsi = computeRSI(data, 14);
    s.rsi?.setData(rsi);
    if (rsi.length > 0) {
      const first = rsi[0].time, last = rsi[rsi.length - 1].time;
      s.rsi70?.setData([{ time: first, value: 70 }, { time: last, value: 70 }]);
      s.rsi30?.setData([{ time: first, value: 30 }, { time: last, value: 30 }]);
    }

    // Show a recent window (not the whole year) so candles are big enough to see them tick.
    if (reset) chartRef.current?.timeScale().setVisibleLogicalRange({ from: Math.max(0, data.length - 90), to: data.length });
    dataSymbolRef.current = symbol;
    appliedLenRef.current = data.length;
    log.debug('LW data applied', { symbol, candles: data.length, mode: reset ? 'full' : 'incremental' });
  }, [data, indicators, chartType, symbol]);

  return <div ref={containerRef} style={{ width: width || '100%', height: height || 400, background: '#0A0E17' }} />;
}

/** Wilder's RSI over close prices, returned as LW line points (starts at first valid value). */
function computeRSI(data: OHLCVData[], period: number): { time: UTCTimestamp; value: number }[] {
  if (data.length <= period) return [];
  const out: { time: UTCTimestamp; value: number }[] = [];
  let avgGain = 0, avgLoss = 0;
  for (let i = 1; i <= period; i++) {
    const diff = data[i].close - data[i - 1].close;
    if (diff > 0) avgGain += diff; else avgLoss -= diff;
  }
  avgGain /= period;
  avgLoss /= period;
  out.push({ time: t(data[period].time), value: avgLoss === 0 ? 100 : 100 - 100 / (1 + avgGain / avgLoss) });
  for (let i = period + 1; i < data.length; i++) {
    const diff = data[i].close - data[i - 1].close;
    avgGain = (avgGain * (period - 1) + (diff > 0 ? diff : 0)) / period;
    avgLoss = (avgLoss * (period - 1) + (diff < 0 ? -diff : 0)) / period;
    out.push({ time: t(data[i].time), value: avgLoss === 0 ? 100 : 100 - 100 / (1 + avgGain / avgLoss) });
  }
  return out;
}
