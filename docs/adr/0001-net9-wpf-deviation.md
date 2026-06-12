# ADR-0001: Runtime target (.NET 9) and WPF presentation layer

- **Status:** Accepted
- **Date:** 2026-06-12
- **Deciders:** System Architect (Erik Denzler), implementing engineer
- **Standard reference:** GLOBAL ENGINEERING STANDARD v1.1.0 §7 (Runtime & Framework
  Targeting), §31 (Exception & Deviation Process), §29 (Rule Classification)

## Context

The AI Review Builder Phase 1 application is specified as a **local Windows desktop tool**
with a **WPF + MVVM** user interface (project specification §UI; GLOBAL STANDARD §6).

Two requirements collide with the GLOBAL ENGINEERING STANDARD:

1. **Runtime target.** GLOBAL STANDARD §7 lists the allowed primary modern runtime as
   **.NET 8 LTS**; "future .NET LTS versions" require explicit approval. The project
   specification requests **.NET 9**, which is a Standard-Term Support (STS) release, not
   LTS. Targeting .NET 9 is therefore a deviation from §7.

2. **Presentation technology / build portability.** WPF requires the Windows Desktop SDK
   (`net9.0-windows`, `<UseWPF>true</UseWPF>`) and can only be compiled on Windows. The
   cross-platform CI used for the Domain, Application, Infrastructure, and Test layers runs
   on Linux, where the WPF UI project cannot be built. GLOBAL STANDARD §35 (Anti-Theater)
   requires a real "0 Errors" compilation proof for completed work.

## Decision

1. **Target .NET 9** for all projects, accepted as an explicit, documented deviation from
   §7. The owner (system architect) approved this trade-off knowingly.

2. **Keep WPF** for the presentation layer. The UI project (`AIReviewBuilder.UI`) targets
   `net9.0-windows` and is compiled and run on Windows only. The **cross-platform layers**
   (`Domain`, `Application`, `Infrastructure`, `Tests`) target plain `net9.0` and are fully
   built and tested on Linux CI, providing the §35 compilation/test proof for all logic
   that carries domain, security, validation, and resource-control responsibilities.

3. **The UI layer remains thin** (GLOBAL STANDARD §6): it contains no domain, validation,
   security, or resource-limit logic. All such logic lives in the cross-platform layers, so
   the portion that cannot be compiled on Linux carries no security-critical behaviour.

## Consequences

- **Positive:** Satisfies the product requirement (native Windows WPF tool); all
  security/DoS/determinism logic is verifiable on portable CI; clean separation keeps the
  unverifiable surface (XAML + view-model marshalling) minimal.
- **Negative / risk:** .NET 9 is STS (18-month support window). A migration to the next LTS
  (.NET 10 LTS) must be revalidated at a major release per §31. WPF compilation is verified
  only on the Windows CI job, not the Linux job.
- **Revalidation:** This deviation must be re-reviewed at the next major release and when the
  runtime support window approaches end-of-life.

## Compliance mapping

| Rule | Status | Note |
|------|--------|------|
| §7 Runtime targeting | Deviation (approved) | .NET 9 STS instead of .NET 8 LTS |
| §6 UI patterns | Compliant | MVVM; no domain logic in UI |
| §31 Deviation process | Compliant | This ADR; architect approval recorded |
| §35 Anti-Theater | Compliant | Cross-platform layers build + test on CI; UI built on Windows CI |
