# LogDashboard.Web

CLEF (.json) Log 監控 Dashboard —— Blazor Server 網頁版。

## 專案結構

```
LogDashboard.Web/
├── Components/
│   ├── App.razor               ← HTML shell
│   ├── Routes.razor
│   ├── _Imports.razor
│   ├── Layout/
│   │   └── MainLayout.razor
│   ├── Pages/
│   │   └── Dashboard.razor     ← 主頁面（全部功能在此）
│   └── Shared/
│       └── LevelRow.razor      ← Sidebar level row component
├── Models/
│   ├── LogEntry.cs
│   └── AppSettings.cs
├── Services/
│   ├── LogParserService.cs     ← 解析 CLEF JSON log 檔
│   ├── DashboardState.cs       ← 所有 filter/統計 state（scoped）
│   └── LogWatcherService.cs    ← 自動刷新 timer（scoped）
├── wwwroot/css/app.css         ← 全部樣式
├── appsettings.json            ← ★ 主要設定檔
└── Program.cs
```

## 設定（appsettings.json）

```json
{
  "LogDashboard": {
    "LogFolder": "D:\\Logs",        // ★ Log 資料夾路徑
    "IncludeSubDirectories": true,  // 是否遞迴子目錄
    "PageSize": 200,                // 每頁顯示筆數
    "AutoRefreshSeconds": 10        // 自動刷新間隔（秒）；0 = 停用
  }
}
```

## 需求

- .NET 10 SDK（開發）或 .NET 10 Runtime（部署）
- https://dotnet.microsoft.com/download/dotnet/10.0

## 本地執行

```bash
cd LogDashboard.Web
dotnet run
# 開啟 http://localhost:5000
```

## 發布（Windows Server）

```bash
# Framework-dependent（需要目標機器安裝 .NET 9 Runtime）
dotnet publish -c Release -o publish/

# 或 Self-contained（不需要安裝 Runtime，檔案較大）
dotnet publish -c Release -r win-x64 --self-contained true -o publish/
```

### 直接執行

```bash
# 複製 publish/ 資料夾到 Server
# 修改 appsettings.json 設定 LogFolder
LogDashboard.Web.exe --urls "http://0.0.0.0:5000"
```

### 以 Windows Service 執行

在 Program.cs 的 `builder.Services` 加入：

```csharp
builder.Services.AddWindowsService(options =>
    options.ServiceName = "LogDashboard");
```

加入 NuGet 套件：

```bash
dotnet add package Microsoft.Extensions.Hosting.WindowsServices
```

然後：

```powershell
sc create LogDashboard binpath="C:\deploy\LogDashboard.Web.exe --urls http://0.0.0.0:5000"
sc start LogDashboard
```

### 掛 IIS（需安裝 ASP.NET Core Hosting Bundle）

發布後在 IIS 建立網站，指向 publish 資料夾。`web.config` 會在發布時自動產生。

## 功能對照（vs Avalonia 版）

| 功能 | 狀態 |
|------|------|
| Level filter sidebar | ✅ |
| Time range chips (1h/4h/12h/1d/7d/1mon/All) | ✅ |
| Full-text search | ✅ |
| Custom date range picker | ✅ |
| All logs tab + Exceptions tab | ✅ |
| Log detail panel（點擊展開） | ✅ |
| Properties 展示 | ✅ |
| Exception 紅色區塊 | ✅ |
| Auto-refresh watcher | ✅ |
| Refresh 間隔切換（5s~5min） | ✅ |
| 手動 Refresh 按鈕 | ✅ |
| Watching 綠色指示燈 | ✅ |
| 路徑從 appsettings.json 讀取 | ✅ |
| 支援 FileShare.ReadWrite（Serilog 並行寫入） | ✅ |
| Dark theme | ✅ |

## Log 格式支援

Serilog CLEF (Compact Log Event Format) — 每行一個 JSON：

```json
{"@t":"2024-01-15T10:23:45.123Z","@l":"Error","@m":"Something failed","@x":"System.Exception...","RequestId":"abc-123"}
```
