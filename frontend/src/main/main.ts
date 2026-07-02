import { app, BrowserWindow, Menu } from 'electron';
import { spawn, ChildProcess } from 'child_process';
import path from 'path';

let mainWindow: BrowserWindow | null = null;
let backendProcess: ChildProcess | null = null;

const isDev = !app.isPackaged;
const DEFAULT_BACKEND_PORT = 8765;
let backendPort = DEFAULT_BACKEND_PORT;

/**
 * Start the C# backend as a child process.
 * Waits for "READY:<port>" signal on stdout before proceeding.
 * Backend auto-discovers a free port starting from 8765.
 * See Spec 21.2 for backend lifecycle.
 */
function startBackend(): Promise<void> {
  return new Promise((resolve, reject) => {
    // __dirname is frontend/dist/main → three levels up reaches the repo root, then backend/.
    const backendPath = isDev
      ? path.join(__dirname, '../../../backend/StockSim.Engine')
      : path.join(process.resourcesPath, 'backend');

    const command = isDev ? 'dotnet' : path.join(backendPath, 'StockSim.Engine.exe');
    const args = isDev
      ? ['run', '--project', backendPath, '--', DEFAULT_BACKEND_PORT.toString()]
      : [DEFAULT_BACKEND_PORT.toString()];

    console.log(`[Electron] Starting backend: ${command} ${args.join(' ')}`);

    backendProcess = spawn(command, args, {
      stdio: ['pipe', 'pipe', 'pipe'],
    });

    let resolved = false;

    backendProcess.stdout?.on('data', (data: Buffer) => {
      const output = data.toString().trim();
      console.log(`[Backend] ${output}`);

      if (!resolved && output.includes('READY')) {
        // Parse port from "READY:<port>" format
        const match = output.match(/READY:(\d+)/);
        if (match) {
          backendPort = parseInt(match[1], 10);
        }
        resolved = true;
        console.log(`[Electron] Backend is ready on port ${backendPort}`);
        resolve();
      }
    });

    backendProcess.stderr?.on('data', (data: Buffer) => {
      console.error(`[Backend ERR] ${data.toString().trim()}`);
    });

    backendProcess.on('error', (err) => {
      console.error(`[Electron] Failed to start backend: ${err.message}`);
      if (!resolved) reject(err);
    });

    backendProcess.on('exit', (code) => {
      console.log(`[Electron] Backend exited with code ${code}`);
      backendProcess = null;
      if (!resolved) reject(new Error(`Backend exited with code ${code}`));
    });

    // Timeout after 30 seconds
    setTimeout(() => {
      if (!resolved) {
        console.error('[Electron] Backend startup timeout');
        reject(new Error('Backend startup timeout'));
      }
    }, 30000);
  });
}

/**
 * Create the main application window.
 * See Spec 2.8 for window size specifications.
 */
function createWindow(): void {
  mainWindow = new BrowserWindow({
    width: 1920,
    height: 1080,
    minWidth: 1280,
    minHeight: 720,
    backgroundColor: '#0A0E17', // bg-primary
    title: 'StockSim',
    show: false, // Show after ready-to-show
    webPreferences: {
      nodeIntegration: false,
      contextIsolation: true,
      preload: path.join(__dirname, 'preload.js'),
    },
  });

  // Remove default menu to prevent Ctrl+W from closing the window
  Menu.setApplicationMenu(null);

  // Show window when content is loaded (no white flash)
  mainWindow.once('ready-to-show', () => {
    mainWindow?.show();
  });

  if (isDev) {
    mainWindow.loadURL(`http://localhost:5173?backendPort=${backendPort}`);
  } else {
    mainWindow.loadFile(path.join(__dirname, '../index.html'), {
      query: { backendPort: backendPort.toString() },
    });
  }

  // DevTools: F12 or Ctrl+Shift+I to toggle
  mainWindow.webContents.openDevTools({ mode: 'detach' });

  mainWindow.on('closed', () => {
    mainWindow = null;
  });
}

/**
 * Cleanup: stop backend when app quits.
 */
function stopBackend(): void {
  if (backendProcess && !backendProcess.killed) {
    console.log('[Electron] Stopping backend');
    backendProcess.kill('SIGTERM');
    backendProcess = null;
  }
}

// App lifecycle
app.whenReady().then(async () => {
  try {
    await startBackend();
    createWindow();
  } catch (err) {
    console.error('[Electron] Startup failed:', err);
    // Start without backend for UI development
    createWindow();
  }
});

app.on('window-all-closed', () => {
  stopBackend();
  app.quit();
});

app.on('before-quit', () => {
  stopBackend();
});
