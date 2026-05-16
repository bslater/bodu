// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TempFileScope.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using System.IO;

namespace Bodu.Text.Configuration.Infrastructure;

/// <summary>
/// Disposable wrapper around a single temporary file. Writes initial content on creation and deletes the file
/// on disposal so individual tests do not leak files into the temp directory.
/// </summary>
internal sealed class TempFileScope : IDisposable
{
    private bool _disposed;

    /// <summary>
    /// Initializes a new scope, creating a temporary file containing <paramref name="content" />.
    /// </summary>
    /// <param name="content">The initial file content.</param>
    /// <param name="extension">An optional extension applied to the temp file (no leading dot).</param>
    internal TempFileScope(string content, string? extension = null)
    {
        string baseName = System.IO.Path.Combine(System.IO.Path.GetTempPath(), System.IO.Path.GetRandomFileName());
        this.Path = extension is null ? baseName : System.IO.Path.ChangeExtension(baseName, extension);
        File.WriteAllText(this.Path, content);
    }

    /// <summary>
    /// Gets the absolute path of the temporary file.
    /// </summary>
    internal string Path { get; }

    /// <summary>
    /// Gets the directory that contains the temporary file. Useful when constructing a
    /// <see cref="Microsoft.Extensions.FileProviders.PhysicalFileProvider" /> for tests that exercise the
    /// configuration bridge.
    /// </summary>
    internal string Directory => System.IO.Path.GetDirectoryName(this.Path)!;

    /// <inheritdoc />
    public void Dispose()
    {
        if (this._disposed)
            return;

        this._disposed = true;
        try
        {
            if (File.Exists(this.Path))
                File.Delete(this.Path);
        }
        catch
        {
            // Best-effort cleanup — leaking a temp file is preferable to throwing during teardown.
        }
    }
}
