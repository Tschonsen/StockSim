using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using StockSim.Engine.Utils;

namespace StockSim.Engine.Services;

/// <summary>
/// WebSocket server for communication with the Electron/React frontend.
/// Handles connection lifecycle, message routing, and heartbeat.
/// See Bible section 21.3 for the full protocol specification.
/// </summary>
public class WebSocketServer : IDisposable
{
    private readonly HttpListener _listener;
    private readonly Logger _log = new("WebSocketServer");
    private readonly int _port;
    private WebSocket? _clientSocket;
    private CancellationTokenSource _cts = new();
    private readonly List<Func<string, string, Task>> _messageHandlers = new();

    public bool IsClientConnected => _clientSocket?.State == WebSocketState.Open;
    public event Action? OnClientConnected;
    public event Action? OnClientDisconnected;

    public int Port => _port;

    public WebSocketServer(int port = 8765)
    {
        _port = port;
        _listener = new HttpListener();
        _listener.Prefixes.Add($"http://localhost:{_port}/");
    }

    /// <summary>
    /// Try to create a WebSocketServer, scanning ports starting from startPort.
    /// Returns the first server that successfully binds.
    /// </summary>
    public static WebSocketServer CreateOnFreePort(int startPort = 8765, int maxAttempts = 10)
    {
        var log = new Logger("WebSocketServer");
        for (int i = 0; i < maxAttempts; i++)
        {
            var port = startPort + i;
            try
            {
                var server = new WebSocketServer(port);
                server._listener.Start();
                server._listener.Stop();
                log.Info($"Port {port} is available");
                // Re-create with fresh listener since Stop() invalidates it
                return new WebSocketServer(port);
            }
            catch (Exception)
            {
                log.Warn($"Port {port} is unavailable, trying next");
            }
        }
        throw new InvalidOperationException($"No free port found in range {startPort}-{startPort + maxAttempts - 1}");
    }

    public async Task StartAsync()
    {
        _log.Info($"Starting WebSocket server on port {_port}");
        _listener.Start();
        _log.Info($"WebSocket server listening on ws://localhost:{_port}");

        // Signal to Electron that backend is ready (includes port for dynamic discovery)
        // NOTE: Must remain Console.WriteLine — Electron reads stdout to detect readiness
        Console.WriteLine($"READY:{_port}");
        _log.Info($"Backend ready signal sent on port {_port}");

        while (!_cts.Token.IsCancellationRequested)
        {
            try
            {
                var context = await _listener.GetContextAsync();

                if (context.Request.IsWebSocketRequest)
                {
                    var wsContext = await context.AcceptWebSocketAsync(null);
                    _log.Info("Client connected");
                    _clientSocket = wsContext.WebSocket;
                    OnClientConnected?.Invoke();
                    await HandleClientAsync(_clientSocket);
                }
                else
                {
                    context.Response.StatusCode = 400;
                    context.Response.Close();
                    _log.Warn("Non-WebSocket request rejected");
                }
            }
            catch (Exception ex) when (!_cts.Token.IsCancellationRequested)
            {
                _log.Error("Error accepting connection", new { error = ex.Message });
            }
        }
    }

    public void OnMessage(Func<string, string, Task> handler)
    {
        _messageHandlers.Add(handler);
    }

    public async Task SendAsync(string type, object payload)
    {
        if (_clientSocket?.State != WebSocketState.Open)
        {
            _log.Warn("Cannot send — no client connected", new { type });
            return;
        }

        var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        var message = JsonSerializer.Serialize(new { type, payload }, options);
        var bytes = Encoding.UTF8.GetBytes(message);

        try
        {
            await _clientSocket.SendAsync(
                new ArraySegment<byte>(bytes),
                WebSocketMessageType.Text,
                true,
                _cts.Token);

            _log.Debug("Message sent", new { type, size = bytes.Length });
        }
        catch (Exception ex)
        {
            _log.Error("Failed to send message", new { type, error = ex.Message });
        }
    }

    private async Task HandleClientAsync(WebSocket socket)
    {
        var buffer = new byte[8192];

        try
        {
            while (socket.State == WebSocketState.Open && !_cts.Token.IsCancellationRequested)
            {
                var result = await socket.ReceiveAsync(
                    new ArraySegment<byte>(buffer),
                    _cts.Token);

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    _log.Info("Client disconnected (close frame)");
                    await socket.CloseAsync(
                        WebSocketCloseStatus.NormalClosure,
                        "Client disconnected",
                        CancellationToken.None);
                    break;
                }

                if (result.MessageType == WebSocketMessageType.Text)
                {
                    var json = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    await HandleMessage(json);
                }
            }
        }
        catch (WebSocketException ex)
        {
            _log.Warn("WebSocket connection error", new { error = ex.Message });
        }
        catch (OperationCanceledException)
        {
            _log.Info("WebSocket connection cancelled");
        }
        finally
        {
            _clientSocket = null;
            OnClientDisconnected?.Invoke();
            _log.Info("Client session ended");
        }
    }

    private async Task HandleMessage(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            var type = doc.RootElement.GetProperty("type").GetString() ?? "unknown";
            var payload = doc.RootElement.TryGetProperty("payload", out var p)
                ? p.GetRawText()
                : "{}";

            _log.Debug("Message received", new { type });

            // Handle ping/pong heartbeat internally
            if (type == "ping")
            {
                await SendAsync("pong", new { timestamp = DateTime.UtcNow });
                return;
            }

            // Route to registered handlers
            foreach (var handler in _messageHandlers)
            {
                await handler(type, payload);
            }
        }
        catch (JsonException ex)
        {
            _log.Error("Failed to parse message", new { error = ex.Message, json });
        }
    }

    private bool _stopped;

    public void Stop()
    {
        if (_stopped) return;
        _stopped = true;
        _log.Info("Stopping WebSocket server");
        _cts.Cancel();
        try { _listener.Stop(); } catch { /* already stopped */ }
    }

    public void Dispose()
    {
        Stop();
        try { _cts.Dispose(); } catch { }
        try { _listener.Close(); } catch { }
    }
}
