import { useEffect, useRef } from 'react';
import { createChart, IChartApi, CandlestickData, HistogramData, LineData, Time, LineWidth, CandlestickSeries, HistogramSeries, LineSeries, AreaSeries } from 'lightweight-charts';
import { createLogger } from '@/services/logger';

const log = createLogger('StockChart');

export interface OHLCVData {
  time: number;
  open: number;
  high: number;
  low: number;
  close: number;
  volume: number;
}

export interface IndicatorLine {
  time: number;
  value: number;
}

export interface IndicatorData {
  sma20?: IndicatorLine[];
  sma50?: IndicatorLine[];
  sma200?: IndicatorLine[];
  ema12?: IndicatorLine[];
  bollingerUpper?: IndicatorLine[];
  bollingerMiddle?: IndicatorLine[];
  bollingerLower?: IndicatorLine[];
  rsi?: IndicatorLine[];
  vwap?: IndicatorLine[];
}

export type ChartType = 'candle' | 'line' | 'area';

export interface CompareStock {
  symbol: string;
  data: OHLCVData[];
  color: string;
}

interface StockChartProps {
  symbol: string;
  data: OHLCVData[];
  indicators?: IndicatorData;
  chartType?: ChartType;
  compareStocks?: CompareStock[];
  width?: number;
  height?: number;
}

// Bible 12.2.4 color spec
const INDICATOR_COLORS = {
  sma20: '#F59E0B',      // Amber
  sma50: '#8B5CF6',      // Purple
  sma200: '#EC4899',     // Pink
  ema12: '#06B6D4',      // Cyan
  bollingerLine: '#60A5FA',  // Blue
  bollingerFill: 'rgba(96, 165, 250, 0.08)',
} as const;

/**
 * TradingView Lightweight Charts wrapper with indicator support.
 * Bible 12.1-12.2: Candlestick + Volume + SMA/EMA/Bollinger overlays.
 */
