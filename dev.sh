#!/bin/bash
# StockSim Development Launcher
# Starts both backend (C# .NET) and frontend (Vite) for development

echo "[DEV] Starting StockSim in development mode..."

# Start backend in background
echo "[DEV] Starting C# backend..."
cd backend/StockSim.Engine
dotnet run &
BACKEND_PID=$!
cd ../..

# Wait for backend to be ready
echo "[DEV] Waiting for backend..."
sleep 3

# Start frontend
echo "[DEV] Starting Vite frontend..."
cd frontend
npx vite --host &
FRONTEND_PID=$!
cd ..

echo "[DEV] Backend PID: $BACKEND_PID"
echo "[DEV] Frontend PID: $FRONTEND_PID"
echo "[DEV] Open http://localhost:5173 in your browser"
echo "[DEV] Press Ctrl+C to stop both"

# Wait for Ctrl+C and cleanup
trap "echo '[DEV] Stopping...'; kill $BACKEND_PID $FRONTEND_PID 2>/dev/null; exit 0" INT TERM
wait
