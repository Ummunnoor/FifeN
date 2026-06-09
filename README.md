# FifeN Solution

## Overview

FifeN is a multi-project .NET 9 solution that follows a layered architecture for a backend application. The main components are:

- `API/` - ASP.NET Core Web API project
- `Application/` - application logic, DTOs, validators, and service interfaces
- `Domain/` - domain entities, enums, and domain-level interfaces
- `Persistence/` - database context, EF Core persistence, repositories, and infrastructure services

The solution uses centralized package management with `Directory.Packages.props` and shared build settings in `Directory.Build.props`.

## Projects

- `API/API.csproj`
- `Application/Application.csproj`
- `Domain/Domain.csproj`
- `Persistence/Persistence.csproj`

## Build Requirements

- .NET SDK 9.0
- Visual Studio 2022 / 2023 or another editor that supports SDK-style .NET projects

## Build and Run

From the solution root:

```bash
dotnet build FifeN.sln
dotnet run --project API/API.csproj
```

If you are using Visual Studio, open `FifeN.sln` and build the solution normally.

## Configuration

The API reads configuration from `appsettings.json`, `appsettings.{Environment}.json`, user-secrets
(Development only), and environment variables (highest precedence). **Secrets are never committed** —
the keys below are blank in `appsettings.json` and must be supplied via user-secrets locally or
environment variables in deployed environments.

### Required keys

| Key | Secret? | Description |
|---|:---:|---|
| `ConnectionStrings:DefaultConnection` | 🔑 | PostgreSQL connection string (host, database, credentials). |
| `Jwt:Key` | 🔑 | Symmetric signing key for JWT access tokens (use ≥ 32 random bytes). |
| `Jwt:Issuer` | | Token issuer, e.g. `FifeN.API`. |
| `Jwt:Audience` | | Token audience, e.g. `FifeN.Client`. |
| `Jwt:ExpiryMinutes` | | Access-token lifetime in minutes (e.g. `30`). |
| `Cloudinary:CloudName` | | Cloudinary account cloud name (not secret). |
| `Cloudinary:ApiKey` | 🔑 | Cloudinary API key. Image storage uses the real Cloudinary adapter when all three Cloudinary values are set, otherwise a local dev stub. |
| `Cloudinary:ApiSecret` | 🔑 | Cloudinary API secret. |

> `Resend:API Key` exists in `appsettings.json` but is **not used by the current MVP** — the legacy
> email-confirmation flow it powered is parked (excluded from compilation); authentication is phone +
> OTP. OTP delivery currently uses a logging dev stub (`LoggingOtpSender`); no SMS-provider key is
> required to run.

### Setting secrets locally (user-secrets)

User-secrets are loaded automatically in the Development environment only.

```bash
cd API
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5432;Database=FifeN;Username=postgres;Password=<password>"
dotnet user-secrets set "Jwt:Key" "<random-key>"
dotnet user-secrets set "Cloudinary:CloudName" "<cloud-name>"
dotnet user-secrets set "Cloudinary:ApiKey" "<api-key>"
dotnet user-secrets set "Cloudinary:ApiSecret" "<api-secret>"
```

### Setting secrets in deployed environments (environment variables)

User-secrets do **not** load outside Development; supply secrets as environment variables instead
(use `__` as the nesting separator). The app fails fast if `Jwt:Key` or the connection string is empty.

```bash
export ConnectionStrings__DefaultConnection="Host=...;Database=FifeN;Username=...;Password=..."
export Jwt__Key="<random-key>"
export Cloudinary__ApiKey="<api-key>"
export Cloudinary__ApiSecret="<api-secret>"
```

## Key Conventions

- `Directory.Build.props` sets central build properties, including nullable reference types, implicit usings, and package version management.
- `Directory.Packages.props` centralizes NuGet package versions for the entire solution.
- Root `.editorconfig` governs C# formatting and analyzer settings.

## Helpful References

- `ARCHITECTURE_AND_IMPROVEMENTS.md`
- `PRODUCTION_READY_SUMMARY.md`

## Notes

This solution is organized to keep API, application logic, domain model, and persistence concerns separated for maintainability, testability, and clean dependency flow.
