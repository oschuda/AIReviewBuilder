using System.Text.Json.Serialization;

namespace AIReviewBuilder.Infrastructure.Profiles;

/// <summary>
/// Plain, non-polymorphic JSON transport record for <see cref="Domain.Entities.ReviewProfile"/>.
/// A dedicated DTO keeps deserialization safe (no type-name handling, §2 "no unsafe
/// deserialization") and decouples the on-disk schema from the domain entity (§18 contracts).
/// </summary>
public sealed record ReviewProfileDto
{
    [JsonPropertyName("id")]
    public string? Id { get; init; }

    [JsonPropertyName("name")]
    public string? Name { get; init; }

    [JsonPropertyName("category")]
    public string? Category { get; init; }

    [JsonPropertyName("prompt")]
    public string? Prompt { get; init; }

    [JsonPropertyName("description")]
    public string? Description { get; init; }
}

// ─────────────────────────────────────────────────────────────────────────────
// 🛡️ MANDATORY AI SELF-AUDIT REPORT
// - Compliance Status: Full — §2 safe deserialization (POCO only); §18 explicit schema.
// - Architectural & Concurrency Risks: None. Immutable record.
// - Security & Trust-Boundary Risks: No polymorphism/type handling; all fields nullable
//   so malformed JSON is validated when mapped to the domain entity (fail-fast).
// - Determinism & Encoding Risks: Category stored as a string name for stable schema.
// - Resource Exhaustion Risks: None at the type level; file size bounded by the store.
// - Explicit Assumptions Made: Mapping to ReviewProfile enforces all invariants.
// - Identified Violations & Deviations: None.
// - Remaining Uncertainty Area: None.
// ─────────────────────────────────────────────────────────────────────────────
