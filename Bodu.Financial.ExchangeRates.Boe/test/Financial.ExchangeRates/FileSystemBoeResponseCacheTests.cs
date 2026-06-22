// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FileSystemBoeResponseCacheTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates;

/// <summary>
/// Verifies the freshness and resilience behavior of <see cref="FileSystemBoeResponseCache" />.
/// </summary>
[TestClass]
public partial class FileSystemBoeResponseCacheTests
{
    /// <summary>
    /// The inclusive start of the range used by the cache tests.
    /// </summary>
    private static readonly DateOnly s_from = new(2023, 1, 1);

    /// <summary>
    /// The inclusive end of the range used by the cache tests.
    /// </summary>
    private static readonly DateOnly s_to = new(2023, 1, 31);

    /// <summary>
    /// Creates a unique temporary directory for a test.
    /// </summary>
    /// <returns>The directory path.</returns>
    private static string CreateTempDirectory() =>
        Path.Combine(Path.GetTempPath(), "bodu-boe-test", Guid.NewGuid().ToString("N"));

    /// <summary>
    /// Removes a temporary directory, ignoring failures.
    /// </summary>
    /// <param name="directory">The directory to remove.</param>
    private static void Cleanup(string directory)
    {
        try
        {
            if (Directory.Exists(directory))
                Directory.Delete(directory, recursive: true);
        }
        catch (IOException)
        {
            // Best-effort cleanup.
        }
    }
}
