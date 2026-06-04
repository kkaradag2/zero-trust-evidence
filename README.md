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
  Zte.Backend.Infrastructure

tests/
  Zte.Backend.Tests

docs/
  Project and experiment documentation

artifacts/
  Screenshots and raw measurement outputs