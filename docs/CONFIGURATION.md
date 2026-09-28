# Environment Configuration

This document describes the configuration required by the Hotel Booking System.
All values shown here are names or placeholders. It intentionally contains no
real passwords, authentication keys, SMTP credentials, or production connection
strings.

## Configuration sources and precedence

The API uses the standard ASP.NET Core configuration system. Later providers
override earlier providers in this order:

1. `HotelBooking.API/appsettings.json`
2. `HotelBooking.API/appsettings.{Environment}.json`
3. .NET User Secrets when the environment is `Development`
4. Environment variables
5. Command-line arguments

Keep safe defaults such as logging levels and token lifetime in source-controlled
JSON. Store confidential or environment-specific values in:

- **Local development:** .NET User Secrets.
- **CI/CD:** protected pipeline secrets exposed only to the application process.
- **Staging/production:** a managed secret store or protected environment
  variables supplied by the hosting platform.

Never commit populated secret files, `.env` files, production connection
strings, passwords, or private keys. `.env`, User Secrets, build artifacts, and
test-result directories are excluded from this repository where applicable.

## Required settings

| Configuration key | Environment variable | Required | Description |
| --- | --- | --- | --- |
| `ConnectionStrings:DefaultConnection` | `ConnectionStrings__DefaultConnection` | Yes | SQL Server connection string used by Entity Framework Core. |
| `Jwt:Key` | `Jwt__Key` | Yes | Confidential symmetric JWT signing key. Use at least 32 random bytes. |
| `Jwt:Issuer` | `Jwt__Issuer` | Yes | Issuer written to and accepted from tokens. |
| `Jwt:Audience` | `Jwt__Audience` | Yes | Audience written to and accepted from tokens. |
| `Jwt:ExpirationMinutes` | `Jwt__ExpirationMinutes` | Yes | Positive token lifetime in minutes. |
| `Admin:FirstName` | `Admin__FirstName` | On first startup | Initial administrator's first name. |
| `Admin:LastName` | `Admin__LastName` | On first startup | Initial administrator's last name. |
| `Admin:UserName` | `Admin__UserName` | On first startup | Initial administrator's username. |
| `Admin:Email` | `Admin__Email` | On first startup | Initial administrator's email. |
| `Admin:Password` | `Admin__Password` | On first startup | Confidential initial administrator password. |
| `Email:Host` | `Email__Host` | For email delivery | SMTP server hostname. |
| `Email:Port` | `Email__Port` | For email delivery | SMTP server port. |
| `Email:SenderEmail` | `Email__SenderEmail` | For email delivery | Sender email address. |
| `Email:SenderName` | `Email__SenderName` | For email delivery | Sender display name. |
| `Email:Username` | `Email__Username` | When required by SMTP | Confidential SMTP username. |
| `Email:Password` | `Email__Password` | When required by SMTP | Confidential SMTP password. |
| `Email:EnableSsl` | `Email__EnableSsl` | For email delivery | Whether the SMTP connection uses TLS/SSL. |
| `RabbitMq:Host` | `RabbitMq__Host` | Yes | RabbitMQ hostname for asynchronous checkout tasks. |
| `RabbitMq:Username` | `RabbitMq__Username` | Yes | RabbitMQ application username. |
| `RabbitMq:Password` | `RabbitMq__Password` | Yes | Confidential RabbitMQ password. |
| `AllowedHosts` | `AllowedHosts` | Hosting-dependent | Host-header allowlist. Do not leave unrestricted in production without review. |
| `Logging:LogLevel:Default` | `Logging__LogLevel__Default` | No | Default application logging level. |
| `Logging:LogLevel:Microsoft.AspNetCore` | `Logging__LogLevel__Microsoft.AspNetCore` | No | ASP.NET Core logging level. |

`Jwt:Key` is checked during application startup. The API will not start when it
is missing or blank. The admin settings are required until the initial admin is
created; startup seeding fails if any of them are absent. SMTP settings are
required for booking-confirmation delivery.

## Database configuration

The application uses Microsoft SQL Server in normal runtime environments.
`DefaultConnection` must identify:

- The SQL Server host and optional named instance or port.
- The Hotel Booking database name.
- Either integrated authentication or a database identity supplied securely.
- Encryption and certificate options appropriate for the environment.

