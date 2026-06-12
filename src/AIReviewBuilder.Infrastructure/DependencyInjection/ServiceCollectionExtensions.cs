using AIReviewBuilder.Application.Abstractions;
using AIReviewBuilder.Application.Services;
using AIReviewBuilder.Domain.Common;
using AIReviewBuilder.Infrastructure.Discovery;
using AIReviewBuilder.Infrastructure.Markdown;
using AIReviewBuilder.Infrastructure.Packaging;
using AIReviewBuilder.Infrastructure.Profiles;
using AIReviewBuilder.Infrastructure.Scanning;
using Microsoft.Extensions.DependencyInjection;

namespace AIReviewBuilder.Infrastructure.DependencyInjection;

/// <summary>
/// Composition root for the AI Review Builder. Registers the Application orchestrator and
/// every Infrastructure implementation behind its Application interface (§3 dependency
/// inversion). All services are stateless or internally synchronized and are registered
/// as singletons (§12).
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the full Phase 1 service graph. <paramref name="profilesDirectoryPath"/>
    /// is the local directory where <c>profiles.json</c> is stored.
    /// </summary>
    public static IServiceCollection AddAIReviewBuilder(this IServiceCollection services, string profilesDirectoryPath)
    {
        Guard.NotNull(services);
        Guard.NotNullOrWhiteSpace(profilesDirectoryPath);

        services.AddSingleton<IRepositoryDiscoveryService, FileSystemRepositoryDiscoveryService>();
        services.AddSingleton<IRepositoryFileScanner, FileSystemRepositoryScanner>();
        services.AddSingleton<IReviewDocumentGenerator, MarkdownReviewDocumentGenerator>();
        services.AddSingleton<ISourceBundlePacker, ZipSourceBundlePacker>();

        services.AddSingleton<IReviewProfileStore>(sp =>
            new JsonReviewProfileStore(
                profilesDirectoryPath,
                sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<JsonReviewProfileStore>>()));

        services.AddSingleton<IReviewOrchestrator, ReviewOrchestrator>();

        return services;
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// 🛡️ MANDATORY AI SELF-AUDIT REPORT
// - Compliance Status: Full — §3 dependency inversion; §12 singleton stateless services.
// - Architectural & Concurrency Risks: All registered types are thread-safe (stateless
//   or semaphore-guarded); singleton lifetime is therefore correct.
// - Security & Trust-Boundary Risks: None added here; wiring only.
// - Determinism & Encoding Risks: None.
// - Resource Exhaustion Risks: None.
// - Explicit Assumptions Made: Caller supplies a writable profiles directory and an
//   ILoggerFactory is registered (e.g. via AddLogging) by the host.
// - Identified Violations & Deviations: None.
// - Remaining Uncertainty Area: None.
// ─────────────────────────────────────────────────────────────────────────────
