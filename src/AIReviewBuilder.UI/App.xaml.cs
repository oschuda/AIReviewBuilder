using System.IO;
using System.Windows;
using AIReviewBuilder.Infrastructure.DependencyInjection;
using AIReviewBuilder.UI.Services;
using AIReviewBuilder.UI.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AIReviewBuilder.UI;

/// <summary>
/// WPF application entry point and composition root. Builds the dependency-injection
/// container (Application + Infrastructure services via <see cref="ServiceCollectionExtensions"/>)
/// and resolves the main window. The UI layer holds no domain logic (§6).
/// </summary>
public partial class App : System.Windows.Application
{
    private ServiceProvider? _services;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        string profilesDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "AIReviewBuilder");

        var collection = new ServiceCollection();
        collection.AddLogging(builder => builder.AddDebug().SetMinimumLevel(LogLevel.Information));
        collection.AddAIReviewBuilder(profilesDirectory);
        collection.AddSingleton<IFolderPicker, WpfFolderPicker>();
        collection.AddSingleton<MainViewModel>();
        collection.AddSingleton<MainWindow>();

        _services = collection.BuildServiceProvider();

        MainWindow window = _services.GetRequiredService<MainWindow>();
        window.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _services?.Dispose();
        base.OnExit(e);
    }
}
