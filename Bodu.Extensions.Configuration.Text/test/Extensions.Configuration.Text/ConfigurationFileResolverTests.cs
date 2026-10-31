// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConfigurationFileResolverTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions.Configuration.Text;

/// <summary>
/// Verifies that <see cref="ConfigurationFileResolver" /> locates a file-backed source's file as <c>AddJsonFile</c>
/// does - a rooted path from the deepest directory on it that exists, any other path through the builder's file
/// provider - and opens it through the file provider or, without one, the file system.
/// </summary>
[TestClass]
public sealed partial class ConfigurationFileResolverTests
{
    /// <summary>The content written to the files the tests resolve.</summary>
    private const string Content = "name = \"resolved\"\n";

    /// <summary>
    /// Returns a directory path in the form <see cref="Microsoft.Extensions.FileProviders.PhysicalFileProvider.Root" />
    /// reports it: fully qualified, without a trailing separator.
    /// </summary>
    /// <param name="directory">The directory path to normalize.</param>
    /// <returns>The normalized path.</returns>
    private static string Normalize(string directory) =>
        Path.TrimEndingDirectorySeparator(Path.GetFullPath(directory));
}
