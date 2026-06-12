using System.Windows;
using AIReviewBuilder.UI.ViewModels;

namespace AIReviewBuilder.UI;

/// <summary>Code-behind for the main window. Wires the injected view model as DataContext only.</summary>
public partial class MainWindow : Window
{
    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        Loaded += async (_, _) => await viewModel.LoadProfilesCommand.ExecuteAsync(null).ConfigureAwait(true);
    }
}
