import { useEffect, useRef } from 'react';
import { createChart, IChartApi, CandlestickData, HistogramData, Time, CandlestickSeries, HistogramSeries } from 'lightweight-charts';
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

interface StockChartProps {
  symbol: string;
  data: OHLCVData[];
  width?: number;
  height?: number;
}

/**
 * TradingView Lightweight Charts wrapper.
 * Renders candlestick chart with volume bars.
 * See Bible section 12.1-12.2 for chart specifications.
 */
export function StockChart({ symbol, data, width, height }: StockChartProps) {
  const containerRef = useRef<HTMLDivElement>(null);
  const chartRef = useRef<IChartApi | null>(null);
  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  const candleSeriesRef = useRef<any>(null);
  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  const volumeSeriesRef = useRef<any>(null);

  // Create chart on mount
  useEffect(() => {
    if (!containerRef.current) return;

    log.info('Creating chart', { symbol });

    const chart = createChart(containerRef.current, {
      width: width || containerRef.current.clientWidth,
      height: height || 400,
      layout: {
        background: { color: '#0A0E17' },        // bg-primary
        textColor: '#9CA3AF',                      // text-secondary
        fontFamily: "'JetBrains Mono', monospace",
        fontSize: 11,
      },
      grid: {
        vertLines: { color: 'rgba(31, 41, 55, 0.5)' },   // grid
        horzLines: { color: 'rgba(31, 41, 55, 0.5)' },
      },
      crosshair: {
        vertLine: { color: '#6B7280', style: 2 },  // crosshair dashed
        horzLine: { color: '#6B7280', style: 2 },
      },
      rightPriceScale: {
        borderColor: '#1F2937',                     // border
      },
      timeScale: {
        borderColor: '#1F2937',
        timeVisible: true,
        secondsVisible: false,
      },
    });

    // Candlestick series (Bible 12.2.3)
    const candleSeries = chart.addSeries(CandlestickSeries, {
      upColor: '#10B981',           // candle-up-body (green)
      downColor: '#EF4444',         // candle-down-body (red)
      borderUpColor: '#10B981',
      borderDownColor: '#EF4444',
      wickUpColor: '#10B981',       // candle-up-wick
      wickDownColor: '#EF4444',     // candle-down-wick
    });

    // Volume histogram (Bible 12.2.3 - under chart, 20% height)
    const volumeSeries = chart.addSeries(HistogramSeries, {
      priceFormat: { type: 'volume' },
      priceScaleId: 'volume',
    });

    chart.priceScale('volume').applyOptions({
      scaleMargins: { top: 0.8, bottom: 0 }, // Bottom 20% of chart
    });

    chartRef.current = chart;
    candleSeriesRef.current = candleSeries;
    volumeSeriesRef.current = volumeSeries;

    // Handle resize
    const handleResize = () => {
      if (containerRef.current) {
        chart.applyOptions({
          width: containerRef.current.clientWidth,
        });
      }
    };

    const observer = new ResizeObserver(handleResize);
    observer.observe(containerRef.current);

    return () => {
      observer.disconnect();
      chart.remove();
      chartRef.current = null;
      candleSeriesRef.current = null;
      volumeSeriesRef.current = null;
      log.debug('Chart destroyed', { symbol });
    };
  }, [symbol]);

  // Update data when it changes
  useEffect(() => {
    if (!candleSeriesRef.current || !volumeSeriesRef.current || data.length === 0) return;

    const candleData: CandlestickData[] = data.map((d) => ({
      time: d.time as Time,
      open: d.open,
      high: d.high,
      low: d.low,
      close: d.close,
    }));

    const volumeData: HistogramData[] = data.map((d) => ({
      time: d.time as Time,
      value: d.volume,
      color: d.close >= d.open
        ? 'rgba(16, 185, 129, 0.3)'   // volume-up (green transparent)
        : 'rgba(239, 68, 68, 0.3)',    // volume-down (red transparent)
    }));

    candleSeriesRef.current.setData(candleData);
    volumeSeriesRef.current.setData(volumeData);

    log.debug('Chart data updated', { symbol, candles: data.length });
  }, [data, symbol]);

  return (
    <div
      ref={containerRef}
      style={{
        width: '100%',
        height: height || 400,
        background: '#0A0E17',
      }}
    />
  );
}
