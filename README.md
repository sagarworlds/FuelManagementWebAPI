# Fuel Management Web API

[![.NET Framework](https://img.shields.io/badge/.NET%20Framework-4.5-blue.svg)](https://dotnet.microsoft.com/)
[![SQLite](https://img.shields.io/badge/Database-SQLite-003B57.svg?logo=sqlite)](https://sqlite.org/)
[![Dapper](https://img.shields.io/badge/ORM-Dapper-lightgrey.svg)](https://github.com/DapperLib/Dapper)

A lightweight and robust backend service designed to track fuel logs, manage users, and monitor fuel consumption/expenses. Built with **ASP.NET Web API (.NET Framework 4.5)**, **SQLite**, and **Dapper Micro-ORM**.

---

## 🚀 Features

- **User Management**: Simple registration, and login that issues a JWT bearer token.
- **Per-user data**: Every fuel endpoint requires a token and only reads or writes the signed-in user's entries.
- **Fuel Tracking**: Record details such as odometer reading (meter reading), total price, fuel added, and personalized notes.
- **Robust Storage**: Uses SQLite for localized, file-based database storage.
- **High Performance**: Employs Dapper ORM for fast and efficient SQL execution.
- **Cross-Domain Support**: Integrates CORS handling for seamless client-side interactions.

---

## 🛠️ Tech Stack

- **Framework**: ASP.NET Web API 2 (.NET Framework 4.5)
- **Database**: SQLite
- **Data Access**: Dapper (Micro-ORM)
- **Authentication**: JWT bearer tokens (HMAC-SHA256, `System.IdentityModel.Tokens.Jwt`)
- **Tests**: NUnit 3
- **Language**: C#

---

## 📁 Repository Structure

```text
FuelManagementWebAPI/
├── FuelManagementWebAPI.sln  # Visual Studio Solution file
└── WebAPI/                   # Main Web API Project
    ├── App_Data/             # Local SQLite database files (SimpleDb.sqlite)
    ├── App_Start/            # Routing, CORS, auth filters (WebApiConfig.cs) and controller wiring (CompositionRoot.cs)
    ├── Auth/                 # JWT issuing/validation and the bearer-token authentication filter
    ├── Controllers/          # API Controllers (UserController, FuelDetailController)
    ├── Data/                 # Repository layer (Dapper queries and connection helpers)
    ├── Model/                # Core Data Transfer Objects & Domain Models (User, FuelDetail)
    ├── Models/               # Utility helper models (Email, SMS)
    ├── Web.config            # Application configuration & AppSettings
    └── Secrets.config.example # Template for the git-ignored Secrets.config (JWT signing secret)
└── WebAPI.Tests/             # NUnit tests (token service and the in-memory API pipeline)
```

---

## 🔌 API Documentation

### 🔐 Authentication

Except for **Register** and **Login**, every endpoint requires a bearer token from `POST api/user/login`:

```http
Authorization: Bearer <Token>
```

Requests without a valid, unexpired token get `401 Unauthorized`. Tokens last `JwtLifetimeMinutes` (8 hours by default); after that, log in again.

### 👤 User Endpoints (`api/user`)

#### 1. Register User
* **URL**: `POST api/user/save`
* **Headers**: `Content-Type: application/json`
* **Request Body**:
  ```json
  {
    "Email": "user@example.com",
    "Password": "SecurePassword123"
  }
  ```
* **Response**: Returns the registered user details including `Id` and timestamps.

#### 2. User Login
* **URL**: `POST api/user/login`
* **Headers**: `Content-Type: application/json`
* **Request Body**:
  ```json
  {
    "Email": "user@example.com",
    "Password": "SecurePassword123"
  }
  ```
* **Response**: `200 OK` with a token on success, or `401 Unauthorized` if the email or password is wrong:
  ```json
  {
    "Token": "eyJ0eXAiOiJKV1QiLCJhbGciOiJIUzI1NiJ9...",
    "ExpiresAt": "2026-07-09T20:00:00Z",
    "UserId": 1,
    "Email": "user@example.com"
  }
  ```

#### 3. List All Users
* **URL**: `GET api/user/get` (requires a token)
* **Response**: An array of registered users.

---

### ⛽ Fuel Detail Endpoints (`api/fueldetail`)

All fuel endpoints require a token and act on the signed-in user's entries only.

#### 1. Save Fuel Log
* **URL**: `POST api/fueldetail/save`
* **Headers**: `Content-Type: application/json`
* **Request Body**:
  ```json
  {
    "UserId": 1,
    "MeterReading": 12450,
    "TotalPrice": 55.50,
    "AddedFuel": 45.2,
    "Note": "Filled tank at Shell",
    "CreatedAt": "2026-07-09T12:00:00Z"
  }
  ```
* **Response**: Returns the created fuel log entry. `UserId` is always taken from the token; a `UserId` in the body is ignored.

#### 2. Get Fuel Logs by User ID
* **URL**: `GET api/fueldetail/getbyuserid?userid={UserId}`
* **Response**: Returns the fuel logs for `userid`, which must be the signed-in user's id (`403 Forbidden` otherwise).

#### 3. Get Fuel Log by Log ID
* **URL**: `GET api/fueldetail/getfueldetailbyid?id={Id}`
* **Response**: Returns the specific fuel log entry, or `404 Not Found` if it doesn't exist or belongs to another user.

#### 4. Get My Fuel Logs
* **URL**: `GET api/fueldetail/get`
* **Response**: Returns all of the signed-in user's fuel logs.

---

## ⚙️ Configuration & Setup

### Prerequisites
1. **Windows OS**
2. **Visual Studio 2017+** (with .NET desktop development and ASP.NET workloads enabled)
3. **.NET Framework 4.5**

### Database Setup
The application is preconfigured to use a local SQLite database file:
- Located at: `WebAPI/App_Data/SimpleDb.sqlite`
- The path is configured in `Web.config`:
  ```xml
  <appSettings>
    <add key="DbConnection" value="/App_data/SimpleDb.sqlite" />
  </appSettings>
  ```

### JWT Secret Setup
Login tokens are signed with a secret that is kept out of source control:
1. Copy `WebAPI/Secrets.config.example` to `WebAPI/Secrets.config` (git-ignored).
2. Set `JwtSecret` to at least 32 random characters; the example file shows a PowerShell one-liner that generates one.

`Web.config` merges `Secrets.config` into its `appSettings`. The API refuses to start if `JwtSecret` is missing or shorter than 32 bytes. Publishing includes `Secrets.config` when it exists, so the server gets the same secret; use a different secret per environment.

### How to Run
1. Clone this repository:
   ```bash
   git clone https://github.com/sagarworlds/FuelManagementWebAPI.git
   ```
2. Open `FuelManagementWebAPI.sln` in Visual Studio.
3. Restore NuGet packages when prompted.
4. Set the `WebAPI` project as the **Startup Project**.
5. Press `F5` or click **Start** to run the API using IIS Express.
6. The browser will launch, and you can access endpoints via `http://localhost:YOUR_PORT/api/...`.

### Running Tests
Open **Test Explorer** in Visual Studio and run all tests; the `WebAPI.Tests` project uses NUnit 3 with the NUnit 3 test adapter. The tests host the API in memory with a fake repository, so they don't need IIS or the SQLite database.
