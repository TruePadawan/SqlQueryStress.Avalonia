# SqlQueryStress

**SqlQueryStress** is a cross-platform SQL Server benchmarking tool that lets you stress-test T-SQL queries with configurable concurrency, iterations, and parameter substitution. It is an [Avalonia](https://avaloniaui.net) port of the original Windows-only SqlQueryStress application, built as part of the [Avalonia Port Challenge](https://avaloniaui.net/blog/avalonia-port-challenge).

<img width="1894" height="1025" alt="image" src="https://github.com/user-attachments/assets/e86cfffc-31ba-49d6-98f6-729bf8f00e9f" />
<img width="1894" height="1025" alt="image" src="https://github.com/user-attachments/assets/bffe0b5d-b3ef-4571-9b68-e2260af4c151" />

## Features

- **Multi-threaded query execution** : Run the same query across multiple threads with a configurable number of iterations per thread.
- **Real-time telemetry** : Monitor elapsed time, iterations completed, average client/CPU/actual seconds per iteration, logical reads, active threads, and exception counts as the benchmark runs.
- **Parameter substitution** : Bind query parameters to values fetched from a separate parameter query, enabling realistic data-driven load testing.
- **SQL Server statistics collection** : Optionally capture `SET STATISTICS IO` and `SET STATISTICS TIME` output to measure logical reads, CPU time, and elapsed time per iteration.
- **Buffer and cache management** : Clear data buffers (`DBCC DROPCLEANBUFFERS`) and free the procedure cache (`DBCC FREEPROCCACHE`) before running benchmarks for reproducible results.
- **Benchmark export** : Save benchmark results to plain text, CSV, or copy to the clipboard.
- **Settings persistence** : Save and load configuration (connection info, query, iterations, threads, etc.) as JSON files.
- **Cancellation support** : Cancel a running benchmark at any time, with an option to kill active queries on the server.
- **Cross-platform** : Built on Avalonia UI, targeting .NET 10.0, with Windows and Linux support.

## Database Support

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

## Requirements

- **.NET 10.0 SDK** or later
- **SQL Server** instance (local, remote, or Azure SQL Database)

## Building

```bash
# Restore dependencies
dotnet restore

# Build the solution
dotnet build

# Run the desktop application
dotnet run --project SqlQueryStress.Desktop
```

## Running Tests

```bash
dotnet test
```

The test project uses **xUnit** and **FluentAssertions**.

## Technologies

- **[Avalonia 12](https://avaloniaui.net)** : Cross-platform UI framework
- **[AvaloniaEdit](https://github.com/AvaloniaUI/AvaloniaEdit)** : Syntax-highlighted SQL editor
- **[CommunityToolkit.Mvvm](https://github.com/CommunityToolkit/dotnet)** : MVVM helpers (observable properties, relay commands)
- **[Microsoft.Data.SqlClient](https://github.com/dotnet/SqlClient)** : SQL Server database driver
- **[Microsoft.Extensions.DependencyInjection](https://dotnet.microsoft.com)** : Dependency injection
- **xUnit + FluentAssertions** : Testing framework

## Usage

1. **Configure the database connection** : Click the **Database** button and enter your server, authentication mode, and database name.
2. **Write your query** : Enter the T-SQL query in the SQL editor on the left.
3. **Set query parameters** (optional) : Click **Parameter Substitution** to map query parameters to a data source query.
4. **Configure the benchmark** : Set the number of iterations, threads, and optional delay between queries.
5. **Adjust options** (optional) : Open **File → Options** to toggle connection pooling, statistics collection, timeouts, and more.
6. **Run** : Click **Go** to start the benchmark. Watch the real-time telemetry panel on the right.
7. **Export results** : After the run completes, use **File → Save Benchmark** to export to text, CSV, or clipboard.

## Options

| Option | Description |
|--------|-------------|
| Enable Connection Pooling | Toggle SQL connection pooling on/off |
| Collect IO Statistics | Parse `SET STATISTICS IO` output for logical reads |
| Collect Time Statistics | Parse `SET STATISTICS TIME` output for CPU and elapsed time |
| Force Client Retrieval of Data | Ensure all result data is fetched to the client |
| Kill Queries on Cancel | Cancel active server queries when the benchmark is stopped |
| Connection Timeout | Timeout in seconds for opening connections |
| Command Timeout | Timeout in seconds for query execution |

## License

This project is licensed under the MIT License, matching the original [ErikEJ/SqlQueryStress](https://github.com/ErikEJ/SqlQueryStress) repository.

## Acknowledgments

- Original SqlQueryStress by [Adam Machanic](http://dataeducation.com/sqlquerystress-the-source-code/)
- [ErikEJ/SqlQueryStress](https://github.com/ErikEJ/SqlQueryStress) GitHub host and MIT license
- [Avalonia UI](https://avaloniaui.net) for the cross-platform framework
