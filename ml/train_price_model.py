"""
StockSim ONNX Price Model — Training Script
=============================================
Downloads Yahoo Finance data, trains a small LSTM that predicts:
  - expected daily return
  - expected daily volatility
per stock, given recent price history + sector + market context.

Usage: python train_price_model.py
Output: ml/price_model.onnx (~500KB)
"""

import os
import sys
import json
import time
import numpy as np
import pandas as pd
import torch
import torch.nn as nn
from torch.utils.data import Dataset, DataLoader
from sklearn.preprocessing import StandardScaler
import onnx
import onnxruntime as ort

# ── Config ──────────────────────────────────────────────────────────────────

LOOKBACK = 20          # 20 trading days of history as input
FEATURES_PER_DAY = 5   # return, volume_ratio, high_low_range, gap, intraday_range
STATIC_FEATURES = 3    # sector_encoding (1-hot reduced to 3 PCA-like buckets), log_market_cap, avg_volatility
TOTAL_INPUT = LOOKBACK * FEATURES_PER_DAY + STATIC_FEATURES  # 20*5 + 3 = 103
HIDDEN_SIZE = 64
NUM_LAYERS = 2
EPOCHS = 30
BATCH_SIZE = 256
LR = 0.001
OUTPUT_PATH = os.path.join(os.path.dirname(__file__), "price_model.onnx")
DATA_CACHE = os.path.join(os.path.dirname(__file__), "data_cache.csv")

# Representative tickers across all 12 StockSim sectors (~200 stocks)
TICKERS_BY_SECTOR = {
    "Technology": ["AAPL", "MSFT", "GOOGL", "NVDA", "META", "AVGO", "ORCL", "CRM", "AMD", "INTC", "ADBE", "CSCO", "IBM", "NOW", "QCOM", "TXN", "AMAT"],
    "Healthcare": ["JNJ", "UNH", "PFE", "ABBV", "MRK", "LLY", "TMO", "ABT", "DHR", "BMY", "AMGN", "GILD", "MDT", "ISRG", "CVS"],
    "Financials": ["JPM", "BAC", "WFC", "GS", "MS", "BLK", "C", "SCHW", "AXP", "USB", "PNC", "TFC", "COF", "MET", "AIG"],
    "Consumer Goods": ["PG", "KO", "PEP", "COST", "WMT", "MCD", "NKE", "SBUX", "TGT", "EL", "CL", "KMB", "GIS", "HSY", "K"],
    "Energy": ["XOM", "CVX", "COP", "SLB", "EOG", "MPC", "PSX", "VLO", "OXY", "HAL", "DVN", "HES", "FANG", "BKR"],
    "Industrials": ["HON", "UPS", "CAT", "DE", "BA", "RTX", "LMT", "GE", "MMM", "EMR", "FDX", "WM", "ETN", "ITW", "ROK"],
    "Real Estate": ["AMT", "PLD", "CCI", "EQIX", "SPG", "PSA", "O", "WELL", "DLR", "AVB", "EQR", "VTR"],
    "Utilities": ["NEE", "DUK", "SO", "D", "AEP", "SRE", "EXC", "XEL", "ED", "WEC", "ES", "PPL"],
    "Materials": ["LIN", "APD", "SHW", "ECL", "DD", "NEM", "FCX", "NUE", "VMC", "MLM", "IFF", "ALB"],
    "Telecommunications": ["VZ", "T", "TMUS", "CMCSA", "CHTR", "LBRDK", "WBD", "PARA", "FOX", "NWSA"],
    "Automotive": ["TSLA", "F", "GM", "TM", "RIVN", "LCID", "APTV", "BWA", "LEA", "ALV"],
    "Aerospace": ["BA", "LMT", "RTX", "NOC", "GD", "TDG", "HWM", "HEI", "TXT", "LHX"],
}

ALL_TICKERS = list(set(t for tickers in TICKERS_BY_SECTOR.values() for t in tickers))
SECTOR_MAP = {}
for sector, tickers in TICKERS_BY_SECTOR.items():
    for t in tickers:
        SECTOR_MAP[t] = sector

SECTORS = sorted(TICKERS_BY_SECTOR.keys())
SECTOR_TO_IDX = {s: i for i, s in enumerate(SECTORS)}

# Simplified sector encoding: map 12 sectors to 3 "buckets" (growth/defensive/cyclical)
SECTOR_BUCKETS = {
    "Technology": [1, 0, 0], "Healthcare": [0.5, 0.5, 0], "Financials": [0, 0, 1],
    "Consumer Goods": [0, 1, 0], "Energy": [0, 0, 1], "Industrials": [0, 0.3, 0.7],
    "Real Estate": [0, 0.7, 0.3], "Utilities": [0, 1, 0], "Materials": [0, 0.3, 0.7],
    "Telecommunications": [0, 0.7, 0.3], "Automotive": [0.7, 0, 0.3], "Aerospace": [0.3, 0, 0.7],
}


