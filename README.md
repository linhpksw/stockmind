# StockMind API

Simple ASP.NET Core Web API for internal stock management with JWT authentication, role-based access, and automated developer safeguards.

## Prerequisites

- [.NET SDK 8.0](https://dotnet.microsoft.com/en-us/download/dotnet/8.0)
- [SQL Server](https://www.microsoft.com/en-us/sql-server/sql-server-downloads) (local or remote)
- [Node.js 18+](https://nodejs.org/) (needed only for Husky Git hooks)

## 1. Clone and configure database

1. Clone the repository and open the folder:
   ```bash
   git clone https://github.com/linhpksw/stockmind.git
   cd stockmind
   ```
2. Create the database **StockMindDB** on SQL Server.
3. Execute the schema script located at `ddl_v251025.sql` against that database.

> The application seeds default roles and users on startup. Default password: `Stockmind@123`.

## 2. Regenerate entity models (when schema changes)

We use reverse engineering to keep the `Models/` folder in sync with the database. Whenever the schema changes, regenerate the entities with:

```bash
dotnet ef dbcontext scaffold "Server=(local); Database=StockMindDB; Uid=sa;Pwd=123;Encrypt=True;TrustServerCertificate=True;" Microsoft.EntityFrameworkCore.SqlServer --output-dir ./Models --force --no-onconfiguring
```
Notes:
- **Do not make manual edits inside the `Models/` folder.** Any customizations there will be overwritten the next time the scaffold runs; place custom logic elsewhere.

## 3. Install Husky Git hooks

Husky enforces automatic formatting, a successful build on commit, and tests on push. Install dependencies once:

```bash
npm install
dotnet tool restore
```

## 4. Run the API

```bash
dotnet run --project stockmind.csproj
```

Swagger UI is available in development at `http://localhost:8080/swagger/index.html`.

## 5. Git workflow guidelines

- **Never push directly to `main`.**
- Sync locally, then create a feature branch off `main`:
  ```bash
  git checkout main
  git pull origin main
  git checkout -b feature/my-change
  ```
- Once done, open a Pull Request back into `main`.
- Ensure all GitHub Actions checks (CI) pass before requesting review/merging.
- Rebase `main` into your branch if CI reports conflicts or outdated code.

All checks must pass before merging into `main`.

---

