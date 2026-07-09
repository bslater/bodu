// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FileSystemRbaWorkbookCacheTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates;

/// <summary>
/// Verifies the freshness and persistence behavior of <see cref="FileSystemRbaWorkbookCache" />.
/// </summary>
[TestClass]
public partial class FileSystemRbaWorkbookCacheTests
{
    private static readonly RbaEraWorkbook s_immutableEra = new("2018-2022", new DateOnly(2018, 1, 1), new DateOnly(2022, 12, 31));
    private static readonly RbaEraWorkbook s_currentEra = new("2023-current", new DateOnly(2023, 1, 1), null);

    private string _directory = string.Empty;

    /// <summary>
    /// Creates a unique cache directory for each test.
    /// </summary>
    [TestInitialize]
    public void Initialize()
    {
        _directory = Path.Combine(Path.GetTempPath(), "bodu-rba-test-" + Guid.NewGuid().ToString("N"));
    }

    /// <summary>
    /// Removes the cache directory after each test.
    /// </summary>
    [TestCleanup]
    public void Cleanup()
    {
        if (Directory.Exists(_directory))
            Directory.Delete(_directory, recursive: true);
    }

}