# ── Data Download ───────────────────────────────────────────────────────────

def download_data() -> pd.DataFrame:
    """Download 5 years of daily OHLCV data from Yahoo Finance."""
    if os.path.exists(DATA_CACHE):
        print(f"Loading cached data from {DATA_CACHE}")
        return pd.read_csv(DATA_CACHE, parse_dates=["Date"])

    import yfinance as yf

    print(f"Downloading data for {len(ALL_TICKERS)} tickers...")
    all_frames = []

    # Download in batches to avoid rate limits
    batch_size = 20
    for i in range(0, len(ALL_TICKERS), batch_size):
        batch = ALL_TICKERS[i:i + batch_size]
        print(f"  Batch {i // batch_size + 1}/{(len(ALL_TICKERS) + batch_size - 1) // batch_size}: {batch[:3]}...")

        try:
            data = yf.download(batch, period="5y", interval="1d", group_by="ticker", auto_adjust=True, threads=True)

            for ticker in batch:
                try:
                    if len(batch) == 1:
                        df = data.copy()
                    else:
                        df = data[ticker].copy()

                    df = df.dropna(subset=["Close"])
                    if len(df) < 252:  # Need at least 1 year
                        print(f"    Skipping {ticker}: only {len(df)} rows")
                        continue

                    df["Ticker"] = ticker
                    df["Sector"] = SECTOR_MAP.get(ticker, "Unknown")
                    df.index.name = "Date"
                    all_frames.append(df.reset_index())
                except Exception as e:
                    print(f"    Error processing {ticker}: {e}")
        except Exception as e:
            print(f"  Batch download error: {e}")
            time.sleep(2)

    if not all_frames:
        print("ERROR: No data downloaded!")
        sys.exit(1)

    result = pd.concat(all_frames, ignore_index=True)
    result.to_csv(DATA_CACHE, index=False)
    print(f"Downloaded {len(result)} rows for {result['Ticker'].nunique()} tickers. Cached to {DATA_CACHE}")
    return result


# ── Feature Engineering ─────────────────────────────────────────────────────

def build_features(df: pd.DataFrame) -> tuple:
    """Build training samples from raw OHLCV data.

    Per-day features (LOOKBACK days):
      0: daily return (close-to-close)
      1: volume ratio (volume / 20-day avg volume)
      2: high-low range / close (intraday volatility)
      3: gap (open/prev_close - 1)
      4: close position in day range ((close-low)/(high-low))

    Static features (per sample):
      0-2: sector bucket encoding (growth/defensive/cyclical)

    Targets:
      0: next-day return
      1: next-day realized volatility (absolute return as proxy)
    """
    X_sequences = []
    X_static = []
    Y = []

    for ticker, group in df.groupby("Ticker"):
        group = group.sort_values("Date").reset_index(drop=True)

        close = group["Close"].values.astype(np.float64)
        high = group["High"].values.astype(np.float64)
        low = group["Low"].values.astype(np.float64)
        open_ = group["Open"].values.astype(np.float64)
        volume = group["Volume"].values.astype(np.float64)

        # Daily returns
        returns = np.zeros(len(close))
        returns[1:] = (close[1:] - close[:-1]) / np.maximum(close[:-1], 0.01)

        # Volume ratio (vs 20-day moving average)
        vol_ma = pd.Series(volume).rolling(20, min_periods=1).mean().values
        vol_ratio = volume / np.maximum(vol_ma, 1.0)

        # High-low range normalized by close
        hl_range = (high - low) / np.maximum(close, 0.01)

        # Gap: open vs previous close
        gaps = np.zeros(len(close))
        gaps[1:] = (open_[1:] - close[:-1]) / np.maximum(close[:-1], 0.01)

        # Close position in day range
        day_range = high - low
        close_pos = np.where(day_range > 0, (close - low) / day_range, 0.5)

        # Sector encoding
        sector = group["Sector"].iloc[0]
        sector_bucket = SECTOR_BUCKETS.get(sector, [0.33, 0.33, 0.33])

        # Build sequences
        for i in range(LOOKBACK + 20, len(close) - 1):  # Skip first 20 days for vol_ma warmup
            seq = np.column_stack([
                returns[i - LOOKBACK:i],
                vol_ratio[i - LOOKBACK:i],
                hl_range[i - LOOKBACK:i],
                gaps[i - LOOKBACK:i],
                close_pos[i - LOOKBACK:i],
            ])  # Shape: (LOOKBACK, 5)

            static = np.array(sector_bucket, dtype=np.float64)

            # Target: next-day return and volatility
            next_return = returns[i + 1] if i + 1 < len(returns) else 0
            next_vol = abs(next_return)  # Absolute return as volatility proxy

            X_sequences.append(seq.flatten())
            X_static.append(static)
            Y.append([next_return, next_vol])

    X_seq = np.array(X_sequences, dtype=np.float32)
    X_stat = np.array(X_static, dtype=np.float32)
    Y = np.array(Y, dtype=np.float32)

    print(f"Built {len(Y)} training samples from {df['Ticker'].nunique()} tickers")
    return X_seq, X_stat, Y


