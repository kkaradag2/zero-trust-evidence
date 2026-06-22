# Zero Trust Evidence

Zero Trust Evidence is a prototype project for comparing two mobile client attestation approaches in a simplified Android client-backend verification scenario:

1. Software/context-aware attestation
2. Hardware/root-of-trust-based attestation

The project evaluates verification time, verification message size, and client-server processing step count.

## Attestation flows

### Software attestation

Software attestation is kept separate from hardware-backed enrollment. The mobile
client requests a backend challenge, returns the challenge id and nonce with
context signals such as platform, app version, emulator state, and rooted state,
and the backend verifies those signals with `SoftwareAttestationService`.

Implemented:

- Backend-issued challenge id and nonce validation.
- Challenge expiration and single-use checks.
- Rule-based risk evaluation for software/device posture signals.

This flow is useful as a context-aware baseline, but it does not prove that a
private key was generated or protected by device hardware.

### Hardware-backed enrollment

Hardware-backed enrollment is handled by a dedicated API:

```http
POST /api/hardware-attestation/enroll
```

The legacy benchmark route remains available:

```http
POST /api/attestation/hardware/enroll
```

The mobile client now requests an enrollment challenge with device/app context,
generates an Android Keystore EC key using the backend nonce as the attestation
challenge, prefers StrongBox when available, falls back to TEE-backed Keystore
generation, and submits:

- `challengeId`
- `nonce`
- `deviceId`
- `appInstanceId`
- `keyAlias`
- `publicKeyBase64`
- `attestationEvidence`
- `certificateChainBase64`

The backend stores accepted devices through an enrolled-device store with the
enrolled device id, device/app id, public key, certificate chain, verification
steps, warnings, hardware/security level metadata when available, and creation
time.

Fully implemented:

- Dedicated hardware enrollment service and API DTOs.
- Challenge id, nonce, expiration, and single-use validation.
- EC SubjectPublicKeyInfo parsing for the submitted public key.
- Required certificate-chain presence.
- Structural parsing of each submitted certificate as X.509.
- Leaf certificate public key comparison against the submitted public key.
- Android Key Attestation extension challenge extraction when the extension is
  present and parseable.
- Attestation challenge comparison against the backend nonce when extracted.
- Deterministic duplicate device/key handling by returning the existing enrolled
  device id with a warning.
- Structured enrollment result containing `accepted`, `enrolledDeviceId`,
  `riskLevel`, `verificationSteps`, `reasons`, and `warnings`.

Partial/TODO:

- Full Android Key Attestation authorization-list validation is not complete.
  The code explicitly marks this as a TODO and returns a medium-risk accepted
  result with warnings when only structural certificate validation succeeds.
- Certificate chain trust anchoring to Google/Android attestation roots is not
  implemented. Invalid or missing certificate chains are rejected, but untrusted
  yet structurally valid development certificates can be used for local tests.
- Hardware-backed key properties beyond parsed Android security-level enum
  values are not fully enforced yet.

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
