using System.IO.Compression;
using AIReviewBuilder.Application.Abstractions;
using AIReviewBuilder.Application.Models;
using AIReviewBuilder.Domain.Common;
using AIReviewBuilder.Domain.Exceptions;
using AIReviewBuilder.Domain.Services;
using AIReviewBuilder.Infrastructure.IO;
using Microsoft.Extensions.Logging;

namespace AIReviewBuilder.Infrastructure.Packaging;

/// <summary>
/// Packs the clean, filtered source files into a structural <c>.zip</c> archive using
/// <see cref="System.IO.Compression"/> (project spec "Local Repository Packer"). Each
/// entry path is re-validated within the repository root, symlinks are skipped, and the
/// DoS ceilings are re-enforced before any bytes are read. The archive is produced
/// atomically with an integrity hash (§21) and uses a fixed entry timestamp so the same
/// inputs yield a reproducible archive (§8 determinism, ISO 9001 reproducibility).
/// </summary>
public sealed partial class ZipSourceBundlePacker : ISourceBundlePacker
{
    private const int CopyBufferSize = 81_920; // 80 KB, below LOH threshold (§18).

    // Fixed, culture-invariant timestamp for reproducible archives.
    private static readonly DateTimeOffset DeterministicEntryTimestamp =
        new(2020, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private readonly ILogger<ZipSourceBundlePacker> _logger;

    public ZipSourceBundlePacker(ILogger<ZipSourceBundlePacker> logger)
    {
        _logger = Guard.NotNull(logger);
    }

    public async Task<SourceBundleResult> PackAsync(SourceBundleRequest request, CancellationToken cancellationToken)
    {
        Guard.NotNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        string root = Path.TrimEndingDirectorySeparator(Path.GetFullPath(request.RepositoryRootPath));
        var start = System.Diagnostics.Stopwatch.StartNew();

        long cumulativeBytes = 0;
        int packedCount = 0;

        string hash = await AtomicFileWriter.WriteAsync(
            request.OutputArchivePath,
            async (stream, ct) =>
            {
                using var archive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true);

                foreach (var file in request.Files)
                {
                    ct.ThrowIfCancellationRequested();

                    string absolutePath = PathBoundaryGuard.ResolveWithinRoot(root, file.RelativePath);
                    var info = new FileInfo(absolutePath);
                    if (!info.Exists || FileSystemSafety.IsReparsePoint(info))
                    {
                        continue;
                    }

                    request.Limits.EnsureSingleFileWithinLimit(info.Length);

                    packedCount++;
                    request.Limits.EnsureFileCountWithinLimit(packedCount);

                    cumulativeBytes += info.Length;
                    request.Limits.EnsureCumulativeWithinLimit(cumulativeBytes);

                    ZipArchiveEntry entry = archive.CreateEntry(file.RelativePath, CompressionLevel.Optimal);
                    entry.LastWriteTime = DeterministicEntryTimestamp;

                    await CopyFileToEntryAsync(absolutePath, entry, ct).ConfigureAwait(false);
                }
            },
            cancellationToken).ConfigureAwait(false);

        start.Stop();
        LogPacked(packedCount, cumulativeBytes);

        return new SourceBundleResult(
            Path.GetFullPath(request.OutputArchivePath),
            packedCount,
            cumulativeBytes,
            hash,
            start.Elapsed);
    }

    private static async Task CopyFileToEntryAsync(string absolutePath, ZipArchiveEntry entry, CancellationToken ct)
    {
        try
        {
            await using var source = new FileStream(
                absolutePath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                CopyBufferSize,
                FileOptions.Asynchronous | FileOptions.SequentialScan);

            await using Stream entryStream = entry.Open();
            await source.CopyToAsync(entryStream, CopyBufferSize, ct).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            throw new InfrastructureException("Failed to read a file while packing the source bundle.", ex);
        }
    }

    [LoggerMessage(EventId = 2300, Level = LogLevel.Information,
        Message = "Source bundle packed {FileCount} files ({ByteCount} uncompressed bytes).")]
    private partial void LogPacked(int fileCount, long byteCount);
}

// ─────────────────────────────────────────────────────────────────────────────
// 🛡️ MANDATORY AI SELF-AUDIT REPORT
// - Compliance Status: Full — System.IO.Compression packer; §21 atomic + post-flush
//   hash; §9 per-entry containment + symlink skip; §10 ceilings verified before read;
//   §8 deterministic entry order/timestamps and '/'-separated names; §17 metadata-only
//   logging; §21/§24 async with ConfigureAwait(false) + token.
// - Architectural & Concurrency Risks: Local accumulators only; no shared mutable state
//   (§12). ZipArchive leaveOpen:true so AtomicFileWriter owns the underlying stream.
// - Security & Trust-Boundary Risks: Entry names come from already-normalised relative
//   paths (no '..', no rooting — enforced by ScannedFile and re-resolved within root),
//   preventing zip-slip on extraction; symlinks skipped to avoid leaking external files.
// - Determinism & Encoding Risks: Fixed entry timestamp + ordered input → reproducible
//   archive bytes (modulo zip library framing).
// - Resource Exhaustion Risks: Size checked before opening each file; streamed copy with
//   an 80 KB buffer; cumulative/count ceilings enforced.
// - Explicit Assumptions Made: request.Files is already filtered/validated by the scan;
//   re-validation here is defense-in-depth.
// - Identified Violations & Deviations: None.
// - Remaining Uncertainty Area: Cross-zip-tool byte-for-byte reproducibility depends on
//   the BCL DeflateStream version; entry metadata is pinned, content is deterministic.
// ─────────────────────────────────────────────────────────────────────────────
