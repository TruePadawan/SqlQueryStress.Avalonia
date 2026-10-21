# SqlQueryStress

**SqlQueryStress** is a cross-platform SQL Server benchmarking tool that lets you stress-test T-SQL queries with
configurable concurrency, iterations, and parameter substitution. It is an [Avalonia](https://avaloniaui.net) port of
the original Windows-only [SqlQueryStress](https://github.com/ErikEJ/SqlQueryStress) application, built as part of
the [Avalonia Port Challenge](https://avaloniaui.net/blog/avalonia-port-challenge).

## Features

- **Multi-threaded query execution** : Run the same query across multiple threads with a configurable number of
  iterations per thread.
- **Real-time telemetry** : Monitor elapsed time, iterations completed, average client/CPU/actual seconds per iteration,
  logical reads, active threads, and exception counts as the benchmark runs.
- **Parameter substitution** : Bind query parameters to values fetched from a separate parameter query, enabling
  realistic data-driven load testing.
- **SQL Server statistics collection** : Optionally capture `SET STATISTICS IO` and `SET STATISTICS TIME` output to
  measure logical reads, CPU time, and elapsed time per iteration.
- **Buffer and cache management** : Run `CHECKPOINT` and clear data buffers (`DBCC DROPCLEANBUFFERS`) and free the
  procedure cache (`DBCC FREEPROCCACHE`) before running benchmarks for reproducible results.
- **Benchmark export** : Save benchmark results to plain text, CSV, or copy to the clipboard.
- **Settings persistence** : Save and load configuration (connection info, query, iterations, threads, etc.) as JSON
  files.
- **Cancellation support** : Cancel a running benchmark at any time, with an option to kill active queries on the
  server.
- **Cross-platform** : Built on Avalonia UI, targeting .NET 10.0, with Windows, Linux, and macOS support.

## Before & After Comparison

Comparing the original Windows-only legacy application with the modernized **Avalonia 12** port (supporting both Light
and Dark themes):

| View                       |                                         Original App (Windows Legacy)                                         |                                            Avalonia 12 Port (Modern)                                            |
|:---------------------------|:-------------------------------------------------------------------------------------------------------------:|:---------------------------------------------------------------------------------------------------------------:|
| **Database Configuration** |       ![Original Database Select](docs/screenshots/original_sqlquerystress_database_window_windows.png)       |         ![Avalonia Database Select](docs/screenshots/sqlquerystress_database_window_linux_darkmode.png)         |
| **Parameter Substitution** | ![Original Parameter Substitution](docs/screenshots/original_sqlquerystress_parameter_sub_window_windows.png) | ![Avalonia Parameter Substitution](docs/screenshots/sqlquerystress_param_substituion_window_linux_darkmode.png) |
| **Options Dialog**         |        ![Original Options Dialog](docs/screenshots/original_sqlquerystress_options_window_windows.png)        |              ![Avalonia Options Dialog](docs/screenshots/sqlquerystress_options_window_linux.png)               |

## Cross-Platform Support

Shipping from a single .NET 10 codebase across **Linux**, **macOS**, and **Windows**:

| Platform                                 |                                               Screenshot                                                |
|:-----------------------------------------|:-------------------------------------------------------------------------------------------------------:|
| **Linux (Dark Mode - Active Benchmark)** | ![SqlQueryStress Active Benchmark on Linux](docs/screenshots/sqlquerystress_active_benchmark_linux.png) |
| **Linux (Light Mode)**                   |       ![SqlQueryStress Light Mode on Linux](docs/screenshots/sqlquerystress_mainwindow_linux.png)       |
| **macOS**                                |         ![SqlQueryStress on macOS](docs/screenshots/sqlquerystress_mainwindow_mac_darkmode.png)         |
| **Windows**                              |       ![SqlQueryStress on macOS](docs/screenshots/sqlquerystress_mainwindow_windows_darkmode.png)       |

## Migration Write-Up

We took our time working on the migration from the original Windows desktop application to the modernized Avalonia port.
We preserved the original functionality while improving on the existing codebase, UI/UX and architecture. Some
improvements include: adding support for macOS and Linux, re-architecting the application to be more testable and
maintainable, and adding automatic syncing with the device's system theme. Building the UI was quite seamless with
Avalonia's layout system, and I particularly liked how easy it was to add automatic light/dark mode with Avalonia.

### What Translated Cleanly

- **Zero-Friction .NET Ecosystem Fit:** There was zero friction migrating the business logic into the Avalonia port. Its
  natural fit with the .NET ecosystem and familiar C# patterns meant we spent no time fighting the framework and much
  more time verifying behavioural correctness and improving the software itself.
- **Core SQL Execution & Telemetry Parsing:** The underlying execution model using `Microsoft.Data.SqlClient`,
  `SqlConnectionStringBuilder`, and regex parsing for `SET STATISTICS IO` and `SET STATISTICS TIME` via
  `SqlConnection.InfoMessage` migrated cleanly.
- **Parameter Substitution:** Configuration serialization and the data-driven parameter substitution logic
  (`ParamServer`) carried over smoothly once decoupled from UI controls.
- **SQL Syntax Editing:** Replacing the legacy Windows code editor with **AvaloniaEdit** allowed us to preserve full
  T-SQL syntax highlighting with native Light and Dark mode reactivity.

### What Needed Rethinking

- **Decoupling Legacy Code-Behind into MVVM:** The original application relied heavily on tightly coupled UI
  code-behind. We re-architected the presentation layer from the ground up using **CommunityToolkit.Mvvm**, dependency
  injection, and testable services (`QueryWorker`, `LoadEngine`,
  `BenchmarkExportService`, etc.) backed by an xUnit test suite.
- **High-Frequency UI Dispatcher Starvation:** During stress testing with 20+ concurrent threads running sub-millisecond
  queries (10,000+ iterations), sending telemetry updates to the UI thread on every single query completion caused
  noticeable freezes when opening child windows like `DatabaseSelectWindow`. We solved this by throttling and batching
  UI telemetry updates and dispatching them at `DispatcherPriority.Background` while ensuring the final state always
  flushes on completion.

### What Caught Us by Surprise

- **Cross-Platform Packaging:** Packaging self-contained builds from a Linux CI environment for Windows, Linux (`.deb`,
  `.rpm`, `.tar.gz`), and macOS (`.app` bundles for both `osx-arm64` and
  `osx-x64`) surfaced subtle runtime differences. On macOS, single-file publishing stripped `libSkiaSharp.dylib`
  resolution, requiring a multi-file `.app` bundle layout inside `Contents/MacOS/`.
- **macOS Gatekeeper Quarantine on Open-Source Builds:** Because open-source builds without a paid Apple Developer ID
  are flagged as *"damaged"* when downloaded via a web browser due to `com.apple.quarantine`, we automated a **Homebrew
  Cask** updater directly in our GitHub Actions release pipeline alongside a one-line terminal installer script
  (`install_mac.sh`) so macOS users on Intel, M-series, and MacBook Neo machines get a zero-friction installation.

## Installation

### macOS

> [!IMPORTANT]
> **macOS Users:** Do not download the `.zip` archive directly from the GitHub Releases page via a web browser. Because
this open-source application is not notarized with a paid Apple Developer account, macOS Gatekeeper flags browser
downloads with an *"App is damaged and can't be opened"* error. Instead, install using either the one-line terminal
installer or Homebrew below so the app launches cleanly out of the box:

#### Option 1: One-Line Terminal Installer (Recommended)

Automatically detects your Mac type (Apple Silicon / MacBook Neo / Intel), installs `SqlQueryStress.app` to
`/Applications`, clears Gatekeeper quarantine flags, and launches the app:

```bash
curl -fsSL https://raw.githubusercontent.com/TruePadawan/SqlQueryStress.Avalonia/master/scripts/install_mac.sh | bash
```

#### Option 2: Homebrew Cask

```bash
# 1. Tap this repository
brew tap truepadawan/sqlquerystress https://github.com/TruePadawan/SqlQueryStress.Avalonia

# 2. Trust the third-party tap (if prompted by your Homebrew version)
brew trust truepadawan/sqlquerystress.avalonia

# 3. Install SqlQueryStress
brew install --cask sqlquerystress
```

### Windows & Linux

Download the latest prebuilt installer or archive for your platform (`win-x64`, `win-arm64`, `.deb`, `.rpm`, or
`.tar.gz`) directly from the [Releases](../../releases) page.

## Database support

- **Microsoft SQL Server** (local and Azure SQL Database)
- **Authentication modes:**
    - Windows Integrated Authentication
    - SQL Server Authentication (username/password)
    - Azure Active Directory : Universal with MFA
- **Connection options:**
    - Read/Write or Read-Only application intent
    - Encryption (Optional, False, True, Strict)
    - Trust server certificate
    - Custom additional connection string parameters
    - Connection pooling toggle

## Requirements (Building from Source)

**Application runtime:**

- A desktop platform listed above.
- Network access from the app to your SQL Server instance (default SQL Server port `1433`, unless configured otherwise).

**To build from source:**

- **.NET SDK 10.0** or later (the projects target `net10.0`).

**SQL Server (required to actually run a benchmark, not to build or unit-test):**

- A reachable SQL Server instance or Azure SQL Database, with credentials (or Windows Integrated Authentication / Entra
  ID) that can connect and execute the queries you test.
- The account used needs permission to run the query, and to run `CHECKPOINT`, `DBCC DROPCLEANBUFFERS`, and
  `DBCC FREEPROCCACHE` if you use the cache-management actions (these typically require elevated permissions).

## Quick start

1. **Configure the database connection** : Click **Database** and enter your server, authentication mode, and database
   name.
2. **Write your query** : Enter the T-SQL query in the SQL editor.
3. **Set query parameters** (optional) : Click **Parameter Substitution** to map query parameters to values returned by
   a data-source query.
4. **Configure the benchmark** : Set the number of iterations, threads, and optional delay between queries.
5. **Adjust options** (optional) : Open **File → Options** to toggle connection pooling, statistics collection,
   timeouts, and more.
6. **Run** : Click **Go** to start the benchmark and watch the real-time telemetry panel.
7. **Export results** : After the run completes, use **File → Save Benchmark** to export to text or CSV, or copy to the
   clipboard.

### Options

| Option                         | Description                                                 |
|--------------------------------|-------------------------------------------------------------|
| Enable Connection Pooling      | Toggle SQL connection pooling on/off                        |
| Collect IO Statistics          | Parse `SET STATISTICS IO` output for logical reads          |
| Collect Time Statistics        | Parse `SET STATISTICS TIME` output for CPU and elapsed time |
| Force Client Retrieval of Data | Ensure all result data is fetched to the client             |
| Kill Queries on Cancel         | Cancel active server commands when the benchmark is stopped |
| Connection Timeout             | Timeout in seconds for opening connections                  |
| Command Timeout                | Timeout in seconds for query execution                      |

## Build and test from source

```bash
# Restore dependencies
dotnet restore

# Build the solution
dotnet build

# Run the desktop application
dotnet run --project SqlQueryStress.Desktop

# Run the unit tests
dotnet test
```

The test project uses **xUnit** and **FluentAssertions**. The unit tests exercise the load engine, workers, parameter
server, and export formatting with mocks and do **not** require a live SQL Server instance.
See [CONTRIBUTING.md](CONTRIBUTING.md) for details on running tests and on SQL Server-dependent scenarios.

## Technologies

- **[Avalonia 12](https://avaloniaui.net)** : Cross-platform UI framework
- **[AvaloniaEdit](https://github.com/AvaloniaUI/AvaloniaEdit)** : Syntax-highlighted SQL editor
- **[CommunityToolkit.Mvvm](https://github.com/CommunityToolkit/dotnet)** : MVVM helpers (observable properties, relay
  commands)
- **[Microsoft.Data.SqlClient](https://github.com/dotnet/SqlClient)** : SQL Server database driver
- **Microsoft.Extensions.DependencyInjection** : Dependency injection
- **xUnit + FluentAssertions** : Testing framework

## Contributing

Contributions are welcome. See [CONTRIBUTING.md](CONTRIBUTING.md) for prerequisites, build/test commands, and how to
report bugs and propose changes.

## License

This project is licensed under the MIT License, matching the
original [ErikEJ/SqlQueryStress](https://github.com/ErikEJ/SqlQueryStress) repository. See [LICENSE](LICENSE).

## Acknowledgments

- Original SqlQueryStress by [Adam Machanic](http://dataeducation.com/sqlquerystress-the-source-code/)
- [ErikEJ/SqlQueryStress](https://github.com/ErikEJ/SqlQueryStress) GitHub host and MIT license
- [Avalonia UI](https://avaloniaui.net) for the cross-platform framework