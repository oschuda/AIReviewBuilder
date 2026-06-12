using System.Collections.Immutable;
using System.Text.Json;
using System.Text.Json.Serialization;
using AIReviewBuilder.Application.Abstractions;
using AIReviewBuilder.Domain.Common;
using AIReviewBuilder.Domain.Entities;
using AIReviewBuilder.Domain.Exceptions;
using AIReviewBuilder.Infrastructure.IO;
using Microsoft.Extensions.Logging;

namespace AIReviewBuilder.Infrastructure.Profiles;

/// <summary>
/// Local JSON-backed <see cref="IReviewProfileStore"/>. Profiles live in a single
/// <c>profiles.json</c> file under a configurable directory. Reads are size-bounded and
/// use a safe, non-polymorphic deserializer (§2). Writes go through the atomic writer
/// (§21). Access is serialized with a semaphore so concurrent callers cannot corrupt the
/// file (§12). Purely local — no network (v1.0).
/// </summary>
public sealed partial class JsonReviewProfileStore : IReviewProfileStore, IDisposable
{
    public const string ProfilesFileName = "profiles.json";

    /// <summary>Hard ceiling on the profiles file to prevent unbounded reads (§10/§23).</summary>
    public const long MaxProfilesFileBytes = 5L * 1024 * 1024;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
        AllowTrailingCommas = false,
        ReadCommentHandling = JsonCommentHandling.Disallow,
        MaxDepth = 32,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private readonly string _filePath;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly ILogger<JsonReviewProfileStore> _logger;
    private bool _disposed;

    public JsonReviewProfileStore(string storageDirectoryPath, ILogger<JsonReviewProfileStore> logger)
    {
        Guard.NotNullOrWhiteSpace(storageDirectoryPath);
        _logger = Guard.NotNull(logger);
        _filePath = Path.Combine(Path.GetFullPath(storageDirectoryPath), ProfilesFileName);
    }

