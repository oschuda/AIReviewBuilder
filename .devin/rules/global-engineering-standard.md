# GLOBAL ENGINEERING STANDARD

**Version:** 1.1.0
**Status:** Active
**Classification:** CRITICAL / HIGH / MEDIUM / LOW (see §29)

> This document is the authoritative engineering standard for this repository. It is stored
> here so the rules travel with the source. `GLOBAL_STANDARD_VERSION = "1.1.0"`.

---

## 1. ENGINEERING PRINCIPLES
- Enforce Clean Architecture, Separation of Concerns, and Dependency Inversion.
- Systems must be modular, testable, and deterministic.
- Infrastructure and domain logic must be strictly separated.
- No hidden state or implicit control flow.

## 2. SECURITY & RESILIENCE BASELINE
- All inputs are untrusted by default.
- Explicit validation at all system boundaries is mandatory.
- No unsafe deserialization mechanisms are permitted.
- Fail-fast behavior is required for invalid or insecure configuration states.
- Security must be designed-in, not applied post-implementation.

## 3. REGULATORY COMPLIANCE BASELINE (MANDATORY CONTEXT)
The system must be designed to comply with: Cyber Resilience Act (CRA); NIS2 Directive;
Regulation (EU) 2023/1230 (Machinery Regulation / MVO); ISO/IEC 27001; IEC 62443; ISO 9001.

## 4. REGULATORY PRECEDENCE RULE
- Regulatory constraints are mandatory and must always be satisfied.
- If engineering principles conflict with regulatory requirements, compliance takes priority.

## 5. SECURITY MODEL REFERENCE FRAMEWORKS
- OWASP Top 10 must be considered for all application layers.
- Input validation, authentication, authorization, and injection prevention follow OWASP.

## 6. UI ARCHITECTURE PATTERNS
- MVVM for UI separation and binding-based architectures.
- UI patterns must not leak into domain or infrastructure layers.

## 7. RUNTIME & FRAMEWORK TARGETING MODEL
Allowed runtimes: .NET 8 LTS (primary modern runtime); .NET Framework 4.8 (legacy interop);
future .NET LTS versions as explicitly approved. Every project must define its primary and
secondary runtimes, compatibility constraints, and IPC model. Cross-runtime usage must be
strictly isolated with explicit communication and no shared in-process dependencies.

## 8. DATA INTEGRITY & DETERMINISM
- All DTOs must be immutable by default.
- All serialization must be deterministic and culture-invariant (`CultureInfo.InvariantCulture`).
- All timestamps must use UTC internally (`DateTime.UtcNow`).
- Encoding must be explicit (UTF-8 without BOM: `new UTF8Encoding(false)`).

## 9. TRUST BOUNDARIES & SYSTEM ISOLATION
- All external systems and inter-process boundaries are untrusted by default.
- Every boundary crossing requires strict validation and normalization.
- External integration components are failure-isolated zones.

## 10. RESOURCE EXHAUSTION PROTECTION
- Unbounded memory growth, long-running unmanaged allocations, and unbounded caching are forbidden.
- Payload sizes/streams must be validated and bounded before allocation.
- Queue growth must be capped; retry storms mitigated via bounded retries and backoff.
- Timeouts and `CancellationToken` are mandatory for external integrations, file access, IPC.

## 11. ERROR HANDLING MODEL & TAXONOMY
- No silent failures or swallowed exceptions.
- All errors must be structured, typed, and traceable using a domain-specific exception taxonomy
  (e.g. Security, Validation, Integration, Infrastructure). Generic exceptions must not be used
  for domain anomalies. Fail-fast on invalid initialization states.

## 12. CONCURRENCY & STATE MANAGEMENT
- Shared mutable state is forbidden unless explicitly synchronized and documented.
- Stateless design is the default. Parallel execution must be bounded and controlled.

## 13. SUPPLY CHAIN SECURITY
- All NuGet dependencies pinned to explicit versions. Floating versions forbidden.
- Vulnerable, deprecated, or unsupported packages are not permitted.

## 14. SECURITY TESTING REQUIREMENTS
- Security-relevant code paths must be testable; validation/sanitization/trust-boundary logic
  requires automated unit + integration tests.
- Critical serialization/IPC channels need automated regression tests.
- SAST and dependency scanning must be integrated into CI/CD.

## 15. AI SELF-AUDIT & META-VERIFICATION REQUIREMENTS
All generated code must conclude with a structured AI SELF-AUDIT REPORT, transitioning from
"author" to independent "auditor". Compulsory output structure:

```markdown
### 🛡️ MANDATORY AI SELF-AUDIT REPORT
- **Compliance Status:** [Full / Partial / Non-Compliant with specific standard reference]
- **Architectural & Concurrency Risks:** [...]
- **Security & Trust-Boundary Risks:** [...]
- **Determinism & Encoding Risks:** [...]
- **Resource Exhaustion Risks:** [...]
- **Explicit Assumptions Made:** [...]
- **Identified Violations & Deviations:** [...]
- **Remaining Uncertainty Area:** [...]
```

