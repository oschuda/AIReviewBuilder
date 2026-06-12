using System.Security.Cryptography;
using AIReviewBuilder.Domain.Common;
using AIReviewBuilder.Domain.Exceptions;

namespace AIReviewBuilder.Infrastructure.IO;

/// <summary>
/// Writes a file atomically: content is streamed to an isolated temporary file in the
/// destination directory, fully flushed, hashed (SHA-256) only after all writes complete,
/// and then promoted to the final path via <see cref="File.Move(string, string, bool)"/>.
/// This satisfies GLOBAL ENGINEERING STANDARD "Atomic File Generation" and APPLICATION
/// SPEC §21 (temp file → hash on finalized content → atomic move).
/// </summary>
public static class AtomicFileWriter
{
    private const int CopyBufferSize = 81_920; // 80 KB, below the LOH threshold (§18).

    /// <summary>
    /// Invokes <paramref name="writeContent"/> against a temporary file, then atomically
    /// promotes it to <paramref name="finalPath"/>. Returns the lowercase hex SHA-256 of
    /// the finalized bytes. The temp file is always cleaned up on failure.
    /// </summary>
    public static async Task<string> WriteAsync(
        string finalPath,
        Func<Stream, CancellationToken, Task> writeContent,
        CancellationToken cancellationToken)
    {
        Guard.NotNullOrWhiteSpace(finalPath);
        Guard.NotNull(writeContent);
        cancellationToken.ThrowIfCancellationRequested();

        string fullFinalPath = Path.GetFullPath(finalPath);
        string? directory = Path.GetDirectoryName(fullFinalPath);
        if (string.IsNullOrEmpty(directory))
        {
            throw new InfrastructureException($"Output path '{finalPath}' has no parent directory.");
        }

        Directory.CreateDirectory(directory);

        // Temp file in the SAME directory guarantees File.Move is an atomic rename
        // (not a cross-volume copy).
        string tempPath = Path.Combine(directory, $".{Path.GetFileName(fullFinalPath)}.{Guid.NewGuid():N}.tmp");

        try
        {
            await using (var stream = new FileStream(
                tempPath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                CopyBufferSize,
                FileOptions.Asynchronous))
            {
                await writeContent(stream, cancellationToken).ConfigureAwait(false);
                await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
            }

            string hash = await ComputeSha256Async(tempPath, cancellationToken).ConfigureAwait(false);

            File.Move(tempPath, fullFinalPath, overwrite: true);
            return hash;
        }
        catch (Exception ex) when (ex is not OperationCanceledException and not DomainException)
        {
            throw new InfrastructureException($"Atomic write to '{Path.GetFileName(fullFinalPath)}' failed.", ex);
        }
        finally
        {
            TryDeleteTemp(tempPath);
        }
    }

    private static async Task<string> ComputeSha256Async(string path, CancellationToken cancellationToken)
    {
        await using var stream = new FileStream(
            path,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            CopyBufferSize,
            FileOptions.Asynchronous | FileOptions.SequentialScan);

        byte[] hash = await SHA256.HashDataAsync(stream, cancellationToken).ConfigureAwait(false);
        return Convert.ToHexStringLower(hash);
    }

    private static void TryDeleteTemp(string tempPath)
    {
        try
        {
            if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }
        }
        catch (IOException)
        {
            // Best-effort cleanup; a stray temp file must never mask the original error.
        }
        catch (UnauthorizedAccessException)
        {
            // Same rationale.
        }
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// 🛡️ MANDATORY AI SELF-AUDIT REPORT
// - Compliance Status: Full — "Atomic File Generation"; APPLICATION SPEC §21 (temp →
//   flush → hash finalized content → move); §34 SHA-256; §11 (wraps technical errors,
//   never swallows — note the catch rethrows as InfrastructureException with inner).
// - Architectural & Concurrency Risks: Unique GUID temp name avoids collisions across
//   concurrent writers; FileShare.None prevents interleaving (§12).
// - Security & Trust-Boundary Risks: Temp file is hidden (dot-prefixed) and removed on
//   all paths; final move is overwrite-atomic so no partial file is ever observable.
// - Determinism & Encoding Risks: Hash is hex lowercase; encoding is decided by the
//   content writer (callers use UTF-8 without BOM).
// - Resource Exhaustion Risks: 80 KB buffer stays below the LOH threshold (§18);
//   async streaming avoids loading whole files into memory.
// - Explicit Assumptions Made: Destination directory and temp share one volume so the
//   move is a rename; OperationCanceled/DomainException propagate unwrapped.
// - Identified Violations & Deviations: None.
// - Remaining Uncertainty Area: On crash between move and return, the file is already
//   the valid finalized content; no half-written artifact can remain.
// ─────────────────────────────────────────────────────────────────────────────