Safe local-development template using Windows integrated authentication:

```text
Server=<local-server-or-instance>;Database=<local-database>;Trusted_Connection=True;TrustServerCertificate=True
```

Production considerations:

- Do not store a database password in `appsettings.json` or this documentation.
- Obtain credentials from the hosting platform's secret store.
- Use a least-privilege database identity.
- Require encrypted connections and use a trusted server certificate.
- Do not use `TrustServerCertificate=True` unless the environment explicitly
  requires and accepts that trade-off.
- Restrict network access to the application and database hosts that need it.

The API does not apply migrations automatically. After configuring the
connection, apply committed migrations explicitly:

```powershell
dotnet ef database update `
  --project HotelBooking.Infrastructure `
  --startup-project HotelBooking.API
```

Back up production data before applying migrations and run migrations through
the deployment process with an appropriately authorized identity.

Integration tests do not use `DefaultConnection`; their application factory
replaces SQL Server with a separate in-memory SQLite database.

## Authentication configuration

JWT bearer authentication validates all of the following:

- Issuer equals `Jwt:Issuer`.
- Audience equals `Jwt:Audience`.
- Signature uses `Jwt:Key`.
- Token lifetime is valid; clock skew is zero.

Use distinct signing keys for development, staging, and production. The key
must be random, long, confidential, and rotated according to the deployment's
security policy. Changing it immediately invalidates tokens signed with the old
key because the application currently accepts one symmetric signing key.

Safe local setup using placeholders:

```powershell
dotnet user-secrets set "Jwt:Key" "<generated-local-signing-key>" --project HotelBooking.API
dotnet user-secrets set "Jwt:Issuer" "HotelBooking.Local" --project HotelBooking.API
dotnet user-secrets set "Jwt:Audience" "HotelBooking.LocalUsers" --project HotelBooking.API
dotnet user-secrets set "Jwt:ExpirationMinutes" "60" --project HotelBooking.API
```

Replace `<generated-local-signing-key>` locally; never place the generated value
in documentation, source control, screenshots, issue trackers, or logs.

## Initial administrator configuration

Outside the `Testing` environment, startup checks whether an administrator
already exists. If none exists, one is created from the `Admin` section.

Safe local setup:

```powershell
dotnet user-secrets set "Admin:FirstName" "<local-admin-first-name>" --project HotelBooking.API
dotnet user-secrets set "Admin:LastName" "<local-admin-last-name>" --project HotelBooking.API
dotnet user-secrets set "Admin:UserName" "<local-admin-username>" --project HotelBooking.API
dotnet user-secrets set "Admin:Email" "<local-admin-email>" --project HotelBooking.API
dotnet user-secrets set "Admin:Password" "<strong-local-admin-password>" --project HotelBooking.API
```

The password is hashed before storage. Nevertheless, the bootstrap password
itself must remain secret. After production bootstrap, store and rotate it using
the organization's account-management process.

## Email configuration

Booking checkout uses SMTP for confirmation email. Configure the values required
by the selected provider without committing them:

```powershell
dotnet user-secrets set "Email:Host" "<smtp-host>" --project HotelBooking.API
dotnet user-secrets set "Email:Port" "<smtp-port>" --project HotelBooking.API
dotnet user-secrets set "Email:SenderEmail" "<sender-address>" --project HotelBooking.API
dotnet user-secrets set "Email:SenderName" "Hotel Booking" --project HotelBooking.API
dotnet user-secrets set "Email:Username" "<smtp-username>" --project HotelBooking.API
dotnet user-secrets set "Email:Password" "<smtp-password>" --project HotelBooking.API
dotnet user-secrets set "Email:EnableSsl" "true" --project HotelBooking.API
```

Use a provider-specific application password or restricted SMTP credential, not
a personal mailbox password. Avoid logging SMTP credentials or full email
contents containing customer information.

Integration tests replace the SMTP sender with an in-memory test implementation
and therefore require no external mail server.

## RabbitMQ configuration

Booking-confirmation emails and failed-refund retries use RabbitMQ through
MassTransit. Docker Compose starts RabbitMQ on port `5672` and exposes its
management UI on port `15672`.

