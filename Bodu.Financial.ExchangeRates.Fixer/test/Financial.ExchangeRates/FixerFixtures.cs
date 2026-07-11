// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FixerFixtures.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates;

/// <summary>
/// Loads embedded Fixer fixtures used by the <c>Bodu.Financial.ExchangeRates.Fixer</c> test suite.
/// </summary>
internal static class FixerFixtures
{
    /// <summary>The prefix under which fixtures are embedded.</summary>
    private const string ResourcePrefix = "Bodu.Financial.ExchangeRates.Fixer.Fixtures.";

    /// <summary>The file name of the sample EUR/USD time-series response (early January 2023).</summary>
    public const string EurUsdTimeSeries = "eurusd-timeseries.json";

    /// <summary>The file name of the sample EUR/USD single-date response.</summary>
    public const string EurUsdHistorical = "eurusd-historical.json";

    /// <summary>The file name of the sample invalid-access-key error response.</summary>
    public const string ErrorInvalidKey = "error-invalid-key.json";

    /// <summary>
    /// Reads the raw bytes of an embedded fixture.
    /// </summary>
    /// <param name="fileName">The fixture file name, for example <c>eurusd-timeseries.json</c>.</param>
    /// <returns>The fixture content.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the fixture is not embedded in the test assembly.
    /// </exception>
    public static byte[] ReadBytes(string fileName)
    {
        using Stream stream = OpenResource(fileName);
        using MemoryStream buffer = new();
        stream.CopyTo(buffer);

        return buffer.ToArray();
    }

    /// <summary>
    /// Opens the embedded resource for a fixture file name.
    /// </summary>
    /// <param name="fileName">The fixture file name.</param>
    /// <returns>The resource stream.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the fixture is not embedded.</exception>
    private static Stream OpenResource(string fileName) =>
        typeof(FixerFixtures).Assembly.GetManifestResourceStream(ResourcePrefix + fileName)
            ?? throw new InvalidOperationException($"Missing embedded fixture '{ResourcePrefix + fileName}'.");
}
