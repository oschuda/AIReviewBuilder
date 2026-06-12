using System.Collections.Immutable;
using AIReviewBuilder.Domain.Common;
using AIReviewBuilder.Domain.Entities;
using AIReviewBuilder.Domain.ValueObjects;

namespace AIReviewBuilder.Application.Models;

/// <summary>
/// Immutable instruction to generate an LLM-optimised <c>review.md</c> from a set of
/// previously scanned files. All paths are absolute and have already passed the scan's
/// trust-boundary checks; the generator re-validates each path before reading (defence
/// in depth, §9).
/// </summary>
public sealed class ReviewDocumentRequest
{
    public ReviewDocumentRequest(
        string repositoryRootPath,
        string repositoryName,
        ReviewProfile profile,
        ImmutableArray<ScannedFile> files,
        string outputFilePath,
        ResourceLimits limits,
        bool includeFileContents = true)
    {
        RepositoryRootPath = Guard.NotNullOrWhiteSpace(repositoryRootPath);
        RepositoryName = Guard.NotNullOrWhiteSpace(repositoryName);
        Profile = Guard.NotNull(profile);
        OutputFilePath = Guard.NotNullOrWhiteSpace(outputFilePath);
        Limits = Guard.NotNull(limits);

        if (files.IsDefault)
        {
            throw new Domain.Exceptions.DomainValidationException($"{nameof(files)} must be initialized.");
        }

        Files = files;
        IncludeFileContents = includeFileContents;
    }

    /// <summary>Canonical absolute repository root.</summary>
    public string RepositoryRootPath { get; }

    /// <summary>Display name used in the document header (sanitised at render time).</summary>
    public string RepositoryName { get; }

    /// <summary>The review profile whose prompt steers the review.</summary>
    public ReviewProfile Profile { get; }

    /// <summary>Files to include, in deterministic order.</summary>
    public ImmutableArray<ScannedFile> Files { get; }

    /// <summary>Absolute path where the final review.md is written (atomically).</summary>
    public string OutputFilePath { get; }

    /// <summary>Resource ceilings re-enforced during content reading.</summary>
    public ResourceLimits Limits { get; }

    /// <summary>When false, only the file index/metrics are emitted (no file bodies).</summary>
    public bool IncludeFileContents { get; }
}

// ─────────────────────────────────────────────────────────────────────────────
// 🛡️ MANDATORY AI SELF-AUDIT REPORT
// - Compliance Status: Full — §8 immutable DTO, §2 validation, §9 (re-validation note).
// - Architectural & Concurrency Risks: None. Immutable.
// - Security & Trust-Boundary Risks: Carries absolute paths and limits so the
//   generator can re-apply the path/symlink/DoS guards independently.
// - Determinism & Encoding Risks: Files preserve deterministic order.
// - Resource Exhaustion Risks: Limits travel with the request so they cannot be lost.
// - Explicit Assumptions Made: RepositoryName may be hostile and is escaped on render.
// - Identified Violations & Deviations: None.
// - Remaining Uncertainty Area: None.
// ─────────────────────────────────────────────────────────────────────────────
