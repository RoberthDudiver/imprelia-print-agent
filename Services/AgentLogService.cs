using System.Collections.ObjectModel;
using System.Windows.Threading;

namespace Imprelia.PrintAgent;

public sealed class LogEntry
{
    public DateTime Time { get; init; }
    public string Level { get; init; } = "INFO";
    public string Module { get; init; } = "Agente";
    public string Message { get; init; } = "";
    public string TimeFormatted => Time.ToString("HH:mm:ss.fff");
}

public sealed class AgentLogService
{
    private readonly Dispatcher _dispatcher;
    private readonly object _lock = new();
    private readonly List<LogEntry> _entries = new();

    public event EventHandler? LogAdded;
    public ObservableCollection<LogEntry> Entries { get; } = new();

    public AgentLogService(Dispatcher dispatcher)
    {
        _dispatcher = dispatcher;
    }

    public void Add(string message, string level = "INFO", string module = "Agente")
    {
        var entry = new LogEntry { Time = DateTime.Now, Level = level, Module = module, Message = message };
        lock (_lock) _entries.Insert(0, entry);

        _dispatcher.InvokeAsync(() =>
        {
            Entries.Insert(0, entry);
            while (Entries.Count > 500) Entries.RemoveAt(Entries.Count - 1);
            LogAdded?.Invoke(this, EventArgs.Empty);
        });
    }

    public void Info(string message, string module = "Agente") => Add(message, "INFO", module);
    public void Warn(string message, string module = "Agente") => Add(message, "WARNING", module);
    public void Error(string message, string module = "Agente") => Add(message, "ERROR", module);
}
