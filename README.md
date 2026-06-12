# AI Review Builder

A **local, offline** Windows desktop tool that scans a source repository, filters files with
include/exclude glob patterns, estimates token/line/byte metrics, and produces:

- an **LLM-optimized `review.md`** (with a selectable review profile prompt), and
- a **source bundle `.zip`** of the filtered files.

There is **no network access, no API keys, and no cloud dependency** — everything runs on the
local machine.

> Built to comply with the **GLOBAL ENGINEERING STANDARD v1.1.0** and the **APPLICATION SYSTEM
> SPECIFICATION** (see `.devin/rules/`). Phase 1 scope.

## Architecture (Clean Architecture)

| Project | Target | Responsibility |
|---------|--------|----------------|
| `AIReviewBuilder.Domain` | `net9.0` | Pure domain: entities, value objects, deterministic services, typed exception taxonomy. Zero external dependencies. |
| `AIReviewBuilder.Application` | `net9.0` | Interfaces (ports), immutable request/result models, the `ReviewOrchestrator`. |
| `AIReviewBuilder.Infrastructure` | `net9.0` | File scanner, repository discovery, Markdown generator, ZIP packer, JSON profile store, atomic file writer. |
| `AIReviewBuilder.UI` | `net9.0-windows` | WPF + MVVM (CommunityToolkit.Mvvm). Thin presentation layer only. |
| `AIReviewBuilder.Tests` | `net9.0` | xUnit unit + integration tests. |

Dependency direction: `UI → Infrastructure → Application → Domain` (Domain depends on nothing).

## Security & resource model (highlights)

- **Path-traversal defense** — every path is canonicalized and contained within the repository
  root; `../` escapes and absolute-path injection are rejected (`PathBoundaryGuard`).
- **Symlink defense** — reparse points (symlinks/junctions) are skipped so traversal cannot
  leak files from outside the repository.
- **DoS ceilings** — max 5,000 files, 5 MB per file, 100 MB cumulative; verified **before**
  reading content into memory. Directory depth is capped.
- **Markdown-injection mitigation** — file paths/metadata are escaped; embedded file contents
  are wrapped in dynamically sized backtick fences (`MarkdownSanitizer`).
- **Atomic file output** — write to a temp file, hash the finalized content (SHA-256), then
  atomic move into place.
- **Determinism** — UTF-8 without BOM, `\n` line endings, `InvariantCulture`, UTC timestamps,
  ordinal sorting, fixed ZIP entry timestamps.
- **Structured logging** — correlation IDs; only metadata (counts, sizes, hashes) is logged —
  never file contents or secrets.

## Build & test

Requires the **.NET 9 SDK**.

```bash
# Cross-platform layers (build on any OS, incl. Linux CI):
dotnet build src/AIReviewBuilder.Infrastructure/AIReviewBuilder.Infrastructure.csproj -c Release
dotnet test  tests/AIReviewBuilder.Tests/AIReviewBuilder.Tests.csproj -c Release
```

```powershell
# Full solution including the WPF UI (Windows only):
dotnet build AIReviewBuilder.sln -c Release
```

> **Platform note:** WPF requires the Windows Desktop SDK, so the `AIReviewBuilder.UI` project
> compiles on Windows only. The cross-platform layers — which contain all domain, security,
> validation, and resource-control logic — build and test on Linux. See
> [`docs/adr/0001-net9-wpf-deviation.md`](docs/adr/0001-net9-wpf-deviation.md).

## Project standards

- C# 13, nullable enabled, **warnings treated as errors**, .NET analyzers (`latest-recommended`).
- Central package management with **pinned** NuGet versions (`Directory.Packages.props`).
- Every primary source file ends with a **🛡️ AI SELF-AUDIT REPORT** block per GLOBAL
  STANDARD §15.
