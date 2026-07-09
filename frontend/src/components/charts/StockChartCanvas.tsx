import { useEffect, useRef } from 'react';
import { createLogger } from '@/services/logger';
import type { OHLCVData, IndicatorData, ChartType } from './StockChart';
import { CanvasChartEngine } from './engine/CanvasChartEngine';

const log = createLogger('StockChartCanvas');

interface StockChartCanvasProps {
  symbol: string;
  data: OHLCVData[];
  indicators?: IndicatorData;
  chartType?: ChartType;
  width?: number;
  height?: number;
}

/**
 * In-house Canvas-2D chart, drop-in replacement for StockChartLW (same props contract).
 * The React layer only owns lifecycle and the setData-vs-tail decision; all rendering lives
 * in the framework-agnostic CanvasChartEngine. Slice 1 renders price + axes only; volume,
 * indicators and interaction follow in later M1 slices.
 */
export function StockChartCanvas({ symbol, data, indicators, chartType = 'candle', width, height }: StockChartCanvasProps) {
  const containerRef = useRef<HTMLDivElement>(null);
  const engineRef = useRef<CanvasChartEngine | null>(null);
  const dataSymbolRef = useRef(''); // symbol the applied bars belong to
  const appliedLenRef = useRef(0); // how many bars are currently applied

  // Create the engine once.
  useEffect(() => {
    if (!containerRef.current) return;
    const engine = new CanvasChartEngine(containerRef.current);
    engineRef.current = engine;
    dataSymbolRef.current = '';
    appliedLenRef.current = 0;
    log.info('Canvas chart created', { symbol });
    return () => {
      engine.destroy();
      engineRef.current = null;
    };
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  // Feed bars whenever data/type/symbol change. A shrinking length or symbol change is a reset
  // (recentre the view); otherwise the window is preserved for live ticks (refined in slice 3).
  useEffect(() => {
    const engine = engineRef.current;
    if (!engine || data.length === 0) return;
    const reset =
      dataSymbolRef.current !== symbol || appliedLenRef.current === 0 || data.length < appliedLenRef.current;
    engine.setData(data, chartType, indicators, reset);
    dataSymbolRef.current = symbol;
    appliedLenRef.current = data.length;
    log.debug('Canvas data applied', { symbol, candles: data.length, mode: reset ? 'full' : 'incremental' });
  }, [data, indicators, chartType, symbol]);

  return <div ref={containerRef} style={{ width: width || '100%', height: height || 400, background: '#0A0E17' }} />;
}
