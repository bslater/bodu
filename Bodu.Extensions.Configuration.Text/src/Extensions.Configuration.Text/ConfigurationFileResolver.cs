// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConfigurationFileResolver.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;

namespace Bodu.Extensions.Configuration.Text;

/// <summary>
/// Locates and opens the file behind a file-backed TOML or Bencode configuration source the way <c>AddJsonFile</c>
/// locates its file.
/// </summary>
internal static class ConfigurationFileResolver
{
    /// <summary>
    /// Resolves a configuration file path to the file provider that serves it and the path within that provider.
    /// </summary>
    /// <param name="builder">The configuration builder whose file provider serves a relative path.</param>
    /// <param name="path">The configured file path.</param>
    /// <returns>The file provider and the path of the file within it.</returns>
    /// <remarks>
    /// This mirrors <see cref="FileConfigurationSource.ResolveFileProvider" />, which <c>AddJsonFile</c> calls, and
    /// <see cref="FileConfigurationSource.EnsureDefaults(IConfigurationBuilder)" />: a rooted path is served by a
    /// <see cref="PhysicalFileProvider" /> at the deepest directory on it that exists, and any other path by the
    /// builder's file provider, which is the provider <c>SetFileProvider</c> sets, the base path <c>SetBasePath</c>
    /// sets, or by default the application's base directory.
    /// </remarks>
    internal static (IFileProvider FileProvider, string Path) Resolve(IConfigurationBuilder builder, string path)
    {
        if (System.IO.Path.IsPathRooted(path))
        {
            string? directory = System.IO.Path.GetDirectoryName(path);
            string pathToFile = System.IO.Path.GetFileName(path);

            // Walk up to a directory that exists, so a file in a directory created later is still found under it.
            while (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                pathToFile = System.IO.Path.Combine(System.IO.Path.GetFileName(directory), pathToFile);
                directory = System.IO.Path.GetDirectoryName(directory);
            }

            if (Directory.Exists(directory))
                return (new PhysicalFileProvider(directory), pathToFile);
        }

        return (builder.GetFileProvider(), path);
    }

    /// <summary>
    /// Opens a configuration file for reading, or reports that it does not exist.
    /// </summary>
    /// <param name="fileProvider">
    /// The file provider that serves the file, or <see langword="null" /> to read <paramref name="path" /> through the
    /// file system, relative to the current directory.
    /// </param>
    /// <param name="path">The path of the file, within <paramref name="fileProvider" /> when one is supplied.</param>
    /// <returns>A stream over the file's contents, or <see langword="null" /> when the file does not exist.</returns>
    internal static Stream? OpenRead(IFileProvider? fileProvider, string path)
    {
        if (fileProvider is null)
            return File.Exists(path) ? File.OpenRead(path) : null;

        IFileInfo file = fileProvider.GetFileInfo(path);
        return file.Exists ? file.CreateReadStream() : null;
    }
}
