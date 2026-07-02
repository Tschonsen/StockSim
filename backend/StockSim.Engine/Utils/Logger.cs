namespace StockSim.Engine.Utils;

public enum LogLevel
{
    Debug = 0,
    Info = 1,
    Warn = 2,
    Error = 3
}

public record LogEntry(
    DateTime Timestamp,
    LogLevel Level,
    string Module,
    string Message,
    object? Data = null
);

/// <summary>
/// Structured logger for the backend simulation engine.
/// Mirrors the frontend logger design for consistency.
/// </summary>
public class Logger
{
    private const int MaxHistory = 5000;
    public string Module { get; }
    // Default level from STOCKSIM_LOG_LEVEL (e.g. "Warn" for fast headless playtests); Debug otherwise.
    private static readonly LogLevel DefaultLevel =
        Enum.TryParse<LogLevel>(Environment.GetEnvironmentVariable("STOCKSIM_LOG_LEVEL"), ignoreCase: true, out var lvl)
            ? lvl : LogLevel.Debug;
    private LogLevel _minLevel = DefaultLevel;
    private readonly List<LogEntry> _history = new();
    private static readonly object _lock = new();

    public Logger(string module)
    {
        Module = module;
    }

    public void SetLevel(LogLevel level) => _minLevel = level;

    public void Debug(string message, object? data = null) => Log(LogLevel.Debug, message, data);
    public void Info(string message, object? data = null) => Log(LogLevel.Info, message, data);
    public void Warn(string message, object? data = null) => Log(LogLevel.Warn, message, data);
    public void Error(string message, object? data = null) => Log(LogLevel.Error, message, data);

    public IReadOnlyList<LogEntry> GetHistory()
    {
        lock (_lock)
        {
            return _history.ToList().AsReadOnly();
        }
    }

    public void ClearHistory()
    {
        lock (_lock)
        {
            _history.Clear();
        }
    }

    private void Log(LogLevel level, string message, object? data)
    {
        if (level < _minLevel) return;

        var entry = new LogEntry(DateTime.UtcNow, level, Module, message, data);

        lock (_lock)
        {
            _history.Add(entry);
            if (_history.Count > MaxHistory)
            {
                _history.RemoveRange(0, _history.Count - MaxHistory);
            }
        }

        var timestamp = entry.Timestamp.ToString("HH:mm:ss.fff");
        var prefix = $"{timestamp} [{level.ToString().ToUpper()}] [{Module}]";

        var output = data != null
            ? $"{prefix} {message} | {System.Text.Json.JsonSerializer.Serialize(data)}"
            : $"{prefix} {message}";

        switch (level)
        {
            case LogLevel.Debug:
            case LogLevel.Info:
                Console.WriteLine(output);
                break;
            case LogLevel.Warn:
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine(output);
                Console.ResetColor();
                break;
            case LogLevel.Error:
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Error.WriteLine(output);
                Console.ResetColor();
                break;
        }
    }
}
