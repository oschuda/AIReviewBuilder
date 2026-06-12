using AIReviewBuilder.Domain.Exceptions;

namespace AIReviewBuilder.Domain.ValueObjects;

/// <summary>
/// Immutable, validated Denial-of-Service ceilings enforced across every scanning,
/// estimation, generation, and packing loop (GLOBAL ENGINEERING STANDARD §10;
/// APPLICATION SPEC §5 resource control model).
///
/// Per APPLICATION SPEC §5 these limits are configurable but:
///   * defaults are conservative and documented (see <see cref="Default"/>);
///   * a limit may never be set to 0 (unbounded) or to a negative value;
///   * the per-file ceiling may never exceed the cumulative ceiling.
/// Invalid configuration fails fast (§7 fail-fast configuration).
/// </summary>
public sealed class ResourceLimits
{
    /// <summary>Default per-file ceiling: 5 MB.</summary>
    public const long DefaultMaxSingleFileBytes = 5L * 1024 * 1024;

    /// <summary>Default cumulative payload ceiling: 100 MB.</summary>
    public const long DefaultMaxCumulativeBytes = 100L * 1024 * 1024;

    /// <summary>Default maximum number of files processed per execution: 5,000.</summary>
    public const int DefaultMaxFileCount = 5_000;

    public ResourceLimits(int maxFileCount, long maxSingleFileBytes, long maxCumulativeBytes)
    {
        if (maxFileCount <= 0)
        {
            throw new DomainValidationException(
                $"{nameof(maxFileCount)} must be positive (unbounded limits are forbidden by APPLICATION SPEC §5).");
        }

        if (maxSingleFileBytes <= 0)
        {
            throw new DomainValidationException(
                $"{nameof(maxSingleFileBytes)} must be positive (unbounded limits are forbidden by APPLICATION SPEC §5).");
        }

        if (maxCumulativeBytes <= 0)
        {
            throw new DomainValidationException(
                $"{nameof(maxCumulativeBytes)} must be positive (unbounded limits are forbidden by APPLICATION SPEC §5).");
        }

        if (maxSingleFileBytes > maxCumulativeBytes)
        {
            throw new DomainValidationException(
                $"{nameof(maxSingleFileBytes)} ({maxSingleFileBytes}) must not exceed {nameof(maxCumulativeBytes)} ({maxCumulativeBytes}).");
        }

        MaxFileCount = maxFileCount;
        MaxSingleFileBytes = maxSingleFileBytes;
        MaxCumulativeBytes = maxCumulativeBytes;
    }

    /// <summary>Maximum number of files processed per execution.</summary>
    public int MaxFileCount { get; }

    /// <summary>Maximum size allowed for a single file, in bytes.</summary>
    public long MaxSingleFileBytes { get; }

    /// <summary>Maximum cumulative payload size allowed, in bytes.</summary>
    public long MaxCumulativeBytes { get; }

    /// <summary>The conservative, documented default ceilings.</summary>
    public static ResourceLimits Default { get; } =
        new(DefaultMaxFileCount, DefaultMaxSingleFileBytes, DefaultMaxCumulativeBytes);

    /// <summary>Throws if <paramref name="fileCount"/> exceeds <see cref="MaxFileCount"/>.</summary>
    public void EnsureFileCountWithinLimit(int fileCount)
    {
        if (fileCount > MaxFileCount)
        {
            throw new ResourceLimitExceededException(nameof(MaxFileCount), MaxFileCount, fileCount);
        }
    }

    /// <summary>Throws if a single file's <paramref name="byteCount"/> exceeds <see cref="MaxSingleFileBytes"/>.</summary>
    public void EnsureSingleFileWithinLimit(long byteCount)
    {
        if (byteCount > MaxSingleFileBytes)
        {
            throw new ResourceLimitExceededException(nameof(MaxSingleFileBytes), MaxSingleFileBytes, byteCount);
        }
    }

    /// <summary>Throws if <paramref name="cumulativeByteCount"/> exceeds <see cref="MaxCumulativeBytes"/>.</summary>
    public void EnsureCumulativeWithinLimit(long cumulativeByteCount)
    {
        if (cumulativeByteCount > MaxCumulativeBytes)
        {
            throw new ResourceLimitExceededException(nameof(MaxCumulativeBytes), MaxCumulativeBytes, cumulativeByteCount);
        }
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// 🛡️ MANDATORY AI SELF-AUDIT REPORT
// - Compliance Status: Full — §10 DoS protection; APPLICATION SPEC §5 (no zero,
//   conservative documented defaults, per-file <= cumulative).
// - Architectural & Concurrency Risks: None. Immutable after construction; the
//   shared Default instance is read-only and safe to share (§12).
// - Security & Trust-Boundary Risks: Central enforcement point against resource
//   exhaustion / malicious payloads; fail-fast on misconfiguration (§7).
// - Determinism & Encoding Risks: None.
// - Resource Exhaustion Risks: This type IS the guard; ceilings cannot be disabled.
// - Explicit Assumptions Made: Byte ceilings fit in Int64; counts fit in Int32.
// - Identified Violations & Deviations: None.
// - Remaining Uncertainty Area: Capacity-tested upper bounds (§5) are a deployment
//   concern; this type only enforces that configured values are internally valid.
// ─────────────────────────────────────────────────────────────────────────────
