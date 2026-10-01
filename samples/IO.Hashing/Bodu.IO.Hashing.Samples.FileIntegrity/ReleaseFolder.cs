// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ReleaseFolder.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;

namespace Bodu.IO.Hashing.Samples.FileIntegrity;

/// <summary>
/// A temporary folder holding three release files with fixed contents, deleted again when disposed.
/// </summary>
/// <remarks>
/// The files are generated rather than committed so the scenarios can damage them freely; every run writes the same
/// bytes, so every digest the sample prints is reproducible. The folder's own path is never printed, because it
/// differs from run to run.
/// </remarks>
public sealed class ReleaseFolder : IDisposable
{
    /// <summary>The names of the release files, in manifest order.</summary>
    public static readonly IReadOnlyList<string> FileNames = ["readme.txt", "app.dat", "assets.pak"];

    /// <summary>
    /// Initializes a new instance of the <see cref="ReleaseFolder" /> class over an existing directory.
    /// </summary>
    /// <param name="path">The directory the release files are written to.</param>
    private ReleaseFolder(string path) => Path = path;

    /// <summary>Gets the full path of the folder.</summary>
    public string Path { get; }

    /// <summary>
    /// Creates a new temporary folder and writes the release files into it.
    /// </summary>
    /// <returns>The populated folder.</returns>
    public static ReleaseFolder Create()
    {
        var folder = new ReleaseFolder(Directory.CreateTempSubdirectory("bodu-fileintegrity-").FullName);

        var readme = new StringBuilder();
        for (var line = 1; line <= 40; line++)
            readme.Append(FormattableString.Invariant($"Line {line}: the quick brown fox jumps over the lazy dog.\n"));

        File.WriteAllText(folder.GetPath("readme.txt"), readme.ToString(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        File.WriteAllBytes(folder.GetPath("app.dat"), Pattern(96 * 1024, multiplier: 7));
        File.WriteAllBytes(folder.GetPath("assets.pak"), Pattern(256 * 1024, multiplier: 13));

        return folder;
    }

    /// <summary>
    /// Returns the full path of a file inside the folder.
    /// </summary>
    /// <param name="fileName">The file name.</param>
    /// <returns>The combined path.</returns>
    public string GetPath(string fileName) =>
        System.IO.Path.Combine(Path, fileName);

    /// <summary>
    /// Deletes the folder and everything in it.
    /// </summary>
    public void Dispose()
    {
        if (Directory.Exists(Path))
            Directory.Delete(Path, recursive: true);
    }

    /// <summary>
    /// Creates a deterministic byte pattern.
    /// </summary>
    /// <param name="length">The number of bytes.</param>
    /// <param name="multiplier">The per-index multiplier that distinguishes one file's contents from another's.</param>
    /// <returns>The pattern.</returns>
    private static byte[] Pattern(int length, int multiplier)
    {
        var bytes = new byte[length];
        for (var i = 0; i < length; i++)
            bytes[i] = (byte)((i * multiplier) ^ (i >> 10));

        return bytes;
    }
}
