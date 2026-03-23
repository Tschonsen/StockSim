import { useState, useEffect, useCallback } from 'react';
import { useMarketStore } from '@/stores/marketStore';
import { OrderSide, OrderType, StockData } from '@/types/market';
import { WebSocketClient } from '@/services/websocket';
import { createLogger } from '@/services/logger';

const log = createLogger('OrderPanel');

interface OrderPanelProps {
  stock: StockData;
  wsClient: WebSocketClient;
}

/**
 * Order entry panel for buying and selling stocks.
 * Bible 3.5.2: Buy/Sell tabs, Market/Limit dropdown, quantity, estimated cost, place button.
 */
export function OrderPanel({ stock, wsClient }: OrderPanelProps) {
  const { portfolio, lastOrderResult, setOrderResult, isMarketOpen } = useMarketStore();

  const [side, setSide] = useState<OrderSide>('Buy');
  const [orderType, setOrderType] = useState<OrderType>('Market');
  const [quantity, setQuantity] = useState('');
  const [limitPrice, setLimitPrice] = useState('');
  const [submitting, setSubmitting] = useState(false);
  const [toast, setToast] = useState<{ message: string; type: 'success' | 'error' } | null>(null);

  // Pre-fill limit price when switching to Limit
  useEffect(() => {
    if (orderType === 'Limit') {
      setLimitPrice(stock.price.toFixed(2));
    }
  }, [orderType, stock.symbol]);

  // Clear toast after 3 seconds
  useEffect(() => {
    if (toast) {
      const timer = setTimeout(() => setToast(null), 3000);
      return () => clearTimeout(timer);
    }
  }, [toast]);

  // Handle order result
  useEffect(() => {
    if (lastOrderResult) {
      setSubmitting(false);
      if (lastOrderResult.success) {
        const order = lastOrderResult.order;
        if (order && order.status === 'Filled') {
          setToast({
            message: `${order.side} ${order.quantity} ${order.symbol} @ $${order.fillPrice?.toFixed(2)}`,
            type: 'success',
          });
          setQuantity('');
        } else if (order && order.status === 'Pending') {
          setToast({ message: `Order queued (market closed)`, type: 'success' });
          setQuantity('');
        } else if (order && order.status === 'Open') {
          setToast({ message: `Limit order placed`, type: 'success' });
          setQuantity('');
        }
      } else {
        setToast({ message: lastOrderResult.error || 'Order failed', type: 'error' });
      }
      setOrderResult(null);
    }
  }, [lastOrderResult, setOrderResult]);

  const qty = parseFloat(quantity) || 0;
  const lmtPrice = parseFloat(limitPrice) || 0;
  const estimatedPrice = side === 'Buy' ? stock.ask : stock.bid;
  const estimatedCost = qty * estimatedPrice;
  const commission = 4.95;
  const totalCost = side === 'Buy' ? estimatedCost + commission : estimatedCost - commission;
  const cash = portfolio?.cash ?? 0;
  const position = portfolio?.positions.find(p => p.symbol === stock.symbol);

  const canSubmit = qty > 0 && !submitting && (
    side === 'Buy'
      ? totalCost <= cash
      : (position ? qty <= position.shares : false)
  ) && (orderType === 'Market' || lmtPrice > 0);

  const handleSubmit = useCallback(() => {
    if (!canSubmit) return;
    setSubmitting(true);

    const payload: Record<string, unknown> = {
      symbol: stock.symbol,
      side,
      type: orderType,
      quantity: qty,
    };

    if (orderType === 'Limit') {
      payload.limitPrice = lmtPrice;
      payload.timeInForce = 'GTC';
    }

    log.info('Placing order', payload);
    wsClient.send('PlaceOrder', payload);
  }, [canSubmit, stock.symbol, side, orderType, qty, lmtPrice, wsClient]);

  const isBuy = side === 'Buy';

  return (
    <div style={{ padding: '12px' }}>
      {/* Buy / Sell Tabs (Bible 3.5.2) */}
      <div style={{ display: 'flex', gap: 0, marginBottom: '12px' }}>
        <button
          onClick={() => setSide('Buy')}
          style={{
            flex: 1,
            height: '36px',
            border: 'none',
            cursor: 'pointer',
            fontFamily: 'var(--font-ui)',
            fontWeight: 700,
            fontSize: '13px',
            textTransform: 'uppercase',
            borderRadius: '4px 0 0 4px',
            background: isBuy ? 'var(--green-dim)' : 'var(--bg-tertiary)',
            color: isBuy ? 'var(--green-primary)' : 'var(--text-secondary)',
          }}
        >
          BUY
        </button>
        <button
          onClick={() => setSide('Sell')}
          style={{
            flex: 1,
            height: '36px',
            border: 'none',
            cursor: 'pointer',
            fontFamily: 'var(--font-ui)',
            fontWeight: 700,
            fontSize: '13px',
            textTransform: 'uppercase',
            borderRadius: '0 4px 4px 0',
            background: !isBuy ? 'var(--red-dim)' : 'var(--bg-tertiary)',
            color: !isBuy ? 'var(--red-primary)' : 'var(--text-secondary)',
          }}
        >
          SELL
        </button>
      </div>

      {/* Order Type Dropdown */}
      <div style={{ marginBottom: '12px' }}>
        <label style={{ display: 'block', fontSize: '11px', color: 'var(--text-secondary)', marginBottom: '4px' }}>
          Order Type
        </label>
        <select
          value={orderType}
          onChange={(e) => setOrderType(e.target.value as OrderType)}
          style={{
            width: '100%',
            height: '36px',
            background: 'var(--bg-input)',
            color: 'var(--text-primary)',
            border: '1px solid var(--border)',
            borderRadius: '4px',
            padding: '0 8px',
            fontFamily: 'var(--font-ui)',
            fontSize: '13px',
          }}
        >
          <option value="Market">Market</option>
          <option value="Limit">Limit</option>
        </select>
      </div>

      {/* Quantity */}
      <div style={{ marginBottom: '12px' }}>
        <label style={{ display: 'block', fontSize: '11px', color: 'var(--text-secondary)', marginBottom: '4px' }}>
          Quantity
        </label>
        <div style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
          <input
            type="number"
            value={quantity}
            onChange={(e) => setQuantity(e.target.value)}
            placeholder="0"
            min="0"
            step="1"
            style={{
              flex: 1,
              height: '36px',
              background: 'var(--bg-input)',
              color: 'var(--text-primary)',
              border: '1px solid var(--border)',
              borderRadius: '4px',
              padding: '0 8px',
              fontFamily: 'var(--font-mono)',
              fontSize: '14px',
            }}
          />
          <span style={{ fontSize: '12px', color: 'var(--text-secondary)' }}>shares</span>
        </div>
      </div>

      {/* Limit Price (only for Limit orders) */}
      {orderType === 'Limit' && (
        <div style={{ marginBottom: '12px' }}>
          <label style={{ display: 'block', fontSize: '11px', color: 'var(--text-secondary)', marginBottom: '4px' }}>
            Limit Price
          </label>
          <div style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
            <span style={{ fontSize: '14px', color: 'var(--text-secondary)' }}>$</span>
            <input
              type="number"
              value={limitPrice}
              onChange={(e) => setLimitPrice(e.target.value)}
              placeholder="0.00"
              min="0"
              step="0.01"
              style={{
                flex: 1,
                height: '36px',
                background: 'var(--bg-input)',
                color: 'var(--text-primary)',
                border: '1px solid var(--border)',
                borderRadius: '4px',
                padding: '0 8px',
                fontFamily: 'var(--font-mono)',
                fontSize: '14px',
              }}
            />
          </div>
        </div>
      )}

      {/* Estimated Cost */}
      <div style={{ marginBottom: '8px' }}>
        <label style={{ display: 'block', fontSize: '11px', color: 'var(--text-secondary)', marginBottom: '4px' }}>
          {isBuy ? 'Estimated Cost' : 'Estimated Proceeds'}
        </label>
        <span className="mono" style={{ fontSize: '16px', color: 'var(--text-primary)' }}>
          ${qty > 0 ? totalCost.toFixed(2) : '0.00'}
        </span>
        {qty > 0 && (
          <span style={{ fontSize: '11px', color: 'var(--text-disabled)', marginLeft: '8px' }}>
            (incl. ${commission} commission)
          </span>
        )}
      </div>

      {/* Available Cash / Position */}
      <div style={{ marginBottom: '12px', fontSize: '12px', color: 'var(--text-secondary)' }}>
        {isBuy ? (
          <span style={{ color: totalCost > cash && qty > 0 ? 'var(--red-primary)' : undefined }}>
            Available Cash: <span className="mono">${cash.toFixed(2)}</span>
          </span>
        ) : (
          <span>
            Position: <span className="mono">{position ? `${position.shares} shares` : 'None'}</span>
          </span>
        )}
      </div>

      {/* Market status hint */}
      {!isMarketOpen && (
        <div style={{ marginBottom: '12px', fontSize: '11px', color: 'var(--text-disabled)' }}>
          Market is closed. Orders will execute at market open.
        </div>
      )}

      {/* Place Order Button */}
      <button
        onClick={handleSubmit}
        disabled={!canSubmit}
        style={{
          width: '100%',
          height: '44px',
          border: 'none',
          borderRadius: '6px',
          fontFamily: 'var(--font-ui)',
          fontWeight: 700,
          fontSize: '14px',
          textTransform: 'uppercase',
          cursor: canSubmit ? 'pointer' : 'not-allowed',
          opacity: canSubmit ? 1 : 0.4,
          background: isBuy ? 'var(--green-primary)' : 'var(--red-primary)',
          color: '#FFFFFF',
        }}
      >
        {submitting ? 'PLACING...' : `PLACE ${side.toUpperCase()} ORDER`}
      </button>

      {/* Toast */}
      {toast && (
        <div
          style={{
            marginTop: '8px',
            padding: '8px 12px',
            borderRadius: '4px',
            fontSize: '12px',
            fontFamily: 'var(--font-mono)',
            background: toast.type === 'success' ? 'var(--green-dim)' : 'var(--red-dim)',
            color: toast.type === 'success' ? 'var(--green-primary)' : 'var(--red-primary)',
          }}
        >
          {toast.message}
        </div>
      )}

      {/* Position Quick View (Bible 3.5.3) */}
      {position && (
        <div style={{ marginTop: '16px', padding: '12px', background: 'var(--bg-tertiary)', borderRadius: '6px' }}>
          <div style={{ fontSize: '13px', fontWeight: 600, color: 'var(--text-secondary)', marginBottom: '8px' }}>
            Your Position
          </div>
          <div className="mono" style={{ fontSize: '13px', color: 'var(--text-primary)', marginBottom: '4px' }}>
            {position.shares} shares @ ${position.averageCost.toFixed(2)}
          </div>
          <div className="mono" style={{ fontSize: '13px', color: 'var(--text-secondary)', marginBottom: '4px' }}>
            Value: ${position.marketValue.toFixed(2)}
          </div>
          <div
            className="mono"
            style={{
              fontSize: '14px',
              color: position.unrealizedPnL >= 0 ? 'var(--green-primary)' : 'var(--red-primary)',
            }}
          >
            P&L: {position.unrealizedPnL >= 0 ? '+' : ''}${position.unrealizedPnL.toFixed(2)}
            {' '}({position.unrealizedPnLPercent >= 0 ? '+' : ''}{position.unrealizedPnLPercent.toFixed(2)}%)
          </div>
          <div style={{ display: 'flex', gap: '8px', marginTop: '8px' }}>
            <button
              onClick={() => {
                setSide('Sell');
                setOrderType('Market');
                setQuantity(position.shares.toString());
              }}
              style={{
                flex: 1,
                height: '32px',
                border: 'none',
                borderRadius: '4px',
                background: 'var(--red-primary)',
                color: '#FFFFFF',
                fontFamily: 'var(--font-ui)',
                fontSize: '12px',
                fontWeight: 700,
                cursor: 'pointer',
              }}
            >
              SELL ALL
            </button>
            <button
              onClick={() => {
                setSide('Sell');
                setOrderType('Market');
                setQuantity('');
              }}
              style={{
                flex: 1,
                height: '32px',
                border: 'none',
                borderRadius: '4px',
                background: 'var(--bg-input)',
                color: 'var(--text-primary)',
                fontFamily: 'var(--font-ui)',
                fontSize: '12px',
                fontWeight: 700,
                cursor: 'pointer',
              }}
            >
              SELL PARTIAL
            </button>
          </div>
        </div>
      )}
    </div>
  );
}
