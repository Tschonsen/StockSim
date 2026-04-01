import { useMemo, useRef, useEffect } from 'react';
import ReactEChartsCore from 'echarts-for-react/lib/core';
import * as echarts from 'echarts/core';
import { CandlestickChart, LineChart, BarChart } from 'echarts/charts';
import {
  GridComponent,
  TooltipComponent,
  DataZoomComponent,
  LegendComponent,
} from 'echarts/components';
import { CanvasRenderer } from 'echarts/renderers';
import { createLogger } from '@/services/logger';

// Register ECharts modules
echarts.use([
  CandlestickChart, LineChart, BarChart,
  GridComponent, TooltipComponent, DataZoomComponent, LegendComponent,
  CanvasRenderer,
]);

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
  vwap: '#06B6D4',       // Cyan
} as const;

const COMPARE_COLORS = ['#F97316', '#A855F7', '#14B8A6', '#F43F5E', '#84CC16'];

function formatTime(ts: number): string {
  const d = new Date(ts * 1000);
  const y = d.getFullYear();
  const m = String(d.getMonth() + 1).padStart(2, '0');
  const day = String(d.getDate()).padStart(2, '0');
  return `${y}-${m}-${day}`;
}

/**
 * Apache ECharts wrapper with indicator support.
 * Bible 12.1-12.2: Candlestick + Volume + SMA/EMA/Bollinger overlays.
 */