export function StockChart({ symbol, data, indicators, chartType = 'candle', compareStocks, width, height }: StockChartProps) {
  const containerRef = useRef<HTMLDivElement>(null);
  const chartRef = useRef<IChartApi | null>(null);
  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  const seriesRefs = useRef<Record<string, any>>({});

  // Create chart on mount
  useEffect(() => {
    if (!containerRef.current) return;

    log.info('Creating chart', { symbol });

    const chart = createChart(containerRef.current, {
      width: width || containerRef.current.clientWidth,
      height: height || 400,
      layout: {
        background: { color: '#0A0E17' },
        textColor: '#9CA3AF',
        fontFamily: "'JetBrains Mono', monospace",
        fontSize: 11,
      },
      grid: {
        vertLines: { color: 'rgba(31, 41, 55, 0.5)' },
        horzLines: { color: 'rgba(31, 41, 55, 0.5)' },
      },
      crosshair: {
        vertLine: { color: '#6B7280', style: 2 },
        horzLine: { color: '#6B7280', style: 2 },
      },
      watermark: { visible: false },
      rightPriceScale: { borderColor: '#1F2937' },
      timeScale: { borderColor: '#1F2937', timeVisible: true, secondsVisible: false },
    });

    // Main price series — depends on chart type (Bible 12.2.2)
    let priceSeries: ReturnType<typeof chart.addSeries>;
    if (chartType === 'line') {
      priceSeries = chart.addSeries(LineSeries, {
        color: '#60A5FA',
        lineWidth: 2 as LineWidth,
        priceLineVisible: true,
      });
    } else if (chartType === 'area') {
      priceSeries = chart.addSeries(AreaSeries, {
        topColor: 'rgba(96, 165, 250, 0.4)',
        bottomColor: 'rgba(96, 165, 250, 0.02)',
        lineColor: '#60A5FA',
        lineWidth: 2 as LineWidth,
      });
    } else {
      priceSeries = chart.addSeries(CandlestickSeries, {
        upColor: '#10B981',
        downColor: '#EF4444',
        borderUpColor: '#10B981',
        borderDownColor: '#EF4444',
        wickUpColor: '#10B981',
        wickDownColor: '#EF4444',
      });
    }
    const candleSeries = priceSeries;

    // Volume histogram
    const volumeSeries = chart.addSeries(HistogramSeries, {
      priceFormat: { type: 'volume' },
      priceScaleId: 'volume',
    });
    chart.priceScale('volume').applyOptions({ scaleMargins: { top: 0.8, bottom: 0 } });

    chartRef.current = chart;
    seriesRefs.current = { candle: candleSeries, volume: volumeSeries };

    const handleResize = () => {
      if (containerRef.current) {
        chart.applyOptions({ width: containerRef.current.clientWidth });
      }
    };
    const observer = new ResizeObserver(handleResize);
    observer.observe(containerRef.current);

    return () => {
      observer.disconnect();
      chart.remove();
      chartRef.current = null;
      seriesRefs.current = {};
      log.debug('Chart destroyed', { symbol });
    };
  }, [symbol, chartType]);

  // Update OHLCV data
  useEffect(() => {
    const { candle, volume } = seriesRefs.current;
    if (!candle || !volume || data.length === 0) return;

    // Set price data based on chart type
    if (chartType === 'candle') {
      const candleData: CandlestickData[] = data.map((d) => ({
        time: d.time as Time, open: d.open, high: d.high, low: d.low, close: d.close,
      }));
      candle.setData(candleData);
    } else {
      // Line and Area use close price only
      const lineData: LineData[] = data.map((d) => ({
        time: d.time as Time, value: d.close,
      }));
      candle.setData(lineData);
    }

    const volumeData: HistogramData[] = data.map((d) => ({
      time: d.time as Time, value: d.volume,
      color: d.close >= d.open ? 'rgba(16, 185, 129, 0.3)' : 'rgba(239, 68, 68, 0.3)',
    }));
    volume.setData(volumeData);
    log.debug('Chart data updated', { symbol, candles: data.length });
  }, [data, symbol]);

  // Update indicator overlays
  useEffect(() => {
    const chart = chartRef.current;
    if (!chart || !indicators) return;

    // Remove old indicator series (keep candle + volume)
    const keepKeys = new Set(['candle', 'volume']);
    for (const [key, series] of Object.entries(seriesRefs.current)) {
      if (!keepKeys.has(key) && series) {
        try { chart.removeSeries(series); } catch { /* already removed */ }
      }
    }
    const newRefs: Record<string, unknown> = {
      candle: seriesRefs.current.candle,
      volume: seriesRefs.current.volume,
    };

    const addLine = (key: string, lineData: IndicatorLine[] | undefined, color: string, lineWidth: LineWidth = 1) => {
      if (!lineData || lineData.length === 0) return;
      const series = chart.addSeries(LineSeries, {
        color, lineWidth, priceLineVisible: false, lastValueVisible: false,
        crosshairMarkerVisible: false,
      });
      series.setData(lineData.map(d => ({ time: d.time as Time, value: d.value } as LineData)));
      newRefs[key] = series;
    };

    addLine('sma20', indicators.sma20, INDICATOR_COLORS.sma20, 1);
    addLine('sma50', indicators.sma50, INDICATOR_COLORS.sma50, 1);
    addLine('sma200', indicators.sma200, INDICATOR_COLORS.sma200, 2);
    addLine('ema12', indicators.ema12, INDICATOR_COLORS.ema12, 1);
    addLine('bollingerUpper', indicators.bollingerUpper, INDICATOR_COLORS.bollingerLine, 1);
    addLine('bollingerLower', indicators.bollingerLower, INDICATOR_COLORS.bollingerLine, 1);
    addLine('vwap', indicators.vwap, '#06B6D4', 1); // Cyan dashed (Bible 12.2.4)

    // Compare overlays (Bible 12.2.5): normalized to % change from first visible candle
    if (compareStocks && compareStocks.length > 0) {
      compareStocks.forEach((cs, idx) => {
        if (cs.data.length === 0) return;
        const basePrice = cs.data[0].close;
        if (basePrice <= 0) return;
        const normalized: IndicatorLine[] = cs.data.map(d => ({
          time: d.time,
          value: ((d.close - basePrice) / basePrice) * 100,
        }));
        addLine(`compare_${idx}`, normalized, cs.color, 2);
      });
    }

    seriesRefs.current = newRefs;
    log.debug('Indicators updated', { symbol, keys: Object.keys(indicators) });
  }, [indicators, symbol]);

  return (
    <div
      ref={containerRef}
      style={{ width: '100%', height: height || 400, background: '#0A0E17' }}
    />
  );
}
