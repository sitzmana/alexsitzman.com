namespace Portfolio.Content;

/// <summary>
/// Raised when a content file cannot be read. Carries the source path so the
/// build failure points at the file the author needs to fix.
/// </summary>
public sealed class ContentException : Exception
{
    public ContentException(string sourcePath, string message, Exception? inner = null)
        : base($"{sourcePath}: {message}", inner)
    {
        SourcePath = sourcePath;
    }

    public string SourcePath { get; }
}
