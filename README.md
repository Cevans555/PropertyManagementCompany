# PropertyManagementCompany
A web application for submitting and reviewing rental applications.

Built with ASP.NET Core MVC and Razor on .NET 10, using ASP.NET Identity, SQL Server and Entity Framework Core.

## Getting started

### Prerequisites
- .NET 10 SDK
- SQL Server LocalDB (installed with Visual Studio's "ASP.NET and web development" workload)

### Database
The connection string is in `src/PropertyManagement.Web/appsettings.json` and points at LocalDB, so no setup is needed. To use a different SQL Server, change `DefaultConnection`.

### Run
Open `PropertyManagement.slnx` in Visual Studio and press F5, or run:
```
dotnet run --project src/PropertyManagement.Web
```

## Solution structure
```
PropertyManagement.slnx
├─ src/
│  ├─ PropertyManagement.Web    MVC app: startup, controllers, views
│  ├─ PropertyManagement.Core   entities and business rules (no database or web code)
│  └─ PropertyManagement.Data   DbContext, migrations, seeding
└─ tests/
   └─ PropertyManagement.Tests  unit tests for Core
```
References point toward Core: Web → Core, Data; Data → Core; Tests → Core.