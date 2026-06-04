using LogDashboard.Web.Models;

namespace LogDashboard.Web.Services;

/// <summary>
/// Per-circuit service that owns the reload timer.
/// Call Start() / Stop() from the Dashboard component.
/// </summary>
public class LogWatcherService : IAsyncDisposable
{
    private readonly LogParserService _parser;
    private readonly DashboardState _state;
    private readonly ILogger<LogWatcherService> _logger;

    private Timer? _timer;
    private int _intervalSeconds = 10;
    private bool _running;

    public LogWatcherService(
        LogParserService parser,
        DashboardState state,
        ILogger<LogWatcherService> logger)
    {
        _parser = parser;
        _state  = state;
        _logger = logger;
    }

    public async Task LoadOnceAsync(string folder, bool includeSubDirs)
    {
        _state.SetLoading(true);
        _state.SetError(null);

        try
        {
            var logs = await _parser.ParseFolderAsync(folder, includeSubDirs);
            _state.SetLogs(logs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading logs from {Folder}", folder);
            _state.SetError($"載入失敗：{ex.Message}");
        }
        finally
        {
            _state.SetLoading(false);
        }
    }

    public void Start(string folder, bool includeSubDirs, int intervalSeconds)
    {
        Stop();
        _intervalSeconds = intervalSeconds;
        _running = true;

        // dueTime 用 interval 而非 Zero，避免按下 ▶ 時立刻重複觸發
        _timer = new Timer(async _ =>
        {
            if (!_running) return;
            await LoadOnceAsync(folder, includeSubDirs);
        }, null, TimeSpan.FromSeconds(_intervalSeconds), TimeSpan.FromSeconds(_intervalSeconds));
    }

    public void Stop()
    {
        _running = false;
        _timer?.Dispose();
        _timer = null;
    }

    public void ChangeInterval(int seconds, string folder, bool includeSubDirs)
    {
        if (_running)
            Start(folder, includeSubDirs, seconds);
    }

    public async ValueTask DisposeAsync()
    {
        Stop();
        await ValueTask.CompletedTask;
    }
}
