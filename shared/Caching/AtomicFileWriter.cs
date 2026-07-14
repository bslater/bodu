// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AtomicFileWriter.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Caching;

/// <summary>
/// Writes text to a file atomically by staging it in a uniquely named temporary file in the same directory and moving
/// it into place, so a concurrent reader never observes a partially written file.
/// </summary>
/// <remarks>
/// <para>
/// The temporary file shares the destination directory so the final move is a same-volume rename rather than a copy,
/// and it is removed on failure so a crashed or rejected write leaves no orphan behind. The write failure is rethrown
/// so a best-effort caller can decide whether to swallow it.
/// </para>
/// <para>
/// This type is a shared cache-infrastructure primitive: it is linked as source into each caching package that needs it
/// (namespace <c>Bodu.Caching</c>) and compiled <see langword="internal" /> per assembly, so no public surface is
/// exposed and no two assemblies collide on the type identity.
/// </para>
/// </remarks>
internal static class AtomicFileWriter
{
    /// <summary>
    /// Writes <paramref name="text" /> to <paramref name="path" /> atomically by writing to a uniquely named temporary
    /// file in the same directory and moving it into place.
    /// </summary>
    /// <param name="path">The destination file path.</param>
    /// <param name="text">The text to write.</param>
    /// <exception cref="IOException">Thrown when the underlying write or move fails.</exception>
    /// <exception cref="UnauthorizedAccessException">
    /// Thrown when the process lacks permission to write the file.
    /// </exception>
    public static void Write(string path, string text)
    {
        string tempPath = $"{path}.{Guid.NewGuid():N}.tmp";

        try
        {
            File.WriteAllText(tempPath, text);
            File.Move(tempPath, path, overwrite: true);
        }
        catch
        {
            TryDelete(tempPath);
            throw;
        }
    }

    /// <summary>
    /// Deletes the file at <paramref name="path" /> if it exists, swallowing file-system failures so cleanup never
    /// masks the original error.
    /// </summary>
    /// <param name="path">The path of the file to delete.</param>
    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path))
                File.Delete(path);
        }
        catch (IOException)
        {
            // Best-effort cleanup: a failure to remove the temp file must not mask the original write error.
        }
        catch (UnauthorizedAccessException)
        {
            // Best-effort cleanup: a failure to remove the temp file must not mask the original write error.
        }
    }
}