For Docker, define `RABBITMQ_PASSWORD` locally. `RABBITMQ_USER` is optional and
defaults to `hotelbooking`. Never commit broker credentials. When running the
API outside Docker, RabbitMQ must be reachable through the `RabbitMq` settings.
The Testing environment does not require an external broker.

## Stripe payment configuration

Checkout uses Stripe Payment Intents. The `Stripe.net` NuGet dependency is
already referenced by `HotelBooking.Infrastructure`; `dotnet restore` installs
it with the other project dependencies.

Create a Stripe test account and store its test credentials with User Secrets:

```powershell
dotnet user-secrets set "Stripe:SecretKey" "sk_test_..." --project HotelBooking.API
dotnet user-secrets set "Stripe:PublishableKey" "pk_test_..." --project HotelBooking.API
dotnet user-secrets set "Stripe:WebhookSecret" "whsec_..." --project HotelBooking.API
dotnet user-secrets set "Stripe:Currency" "usd" --project HotelBooking.API
```

Install the Stripe CLI separately for local webhook forwarding, authenticate,
and run:

```powershell
stripe login
stripe listen --forward-to https://localhost:<api-port>/api/payments/webhooks/stripe
```

Copy the `whsec_...` value printed by `stripe listen` into
`Stripe:WebhookSecret`. Never commit any Stripe secret key or webhook secret.
The frontend must use Stripe.js/Elements with the publishable key so card data
never passes through this API.

## Environment-specific behavior

Select the runtime environment with `ASPNETCORE_ENVIRONMENT`. Common values are
`Development`, `Staging`, `Production`, and `Testing`.

### Development

- Launch profiles set `ASPNETCORE_ENVIRONMENT=Development`.
- `appsettings.Development.json` overrides base settings.
- User Secrets are loaded automatically.
- Swagger and Swagger UI are enabled.
- The initial administrator is seeded when none exists.
- SQL Server is used and migrations must be applied manually.

Run locally with:

```powershell
dotnet run --project HotelBooking.API --launch-profile https
```

### Testing

- Integration tests set the environment to `Testing` internally.
- SQL Server is replaced with in-memory SQLite.
- SMTP is replaced by a test sender.
- Test-only JWT settings are injected by the test host.
- Initial administrator seeding is disabled.

Do not use the `Testing` environment to host a real deployment.

### Staging and production

- Swagger is disabled by the current startup pipeline.
- User Secrets are not a deployment secret store.
- Supply sensitive settings through the hosting platform or managed secret
  provider.
- Apply database migrations as an explicit deployment step.
- The initial administrator is seeded if one does not exist, so bootstrap values
  must be available securely on first startup.
- Terminate HTTPS correctly at the application or trusted reverse proxy.
- Restrict `AllowedHosts`, database access, log access, and SMTP credentials.

Example environment-variable names, with intentionally omitted values:

```text
ASPNETCORE_ENVIRONMENT=Production
ConnectionStrings__DefaultConnection=<provided-by-secret-store>
Jwt__Key=<provided-by-secret-store>
Jwt__Issuer=<production-issuer>
Jwt__Audience=<production-audience>
Jwt__ExpirationMinutes=<positive-number>
Admin__Password=<provided-by-secret-store>
Email__Password=<provided-by-secret-store>
```

## Verifying configuration safely

1. Confirm that required key names exist without printing their values.
2. Apply migrations against the intended database.
3. Start the API and verify that startup completes.
4. In development, open Swagger and test registration/login.
5. Verify that an authenticated request accepts a newly issued token.
6. Exercise email only with a designated non-production recipient.
7. Check logs for failures, but never log tokens, passwords, connection strings,
   or SMTP credentials.

Useful local verification command:

```powershell
dotnet build HotelBooking.sln
```

Avoid capturing the output of commands that enumerate User Secrets because they
display configured values. Never paste that output into logs, documentation, or
support channels.

## Security checklist

- [ ] No real secret appears in a committed JSON, Markdown, `.http`, or source file.
- [ ] Development, staging, and production use different JWT keys.
- [ ] Production database traffic is encrypted and uses least privilege.
- [ ] Admin and SMTP passwords come from a protected secret provider.
- [ ] Secret values are not written to application or CI logs.
- [ ] Access to deployment secrets and backups is restricted and audited.
- [ ] Compromised credentials can be rotated without a source-code change.
