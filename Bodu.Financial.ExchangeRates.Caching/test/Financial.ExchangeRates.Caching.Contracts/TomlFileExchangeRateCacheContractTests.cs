// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TomlFileExchangeRateCacheContractTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates.Caching.Contracts;

/// <summary>
/// Runs the shared <see cref="IExchangeRateCache" /> contract against the TOML file cache, each test using an isolated
/// temporary directory that is removed on cleanup.
/// </summary>
[TestClass]
public sealed class TomlFileExchangeRateCacheContractTests
    : ExchangeRateCacheContractTests<TomlFileExchangeRateCache>
{
    /// <summary>
    /// The temporary directories created during a test, removed on cleanup.
    /// </summary>
    private readonly List<string> _directories = new();

    /// <inheritdoc />
    protected override TomlFileExchangeRateCache CreateCache()
    {
        string directory = Path.Combine(Path.GetTempPath(), "bodu-exchange-rates-tests", Guid.NewGuid().ToString("N"));
        _directories.Add(directory);
        return new TomlFileExchangeRateCache(new FileExchangeRateCacheOptions { Provider = Provider, CacheDirectory = directory });
    }

    /// <summary>
    /// Removes every temporary directory created during the test.
    /// </summary>
    [TestCleanup]
    public void Cleanup()
    {
        foreach (string directory in _directories)
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
            catch (UnauthorizedAccessException)
            {
                // Best-effort cleanup.
            }
        }
    }
}
