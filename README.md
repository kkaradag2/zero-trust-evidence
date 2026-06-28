# Zero Trust Evidence

Zero Trust Evidence is a prototype platform for measuring the runtime cost of
Android hardware-backed attestation in a Zero Trust API access flow.

The project focuses on a cost-freshness benchmark: how often a mobile client
refreshes hardware-backed proof, how many API requests reuse an existing proof,
and how much latency is introduced by each fresh attestation step.

## Research Focus

The benchmark is designed around one question:

> What is the runtime cost of keeping Android hardware-backed attestation
> evidence fresh enough for Zero Trust API access?

The measured runtime path is:

1. The Android client requests an attestation challenge from the backend.
2. The client signs the challenge with an Android Keystore hardware-backed key.
3. The backend verifies the signed proof.
4. The client records per-request timing rows for the benchmark policy.
5. The backend stores the benchmark run and runtime measurement rows.
6. The embedded dashboard visualizes the results and exports the dataset as CSV.

Enrollment is treated as setup. It is required before runtime verification, but
it is not included in runtime cost averages.

## System Roles

```text
Mobile app         Benchmark runner
Backend API        Benchmark and measurement record system
Embedded dashboard Result inspection and CSV export
```

The mobile app owns benchmark execution. The backend owns durable in-memory
recording for the current process. The dashboard is read-only except for CSV
export generated from stored runtime measurement rows.

## Benchmark Policies

The mobile benchmark runs a fixed number of request iterations and applies a
freshness policy:

| Policy | Meaning |
| --- | --- |
| Every request | A fresh hardware attestation is performed for every request. |
| Every 3 requests | One fresh proof is generated every third request. |
| Every 5 requests | One fresh proof is generated every fifth request. |
| Every 10 requests | One fresh proof is generated every tenth request. |
| Session only | One fresh proof is generated at the start; later rows reuse it. |

Every request produces one runtime measurement row. Reuse rows are stored with
zero proof cost so the exported dataset keeps the exact request count and policy
shape.

## Runtime Measurement Model

Each runtime row records:

- `policyK`
- `policyLabel`
- `runIndex`
- `totalRequests`
- `attestationPerformed`
- `challengeFetchMs`
- `deviceSigningMs`
- `backendVerificationMs`
- `freshProofCostMs`
- `operationTotalMs`
- `success`
- `errorMessage`
- `timestamp`

For fresh rows:

```text
freshProofCostMs = challengeFetchMs + deviceSigningMs + backendVerificationMs
```

For reuse rows, proof timing fields are `0` and `attestationPerformed` is
`false`.

## Architecture

```text
src/
  Zte.Backend.Api              ASP.NET Core API and embedded dashboard host
  Zte.Backend.Application      Application contracts and attestation services
  Zte.Backend.Domain           Domain entities and benchmark models
  Zte.Backend.Infrastructure   In-memory stores for benchmark and measurement data
  Zte.Client.Mobile            React Native Android benchmark runner
  Zte.Dashboard.Client         Vite React dashboard client

tests/
  Zte.Backend.Tests            Backend unit and controller tests
```

## Key API Endpoints

Benchmark lifecycle:

```http
POST /api/benchmarks
GET  /api/benchmarks
GET  /api/benchmarks/{id}
POST /api/benchmarks/{id}/complete
POST /api/benchmarks/{id}/fail
```

Runtime measurement storage:

```http
POST /api/benchmarks/{id}/runtime-measurements
GET  /api/benchmarks/{id}/runtime-measurements
```

Attestation flow:

```http
POST /api/attestation/challenge
POST /api/hardware-attestation/enroll
POST /api/attestation/hardware/verify
```

Legacy backend-side verification measurements are still present internally for
debugging, but the dashboard and CSV export are centered on runtime measurement
rows.

## Dashboard

The dashboard is a Vite React app embedded into the ASP.NET Core API. It is
served from:

```text
http://localhost:5145/dashboard
```

The detail page shows:

- benchmark policy
- total request count
- fresh attestation count
- saved runtime measurement count
- average challenge fetch time
- average device signing time
- average backend verification time
- average fresh proof cost
- runtime measurement table
- CSV export for the paper dataset

CSV export is generated only from runtime measurement rows and includes
benchmark, mobile device, backend system, policy, and timing context.

## Getting Started

### Prerequisites

- .NET SDK compatible with the solution
- Node.js 22.11 or newer for the mobile app
- npm
- Android Studio or Android SDK tooling for running the React Native Android app

### Restore and test the backend

```powershell
dotnet restore
dotnet test
```

### Build the embedded dashboard

```powershell
cd src\Zte.Dashboard.Client
npm install
npm run build
```

The dashboard build writes static assets to:

```text
src/Zte.Backend.Api/wwwroot/dashboard
```

### Run the backend API

```powershell
cd ..\..
dotnet run --project src\Zte.Backend.Api
```

Default HTTP URL:

```text
http://localhost:5145
```

Dashboard URL:

```text
http://localhost:5145/dashboard
```

### Run the mobile benchmark app

```powershell
cd src\Zte.Client.Mobile
npm install
npm run android
```

The Android emulator uses `http://10.0.2.2:5145` to reach the backend running on
the host machine.

## Development Commands

Backend:

```powershell
dotnet build
dotnet test
```

Dashboard:

```powershell
cd src\Zte.Dashboard.Client
npm run build
```

Mobile:

```powershell
cd src\Zte.Client.Mobile
npm test -- --runInBand
npm run lint
npx tsc --noEmit
```

## Current Limitations

- Stores are in-memory and reset when the backend process restarts.
- Android Key Attestation certificate chain validation is structural and not a
  complete production trust-chain validation against Google/Android roots.
- The project is a research prototype, not a production Zero Trust enforcement
  gateway.
- Dashboard CSV export is intended for benchmark analysis, not long-term data
  warehousing.

## Intended Output

After running a benchmark, the dashboard should make it easy to verify:

```text
Saved Measurements = Total Requests
Fresh Attestations = expected count for the selected policy
Avg Fresh Proof Cost ~= Avg Challenge Fetch
                       + Avg Device Signing
                       + Avg Backend Verification
```

The exported CSV provides one row per benchmark request iteration and can be
used directly for cost-freshness analysis in the accompanying paper or report.
