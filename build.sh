#!/bin/bash
# StockSim — Full Build Script
# Builds backend (self-contained) + frontend + NSIS installer
# Output: frontend/release/StockSim Setup X.X.X.exe

set -e

echo "=== StockSim Build ==="

# 1. Backend: self-contained publish (no .NET Runtime needed on target)
echo "[1/3] Publishing backend (self-contained, win-x64)..."
cd backend/StockSim.Engine
dotnet publish -c Release -r win-x64 --self-contained true -o bin/Release/net8.0/publish
cd ../..
echo "  -> Backend published to backend/StockSim.Engine/bin/Release/net8.0/publish"

# 2. Frontend: build React + compile Electron main process
echo "[2/3] Building frontend..."
cd frontend
npm run build
npx tsc -p tsconfig.electron.json
echo "  -> Frontend built to frontend/dist"

# 3. Package: electron-builder creates NSIS installer
echo "[3/3] Packaging installer..."
npx electron-builder --win
cd ..

echo ""
echo "=== Build complete ==="
echo "Installer: frontend/release/"
ls -lh frontend/release/*.exe 2>/dev/null || echo "(no .exe found — check logs above)"
