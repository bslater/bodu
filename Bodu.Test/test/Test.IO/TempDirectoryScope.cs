// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TempDirectoryScope.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Test.IO;

/// <summary>
/// A disposable wrapper around a uniquely named temporary directory. Creates the directory on construction and
/// recursively deletes it on disposal so tests that need an on-disk layout do not leak files.
/// </summary>
/// <remarks>
/// This class is intended exclusively for test harness use and must not appear in production code.
/// </remarks>
public sealed class TempDirectoryScope
    : IDisposable
{
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="TempDirectoryScope" /> class, creating an empty temporary
    /// directory.
    /// </summary>
    public TempDirectoryScope()
    {
        Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), System.IO.Path.GetRandomFileName());
        System.IO.Directory.CreateDirectory(Path);
    }

    /// <summary>
    /// Gets the absolute path of the temporary directory.
    /// </summary>
    /// <value>The full path of the directory created for the scope.</value>
    public string Path { get; }

    /// <summary>
    /// Combines the scope directory with <paramref name="name" /> to produce an absolute path, without creating
    /// anything on disk.
    /// </summary>
    /// <param name="name">The file or subdirectory name relative to the scope directory.</param>
    /// <returns>The combined absolute path.</returns>
    public string Combine(string name) =>
        System.IO.Path.Combine(Path, name);

    /// <summary>
    /// Writes <paramref name="content" /> to a file named <paramref name="name" /> inside the scope directory,
    /// overwriting any existing file.
    /// </summary>
    /// <param name="name">The file name relative to the scope directory.</param>
    /// <param name="content">The content to write.</param>
    /// <returns>The absolute path of the written file.</returns>
    public string WriteFile(string name, string content)
    {
        string path = Combine(name);
        File.WriteAllText(path, content);
        return path;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        try
        {
            if (System.IO.Directory.Exists(Path))
                System.IO.Directory.Delete(Path, recursive: true);
        }
        catch
        {
            // Best-effort cleanup — leaking a temp directory is preferable to throwing during teardown.
        }
    }
}
