using System.Collections.Immutable;
using AIReviewBuilder.Domain.Entities;

namespace AIReviewBuilder.Infrastructure.Profiles;

/// <summary>
/// The built-in review profiles seeded into an empty local profile store
/// (project spec: General, Security, Architecture, Performance, Documentation).
/// Prompts are static, local, and contain no secrets or network references (v1.0 local).
/// </summary>
public static class DefaultReviewProfiles
{
    public static ImmutableArray<ReviewProfile> All { get; } =
    [
        new ReviewProfile(
            "general",
            "General Review",
            ReviewProfileCategory.General,
            "You are an expert software engineer performing a thorough general code review. "
            + "Assess correctness, readability, naming, error handling, and test coverage. "
            + "Call out bugs, risky assumptions, and maintainability concerns with concrete, "
            + "actionable suggestions. Reference files and line ranges where possible.",
            "Balanced, all-round review."),

        new ReviewProfile(
            "security",
            "Security Review",
            ReviewProfileCategory.Security,
            "You are an application security specialist. Review the code against the OWASP Top 10 "
            + "and secure-coding best practices. Focus on input validation, injection, authentication "
            + "and authorization, unsafe deserialization, path traversal, secrets handling, and "
            + "resource-exhaustion (DoS) vectors. For each finding state severity, impact, and a fix.",
            "OWASP-focused security audit."),

        new ReviewProfile(
            "architecture",
            "Architecture Review",
            ReviewProfileCategory.Architecture,
            "You are a software architect. Evaluate separation of concerns, dependency direction, "
            + "cohesion and coupling, abstraction boundaries, and adherence to Clean Architecture. "
            + "Identify layering violations, leaky abstractions, and opportunities to simplify or "
            + "decouple. Recommend structural improvements with trade-offs.",
            "Structure, layering, and design."),

        new ReviewProfile(
            "performance",
            "Performance Review",
            ReviewProfileCategory.Performance,
            "You are a performance engineer. Identify hot paths, unnecessary allocations, blocking "
            + "calls, N+1 patterns, and unbounded growth. Consider asynchronous correctness, buffering, "
            + "and memory pressure. Suggest measurable optimizations and note where profiling is needed.",
            "Throughput, latency, and memory."),

        new ReviewProfile(
            "documentation",
            "Documentation Review",
            ReviewProfileCategory.Documentation,
            "You are a technical writer and engineer. Assess the clarity and completeness of public "
            + "API docs, comments, and READMEs. Flag missing, outdated, or misleading documentation and "
            + "propose concise improvements that help new contributors understand the code.",
            "Docs, comments, and clarity."),
    ];
}

// ─────────────────────────────────────────────────────────────────────────────
// 🛡️ MANDATORY AI SELF-AUDIT REPORT
// - Compliance Status: Full — matches the five profiles in the project spec; purely
//   local content (no network, no secrets, §33).
// - Architectural & Concurrency Risks: None. Immutable static data; safe to share (§12).
// - Security & Trust-Boundary Risks: Prompts are static literals; no injection surface.
// - Determinism & Encoding Risks: Fixed order and content (§8).
// - Resource Exhaustion Risks: Prompt lengths are well within ReviewProfile limits.
// - Explicit Assumptions Made: These defaults are seeded only when the store is empty.
// - Identified Violations & Deviations: None.
// - Remaining Uncertainty Area: None.
// ─────────────────────────────────────────────────────────────────────────────