# ── Model ───────────────────────────────────────────────────────────────────

class PriceModel(nn.Module):
    """Small LSTM that predicts next-day return and volatility.

    Input: concatenated [sequence_features (100), static_features (3)] = 103 floats
    The sequence part is reshaped to (LOOKBACK, FEATURES_PER_DAY) for LSTM processing.
    Output: [expected_return, expected_volatility]
    """

    def __init__(self):
        super().__init__()
        self.lstm = nn.LSTM(
            input_size=FEATURES_PER_DAY,
            hidden_size=HIDDEN_SIZE,
            num_layers=NUM_LAYERS,
            batch_first=True,
            dropout=0.2,
        )
        # Combine LSTM output with static features
        self.fc = nn.Sequential(
            nn.Linear(HIDDEN_SIZE + STATIC_FEATURES, 32),
            nn.ReLU(),
            nn.Dropout(0.1),
            nn.Linear(32, 2),  # [return, volatility]
        )

    def forward(self, x: torch.Tensor) -> torch.Tensor:
        # x shape: (batch, TOTAL_INPUT) = (batch, 103)
        seq_part = x[:, :LOOKBACK * FEATURES_PER_DAY]  # (batch, 100)
        static_part = x[:, LOOKBACK * FEATURES_PER_DAY:]  # (batch, 3)

        # Reshape sequence for LSTM: (batch, LOOKBACK, FEATURES_PER_DAY)
        seq = seq_part.view(-1, LOOKBACK, FEATURES_PER_DAY)

        lstm_out, _ = self.lstm(seq)  # (batch, LOOKBACK, HIDDEN_SIZE)
        last_hidden = lstm_out[:, -1, :]  # (batch, HIDDEN_SIZE) — last timestep

        combined = torch.cat([last_hidden, static_part], dim=1)
        output = self.fc(combined)

        # Volatility must be positive
        return_pred = output[:, 0:1]
        vol_pred = torch.abs(output[:, 1:2]) + 1e-6

        return torch.cat([return_pred, vol_pred], dim=1)


class PriceDataset(Dataset):
    def __init__(self, X_seq, X_stat, Y):
        # Concatenate sequence and static features into single input
        self.X = torch.tensor(np.concatenate([X_seq, X_stat], axis=1), dtype=torch.float32)
        self.Y = torch.tensor(Y, dtype=torch.float32)

    def __len__(self):
        return len(self.Y)

    def __getitem__(self, idx):
        return self.X[idx], self.Y[idx]


# ── Training ────────────────────────────────────────────────────────────────

