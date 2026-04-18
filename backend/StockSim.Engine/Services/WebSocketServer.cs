using System.Net;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using StockSim.Engine.Utils;

namespace StockSim.Engine.Services;

/// <summary>
/// WebSocket server for communication with the Electron/React frontend.
/// Uses raw TcpListener on 127.0.0.1 to avoid Windows Firewall / http.sys issues.
/// Handles connection lifecycle, message routing, and heartbeat.
/// See Spec section 21.3 for the full protocol specification.
/// </summary>
public class WebSocketServer : IDisposable
{
    private readonly TcpListener _listener;
    private readonly Logger _log = new("WebSocketServer");
    private readonly int _port;
    private WebSocket? _clientSocket;
    private CancellationTokenSource _cts = new();
    private readonly List<Func<string, string, Task>> _messageHandlers = new();

    public bool IsClientConnected => _clientSocket?.State == WebSocketState.Open;
    public event Action? OnClientConnected;
    public event Action? OnClientDisconnected;

    public int Port => _port;

    public WebSocketServer(int port)
    {
        _port = port;
        _listener = new TcpListener(IPAddress.Loopback, port);
    }

    /// <summary>
    /// Create a WebSocketServer on a free port assigned by the OS.
    /// Binds to 127.0.0.1:0 — the OS assigns a guaranteed free port.
    /// </summary>
    public static WebSocketServer CreateOnFreePort()
    {
        var log = new Logger("WebSocketServer");
        // Bind with port 0, OS assigns a free port
        var tempListener = new TcpListener(IPAddress.Loopback, 0);
        tempListener.Start();
        var port = ((IPEndPoint)tempListener.LocalEndpoint).Port;
        tempListener.Stop();
        log.Info($"OS assigned free port {port}");
        return new WebSocketServer(port);
    }

    public async Task StartAsync()
    {
        _log.Info($"Starting WebSocket server on 127.0.0.1:{_port}");
        _listener.Start();
        _log.Info($"WebSocket server listening on ws://127.0.0.1:{_port}");

        // Signal to Electron that backend is ready (includes port for dynamic discovery)
        // NOTE: Must remain Console.WriteLine — Electron reads stdout to detect readiness
        Console.WriteLine($"READY:{_port}");
        _log.Info($"Backend ready signal sent on port {_port}");

        while (!_cts.Token.IsCancellationRequested)
        {
            try
            {
                var tcpClient = await _listener.AcceptTcpClientAsync(_cts.Token);
                _ = HandleTcpClientAsync(tcpClient);
            }
            catch (Exception ex) when (!_cts.Token.IsCancellationRequested)
            {
                _log.Error("Error accepting connection", new { error = ex.Message });
            }
            catch (OperationCanceledException) { break; }
        }
    }

    private async Task HandleTcpClientAsync(TcpClient tcpClient)
    {
        var stream = tcpClient.GetStream();
        try
        {
            // Read the HTTP upgrade request
            var buffer = new byte[4096];
            var bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, _cts.Token);
            var request = Encoding.UTF8.GetString(buffer, 0, bytesRead);

            // Check if this is a WebSocket upgrade request
            if (!request.Contains("Upgrade: websocket", StringComparison.OrdinalIgnoreCase))
            {
                var badResponse = "HTTP/1.1 400 Bad Request\r\n\r\n"u8.ToArray();
                await stream.WriteAsync(badResponse, _cts.Token);
                tcpClient.Close();
                _log.Warn("Non-WebSocket request rejected");
                return;
            }

            // Extract Sec-WebSocket-Key for the handshake
            var key = ExtractWebSocketKey(request);
            if (key == null)
            {
                tcpClient.Close();
                _log.Warn("Missing Sec-WebSocket-Key");
                return;
            }

            // Send the WebSocket upgrade response
            var acceptKey = ComputeAcceptKey(key);
            var response = $"HTTP/1.1 101 Switching Protocols\r\n" +
                           $"Upgrade: websocket\r\n" +
                           $"Connection: Upgrade\r\n" +
                           $"Sec-WebSocket-Accept: {acceptKey}\r\n\r\n";
            var responseBytes = Encoding.UTF8.GetBytes(response);
            await stream.WriteAsync(responseBytes, _cts.Token);

            // Create WebSocket from the upgraded stream
            var ws = WebSocket.CreateFromStream(stream, new WebSocketCreationOptions
            {
                IsServer = true,
                KeepAliveInterval = TimeSpan.FromSeconds(30),
            });

            _log.Info("Client connected");
            _clientSocket = ws;
            OnClientConnected?.Invoke();
            await HandleClientAsync(ws);
        }
        catch (Exception ex) when (!_cts.Token.IsCancellationRequested)
        {
            _log.Error("Error during WebSocket handshake", new { error = ex.Message });
        }
        finally
        {
            tcpClient.Close();
        }
    }

    private static string? ExtractWebSocketKey(string request)
    {
        foreach (var line in request.Split('\n'))
        {
            if (line.StartsWith("Sec-WebSocket-Key:", StringComparison.OrdinalIgnoreCase))
            {
                return line.Substring("Sec-WebSocket-Key:".Length).Trim().TrimEnd('\r');
            }
        }
        return null;
    }

    private static string ComputeAcceptKey(string key)
    {
        // RFC 6455: concatenate key with magic GUID, SHA-1 hash, base64 encode
        var combined = key + "258EAFA5-E914-47DA-95CA-C5AB0DC85B11";
        var hash = SHA1.HashData(Encoding.UTF8.GetBytes(combined));
        return Convert.ToBase64String(hash);
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
    }
}
