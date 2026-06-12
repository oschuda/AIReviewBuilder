using Microsoft.Win32;

namespace AIReviewBuilder.UI.Services;

/// <summary>WPF/Win32 implementation of <see cref="IFolderPicker"/> using OpenFolderDialog.</summary>
public sealed class WpfFolderPicker : IFolderPicker
{
    public string? PickFolder(string title)
    {
        var dialog = new OpenFolderDialog
        {
            Title = title,
            Multiselect = false,
        };

        return dialog.ShowDialog() == true ? dialog.FolderName : null;
    }
}
