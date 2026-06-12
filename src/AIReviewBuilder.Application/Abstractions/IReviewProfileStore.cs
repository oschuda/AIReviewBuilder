using System.Collections.Immutable;
using AIReviewBuilder.Domain.Entities;

namespace AIReviewBuilder.Application.Abstractions;

/// <summary>
/// Local JSON-backed repository of <see cref="ReviewProfile"/>. Implemented in
/// Infrastructure. Purely local — no network access (project spec v1.0).
/// </summary>
public interface IReviewProfileStore
{
    /// <summary>Loads all profiles, seeding the built-in defaults if none exist yet.</summary>
    Task<ImmutableArray<ReviewProfile>> GetAllAsync(CancellationToken cancellationToken);

    /// <summary>Returns the profile with the given id, or null if not found.</summary>
    Task<ReviewProfile?> FindByIdAsync(string id, CancellationToken cancellationToken);

    /// <summary>Persists (inserts or replaces) a profile atomically.</summary>
    Task SaveAsync(ReviewProfile profile, CancellationToken cancellationToken);
}

// ─────────────────────────────────────────────────────────────────────────────
// 🛡️ MANDATORY AI SELF-AUDIT REPORT
// - Compliance Status: Full — §3 interface in Application; purely-local design.
// - Architectural & Concurrency Risks: None. Pure contract.
// - Security & Trust-Boundary Risks: Implementations must use safe JSON
//   deserialization (no polymorphic type handling, §2 no unsafe deserialization).
// - Determinism & Encoding Risks: Implementations serialize as UTF-8 (no BOM),
//   culture-invariant (§8).
// - Resource Exhaustion Risks: Implementations must bound profile file size.
// - Explicit Assumptions Made: Profiles live in a local writable directory.
// - Identified Violations & Deviations: None.
// - Remaining Uncertainty Area: None.
// ─────────────────────────────────────────────────────────────────────────────