export function StockChart({ symbol, data, indicators, chartType = 'candle', compareStocks, width, height }: StockChartProps) {
  const chartRef = useRef<ReactEChartsCore>(null);
  const prevSymbolRef = useRef(symbol);

  // Clear chart on symbol change to prevent stale data
  useEffect(() => {
    if (prevSymbolRef.current !== symbol) {
      log.info('Chart symbol changed', { from: prevSymbolRef.current, to: symbol });
      prevSymbolRef.current = symbol;
      try {
        const instance = chartRef.current?.getEchartsInstance?.();
        if (instance && !instance.isDisposed?.()) instance.clear();
      } catch { /* chart may already be disposed */ }
    }
    return () => {
      try {
        const instance = chartRef.current?.getEchartsInstance?.();
        if (instance && !instance.isDisposed?.()) instance.dispose();
      } catch { /* ignore dispose errors on unmount */ }
    };
  }, [symbol]);

  const option = useMemo(() => {
    if (data.length === 0) return {};

    const categoryData = data.map(d => formatTime(d.time));
    const ohlcData = data.map(d => [d.open, d.close, d.low, d.high]);
    const closeData = data.map(d => d.close);
    const volumeData = data.map((d) => ({
      value: d.volume,
      itemStyle: {
        color: d.close >= d.open ? 'rgba(16, 185, 129, 0.3)' : 'rgba(239, 68, 68, 0.3)',
      },
    }));

    // Build series array
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    const series: any[] = [];

    // Main price series
    if (chartType === 'candle') {
      series.push({
        name: symbol,
        type: 'candlestick',
        data: ohlcData,
        xAxisIndex: 0,
        yAxisIndex: 0,
        barWidth: '80%',
        barMinWidth: 6,
        barMaxWidth: 40,
        itemStyle: {
          color: '#10B981',        // up body (filled)
          color0: '#EF4444',       // down body (filled)
          borderColor: '#10B981',  // up border
          borderColor0: '#EF4444', // down border
          borderWidth: 1.5,
        },
        emphasis: {
          itemStyle: {
            borderWidth: 2,
          },
        },
      });
    } else if (chartType === 'area') {
      series.push({
        name: symbol,
        type: 'line',
        data: closeData,
        xAxisIndex: 0,
        yAxisIndex: 0,
        showSymbol: false,
        lineStyle: { color: '#60A5FA', width: 2 },
        areaStyle: {
          color: new echarts.graphic.LinearGradient(0, 0, 0, 1, [
            { offset: 0, color: 'rgba(96, 165, 250, 0.4)' },
            { offset: 1, color: 'rgba(96, 165, 250, 0.02)' },
          ]),
        },
        itemStyle: { color: '#60A5FA' },
      });
    } else {
      // line
      series.push({
        name: symbol,
        type: 'line',
        data: closeData,
        xAxisIndex: 0,
        yAxisIndex: 0,
        showSymbol: false,
        lineStyle: { color: '#60A5FA', width: 2 },
        itemStyle: { color: '#60A5FA' },
      });
    }

    // Volume bars
    series.push({
      name: 'Volume',
      type: 'bar',
      data: volumeData,
      xAxisIndex: 1,
      yAxisIndex: 1,
      barWidth: '60%',
    });

    // Helper to add indicator lines
    const addIndicator = (name: string, lineData: IndicatorLine[] | undefined, color: string, width = 1) => {
      if (!lineData || lineData.length === 0) return;
      // Map indicator data to category axis by matching timestamps
      const timeToValue = new Map(lineData.map(d => [formatTime(d.time), d.value]));
      const mapped = categoryData.map(t => timeToValue.get(t) ?? null);
      series.push({
        name,
        type: 'line',
        data: mapped,
        xAxisIndex: 0,
        yAxisIndex: 0,
        showSymbol: false,
        lineStyle: { color, width },
        itemStyle: { color },
        connectNulls: true,
      });
    };

    if (indicators) {
      addIndicator('SMA 20', indicators.sma20, INDICATOR_COLORS.sma20);
      addIndicator('SMA 50', indicators.sma50, INDICATOR_COLORS.sma50);
      addIndicator('SMA 200', indicators.sma200, INDICATOR_COLORS.sma200, 2);
      addIndicator('EMA 12', indicators.ema12, INDICATOR_COLORS.ema12);
      addIndicator('BB Middle', indicators.bollingerMiddle, INDICATOR_COLORS.bollingerLine, 1);
      // Bollinger Band fill: Upper fills down with band color, Lower fills down with bg color to mask
      if (indicators.bollingerUpper && indicators.bollingerUpper.length > 0) {
        const timeToValue = new Map(indicators.bollingerUpper.map(d => [formatTime(d.time), d.value]));
        const mapped = categoryData.map(t => timeToValue.get(t) ?? null);
        series.push({
          name: 'BB Upper',
          type: 'line',
          data: mapped,
          xAxisIndex: 0,
          yAxisIndex: 0,
          showSymbol: false,
          lineStyle: { color: INDICATOR_COLORS.bollingerLine, width: 1 },
          itemStyle: { color: INDICATOR_COLORS.bollingerLine },
          areaStyle: { color: INDICATOR_COLORS.bollingerFill, origin: 'auto' },
          connectNulls: true,
          z: 1,
        });
      }
      if (indicators.bollingerLower && indicators.bollingerLower.length > 0) {
        const timeToValue = new Map(indicators.bollingerLower.map(d => [formatTime(d.time), d.value]));
        const mapped = categoryData.map(t => timeToValue.get(t) ?? null);
        series.push({
          name: 'BB Lower',
          type: 'line',
          data: mapped,
          xAxisIndex: 0,
          yAxisIndex: 0,
          showSymbol: false,
          lineStyle: { color: INDICATOR_COLORS.bollingerLine, width: 1 },
          itemStyle: { color: INDICATOR_COLORS.bollingerLine },
          areaStyle: { color: '#0A0E17', origin: 'auto' },
          connectNulls: true,
          z: 2,
        });
      }
      addIndicator('VWAP', indicators.vwap, INDICATOR_COLORS.vwap);
    }

    // Compare overlays (Bible 12.2.5): normalized to % change on separate Y-axis
    const hasCompare = compareStocks && compareStocks.length > 0;
    if (hasCompare) {
      compareStocks.forEach((cs, idx) => {
        if (cs.data.length === 0) return;
        const basePrice = cs.data[0].close;
        if (basePrice <= 0) return;
        const timeToValue = new Map(
          cs.data.map(d => [formatTime(d.time), ((d.close - basePrice) / basePrice) * 100])
        );
        const mapped = categoryData.map(t => timeToValue.get(t) ?? null);
        const color = cs.color || COMPARE_COLORS[idx % COMPARE_COLORS.length];
        series.push({
          name: cs.symbol,
          type: 'line',
          data: mapped,
          xAxisIndex: 0,
          yAxisIndex: 2,  // separate % axis
          showSymbol: false,
          lineStyle: { color, width: 2 },
          itemStyle: { color },
          connectNulls: true,
        });
      });
    }

    log.debug('Chart options built', { symbol, candles: data.length, seriesCount: series.length });

    return {
      animation: false,
      backgroundColor: '#0A0E17',
      textStyle: {
        color: '#9CA3AF',
        fontFamily: "'JetBrains Mono', monospace",
        fontSize: 11,
      },
      tooltip: {
        trigger: 'axis',
        axisPointer: { type: 'cross' },
        backgroundColor: 'rgba(15, 23, 42, 0.95)',
        borderColor: '#1F2937',
        textStyle: { color: '#E5E7EB', fontFamily: "'JetBrains Mono', monospace", fontSize: 11 },
      },
      axisPointer: {
        link: [{ xAxisIndex: 'all' }],
        lineStyle: { color: '#6B7280', type: 'dashed' },
      },
      grid: [
        { left: 60, right: 60, top: 30, height: '66%' },
        { left: 60, right: 60, top: '82%', height: '12%' },
      ],
      // OHLC label top-left (like TradingView)
      graphic: data.length > 0 ? [{
        type: 'group',
        left: 65,
        top: 5,
        children: (() => {
          const last = data[data.length - 1];
          const chg = last.close - last.open;
          const chgPct = last.open > 0 ? (chg / last.open * 100) : 0;
          const color = chg >= 0 ? '#10B981' : '#EF4444';
          return [
            { type: 'text', style: { text: `O ${last.open.toFixed(2)}  H ${last.high.toFixed(2)}  L ${last.low.toFixed(2)}  C ${last.close.toFixed(2)}  `, fill: '#9CA3AF', font: '11px JetBrains Mono' } },
            { type: 'text', left: 280, style: { text: `${chg >= 0 ? '+' : ''}${chg.toFixed(2)} (${chg >= 0 ? '+' : ''}${chgPct.toFixed(2)}%)`, fill: color, font: 'bold 11px JetBrains Mono' } },
          ];
        })(),
      }] : [],
      xAxis: [
        {
          type: 'category',
          data: categoryData,
          gridIndex: 0,
          axisLine: { lineStyle: { color: '#1F2937' } },
          axisTick: { show: false },
          axisLabel: { show: false },
          splitLine: { show: true, lineStyle: { color: 'rgba(31, 41, 55, 0.5)' } },
        },
        {
          type: 'category',
          data: categoryData,
          gridIndex: 1,
          axisLine: { lineStyle: { color: '#1F2937' } },
          axisTick: { show: false },
          axisLabel: { color: '#9CA3AF', fontSize: 10 },
          splitLine: { show: false },
        },
      ],
      yAxis: [
        {
          type: 'value',
          gridIndex: 0,
          position: 'right',
          scale: true,
          axisLine: { lineStyle: { color: '#1F2937' } },
          axisTick: { show: false },
          splitLine: { lineStyle: { color: 'rgba(31, 41, 55, 0.5)' } },
          axisLabel: { color: '#9CA3AF', fontSize: 10 },
        },
        {
          type: 'value',
          gridIndex: 1,
          position: 'right',
          axisLine: { show: false },
          axisTick: { show: false },
          splitLine: { show: false },
          axisLabel: { show: false },
        },
        // Compare stocks: separate % Y-axis on left side
        ...(hasCompare ? [{
          type: 'value' as const,
          gridIndex: 0,
          position: 'left' as const,
          axisLine: { lineStyle: { color: '#F97316' } },
          axisTick: { show: false },
          splitLine: { show: false },
          axisLabel: { color: '#F97316', fontSize: 10, formatter: '{value}%' },
        }] : []),
      ],
      dataZoom: [
        {
          type: 'inside',
          xAxisIndex: [0, 1],
          start: data.length > 90 ? Math.max(0, 100 - (90 / data.length) * 100) : 0,
          end: 100,
        },
        {
          type: 'slider',
          xAxisIndex: [0, 1],
          bottom: 5,
          height: 20,
          borderColor: '#1F2937',
          backgroundColor: 'rgba(10, 14, 23, 0.8)',
          fillerColor: 'rgba(96, 165, 250, 0.15)',
          handleStyle: { color: '#60A5FA', borderColor: '#60A5FA' },
          textStyle: { color: '#9CA3AF', fontSize: 10 },
          dataBackground: {
            lineStyle: { color: '#1F2937' },
            areaStyle: { color: 'rgba(31, 41, 55, 0.3)' },
          },
          start: data.length > 90 ? Math.max(0, 100 - (90 / data.length) * 100) : 0,
          end: 100,
        },
      ],
      series,
    };
  }, [data, indicators, chartType, compareStocks, symbol]);

  if (!option || Object.keys(option).length === 0) {
    return <div style={{ width: width || '100%', height: height || 400, background: '#0A0E17' }} />;
  }

  return (
    <ReactEChartsCore
      ref={chartRef}
      echarts={echarts}
      option={option}
      style={{ width: width || '100%', height: height || 400, background: '#0A0E17' }}
      notMerge={true}
      lazyUpdate={true}
    />
  );
}
