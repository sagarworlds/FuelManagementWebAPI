# Fuel Management Web API

[![.NET Framework](https://img.shields.io/badge/.NET%20Framework-4.8-512BD4.svg)](https://dotnet.microsoft.com/download/dotnet-framework/net48)
[![ASP.NET Web API](https://img.shields.io/badge/ASP.NET%20Web%20API-5.2.9-512BD4.svg)](https://learn.microsoft.com/aspnet/web-api/)
[![SQLite](https://img.shields.io/badge/SQLite-1.0.119-003B57.svg?logo=sqlite)](https://system.data.sqlite.org/)
[![Tests](https://img.shields.io/badge/tests-NUnit%203-2E7D32.svg)](https://nunit.org/)

The REST API behind **Fuel Management**, an application for logging vehicle fill-ups and tracking fuel spend, consumption and mileage. It handles user accounts and token-based authentication, and stores each user's fuel log in a local SQLite database.

The browser client lives in a separate repository: [FuelManagementAngular](https://github.com/sagarworlds/FuelManagementAngular).

## Contents

- [Features](#features)
- [Technology](#technology)
- [Architecture](#architecture)
- [Getting started](#getting-started)
- [Configuration](#configuration)
- [API reference](#api-reference)
- [Security](#security)
- [Testing](#testing)
- [Deployment](#deployment)
- [Upgrading from earlier versions](#upgrading-from-earlier-versions)
- [Project structure](#project-structure)
- [Known limitations](#known-limitations)

## Features

- **Accounts:** registration, sign-in and password changes.
- **Token authentication:** sign-in returns a signed JSON Web Token (JWT). Changing a password immediately signs out every other session.
- **Private data:** each user reads and writes only their own fuel entries. The user is always taken from the token, never from the request.
- **Validated input:** invalid values are rejected with `400 Bad Request` and a message for each field.
- **Consistent time handling:** all dates are stored and returned in UTC.
- **Zero-setup database:** the SQLite database and its tables are created on first start.

## Technology

| Area | Choice |
| --- | --- |
| Runtime | .NET Framework 4.8 |
| Web framework | ASP.NET Web API 2 (5.2.9), ASP.NET MVC 5.2.9 for the home and help pages |
| Database | SQLite via System.Data.SQLite 1.0.119 |
| Data access | Dapper 1.60.6 |
| Authentication | JWT bearer tokens, HMAC-SHA256 (System.IdentityModel.Tokens.Jwt 4.0) |
| Password hashing | bcrypt, cost 12, via BCrypt.Net-Next |
| Serialization | Newtonsoft.Json 13 |
| Tests | NUnit 3 |

## Architecture

Every API request passes through the same pipeline:

```text
HTTP request
  -> CrossDomainHandler        CORS: answers preflights, adds headers for allowed origins
  -> JwtAuthenticationFilter   validates the bearer token and its session stamp
  -> AuthorizeAttribute        global; rejects anonymous requests except [AllowAnonymous] actions
  -> Controller                UserController, FuelDetailController (created by CompositionRoot)
  -> ICustomerRepository       SqLiteCustomerRepository (Dapper)
  -> SQLite                    App_Data/SimpleDb.sqlite
```

- **Startup:** `Global.asax` first calls `DatabaseConfig.Initialize()`, which creates the database schema if it's missing and hashes any passwords still stored in plain text. It then calls `WebApiConfig.Register()`, which reads the configuration and builds the pipeline.
- **Dependency injection:** `CompositionRoot` creates the controllers and supplies their dependencies (repository, token service, password hasher) through their constructors. The tests use the same composition with an in-memory repository.

## Getting started

### Prerequisites

- Windows with Visual Studio 2019 or later and the **ASP.NET and web development** workload.
- .NET Framework 4.8 Developer Pack, included with Visual Studio 2019 16.3 and later.

### Set up and run

1. Clone the repository and open the solution:
   ```bash
   git clone https://github.com/sagarworlds/FuelManagementWebAPI.git
   ```
   Then open `FuelManagementWebAPI.sln` in Visual Studio.
2. Restore NuGet packages: right-click the solution, then choose **Restore NuGet Packages**.
3. Create the secrets file. Copy `WebAPI/Secrets.config.example` to `WebAPI/Secrets.config` and set `JwtSecret` to a random value of at least 32 characters. The example file contains a PowerShell command that generates one. The API won't start without it.
4. Set **WebAPI** as the startup project and press **F5**. The API runs on IIS Express at `http://localhost:<port>/api/...`.
5. Create an account, either from the Angular client's Register page or directly:
   ```bash
   curl -X POST http://localhost:<port>/api/User/Save \
        -H "Content-Type: application/json" \
        -d '{"Email":"you@example.com","Password":"choose-a-password"}'
   ```

## Configuration

Settings live in the `appSettings` section of `WebAPI/Web.config`. Secrets go in `WebAPI/Secrets.config`, which is git-ignored and merged into `appSettings` at runtime.

| Setting | File | Default | Purpose |
| --- | --- | --- | --- |
| `JwtSecret` | `Secrets.config` | *(none; required)* | Key that signs login tokens. At least 32 bytes. Anyone who has it can sign in as any user, so use a different value in each environment. |
| `JwtLifetimeMinutes` | `Web.config` | `480` | How long a login token stays valid. |
| `AllowedOrigins` | `Web.config` | `http://localhost:4200` | Comma-separated browser origins (`scheme://host[:port]`) that may call the API from another origin. `*` allows any origin. |
| `DbConnection` | `Web.config` | `/App_Data/SimpleDb.sqlite` | Location of the SQLite database, relative to the application folder. |

**CORS:** a web client served from a different origin than the API must be listed in `AllowedOrigins`, or the browser will block its requests. A client served from the same origin needs no entry. Preflight requests from unlisted origins receive `403 Forbidden`.

## API reference

Routes follow the pattern `api/{controller}/{action}`. Request and response bodies are JSON with PascalCase property names.

### Authentication

Obtain a token from `POST api/User/Login` and send it with every other request:

```http
Authorization: Bearer <token>
```

A token is rejected with `401 Unauthorized` in any of these cases:
- it has expired (see `JwtLifetimeMinutes`)
- its user has changed password since it was issued
- its user no longer exists

### Endpoints

| Method | Route | Auth | Description |
| --- | --- | --- | --- |
| `POST` | `api/User/Save` | Public | Register a new account. |
| `POST` | `api/User/Login` | Public | Sign in and receive a token. |
| `POST` | `api/User/ChangePassword` | Token | Change the password and receive a new token. |
| `GET` | `api/FuelDetail/Get` | Token | List the signed-in user's fuel entries. |
| `GET` | `api/FuelDetail/GetFuelDetailById?Id={id}` | Token | Get one of the signed-in user's entries. |
| `GET` | `api/FuelDetail/GetByUserId?UserId={id}` | Token | List entries for a user id; only the signed-in user's own id is permitted. |
| `POST` | `api/FuelDetail/Save` | Token | Record a fill-up for the signed-in user. |

#### Register: `POST api/User/Save`

```json
{ "Email": "you@example.com", "Password": "choose-a-password" }
```

- **Rules:** `Email` must be a valid address of up to 254 characters. `Password` must be 8 to 100 characters.
- **`200 OK`:** returns the new user's `Id`, `Email`, `CreatedAt` and `ModifiedAt`. The password is never returned.
- **`400 Bad Request`:** returns the validation errors.
- **`409 Conflict`:** the email is already registered, ignoring letter case.

#### Sign in: `POST api/User/Login`

```json
{ "Email": "you@example.com", "Password": "choose-a-password" }
```

**`200 OK`:**

```json
{
  "Token": "eyJ0eXAiOiJKV1QiLCJhbGciOiJIUzI1NiJ9...",
  "ExpiresAt": "2026-07-09T20:00:00Z",
  "UserId": 1,
  "Email": "you@example.com"
}
```

- Email matching ignores letter case.
- A wrong email or password returns `401 Unauthorized`, without revealing which one was wrong.

#### Change password: `POST api/User/ChangePassword`

```json
{ "CurrentPassword": "choose-a-password", "NewPassword": "a-new-password" }
```

- **`200 OK`:** returns a new token, in the same shape as sign-in. Every token issued before the change stops working, which signs out other devices.
- **`400 Bad Request`:** the current password is wrong, or the new one isn't 8 to 100 characters. This is deliberately not `401`, so clients don't mistake it for an expired session.

#### Record a fill-up: `POST api/FuelDetail/Save`

```json
{
  "MeterReading": 12450,
  "TotalPrice": 2250.00,
  "AddedFuel": 22.5,
  "Note": "Full tank",
  "CreatedAt": "2026-07-09T18:30:00Z"
}
```

- **Rules:**
  - `MeterReading` must be a positive whole number.
  - `TotalPrice` and `AddedFuel` must be greater than 0.
  - `CreatedAt` is required and may not be in the future.
  - `Note` is optional, up to 1000 characters.
- **Response:** returns the stored entry.
- **Server-set fields:** `UserId` comes from the token and `ModifiedAt` is set by the server; any values sent for them are ignored.

#### Reading entries

- `GET api/FuelDetail/Get` returns all of the signed-in user's entries.
- `GET api/FuelDetail/GetFuelDetailById?Id={id}` returns `404 Not Found` for entries belonging to other users.
- `GET api/FuelDetail/GetByUserId?UserId={id}` returns `403 Forbidden` for any id other than the signed-in user's.

### Dates

- **Stored and returned in UTC.** Responses always end in `Z`, for example `2026-07-09T18:30:00Z`.
- **Accepted input:** ISO-8601 values with an offset (`Z` or `+05:30`) are converted to UTC. A value without an offset is treated as UTC.

### Status codes

| Code | Meaning |
| --- | --- |
| `200` | Success. |
| `400` | Invalid input. For field errors the body lists them under `ModelState`; otherwise it has a `Message`. |
| `401` | Missing, invalid or revoked token, or wrong credentials at sign-in. |
| `403` | The request targets another user's data, or a CORS preflight came from an origin that isn't allowed. |
| `404` | The resource doesn't exist or belongs to another user. |
| `409` | The email address is already registered. |

## Security

- **Password storage:** bcrypt with a work factor of 12. Passwords are pre-hashed with SHA-384, so characters beyond bcrypt's 72-byte limit still count. When the email is unknown, sign-in checks against a dummy hash, so response times don't reveal which accounts exist.
- **Tokens:** HMAC-SHA256 JWTs with a fixed issuer and audience and no clock-skew allowance.
  - Each token carries a *session stamp* derived from the user's password hash. It is checked on every request, so a password change revokes all earlier tokens.
  - Malformed tokens, tampered tokens and unsigned (`alg: none`) tokens are all rejected.
- **Authorization:** all actions require a token unless explicitly marked `[AllowAnonymous]` (register and sign-in). Data access is scoped to the user identified by the token.
- **CORS:** only origins listed in `AllowedOrigins` receive CORS headers.
- **Secrets and data stay out of source control:**
  - `Secrets.config` and the SQLite database are git-ignored.
  - The publish profile excludes `App_Data`, so a deployment never overwrites the live database.
- **Fail-fast startup:** the API refuses to start if `JwtSecret` is missing or shorter than 32 bytes.

## Testing

`WebAPI.Tests` is an NUnit 3 project that runs without IIS. It covers:

- **Token service:** issuing, expiry, tampering, wrong keys, unsigned and malformed tokens.
- **Password hashing** and the migration of plain-text passwords.
- **The full Web API pipeline, hosted in memory with a fake repository:**
  - routing and CORS
  - authentication and per-user scoping
  - validation and UTC handling
  - registration, sign-in and password changes, including session revocation
- **`SqLiteCustomerRepository` against a real, temporary SQLite database.** This covers schema creation, UTC round-trips, both legacy date formats, and the user queries.

**Visual Studio:** open **Test Explorer** and choose **Run All**. The NUnit 3 test adapter is included as a package.

**Command line (Developer Command Prompt):**

```bat
nuget restore FuelManagementWebAPI.sln
msbuild FuelManagementWebAPI.sln /p:Configuration=Debug
vstest.console WebAPI.Tests\bin\Debug\WebAPI.Tests.dll /TestAdapterPath:WebAPI.Tests\bin\Debug
```

## Deployment

1. **Publish.** In Visual Studio, right-click **WebAPI**, choose **Publish** and use the **FolderProfile** profile. It publishes to `WebAPI\bin\Release\Publish` and excludes `App_Data`.
2. **Server.** The server needs Windows with IIS and .NET Framework 4.8, which ships with Windows 10 1903+ and Windows Server 2022+; install it on older versions. Create an IIS site or application, for example `/API`, with an application pool on **.NET CLR v4.0** in **Integrated** pipeline mode.
3. **Copy the published files**, but never overwrite the server's existing `App_Data` folder.
4. **Secrets and settings.** On the server, create `Secrets.config` next to `Web.config` with a production `JwtSecret`, and set `AllowedOrigins` to the address the web client is served from. If a local `Secrets.config` exists when you publish, it is copied into the output, so replace it rather than deploying your development secret.
5. **Permissions.** Give the application pool identity **Modify** permission on `App_Data`. SQLite writes the database file and a journal file next to it.
6. **First request.** On the first request the application creates the database if needed and migrates legacy passwords.

## Upgrading from earlier versions

These changes affect existing installations and clients:

- **Passwords:** plain-text passwords in an existing database are hashed automatically on first start. Users keep their passwords.
- **Tokens:** tokens issued before the upgrade don't carry a session stamp and are rejected, so every user signs in once more.
- **Sign-in response:** it now returns `{ Token, ExpiresAt, UserId, Email }` instead of the user record, and wrong credentials return `401` (previously `404`).
- **Removed endpoint:** `GET api/User/Get`, which listed all users, no longer exists.
- **Per-user data:** `GET api/FuelDetail/Get` returns only the signed-in user's entries, not every user's.
- **Database file:** it is no longer part of the repository. Keep a backup of any existing `App_Data/SimpleDb.sqlite` before pulling, because git will remove the tracked copy from your working folder.
- **Runtime and packages:** the project targets .NET Framework 4.8. Its packages were updated to their current compatible releases, and the unused Entity Framework and ASP.NET CORS packages were removed.

## Project structure

```text
FuelManagementWebAPI/
├── FuelManagementWebAPI.sln
├── WebAPI/
│   ├── App_Data/                  SQLite database (created at runtime; git-ignored)
│   ├── App_Start/
│   │   ├── CompositionRoot.cs     Creates controllers with their dependencies
│   │   ├── CrossDomainHandler.cs  CORS allowlist
│   │   ├── DatabaseConfig.cs      Schema creation and password migration at startup
│   │   └── WebApiConfig.cs        Routes, filters, JSON settings
│   ├── Auth/                      JWT service, authentication filter, session stamps, password hashing
│   ├── Controllers/               UserController, FuelDetailController
│   ├── Data/                      Repository interface, SQLite repository, schema
│   ├── Model/                     Request/response and data models, settings accessor
│   ├── Areas/HelpPage/            Generated API help pages (/Help)
│   ├── Web.config                 Application settings
│   └── Secrets.config.example     Template for the git-ignored Secrets.config
└── WebAPI.Tests/                  NUnit tests
```

## Known limitations

- **Outdated jQuery on the help pages.** The home page (`/`) and the generated help pages (`/Help`) use Bootstrap 3.4.1, but still load the jQuery 1.10 files that came with the original project template. The API itself doesn't use them; update jQuery, or remove these pages, before the site is exposed publicly.
- **Single-file database.** SQLite suits a single-server deployment with modest traffic. Several servers or heavy concurrent writes would need a server-based database.
