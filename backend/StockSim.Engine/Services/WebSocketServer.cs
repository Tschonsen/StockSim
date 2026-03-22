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

    public WebSocketServer(int port = 8765)
    {
        _port = port;
        _listener = new HttpListener();
        _listener.Prefixes.Add($"http://localhost:{_port}/");
    }

    public async Task StartAsync()
    {
        _log.Info($"Starting WebSocket server on port {_port}");
        _listener.Start();
        _log.Info($"WebSocket server listening on ws://localhost:{_port}");

        // Signal to Electron that backend is ready
        Console.WriteLine("READY");

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

        var message = JsonSerializer.Serialize(new { type, payload });
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

    public void Stop()
    {
        _log.Info("Stopping WebSocket server");
        _cts.Cancel();
        _listener.Stop();
    }

    public void Dispose()
    {
        Stop();
        _cts.Dispose();
        _listener.Close();
    }
}
