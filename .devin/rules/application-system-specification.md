# APPLICATION SYSTEM SPECIFICATION — AI Review Builder

**Version:** 1.0.0
**Status:** Active
**Target Standard:** GLOBAL ENGINEERING STANDARD v1.1.0

> Phase 1 is a **purely local** desktop application (no network, no cloud, no external LLM
> calls). Sections referencing bridge processes / IPC describe Phase 2+ scope and are recorded
> here for forward compatibility.

## 1. APPLICATION CONTEXT
Defines system-specific architecture, runtime behavior, integration boundaries, and operational
constraints. Must comply with the GLOBAL ENGINEERING STANDARD.

## 2. REGULATORY ALIGNMENT
Designed for a compliance-aware environment: CRA, NIS2, EU 2023/1230 (MVO), ISO/IEC 27001,
IEC 62443, ISO 9001 (where applicable).

## 3. ARCHITECTURAL MODEL
- Layered Clean Architecture with strict Separation of Concerns.
- Domain logic must not depend on infrastructure.
- External integrations isolated behind explicit interfaces.
- Extension via composition (Strategy), not modification of orchestration logic.

## 4. TRUST & INTEGRATION MODEL
- External systems/IPC streams untrusted by default; strict validation and normalization.
- Failures contained within integration layers; no host destabilization.
- Validation ownership is explicit and not implicitly delegated across layers.

## 5. EXECUTION & RESOURCE CONTROL MODEL
Fail-safe, resource-bounded, predictable. Payloads validated against size ceilings before
allocation. Typed/structured errors only; no silent degradation.

**Concrete resource bounds (Phase 1 local + Phase 2+ bridge):**

| Limit | Value | Notes |
|-------|-------|-------|
| Maximum files per execution | 5,000 files | Prevents unbounded traversal |
| Maximum size per file | 5 MB | Single-file ceiling |
| Maximum cumulative payload | 100 MB | Hard limit for entire payload |
| Maximum JSON payload from bridge | 50 MB | Phase 2+ |
| Maximum line items in BOM | 10,000 items | Phase 2+ |
| Maximum string length (input params) | 255 characters | Injection prevention |
| Allocations > 85 KB | Must use `ArrayPool<T>` | LOH fragmentation prevention |

**Configuration governance:** hard limits configurable through validated configuration;
conservative documented defaults; changes require review + audit trail; never 0 (unbounded) or
above tested capacity. Deviations require an ADR.

## 6. DATA FLOW MODEL
Deterministic, explicit data flows. Immutable DTOs/IPC contracts. Deterministic, culture-invariant
serialization, UTF-8 without BOM. UTC timestamps and audit metadata.

## 7. CONFIGURATION MODEL
Strict startup validation; fail-fast on invalid/insecure config; no insecure runtime fallback;
security-relevant changes traceable.

## 8. OBSERVABILITY MODEL
Structured logs for all orchestration/processing/integration/export; correlation IDs across
boundaries; no sensitive data in logs.

## 9. TESTING & VERIFICATION MODEL
Automated tests for parsers/IPC/interfaces; strict contract tests for serialization; testable
resource-exhaustion and retry-boundary protections; components support the AI Self-Audit protocol.

## 10. EXTENSIBILITY MODEL
Composition-based extension (Strategy); no implicit coupling; explicit documented extension points;
extensibility must not compromise determinism or trust boundaries.

## 11. ERROR MODEL
Explicit, typed, structured errors; no silent handling/crash loops; failures traceable to origin
and correlation IDs; no generic exceptions for domain/integration anomalies.

## 12. UI ARCHITECTURE
Phase 1 introduces a thin WPF + MVVM presentation layer. MVVM must not leak into domain or
infrastructure layers. (The original headless spec is superseded for Phase 1; see ADR-0001.)

## 13. OPERATIONAL RECOVERY MODEL (Phase 2+ bridges)
Deterministic bounded recovery; no unbounded restart loops; timeout 300s (30s dev default);
max 3 retries with exponential backoff (1s/2s/4s); `UseShellExecute=false`; documented exit codes.

## 14. RUNTIME DECLARATION
- **Primary runtime:** .NET 9 (see ADR-0001 deviation from §7's .NET 8 LTS).
- **Secondary / interop runtimes:** .NET Framework 4.8 isolated bridge processes (Phase 2+).
- **IPC:** asynchronous stdio streaming via `Process.Start` with trust-boundary validation (Phase 2+).
- **UI pattern:** WPF + MVVM (Windows-only; see ADR-0001).

## 15. COMPLIANCE PRIORITY RULE
Global engineering/regulatory constraints override application-specific decisions; resolve
conflicts in favor of security, determinism, stability, and compliance.

## 16. AI SELF-AUDIT INTEGRATION RULE
GLOBAL STANDARD §15 applies to all generated code, trust-boundary/IPC architectural proposals,
and security-relevant configuration schemas. The audit block appears immediately after each file
and references the relevant section(s).

## 17–28
State consistency, contract versioning, machine-safety containment, configuration change control,
**file export atomicity** (write temp → flush → hash → move), human review/release governance,
rule-classification mapping, deviations (ADR-tracked), threat-modeling scope, secrets-management
scope, cryptography scope (SHA-256 permitted; no custom crypto), and glossary — as defined in the
GLOBAL ENGINEERING STANDARD and applied to this application.

## 29. STANDARD COMPLIANCE DECLARATION
This specification complies with GLOBAL ENGINEERING STANDARD v1.1.0. Known deviation: runtime
target / WPF (ADR-0001).
