using System.Collections.Immutable;
using AIReviewBuilder.Domain.Common;
using AIReviewBuilder.Domain.Entities;
using AIReviewBuilder.Domain.ValueObjects;

namespace AIReviewBuilder.Application.Models;

/// <summary>
/// Immutable instruction to pack the clean, filtered source files of a repository into
/// a structural <c>.zip</c> archive (project spec "Local Repository Packer"). Paths are
/// re-validated against the root and DoS limits during packing (§9, §10).
/// </summary>
public sealed class SourceBundleRequest
{
    public SourceBundleRequest(
        string repositoryRootPath,
        ImmutableArray<ScannedFile> files,
        string outputArchivePath,
        ResourceLimits limits)
    {
        RepositoryRootPath = Guard.NotNullOrWhiteSpace(repositoryRootPath);
        OutputArchivePath = Guard.NotNullOrWhiteSpace(outputArchivePath);
        Limits = Guard.NotNull(limits);

        if (files.IsDefault)
        {
            throw new Domain.Exceptions.DomainValidationException($"{nameof(files)} must be initialized.");
        }

        Files = files;
    }

    /// <summary>Canonical absolute repository root.</summary>
    public string RepositoryRootPath { get; }

    /// <summary>Files to pack, in deterministic order.</summary>
    public ImmutableArray<ScannedFile> Files { get; }

    /// <summary>Absolute path where the final .zip is written (atomically).</summary>
    public string OutputArchivePath { get; }

    /// <summary>Resource ceilings re-enforced during packing.</summary>
    public ResourceLimits Limits { get; }
}

// ─────────────────────────────────────────────────────────────────────────────
// 🛡️ MANDATORY AI SELF-AUDIT REPORT
// - Compliance Status: Full — §8 immutable DTO, §2 validation, §9/§10 (limits carried).
// - Architectural & Concurrency Risks: None. Immutable.
// - Security & Trust-Boundary Risks: Limits and root travel with the request so the
//   packer re-enforces traversal/symlink/DoS guards while reading entries.
// - Determinism & Encoding Risks: Files preserve deterministic order for stable zips.
// - Resource Exhaustion Risks: Bounded by Limits.
// - Explicit Assumptions Made: Output path is on a writable local volume.
// - Identified Violations & Deviations: None.
// - Remaining Uncertainty Area: None.
// ─────────────────────────────────────────────────────────────────────────────
