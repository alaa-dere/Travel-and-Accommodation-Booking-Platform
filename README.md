# Hotel Booking System

An ASP.NET Core Web API for hotel discovery and booking. The solution includes
authentication, hotel and room administration, search, promotions, carts,
checkout, invoices, reviews, nearby attractions, and customer recommendations.

## Prerequisites

Install the following tools before setting up the project:

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0). The repository
  uses SDK `8.0.0` and allows newer .NET 8 feature-band versions.
- [SQL Server](https://www.microsoft.com/sql-server/sql-server-downloads), such
  as SQL Server Developer or Express, for the application database.
- Git.
- Optional: JetBrains Rider, Visual Studio 2022, or Visual Studio Code.
- Optional: the EF Core CLI tool if it is not already installed.

Verify the required tools:

```powershell
dotnet --version
git --version
```

Install the EF Core CLI tool when needed:

```powershell
dotnet tool install --global dotnet-ef --version 8.*
```

If it is already installed, it can be updated with:

```powershell
dotnet tool update --global dotnet-ef --version 8.*
```

## Project setup

Clone the repository and enter its directory:

```powershell
git clone <repository-url>
cd HotelBooking
```

Restore the NuGet packages:

```powershell
dotnet restore HotelBooking.sln
```

The solution is organized into these projects:

- `HotelBooking.API`: HTTP controllers, application startup, authentication,
  Swagger, and dependency registration.
- `HotelBooking.Application`: application services, DTOs, interfaces, and
  business workflows.
- `HotelBooking.Domain`: domain entities and enums.
- `HotelBooking.Infrastructure`: Entity Framework Core, SQL Server repositories,
  migrations, security, email, PDF generation, and admin seeding.
- `HotelBooking.UnitTests`: unit tests.
- `HotelBooking.IntegrationTests`: end-to-end API tests using an in-memory SQLite
  database.

## Configuration

The default non-secret settings are in
`HotelBooking.API/appsettings.json`. Do not commit real passwords, JWT keys, SMTP
credentials, or production connection strings to that file.

For local development, configure secrets from the repository root using .NET
User Secrets:

```powershell
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=localhost\MSSQLSERVER01;Database=HotelBookingDb;Trusted_Connection=True;TrustServerCertificate=True" --project HotelBooking.API
dotnet user-secrets set "Jwt:Key" "replace-with-a-long-random-key-of-at-least-32-characters" --project HotelBooking.API
dotnet user-secrets set "Admin:Password" "replace-with-a-strong-admin-password" --project HotelBooking.API
```

Change the SQL Server instance in the connection string when your local instance
has a different name. For example, SQL Server Express commonly uses
`localhost\SQLEXPRESS`.

The application uses the following configuration sections:

| Setting | Required | Purpose |
| --- | --- | --- |
| `ConnectionStrings:DefaultConnection` | Yes | SQL Server connection used by Entity Framework Core. |
| `Jwt:Key` | Yes | Secret signing key for authentication tokens. Use a long, random value. |
| `Jwt:Issuer` | Yes | Expected JWT issuer. Defaults to `HotelBooking`. |
| `Jwt:Audience` | Yes | Expected JWT audience. Defaults to `HotelBookingUsers`. |
| `Jwt:ExpirationMinutes` | Yes | Token lifetime in minutes. Defaults to `60`. |
| `Admin:FirstName` | Yes | Initial administrator's first name. |
| `Admin:LastName` | Yes | Initial administrator's last name. |
| `Admin:UserName` | Yes | Initial administrator's login username. |
| `Admin:Email` | Yes | Initial administrator's email address. |
| `Admin:Password` | Yes | Initial administrator's password; store it as a secret. |
| `Email:Host` | For email delivery | SMTP server hostname. |
| `Email:Port` | For email delivery | SMTP port; defaults to `587`. |
| `Email:SenderEmail` | For email delivery | Address shown as the sender. |
| `Email:SenderName` | For email delivery | Sender display name. |
| `Email:Username` | For email delivery | SMTP username. |
| `Email:Password` | For email delivery | SMTP password; store it as a secret. |
| `Email:EnableSsl` | For email delivery | Enables TLS/SSL for SMTP. |

To use booking-confirmation email locally, configure the SMTP values as secrets:

```powershell
dotnet user-secrets set "Email:Host" "smtp.example.com" --project HotelBooking.API
dotnet user-secrets set "Email:Port" "587" --project HotelBooking.API
dotnet user-secrets set "Email:SenderEmail" "bookings@example.com" --project HotelBooking.API
dotnet user-secrets set "Email:SenderName" "Hotel Booking" --project HotelBooking.API
dotnet user-secrets set "Email:Username" "smtp-user" --project HotelBooking.API
dotnet user-secrets set "Email:Password" "smtp-password" --project HotelBooking.API
dotnet user-secrets set "Email:EnableSsl" "true" --project HotelBooking.API
```

User Secrets are intended for local development only. In deployed environments,
provide the same values through a secure configuration provider or environment
variables. ASP.NET Core environment-variable names use double underscores, for
example `Jwt__Key` and `ConnectionStrings__DefaultConnection`.

## Database setup

1. Start SQL Server and confirm that the configured account can create and access
   databases.
2. Configure `ConnectionStrings:DefaultConnection` as described above.
3. Apply the committed Entity Framework Core migrations:

```powershell
dotnet ef database update --project HotelBooking.Infrastructure --startup-project HotelBooking.API
```

This creates or updates the `HotelBookingDb` database. The API does not apply
migrations automatically, so this command must succeed before the first run.

When the application starts outside the `Testing` environment, it creates the
initial administrator if no administrator exists. The values come from the
`Admin` configuration section. Seeding is safe to run repeatedly because it
does nothing after an administrator has been created.

## Build

Build the entire solution:

```powershell
dotnet build HotelBooking.sln
```

For a release build:

```powershell
dotnet build HotelBooking.sln --configuration Release
```

## Run the API

Run the HTTPS development profile:

```powershell
dotnet run --project HotelBooking.API --launch-profile https
```

The development endpoints are:

- API: `https://localhost:7288` or `http://localhost:5270`
- Swagger UI: `https://localhost:7288/swagger`

Swagger is enabled only when `ASPNETCORE_ENVIRONMENT` is `Development`. Use the
authentication endpoints to obtain a JWT, then select **Authorize** in Swagger
and enter the token. The Swagger security scheme adds the `Bearer` prefix.

If the local HTTPS certificate is not trusted, run:

```powershell
dotnet dev-certs https --trust
```

Stop the API with `Ctrl+C`.

## Run tests

Run all unit and integration tests:

```powershell
dotnet test HotelBooking.sln
```

Run either test project separately:

```powershell
dotnet test HotelBooking.UnitTests\HotelBooking.UnitTests.csproj
dotnet test HotelBooking.IntegrationTests\HotelBooking.IntegrationTests.csproj
```

Integration tests use their own in-memory SQLite database. They do not require
the local SQL Server database or SMTP credentials.

To collect code coverage:

```powershell
dotnet test HotelBooking.sln --collect:"XPlat Code Coverage"
```

Coverage files are written below each test project's `TestResults` directory.

## Common setup problems

- **`JWT Key is missing`**: configure `Jwt:Key` with User Secrets or an
  environment variable.
- **`Admin bootstrap configuration is missing`**: configure `Admin:Password` and
  ensure the other `Admin` values are present.
- **SQL Server connection failure**: verify the server/instance name, confirm SQL
  Server is running, and update `ConnectionStrings:DefaultConnection`.
- **Database-table errors on startup**: run `dotnet ef database update` before
  starting the API.
- **HTTPS certificate warning**: run `dotnet dev-certs https --trust`, or use the
  HTTP endpoint for local development.
