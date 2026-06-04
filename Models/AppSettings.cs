namespace LogDashboard.Web.Models;

public class AppSettings
{
    /// <summary>監控的 Log 資料夾（可設定多個，用逗號分隔）</summary>
    public string LogFolder { get; set; } = string.Empty;

    /// <summary>是否遞迴搜尋子目錄</summary>
    public bool IncludeSubDirectories { get; set; } = true;

    /// <summary>每頁顯示筆數</summary>
    public int PageSize { get; set; } = 200;

    /// <summary>自動刷新間隔（秒），0 = 停用</summary>
    public int AutoRefreshSeconds { get; set; } = 10;
}
