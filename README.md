# POS System

ASP.NET Core MVC POS system me produkte, shitje, kategori, kliente, zbritje, inventar baze dhe role perdoruesish.

Dokumentimi i analizes dhe roadmap-it per upgrade ne platforme serioze gjendet ketu:

- [DOKUMENTIMI_PLATFORMES_POS.md](DOKUMENTIMI_PLATFORMES_POS.md)
- [ROADMAP_CLOUD_MULTI_TENANT_POS.md](ROADMAP_CLOUD_MULTI_TENANT_POS.md)

## Development

Build:

```powershell
dotnet build .\WebApplication3.csproj
```

Run with Docker and PostgreSQL:

```powershell
docker compose up --build
```

Short command:

```powershell
.\docker-up.ps1
```

Stop Docker:

```powershell
.\docker-down.ps1
```

Run only PostgreSQL:

```powershell
docker compose up -d postgres
```

Apply PostgreSQL migrations from the host:

```powershell
$env:ConnectionStrings__Postgres='Host=localhost;Port=5432;Database=pos_platform;Username=pos_user;Password=pos_password'
dotnet ef database update --project .\WebApplication3.csproj
```