    public async Task<ImmutableArray<ReviewProfile>> GetAllAsync(CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            return await LoadOrSeedUnlockedAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<ReviewProfile?> FindByIdAsync(string id, CancellationToken cancellationToken)
    {
        Guard.NotNullOrWhiteSpace(id);
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            ImmutableArray<ReviewProfile> all = await LoadOrSeedUnlockedAsync(cancellationToken).ConfigureAwait(false);
            return all.FirstOrDefault(p => string.Equals(p.Id, id, StringComparison.Ordinal));
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task SaveAsync(ReviewProfile profile, CancellationToken cancellationToken)
    {
        Guard.NotNull(profile);
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            ImmutableArray<ReviewProfile> existing = await LoadOrSeedUnlockedAsync(cancellationToken).ConfigureAwait(false);

            var byId = new Dictionary<string, ReviewProfile>(StringComparer.Ordinal);
            foreach (ReviewProfile p in existing)
            {
                byId[p.Id] = p;
            }

            byId[profile.Id] = profile;

            ImmutableArray<ReviewProfile> merged = byId.Values
                .OrderBy(p => p.Id, StringComparer.Ordinal)
                .ToImmutableArray();

            await WriteUnlockedAsync(merged, cancellationToken).ConfigureAwait(false);
            LogSaved(profile.Id);
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task<ImmutableArray<ReviewProfile>> LoadOrSeedUnlockedAsync(CancellationToken cancellationToken)
    {
        if (!File.Exists(_filePath))
        {
            await WriteUnlockedAsync(DefaultReviewProfiles.All, cancellationToken).ConfigureAwait(false);
            LogSeeded(DefaultReviewProfiles.All.Length);
            return DefaultReviewProfiles.All;
        }

        var info = new FileInfo(_filePath);
        if (info.Length > MaxProfilesFileBytes)
        {
            throw new ResourceLimitExceededException(nameof(MaxProfilesFileBytes), MaxProfilesFileBytes, info.Length);
        }

        List<ReviewProfileDto>? dtos;
        try
        {
            await using var stream = new FileStream(
                _filePath, FileMode.Open, FileAccess.Read, FileShare.Read, 1 << 16, FileOptions.Asynchronous | FileOptions.SequentialScan);
            dtos = await JsonSerializer.DeserializeAsync<List<ReviewProfileDto>>(stream, JsonOptions, cancellationToken).ConfigureAwait(false);
        }
        catch (JsonException ex)
        {
            throw new InfrastructureException("The profiles store contains invalid JSON.", ex);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            throw new InfrastructureException("Failed to read the profiles store.", ex);
        }

        if (dtos is null)
        {
            return ImmutableArray<ReviewProfile>.Empty;
        }

        var builder = ImmutableArray.CreateBuilder<ReviewProfile>(dtos.Count);
        foreach (ReviewProfileDto dto in dtos)
        {
            builder.Add(MapToEntity(dto));
        }

        return builder.ToImmutable();
    }

    private async Task WriteUnlockedAsync(ImmutableArray<ReviewProfile> profiles, CancellationToken cancellationToken)
    {
        List<ReviewProfileDto> dtos = profiles.Select(MapToDto).ToList();
        await AtomicFileWriter.WriteAsync(
            _filePath,
            async (stream, ct) => await JsonSerializer.SerializeAsync(stream, dtos, JsonOptions, ct).ConfigureAwait(false),
            cancellationToken).ConfigureAwait(false);
    }

    private static ReviewProfile MapToEntity(ReviewProfileDto dto)
    {
        if (!Enum.TryParse(dto.Category, ignoreCase: true, out ReviewProfileCategory category)
            || !Enum.IsDefined(category))
        {
            throw new DomainValidationException($"Profile '{dto.Id}' has an unknown category '{dto.Category}'.");
        }

        // ReviewProfile's constructor enforces the remaining invariants (null/length).
        return new ReviewProfile(
            dto.Id ?? string.Empty,
            dto.Name ?? string.Empty,
            category,
            dto.Prompt ?? string.Empty,
            dto.Description);
    }

    private static ReviewProfileDto MapToDto(ReviewProfile profile) => new()
    {
        Id = profile.Id,
        Name = profile.Name,
        Category = profile.Category.ToString(),
        Prompt = profile.Prompt,
        Description = profile.Description,
    };

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _gate.Dispose();
        _disposed = true;
    }

    [LoggerMessage(EventId = 2400, Level = LogLevel.Information, Message = "Seeded {Count} default review profiles.")]
    private partial void LogSeeded(int count);

    [LoggerMessage(EventId = 2401, Level = LogLevel.Information, Message = "Saved review profile {ProfileId}.")]
    private partial void LogSaved(string profileId);
}

// ─────────────────────────────────────────────────────────────────────────────
// 🛡️ MANDATORY AI SELF-AUDIT REPORT
// - Compliance Status: Full — §2 safe (non-polymorphic) deserialization with bounded
//   depth and no comments/trailing commas; §10/§23 file-size ceiling before read; §21
//   atomic writes; §8 UTF-8 (System.Text.Json default, no BOM) and ordinal ordering;
//   §11 typed errors wrapping JSON/IO failures; §12 semaphore-serialized access; §17
//   id/count-only logging; purely local (no network).
// - Architectural & Concurrency Risks: Non-reentrant SemaphoreSlim; public methods take
//   the gate once and call *Unlocked helpers to avoid self-deadlock. Disposable gate.
// - Security & Trust-Boundary Risks: Invalid/unknown category or missing fields fail
//   fast via the domain entity invariants; no secrets are persisted.
// - Determinism & Encoding Risks: Category serialized by name; profiles sorted by id.
// - Resource Exhaustion Risks: MaxProfilesFileBytes caps reads; depth capped at 32.
// - Explicit Assumptions Made: A single-writer desktop usage; the storage directory is
//   writable and created by the atomic writer on first seed.
// - Identified Violations & Deviations: None.
// - Remaining Uncertainty Area: Cross-process concurrent writers are out of scope; the
//   semaphore guards in-process access only (acceptable for a local desktop app).
// ─────────────────────────────────────────────────────────────────────────────
