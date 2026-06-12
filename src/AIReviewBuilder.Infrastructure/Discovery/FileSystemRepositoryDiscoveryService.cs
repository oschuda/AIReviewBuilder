using System.Collections.Immutable;
using AIReviewBuilder.Application.Abstractions;
using AIReviewBuilder.Application.Models;
using AIReviewBuilder.Domain.Common;
using AIReviewBuilder.Domain.Entities;
using AIReviewBuilder.Domain.Exceptions;
using AIReviewBuilder.Domain.Services;
using AIReviewBuilder.Infrastructure.IO;
using Microsoft.Extensions.Logging;

namespace AIReviewBuilder.Infrastructure.Discovery;

/// <summary>
/// File-system implementation of <see cref="IRepositoryDiscoveryService"/>. Performs a
/// bounded, iterative walk beneath a search root and flags directories that expose a
/// marker artifact (.git, .sln, .csproj, pyproject.toml, package.json). Once a directory
/// is recognised as a repository it is not descended into further. Symlinks are skipped
/// and every path is contained within the search root (§9).
/// </summary>
public sealed partial class FileSystemRepositoryDiscoveryService : IRepositoryDiscoveryService
{
    private readonly ILogger<FileSystemRepositoryDiscoveryService> _logger;

    public FileSystemRepositoryDiscoveryService(ILogger<FileSystemRepositoryDiscoveryService> logger)
    {
        _logger = Guard.NotNull(logger);
    }

    public Task<ImmutableArray<RepositoryLocation>> DiscoverAsync(
        string searchRootPath,
        int maxDepth,
        CancellationToken cancellationToken)
    {
        Guard.NotNullOrWhiteSpace(searchRootPath);
        if (maxDepth <= 0)
        {
            throw new DomainValidationException($"{nameof(maxDepth)} must be positive.");
        }

        cancellationToken.ThrowIfCancellationRequested();

        string root = Path.TrimEndingDirectorySeparator(Path.GetFullPath(searchRootPath));
        if (!Directory.Exists(root))
        {
            throw new InfrastructureException($"Search root directory does not exist: '{searchRootPath}'.");
        }

        var found = new List<RepositoryLocation>();
        var stack = new Stack<(string Path, int Depth)>();
        stack.Push((root, 0));

        while (stack.Count > 0)
        {
            cancellationToken.ThrowIfCancellationRequested();
            (string currentDir, int depth) = stack.Pop();

            ImmutableArray<RepositoryArtifact> artifacts = DetectArtifacts(currentDir);
            if (!artifacts.IsEmpty)
            {
                string name = Path.GetFileName(currentDir);
                if (string.IsNullOrEmpty(name))
                {
                    name = currentDir;
                }

                found.Add(new RepositoryLocation(currentDir, new DiscoveredRepository(name, artifacts)));
                continue; // Do not descend into a recognised repository.
            }

            if (depth >= maxDepth)
            {
                continue;
            }

            foreach (DirectoryInfo subDir in EnumerateSubdirectories(currentDir))
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (FileSystemSafety.IsReparsePoint(subDir))
                {
                    continue;
                }

                string contained = PathBoundaryGuard.EnsureWithinRoot(root, subDir.FullName);
                stack.Push((contained, depth + 1));
            }
        }

        ImmutableArray<RepositoryLocation> ordered = found
            .OrderBy(r => r.AbsoluteRootPath, StringComparer.Ordinal)
            .ToImmutableArray();

        LogDiscovery(ordered.Length);
        return Task.FromResult(ordered);
    }

    private static ImmutableArray<RepositoryArtifact> DetectArtifacts(string directory)
    {
        var builder = ImmutableArray.CreateBuilder<RepositoryArtifact>();
        try
        {
            if (Directory.Exists(Path.Combine(directory, ".git")))
            {
                builder.Add(RepositoryArtifact.GitRepository);
            }

            if (HasFileMatching(directory, "*.sln"))
            {
                builder.Add(RepositoryArtifact.VisualStudioSolution);
            }

            if (HasFileMatching(directory, "*.csproj"))
            {
                builder.Add(RepositoryArtifact.CSharpProject);
            }

            if (File.Exists(Path.Combine(directory, "pyproject.toml")))
            {
                builder.Add(RepositoryArtifact.PythonProject);
            }

            if (File.Exists(Path.Combine(directory, "package.json")))
            {
                builder.Add(RepositoryArtifact.NodeProject);
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            throw new InfrastructureException("Failed to inspect a directory during repository discovery.", ex);
        }

        return builder.ToImmutable();
    }

    private static bool HasFileMatching(string directory, string pattern)
    {
        using IEnumerator<string> enumerator =
            Directory.EnumerateFiles(directory, pattern, SearchOption.TopDirectoryOnly).GetEnumerator();
        return enumerator.MoveNext();
    }

    private static DirectoryInfo[] EnumerateSubdirectories(string directory)
    {
        try
        {
            return new DirectoryInfo(directory)
                .EnumerateDirectories()
                .OrderBy(d => d.Name, StringComparer.Ordinal)
                .ToArray();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            throw new InfrastructureException("Failed to enumerate subdirectories during discovery.", ex);
        }
    }

    [LoggerMessage(EventId = 2100, Level = LogLevel.Information,
        Message = "Repository discovery found {RepositoryCount} repositories.")]
    private partial void LogDiscovery(int repositoryCount);
}

// ─────────────────────────────────────────────────────────────────────────────
// 🛡️ MANDATORY AI SELF-AUDIT REPORT
// - Compliance Status: Full — §9 (containment + symlink skip), §10 (depth bound), §8
//   (ordinal ordering), §11 (typed IO errors), §17 (count-only logging), §21 (token).
// - Architectural & Concurrency Risks: Iterative walk; no shared mutable state (§12).
// - Security & Trust-Boundary Risks: Every descended directory is contained within the
//   search root; reparse points skipped to avoid escaping via junctions.
// - Determinism & Encoding Risks: Results sorted by ordinal path; artifact order fixed
//   by DiscoveredRepository.
// - Resource Exhaustion Risks: maxDepth caps recursion; HasFileMatching short-circuits
//   on the first match instead of materialising all matches.
// - Explicit Assumptions Made: A directory with any marker is a repository root and is
//   not descended into; this avoids walking large trees and nested-repo ambiguity.
// - Identified Violations & Deviations: Returns a completed Task (no awaits) because the
//   walk is synchronous I/O; the async contract is preserved without fake awaits.
// - Remaining Uncertainty Area: Monorepos containing multiple nested solutions surface
//   only the outermost recognised root; acceptable for Phase 1.
// ─────────────────────────────────────────────────────────────────────────────
