// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FileNotableDateCacheContractTestsBase.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Globalization.Calendar.Caching;

namespace Bodu.Globalization.Calendar.Caching.Contracts;

/// <summary>
/// Extends the <see cref="NotableDateCacheContractTests" /> with a per-test temporary directory that the file-backed
/// contract subclasses store into and that is removed after each test.
/// </summary>
public abstract class FileNotableDateCacheContractTestsBase
    : NotableDateCacheContractTests
{
    /// <summary>The temporary directory created for the running test.</summary>
    private string? _directory;

    /// <summary>
    /// Creates the per-test temporary directory.
    /// </summary>
    [TestInitialize]
    public void Initialize()
    {
        _directory = Path.Combine(Path.GetTempPath(), "bodu-notable-dates-contract", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_directory);
    }

    /// <summary>
    /// Removes the per-test temporary directory.
    /// </summary>
    [TestCleanup]
    public void Cleanup()
    {
        if (_directory is not null && Directory.Exists(_directory))
            Directory.Delete(_directory, recursive: true);
    }

    /// <inheritdoc />
    protected override INotableDateCache CreateCache() =>
        CreateCache(new FileNotableDateCacheOptions { CacheDirectory = _directory });

    /// <summary>
    /// Creates the concrete file cache over the supplied options.
    /// </summary>
    /// <param name="options">The file-cache options bound to the per-test directory.</param>
    /// <returns>A new file cache instance.</returns>
    protected abstract INotableDateCache CreateCache(FileNotableDateCacheOptions options);
}
