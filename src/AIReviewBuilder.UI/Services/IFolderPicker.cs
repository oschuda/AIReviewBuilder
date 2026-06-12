namespace AIReviewBuilder.UI.Services;

/// <summary>
/// Abstraction over the native folder-selection dialog so the view model stays free of
/// direct Win32/WPF dialog dependencies (testability, §6 UI separation).
/// </summary>
public interface IFolderPicker
{
    /// <summary>Returns the selected absolute folder path, or null if cancelled.</summary>
    string? PickFolder(string title);
}
