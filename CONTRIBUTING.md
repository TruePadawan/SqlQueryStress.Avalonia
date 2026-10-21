# Contributing to SqlQueryStress

Thanks for your interest in improving SqlQueryStress. This guide covers the practical steps to build, test, and propose changes. It is intentionally short.

By contributing you agree that your contributions are licensed under the [MIT License](LICENSE).

## Prerequisites

- **.NET SDK 10.0** or later. The projects target `net10.0`.
  - Check with `dotnet --list-sdks`.
- **SQL Server** is *not* required to build the solution or run the unit tests. It is only needed if you want to manually exercise the app against a real server (see [Testing against SQL Server](#testing-against-sql-server)).

## Get the code and build

```bash
git clone https://github.com/TruePadawan/SqlQueryStress.Avalonia.git
cd SqlQueryStress.Avalonia

dotnet restore
dotnet build
```

Solution layout:

- `SqlQueryStress` : core library (Avalonia UI, view models, services, load engine).
- `SqlQueryStress.Desktop` : desktop entry point.
- `SqlQueryStress.UnitTests` : xUnit + FluentAssertions unit tests.

The solution file is `SqlQueryStress.slnx`.

## Run the app

```bash
dotnet run --project SqlQueryStress.Desktop
```

## Tests

```bash
dotnet test
```

The unit tests run against mocks and **do not require a live SQL Server instance**. They cover the load engine, query workers, the parameter server, and export formatting. Please keep the suite green and add tests for behavior you change.

You can run a single project or filter:

```bash
dotnet test SqlQueryStress.UnitTests
dotnet test --filter FullyQualifiedName~LoadEngineTests
```

### Testing against SQL Server

There is currently no automated integration test suite in the repository; the app-level behavior against a real server is exercised manually. If you add or run manual tests, you will need:

- A reachable SQL Server instance or Azure SQL Database (local instance, container, or remote server).
- Credentials for the authentication mode you use (Windows Integrated, SQL Server Authentication, or Entra ID Universal with MFA).
- Permissions to run your test query and, for the cache-management actions, `CHECKPOINT`, `DBCC DROPCLEANBUFFERS`, and `DBCC FREEPROCCACHE`.

If you introduce automated tests that require SQL Server, keep them **separate** from the mock-based unit tests and document how to provision the server (for example, with a service container in CI) so that a clean checkout can still run the fast unit tests without it.

## Reporting bugs and proposing changes

- **Bug reports and feature requests:** open a GitHub issue at <https://github.com/TruePadawan/SqlQueryStress.Avalonia/issues>. Include the app version, OS, SQL Server version/provider, authentication mode, and steps to reproduce. **Do not paste real credentials, connection strings, or customer data.**
- **Pull requests:** fork the repository, create a topic branch, and open a PR against `master`. Describe what changed and why, and note anything you could not verify.

## PR scope and quality checks

- Keep changes focused and reviewable. Avoid unrelated refactors.
- **Do not replace the dedicated-thread worker model** in the load engine with `Task.Run`, async SQL execution, or a broad concurrency redesign; that architecture is intentional.
- Before opening a PR, make sure the following pass locally:

  ```bash
  dotnet build
  dotnet test
  ```

- Report honestly anything you could not run (for example, platform-specific manual checks).