def train_model(X_seq, X_stat, Y):
    """Train the LSTM model."""

    # Normalize features
    scaler_seq = StandardScaler()
    X_seq_scaled = scaler_seq.fit_transform(X_seq)

    scaler_stat = StandardScaler()
    X_stat_scaled = scaler_stat.fit_transform(X_stat)

    # Clip targets to avoid extreme outliers
    Y_clipped = np.clip(Y, -0.20, 0.20)

    # Train/val split (80/20, time-based: last 20% as validation)
    split = int(len(Y_clipped) * 0.8)
    train_ds = PriceDataset(X_seq_scaled[:split], X_stat_scaled[:split], Y_clipped[:split])
    val_ds = PriceDataset(X_seq_scaled[split:], X_stat_scaled[split:], Y_clipped[split:])

    train_loader = DataLoader(train_ds, batch_size=BATCH_SIZE, shuffle=True)
    val_loader = DataLoader(val_ds, batch_size=BATCH_SIZE)

    device = torch.device("cuda" if torch.cuda.is_available() else "cpu")
    print(f"Training on {device}")

    model = PriceModel().to(device)
    optimizer = torch.optim.Adam(model.parameters(), lr=LR)
    scheduler = torch.optim.lr_scheduler.ReduceLROnPlateau(optimizer, patience=3, factor=0.5)

    # Custom loss: MSE for return, MSE for volatility (weighted)
    def loss_fn(pred, target):
        return_loss = nn.MSELoss()(pred[:, 0], target[:, 0])
        vol_loss = nn.MSELoss()(pred[:, 1], target[:, 1])
        return return_loss + 0.5 * vol_loss

    best_val_loss = float("inf")
    best_state = None

    for epoch in range(EPOCHS):
        # Train
        model.train()
        train_loss = 0
        for X_batch, Y_batch in train_loader:
            X_batch, Y_batch = X_batch.to(device), Y_batch.to(device)
            pred = model(X_batch)
            loss = loss_fn(pred, Y_batch)
            optimizer.zero_grad()
            loss.backward()
            torch.nn.utils.clip_grad_norm_(model.parameters(), 1.0)
            optimizer.step()
            train_loss += loss.item()

        # Validate
        model.eval()
        val_loss = 0
        with torch.no_grad():
            for X_batch, Y_batch in val_loader:
                X_batch, Y_batch = X_batch.to(device), Y_batch.to(device)
                pred = model(X_batch)
                val_loss += loss_fn(pred, Y_batch).item()

        train_loss /= len(train_loader)
        val_loss /= len(val_loader)
        scheduler.step(val_loss)

        if val_loss < best_val_loss:
            best_val_loss = val_loss
            best_state = model.state_dict().copy()

        if (epoch + 1) % 5 == 0 or epoch == 0:
            print(f"  Epoch {epoch + 1:3d}/{EPOCHS}: train_loss={train_loss:.6f}, val_loss={val_loss:.6f}, lr={optimizer.param_groups[0]['lr']:.6f}")

    model.load_state_dict(best_state)
    print(f"Best validation loss: {best_val_loss:.6f}")

    # Save scaler params for C# runtime
    scaler_params = {
        "seq_mean": scaler_seq.mean_.tolist(),
        "seq_scale": scaler_seq.scale_.tolist(),
        "stat_mean": scaler_stat.mean_.tolist(),
        "stat_scale": scaler_stat.scale_.tolist(),
    }
    scaler_path = os.path.join(os.path.dirname(__file__), "scaler_params.json")
    with open(scaler_path, "w") as f:
        json.dump(scaler_params, f)
    print(f"Scaler params saved to {scaler_path}")

    return model


# ── ONNX Export ─────────────────────────────────────────────────────────────

def export_onnx(model: PriceModel):
    """Export model to ONNX format."""
    model.eval()
    model.cpu()

    dummy_input = torch.randn(1, TOTAL_INPUT)

    torch.onnx.export(
        model,
        dummy_input,
        OUTPUT_PATH,
        input_names=["input"],
        output_names=["output"],
        dynamic_axes={"input": {0: "batch"}, "output": {0: "batch"}},
        opset_version=17,
    )

    # Validate
    onnx_model = onnx.load(OUTPUT_PATH)
    onnx.checker.check_model(onnx_model)

    # Test with onnxruntime
    session = ort.InferenceSession(OUTPUT_PATH)
    test_input = np.random.randn(5, TOTAL_INPUT).astype(np.float32)
    result = session.run(None, {"input": test_input})

    print(f"\nONNX model exported to {OUTPUT_PATH}")
    print(f"  Model size: {os.path.getsize(OUTPUT_PATH) / 1024:.1f} KB")
    print(f"  Input shape: (batch, {TOTAL_INPUT})")
    print(f"  Output shape: {result[0].shape}")
    print(f"  Sample predictions (return, vol):")
    for i in range(min(3, len(result[0]))):
        print(f"    [{result[0][i][0]:+.4f}, {result[0][i][1]:.4f}]")

    # Count parameters
    total_params = sum(p.numel() for p in model.parameters())
    print(f"  Total parameters: {total_params:,}")


# ── Main ────────────────────────────────────────────────────────────────────

if __name__ == "__main__":
    print("=" * 60)
    print("StockSim ONNX Price Model — Training")
    print("=" * 60)

    # Step 1: Download data
    print("\n[1/3] Downloading data...")
    df = download_data()

    # Step 2: Build features
    print("\n[2/3] Building features...")
    X_seq, X_stat, Y = build_features(df)

    # Step 3: Train
    print("\n[3/3] Training model...")
    model = train_model(X_seq, X_stat, Y)

    # Step 4: Export
    print("\n[4/4] Exporting ONNX...")
    export_onnx(model)

    print("\nDone! Next step: integrate price_model.onnx into C# backend (Session 23)")
