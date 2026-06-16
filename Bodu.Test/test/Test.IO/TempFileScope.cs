// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TempFileScope.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Test.IO;

/// <summary>
/// A disposable wrapper around a single temporary file. Writes the supplied content on creation and deletes the file on
/// disposal so individual tests do not leak files into the temp directory.
/// </summary>
/// <remarks>
/// This class is intended exclusively for test harness use and must not appear in production code.
/// </remarks>
public sealed class TempFileScope
    : IDisposable
{
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="TempFileScope" /> class, creating a temporary file containing
    /// <paramref name="content" />.
    /// </summary>
    /// <param name="content">The initial file content.</param>
    /// <param name="extension">An optional extension applied to the temp file, without a leading dot.</param>
    public TempFileScope(string content, string? extension = null)
    {
        string baseName = System.IO.Path.Combine(System.IO.Path.GetTempPath(), System.IO.Path.GetRandomFileName());
        Path = extension is null ? baseName : System.IO.Path.ChangeExtension(baseName, extension);
        File.WriteAllText(Path, content);
    }

    /// <summary>
    /// Gets the absolute path of the temporary file.
    /// </summary>
    /// <returns>The full path of the file created for the scope.</returns>
    public string Path { get; }

    /// <summary>
    /// Gets the directory that contains the temporary file.
    /// </summary>
    /// <returns>The absolute path of the containing directory.</returns>
    public string Directory => System.IO.Path.GetDirectoryName(Path)!;

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        try
        {
            if (File.Exists(Path))
                File.Delete(Path);
        }
        catch
        {
            // Best-effort cleanup — leaking a temp file is preferable to throwing during teardown.
        }
    }
}
