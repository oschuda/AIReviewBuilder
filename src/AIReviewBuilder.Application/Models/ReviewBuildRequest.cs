using AIReviewBuilder.Domain.Common;
using AIReviewBuilder.Domain.Entities;
using AIReviewBuilder.Domain.ValueObjects;

namespace AIReviewBuilder.Application.Models;

/// <summary>
/// High-level, immutable request describing an end-to-end review build: scan a
/// repository, then optionally emit <c>review.md</c> and/or a source <c>.zip</c> bundle.
/// </summary>
public sealed class ReviewBuildRequest
{
    public const string DefaultReviewFileName = "review.md";
    public const string DefaultBundleFileName = "source-bundle.zip";

    public ReviewBuildRequest(
        string repositoryRootPath,
        string repositoryName,
        ReviewProfile profile,
        string outputDirectoryPath,
        FileSelectionCriteria? criteria = null,
        ResourceLimits? limits = null,
        bool generateMarkdown = true,
        bool generateSourceBundle = false,
        bool includeFileContents = true)
    {
        RepositoryRootPath = Guard.NotNullOrWhiteSpace(repositoryRootPath);
        RepositoryName = Guard.NotNullOrWhiteSpace(repositoryName);
        Profile = Guard.NotNull(profile);
        OutputDirectoryPath = Guard.NotNullOrWhiteSpace(outputDirectoryPath);
        Criteria = criteria ?? FileSelectionCriteria.Default;
        Limits = limits ?? ResourceLimits.Default;
        GenerateMarkdown = generateMarkdown;
        GenerateSourceBundle = generateSourceBundle;
        IncludeFileContents = includeFileContents;

        if (!generateMarkdown && !generateSourceBundle)
        {
            throw new Domain.Exceptions.DomainValidationException(
                "At least one output (Markdown or source bundle) must be requested.");
        }
    }

    public string RepositoryRootPath { get; }

    public string RepositoryName { get; }

    public ReviewProfile Profile { get; }

    public string OutputDirectoryPath { get; }

    public FileSelectionCriteria Criteria { get; }

    public ResourceLimits Limits { get; }

    public bool GenerateMarkdown { get; }

    public bool GenerateSourceBundle { get; }

    public bool IncludeFileContents { get; }
}

// ─────────────────────────────────────────────────────────────────────────────
// 🛡️ MANDATORY AI SELF-AUDIT REPORT
// - Compliance Status: Full — §8 immutable DTO; §2 validation; §7 conservative
//   defaults (Default criteria/limits).
// - Architectural & Concurrency Risks: None. Immutable.
// - Security & Trust-Boundary Risks: Carries root + limits so downstream stages
//   re-enforce guards; rejects a no-op request (no output) fail-fast.
// - Determinism & Encoding Risks: None.
// - Resource Exhaustion Risks: Defaults to ResourceLimits.Default when unset.
// - Explicit Assumptions Made: Output directory will be created if missing by the
//   orchestrator/infrastructure.
// - Identified Violations & Deviations: None.
// - Remaining Uncertainty Area: None.
// ─────────────────────────────────────────────────────────────────────────────
