# Fuel Management Web API

[![.NET Framework](https://img.shields.io/badge/.NET%20Framework-4.5-blue.svg)](https://dotnet.microsoft.com/)
[![SQLite](https://img.shields.io/badge/Database-SQLite-003B57.svg?logo=sqlite)](https://sqlite.org/)
[![Dapper](https://img.shields.io/badge/ORM-Dapper-lightgrey.svg)](https://github.com/DapperLib/Dapper)

A lightweight and robust backend service designed to track fuel logs, manage users, and monitor fuel consumption/expenses. Built with **ASP.NET Web API (.NET Framework 4.5)**, **SQLite**, and **Dapper Micro-ORM**.

---

## 🚀 Features

- **User Management**: Simple registration and secure login mechanisms.
- **Fuel Tracking**: Record details such as odometer reading (meter reading), total price, fuel added, and personalized notes.
- **Robust Storage**: Uses SQLite for localized, file-based database storage.
- **High Performance**: Employs Dapper ORM for fast and efficient SQL execution.
- **Cross-Domain Support**: Integrates CORS handling for seamless client-side interactions.

---

## 🛠️ Tech Stack

- **Framework**: ASP.NET Web API 2 (.NET Framework 4.5)
- **Database**: SQLite
- **Data Access**: Dapper (Micro-ORM)
- **Language**: C#

---

## 📁 Repository Structure

```text
FuelManagementWebAPI/
├── FuelManagementWebAPI.sln  # Visual Studio Solution file
└── WebAPI/                   # Main Web API Project
    ├── App_Data/             # Local SQLite database files (SimpleDb.sqlite)
    ├── App_Start/            # Routing configuration (WebApiConfig.cs) & CORS middleware
    ├── Controllers/          # API Controllers (UserController, FuelDetailController)
    ├── Data/                 # Repository layer (Dapper queries and connection helpers)
    ├── Model/                # Core Data Transfer Objects & Domain Models (User, FuelDetail)
    ├── Models/               # Utility helper models (Email, SMS)
    └── Web.config            # Application configuration & AppSettings
```

---

## 🔌 API Documentation

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
* **Response**: Returns the matching user object on success, or `404 Not Found` if invalid.

#### 3. List All Users
* **URL**: `GET api/user/get`
* **Response**: An array of registered users.

---

### ⛽ Fuel Detail Endpoints (`api/fueldetail`)

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
* **Response**: Returns the created fuel log entry.

#### 2. Get Fuel Logs by User ID
* **URL**: `GET api/fueldetail/getbyuserid?userid={UserId}`
* **Response**: Returns a list of fuel logs associated with the specified User ID.

#### 3. Get Fuel Log by Log ID
* **URL**: `GET api/fueldetail/getfueldetailbyid?id={Id}`
* **Response**: Returns the specific fuel log entry.

#### 4. Get All Fuel Logs
* **URL**: `GET api/fueldetail/get`
* **Response**: Returns a list of all fuel logs in the system.

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
