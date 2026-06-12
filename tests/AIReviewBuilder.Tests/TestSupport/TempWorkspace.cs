using System.Text;

namespace AIReviewBuilder.Tests.TestSupport;

/// <summary>
/// Disposable, isolated temporary directory for integration tests that touch the file
/// system. Always cleaned up on dispose.
/// </summary>
public sealed class TempWorkspace : IDisposable
{
    private static readonly UTF8Encoding Utf8NoBom = new(false);

    public TempWorkspace()
    {
        Root = Path.Combine(Path.GetTempPath(), "airb-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Root);
    }

    public string Root { get; }

    public string WriteFile(string relativePath, string content)
    {
        string full = Path.Combine(Root, relativePath.Replace('/', Path.DirectorySeparatorChar));
        string dir = Path.GetDirectoryName(full)!;
        Directory.CreateDirectory(dir);
        File.WriteAllText(full, content, Utf8NoBom);
        return full;
    }

    public string WriteBytes(string relativePath, byte[] content)
    {
        string full = Path.Combine(Root, relativePath.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(full)!);
        File.WriteAllBytes(full, content);
        return full;
    }

    public string Combine(string relativePath) =>
        Path.Combine(Root, relativePath.Replace('/', Path.DirectorySeparatorChar));

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(Root))
            {
                Directory.Delete(Root, recursive: true);
            }
        }
        catch (IOException)
        {
            // Best-effort cleanup.
        }
        catch (UnauthorizedAccessException)
        {
            // Best-effort cleanup.
        }
    }
}
