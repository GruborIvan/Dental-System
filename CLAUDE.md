# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build & Run

```bash
# Build
dotnet build Ordinacija.sln

# Run
dotnet run --project Ordinacija/Ordinacija.csproj
```

No test projects exist in this solution.

## Architecture

Feature-based WPF MVVM app targeting .NET 8. Each feature under `Ordinacija/Features/` follows a strict layered pattern:

```
View.xaml → ViewModel → IService/Service → IRepository/Repository → Dapper SQL
```

Features: `Patients`, `Doctors`, `MedicalReports`, `MedicalRecords`, `Certificates`, `Login`, `ReportPrint`.

DI is wired in `App.xaml.cs` using `Microsoft.Extensions.Hosting`. Services and repositories are `Singleton`; `ISchemaRepository`, `ILoginRepository`, and ViewModels are `Transient`.

`App` exposes static `IMapper Mapper` and `IConfiguration Configuration` properties used throughout.

## Database

Connects to a Pantheon SQL Server database. The active connection string key is `PantheonDB` (requires `TrustServerCertificate=True`).

**Credentials are not committed.** The tracked `appsettings.json` only holds placeholders. The real connection strings live in `Ordinacija/appsettings.Local.json` (gitignored), which is layered on top of `appsettings.json` in `App.xaml.cs` and copied to the output/publish folder when it exists. On a fresh clone, create that file with the same `ConnectionStrings` structure. Never put a real password in `appsettings.json`.

All data access uses **Dapper** with raw SQL — EF Core is configured but not used for queries.

- Patient/Doctor records live in `THE_SetSubj` / `tHE_SetSubj` (read vs. write table names differ by case).
- `AcSupplier = 'F'` identifies patient records; doctors use a different value.
- `AcName2` stores `"FirstName LastName"` as one string — split by `FirstNameResolver` / `LastNameResolver` in `Extensions/Resolvers/`.
- New record IDs are generated via `MAX(id) + 1` inside a transaction.

## Domain Mapping

`Extensions/MappingProfile.cs` contains all AutoMapper mappings between `*Dbo` (Pantheon schema) and domain models. Custom resolvers handle non-standard field splitting. Field names follow Pantheon conventions: `AcFieldSA`–`AcFieldSI`, `AdBirthDate`, etc.

## PDF Generation

`ReportPrint` feature fetches HTML templates from the DB via `SchemaRepository.GetTemplateSchemaByKey(schemaKey)`, renders them using iText7, and writes PDFs to the app's base directory. On startup, PDFs older than 7 days are auto-deleted.

## Key Patterns

- `BaseViewModel` (`Features/Common/BaseViewModel.cs`) — implements `INotifyPropertyChanged` with `OnPropertyChanged()` and `SetProperty<T>()`.
- `RelayCommand` (`Helpers/RelayCommand.cs`) — generic `ICommand` supporting async and `CanExecute`.
- Repository write operations always use a `SqlTransaction` with explicit commit/rollback.