## 16. AI CODE GENERATION RULES
- No placeholder security implementations or mock authentications.
- `// TODO` blocks for security-critical logic are forbidden.
- No bypass of validation, exception handling, or compiler warnings for convenience.
- Secure implementation must be completely written out and production-ready.

## 17. OBSERVABILITY & TRACEABILITY
- All operations/orchestration/exports must be traceable via structured logging.
- Correlation IDs are mandatory across process boundaries.
- No sensitive data may appear in logs. Log only metadata (names, quantities, durations).

## 18. UNMANAGED RESOURCE MANAGEMENT
- `SafeHandle` mandatory for native handles; `GC.AddMemoryPressure`/`RemoveMemoryPressure` for
  large (>85 KB) native allocations; finalizers restricted to SafeHandle wrappers.

## 19. COM THREADING MODEL
- `[STAThread]` on entry points performing STA COM interop; no STA calls from MTA threads.

## 20. PROCESS ISOLATION FOR BRIDGE PROCESSES
- `ProcessStartInfo.UseShellExecute = false`; mandatory timeouts; documented exit-code semantics.

## 21. ASYNCHRONOUS PATTERN COMPLIANCE
- `ConfigureAwait(false)` mandatory for all library/non-UI execution paths.
- `CancellationToken` threaded through the entire async call stack.
- `async void` is forbidden (except UI event handlers).

## 22. .NET FRAMEWORK 4.8 COMPATIBILITY RULES
- C# 7.3 max; `HttpClient` enforces TLS 1.2; `ArrayPool<T>` for heavy recurrent allocations.

## 23. MEMORY & BUFFER SAFETY
- Validate externally supplied lengths/offsets/buffer sizes before allocation; prefer bounded,
  chunked, streaming processing for large payloads.

## 24. DEADLOCK & BLOCKING PREVENTION
- `Task.Result`, `.Wait()`, `.GetAwaiter().GetResult()` forbidden except console `Main()`.
- Sync-over-async across IPC/network boundaries is forbidden.

## 25. DOMAIN MODEL INTEGRITY
- Domain invariants enforced at entity/aggregate boundaries; invalid state impossible by construction.

## 26. AUDIT TRAIL IMMUTABILITY
- Audit logs append-only; security events carry timestamps, correlation IDs, origin context.

## 27. CI/CD GOVERNANCE
- CI/CD pipelines are part of the trusted supply chain and must fail on critical violations.

## 28. AI OUTPUT VALIDATION RULE
- AI-generated code is untrusted until reviewed; human review mandatory for security-relevant code.

## 29. RULE CLASSIFICATION & PRIORITIES
| Severity | Label | Consequence |
|----------|-------|-------------|
| 🔴 CRITICAL | MUST | Blocking CI/CD, mandatory fix |
| 🟠 HIGH | SHALL | Architecture board approval required |
| 🟡 MEDIUM | SHOULD | Documented justification |
| ⚪ LOW | MAY | Best practice |

**CRITICAL rules include:** §2, §4, §9, §10, §11, §15, §16, §28, §35.

## 30. COMPLIANCE COST ACKNOWLEDGMENT
These rules intentionally increase development effort for regulatory and operational safety.

## 31. EXCEPTION & DEVIATION PROCESS
Deviations from CRITICAL/HIGH rules require a documented ADR, architect/security-lead approval,
and revalidation at major releases.

## 32. THREAT MODELING REQUIREMENTS
STRIDE-based threat modeling for security-relevant components, especially on trust-boundary or
IPC contract changes.

## 33. SECRETS MANAGEMENT
Hardcoded secrets forbidden (CRITICAL). Secrets loaded from secure stores with rotation support.

## 34. CRYPTOGRAPHIC BASELINE
Only vetted `System.Security.Cryptography` algorithms. Deprecated algorithms (MD5, SHA-1, DES)
prohibited.

## 35. ANTI-SIMULATION & DOMAIN-FIRST ENFORCEMENT (Anti-Theater Rule)
No claim of progress/phase-completion/production-readiness while core workflows are stubs.
Completion requires, in the same response: complete compilable source, relevant automated tests,
and a compilation proof (0 errors). Faked commits/push notices and "Phase X delivered" without
substantial code are prohibited. **Severity: CRITICAL.**

## 36. STANDARD VERSIONING & CHANGE GOVERNANCE
Semantic versioning; each project declares the targeted version (`GLOBAL_STANDARD_VERSION = "1.1.0"`).

## 37. GLOSSARY
| Term | Definition |
|------|-------------|
| Bridge | Isolated .NET Framework 4.8 process for legacy interop via IPC. |
| IPC | Inter-Process Communication via `Process.Start` with stdio redirection. |
| Anti-Theater | Simulation of progress without real domain implementation. |
