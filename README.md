# SqlQueryStress

**SqlQueryStress** is a cross-platform SQL Server benchmarking tool that lets you stress-test T-SQL queries with configurable concurrency, iterations, and parameter substitution. It is an [Avalonia](https://avaloniaui.net) port of the original Windows-only [SqlQueryStress](https://github.com/ErikEJ/SqlQueryStress) application, built as part of the [Avalonia Port Challenge](https://avaloniaui.net/blog/avalonia-port-challenge).

<img width="1894" height="1025" alt="SqlQueryStress main window showing a SQL editor and the telemetry panel" src="https://github.com/user-attachments/assets/e86cfffc-31ba-49d6-98f6-729bf8f00e9f" />
<img width="1894" height="1025" alt="SqlQueryStress benchmark results view" src="https://github.com/user-attachments/assets/bffe0b5d-b3ef-4571-9b68-e2260af4c151" />

## Features

- **Multi-threaded query execution** : Run the same query across multiple threads with a configurable number of iterations per thread.
- **Real-time telemetry** : Monitor elapsed time, iterations completed, average client/CPU/actual seconds per iteration, logical reads, active threads, and exception counts as the benchmark runs.
- **Parameter substitution** : Bind query parameters to values fetched from a separate parameter query, enabling realistic data-driven load testing.
- **SQL Server statistics collection** : Optionally capture `SET STATISTICS IO` and `SET STATISTICS TIME` output to measure logical reads, CPU time, and elapsed time per iteration.
- **Buffer and cache management** : Run `CHECKPOINT` and clear data buffers (`DBCC DROPCLEANBUFFERS`) and free the procedure cache (`DBCC FREEPROCCACHE`) before running benchmarks for reproducible results.
- **Benchmark export** : Save benchmark results to plain text, CSV, or copy to the clipboard.
- **Settings persistence** : Save and load configuration (connection info, query, iterations, threads, etc.) as JSON files.
- **Cancellation support** : Cancel a running benchmark at any time, with an option to kill active queries on the server.
- **Cross-platform** : Built on Avalonia UI, targeting .NET 10.0, with Desktop support in the solution.

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

## Downloads and installation

Download the package for your OS from the [releases page](https://github.com/TruePadawan/SqlQueryStress.Avalonia/releases).

### macOS

The release ships an unsigned, un-notarized `.app` bundle inside the zip, so macOS Gatekeeper will warn that it cannot verify the developer.

1. Download the `osx-arm64` (Apple Silicon) or `osx-x64` (Intel) zip and inspect the archive before running it.
2. Extract it and move `SqlQueryStress.app` to your `Applications` folder.
3. Try to open it. If macOS blocks it, you can clear the quarantine attribute for this one app:

   ```bash
   xattr -dr com.apple.quarantine /Applications/SqlQueryStress.app
   ```

   Alternatively, right-click the app and choose **Open**, or use **System Settings → Privacy & Security → Open Anyway**.

> [!WARNING]
> Removing the quarantine attribute weakens a macOS security safeguard **for that downloaded app**. It does **not** sign or notarize the app, and it does not verify that the download is authentic. Only do this if you trust the source and have verified the download. There is no remote installer script in this repository; any command you run should be one you have read and understood.

## Requirements

**Application runtime:**

- A desktop platform listed above.
- Network access from the app to your SQL Server instance (default SQL Server port `1433`, unless configured otherwise).

**To build from source:**

- **.NET SDK 10.0** or later (the projects target `net10.0`).

**SQL Server (required to actually run a benchmark, not to build or unit-test):**

- A reachable SQL Server instance or Azure SQL Database, with credentials (or Windows Integrated Authentication / Entra ID) that can connect and execute the queries you test.
- The account used needs permission to run the query, and to run `CHECKPOINT`, `DBCC DROPCLEANBUFFERS`, and `DBCC FREEPROCCACHE` if you use the cache-management actions (these typically require elevated permissions).

## Quick start

1. **Configure the database connection** : Click **Database** and enter your server, authentication mode, and database name.
2. **Write your query** : Enter the T-SQL query in the SQL editor.
3. **Set query parameters** (optional) : Click **Parameter Substitution** to map query parameters to values returned by a data-source query.
4. **Configure the benchmark** : Set the number of iterations, threads, and optional delay between queries.
5. **Adjust options** (optional) : Open **File → Options** to toggle connection pooling, statistics collection, timeouts, and more.
6. **Run** : Click **Go** to start the benchmark and watch the real-time telemetry panel.
7. **Export results** : After the run completes, use **File → Save Benchmark** to export to text or CSV, or copy to the clipboard.

### Options

| Option | Description |
|--------|-------------|
| Enable Connection Pooling | Toggle SQL connection pooling on/off |
| Collect IO Statistics | Parse `SET STATISTICS IO` output for logical reads |
| Collect Time Statistics | Parse `SET STATISTICS TIME` output for CPU and elapsed time |
| Force Client Retrieval of Data | Ensure all result data is fetched to the client |
| Kill Queries on Cancel | Cancel active server commands when the benchmark is stopped |
| Connection Timeout | Timeout in seconds for opening connections |
| Command Timeout | Timeout in seconds for query execution |

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

The test project uses **xUnit** and **FluentAssertions**. The unit tests exercise the load engine, workers, parameter server, and export formatting with mocks and do **not** require a live SQL Server instance. See [CONTRIBUTING.md](CONTRIBUTING.md) for details on running tests and on SQL Server-dependent scenarios.

## Technologies

- **[Avalonia 12](https://avaloniaui.net)** : Cross-platform UI framework
- **[AvaloniaEdit](https://github.com/AvaloniaUI/AvaloniaEdit)** : Syntax-highlighted SQL editor
- **[CommunityToolkit.Mvvm](https://github.com/CommunityToolkit/dotnet)** : MVVM helpers (observable properties, relay commands)
- **[Microsoft.Data.SqlClient](https://github.com/dotnet/SqlClient)** : SQL Server database driver
- **[Microsoft.Extensions.DependencyInjection](https://dotnet.microsoft.com)** : Dependency injection
- **xUnit + FluentAssertions** : Testing framework

## Contributing

Contributions are welcome. See [CONTRIBUTING.md](CONTRIBUTING.md) for prerequisites, build/test commands, and how to report bugs and propose changes.

## License

This project is licensed under the MIT License, matching the original [ErikEJ/SqlQueryStress](https://github.com/ErikEJ/SqlQueryStress) repository. See [LICENSE](LICENSE).

## Acknowledgments

- Original SqlQueryStress by [Adam Machanic](http://dataeducation.com/sqlquerystress-the-source-code/)
- [ErikEJ/SqlQueryStress](https://github.com/ErikEJ/SqlQueryStress) GitHub host and MIT license
- [Avalonia UI](https://avaloniaui.net) for the cross-platform framework
