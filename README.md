# Zero Trust Evidence

Zero Trust Evidence is a prototype project for comparing two mobile client attestation approaches in a simplified Android client-backend verification scenario:

1. Software/context-aware attestation
2. Hardware/root-of-trust-based attestation

The project evaluates verification time, verification message size, and client-server processing step count.

## Structure

```text
src/
  Zte.Backend.Api
  Zte.Backend.Application
  Zte.Backend.Domain
  Zte.Dashboard.Client
  Zte.Backend.Infrastructure

tests/
  Zte.Backend.Tests

docs/
  Project and experiment documentation

artifacts/
  Screenshots and raw measurement outputs
```

## Dashboard

The embedded dashboard client lives in `src/Zte.Dashboard.Client`. It is a Vite
React app that builds static files into `src/Zte.Backend.Api/wwwroot/dashboard`
and is served by the API at `/dashboard`.

```powershell
cd src\Zte.Dashboard.Client
npm install
npm run build

cd ..\..
dotnet run --project src\Zte.Backend.Api
```

Open `http://localhost:5145/dashboard` after starting the backend. During UI
development, the dashboard can also run separately with `npm run dev`; API calls
to `/api` are proxied to `http://localhost:5145`.
