using System.Collections.ObjectModel;
using System.Globalization;
using AIReviewBuilder.Application.Abstractions;
using AIReviewBuilder.Application.Models;
using AIReviewBuilder.Domain.Entities;
using AIReviewBuilder.Domain.Exceptions;
using AIReviewBuilder.UI.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;

namespace AIReviewBuilder.UI.ViewModels;

/// <summary>
/// Main window view model. Orchestrates the local-only Phase 1 workflow (load profiles →
/// scan → build review.md / source bundle) by delegating to the Application services. All
/// validation, security, and resource limits live in lower layers; this class only marshals
/// user intent and reports status (§6 UI separation).
/// </summary>
public sealed partial class MainViewModel : ObservableObject
{
    private readonly IReviewProfileStore _profileStore;
    private readonly IRepositoryFileScanner _scanner;
    private readonly IReviewOrchestrator _orchestrator;
    private readonly IFolderPicker _folderPicker;
    private readonly ILogger<MainViewModel> _logger;

    public MainViewModel(
        IReviewProfileStore profileStore,
        IRepositoryFileScanner scanner,
        IReviewOrchestrator orchestrator,
        IFolderPicker folderPicker,
        ILogger<MainViewModel> logger)
    {
        _profileStore = profileStore;
        _scanner = scanner;
        _orchestrator = orchestrator;
        _folderPicker = folderPicker;
        _logger = logger;
    }

    public ObservableCollection<ReviewProfile> Profiles { get; } = [];

    public ObservableCollection<ScannedFile> ScannedFiles { get; } = [];

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ScanCommand))]
    [NotifyCanExecuteChangedFor(nameof(BuildCommand))]
    private string _repositoryRootPath = string.Empty;

    [ObservableProperty]
    private string _outputDirectoryPath = string.Empty;

    [ObservableProperty]
    private ReviewProfile? _selectedProfile;

    [ObservableProperty]
    private bool _generateMarkdown = true;

    [ObservableProperty]
    private bool _generateSourceBundle;

    [ObservableProperty]
    private bool _includeFileContents = true;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ScanCommand))]
    [NotifyCanExecuteChangedFor(nameof(BuildCommand))]
    private bool _isBusy;

    [ObservableProperty]
    private string _statusMessage = "Ready.";

    [ObservableProperty]
    private int _fileCount;

    [ObservableProperty]
    private long _totalBytes;

    [ObservableProperty]
    private int _approximateTokens;

    [RelayCommand]
    private async Task LoadProfilesAsync(CancellationToken cancellationToken)
    {
        try
        {
            IsBusy = true;
            StatusMessage = "Loading profiles…";
            var profiles = await _profileStore.GetAllAsync(cancellationToken).ConfigureAwait(true);

            Profiles.Clear();
            foreach (ReviewProfile profile in profiles)
            {
                Profiles.Add(profile);
            }

            SelectedProfile = Profiles.Count > 0 ? Profiles[0] : null;
            StatusMessage = string.Create(CultureInfo.CurrentCulture, $"Loaded {Profiles.Count} profiles.");
        }
        catch (DomainException ex)
        {
            StatusMessage = $"Failed to load profiles: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand(CanExecute = nameof(CanOperate))]
    private async Task ScanAsync(CancellationToken cancellationToken)
    {
        try
        {
            IsBusy = true;
            StatusMessage = "Scanning repository…";

            FileScanResult result = await _scanner
                .ScanAsync(RepositoryRootPath, Domain.ValueObjects.FileSelectionCriteria.Default, Domain.ValueObjects.ResourceLimits.Default, cancellationToken)
                .ConfigureAwait(true);

            ScannedFiles.Clear();
            foreach (ScannedFile file in result.Files)
            {
                ScannedFiles.Add(file);
            }

            FileCount = result.FileCount;
            TotalBytes = result.Aggregate.ByteCount;
            ApproximateTokens = result.Aggregate.ApproximateTokenCount;
            StatusMessage = string.Create(CultureInfo.CurrentCulture, $"Scan complete: {FileCount} files, ~{ApproximateTokens} tokens.");
        }
        catch (DomainException ex)
        {
            StatusMessage = $"Scan failed: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand(CanExecute = nameof(CanOperate))]
    private async Task BuildAsync(CancellationToken cancellationToken)
    {
        if (SelectedProfile is null)
        {
            StatusMessage = "Select a review profile first.";
            return;
        }

        string outputDirectory = string.IsNullOrWhiteSpace(OutputDirectoryPath)
            ? RepositoryRootPath
            : OutputDirectoryPath;

        try
        {
            IsBusy = true;
            StatusMessage = "Building review artifacts…";

            var request = new ReviewBuildRequest(
                RepositoryRootPath,
                System.IO.Path.GetFileName(System.IO.Path.TrimEndingDirectorySeparator(RepositoryRootPath)),
                SelectedProfile,
                outputDirectory,
                criteria: null,
                limits: null,
                generateMarkdown: GenerateMarkdown,
                generateSourceBundle: GenerateSourceBundle,
                includeFileContents: IncludeFileContents);

            ReviewBuildResult result = await _orchestrator.BuildAsync(request, cancellationToken).ConfigureAwait(true);

            FileCount = result.Scan.FileCount;
            TotalBytes = result.Scan.Aggregate.ByteCount;
            ApproximateTokens = result.Scan.Aggregate.ApproximateTokenCount;

            string documentInfo = result.Document is null ? "no document" : $"review.md ({result.Document.ContentSha256[..12]}…)";
            string bundleInfo = result.Bundle is null ? "no bundle" : $"bundle ({result.Bundle.ArchiveSha256[..12]}…)";
            StatusMessage = $"Build complete [{result.CorrelationId[..8]}]: {documentInfo}, {bundleInfo}.";
        }
        catch (DomainException ex)
        {
            StatusMessage = $"Build failed: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void BrowseRepository()
    {
        string? selected = _folderPicker.PickFolder("Select repository root");
        if (selected is not null)
        {
            RepositoryRootPath = selected;
        }
    }

    [RelayCommand]
    private void BrowseOutput()
    {
        string? selected = _folderPicker.PickFolder("Select output directory");
        if (selected is not null)
        {
            OutputDirectoryPath = selected;
        }
    }

    private bool CanOperate() => !IsBusy && !string.IsNullOrWhiteSpace(RepositoryRootPath);
}
