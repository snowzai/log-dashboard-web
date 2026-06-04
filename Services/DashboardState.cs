using LogDashboard.Web.Models;

namespace LogDashboard.Web.Services;

/// <summary>
/// Per-circuit (per-tab) state for the dashboard.
/// All filtering, selection and statistics live here.
/// Components subscribe to OnChange to re-render.
/// </summary>
public class DashboardState
{
    // ── Raw data ────────────────────────────────────────────
    private List<LogEntry> _allLogs = [];

    // ── Events ──────────────────────────────────────────────
    public event Action? OnChange;

    // ── Filter state ────────────────────────────────────────
    public string SelectedLevel { get; private set; } = "All";
    public string SelectedTimeRange { get; private set; } = "1h";
    public string SearchText { get; private set; } = string.Empty;

    public DateTime? StartDate { get; private set; }
    public TimeSpan? StartTime { get; private set; }
    public DateTime? EndDate { get; private set; }
    public TimeSpan? EndTime { get; private set; }

    public bool IsCustomDateRange => StartDate.HasValue || EndDate.HasValue;

    // ── Selection ───────────────────────────────────────────
    public LogEntry? SelectedLog { get; private set; }

    // ── Computed collections ─────────────────────────────────
    public List<LogEntry> FilteredLogs { get; private set; } = [];
    public List<LogEntry> ExceptionLogs { get; private set; } = [];

    // ── Statistics ───────────────────────────────────────────
    public int TotalCount { get; private set; }
    public int FatalCount { get; private set; }
    public int ErrorCount { get; private set; }
    public int WarningCount { get; private set; }
    public int InformationCount { get; private set; }
    public int DebugCount { get; private set; }
    public int VerboseCount { get; private set; }
    public int ExceptionCount { get; private set; }

    // ── Watcher / loading ────────────────────────────────────
    public bool IsLoading { get; private set; }
    public bool IsWatching { get; set; }
    public string CurrentFolder { get; private set; } = string.Empty;
    public string? ErrorMessage { get; private set; }

    // ── Public mutators ──────────────────────────────────────

    public void SetFolder(string folder)
    {
        CurrentFolder = folder;
        NotifyChange();
    }

    public void SetLoading(bool loading)
    {
        IsLoading = loading;
        NotifyChange();
    }

    public void SetError(string? message)
    {
        ErrorMessage = message;
        NotifyChange();
    }

    public void SetLogs(IEnumerable<LogEntry> logs)
    {
        _allLogs = logs.OrderByDescending(x => x.Timestamp).ToList();
        ApplyFilter();
    }

    public void SetLevelFilter(string level)
    {
        SelectedLevel = level;
        ApplyFilter();
    }

    public void SetTimeRange(string range)
    {
        SelectedTimeRange = range;
        ApplyFilter();
    }

    public void SetSearch(string text)
    {
        SearchText = text;
        ApplyFilter();
    }

    public void SetDateRange(DateTime? startDate, TimeSpan? startTime, DateTime? endDate, TimeSpan? endTime)
    {
        StartDate = startDate;
        StartTime = startTime;
        EndDate = endDate;
        EndTime = endTime;
        ApplyFilter();
    }

    public void ClearDateRange()
    {
        StartDate = null;
        StartTime = null;
        EndDate = null;
        EndTime = null;
        ApplyFilter();
    }

    public void SelectLog(LogEntry? log)
    {
        SelectedLog = log;
        NotifyChange();
    }

    // ── Private ──────────────────────────────────────────────

    private DateTime? CustomStart =>
        StartDate.HasValue ? StartDate.Value.Date + (StartTime ?? TimeSpan.Zero) : null;

    private DateTime? CustomEnd =>
        EndDate.HasValue ? EndDate.Value.Date + (EndTime ?? new TimeSpan(23, 59, 59)) : null;

    private DateTime? GetTimeRangeStart() => SelectedTimeRange switch
    {
        "1h"   => DateTime.Now.AddHours(-1),
        "4h"   => DateTime.Now.AddHours(-4),
        "12h"  => DateTime.Now.AddHours(-12),
        "1d"   => DateTime.Now.AddDays(-1),
        "7d"   => DateTime.Now.AddDays(-7),
        "1mon" => DateTime.Now.AddMonths(-1),
        _      => null
    };

    private void ApplyFilter()
    {
        IEnumerable<LogEntry> q = _allLogs;

        // ── Time filter ──
        if (IsCustomDateRange)
        {
            if (CustomStart.HasValue) q = q.Where(x => x.Timestamp >= CustomStart.Value);
            if (CustomEnd.HasValue)   q = q.Where(x => x.Timestamp <= CustomEnd.Value);
        }
        else
        {
            var since = GetTimeRangeStart();
            if (since.HasValue) q = q.Where(x => x.Timestamp >= since.Value);
        }

        // ── Search ──
        if (!string.IsNullOrWhiteSpace(SearchText))
            q = q.Where(x =>
                (x.Message.Contains(SearchText, StringComparison.OrdinalIgnoreCase)) ||
                (x.Exception?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ?? false) ||
                x.Properties.Any(p => p.Value?.ToString()?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) == true));

        var baseList = q.ToList();

        // ── Statistics (before level filter) ──
        TotalCount       = baseList.Count;
        FatalCount       = baseList.Count(x => x.Level == "Fatal");
        ErrorCount       = baseList.Count(x => x.Level == "Error");
        WarningCount     = baseList.Count(x => x.Level == "Warning");
        InformationCount = baseList.Count(x => x.Level == "Information");
        DebugCount       = baseList.Count(x => x.Level == "Debug");
        VerboseCount     = baseList.Count(x => x.Level == "Verbose");

        // ── Level filter ──
        FilteredLogs = SelectedLevel == "All"
            ? baseList
            : baseList.Where(x => string.Equals(x.Level, SelectedLevel, StringComparison.OrdinalIgnoreCase)).ToList();

        // ── Exception tab ──
        ExceptionLogs = baseList.Where(x => x.Level is "Error" or "Fatal").ToList();
        ExceptionCount = ExceptionLogs.Count;

        NotifyChange();
    }

    private void NotifyChange() => OnChange?.Invoke();
}
