// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FredFixtures.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates;

/// <summary>
/// Loads embedded FRED fixtures used by the <c>Bodu.Financial.ExchangeRates.Fred</c> test suite.
/// </summary>
internal static class FredFixtures
{
    /// <summary>The prefix under which fixtures are embedded.</summary>
    private const string ResourcePrefix = "Bodu.Financial.ExchangeRates.Fred.Fixtures.";

    /// <summary>The file name of the sample DEXUSEU (EUR/USD) observations response (early January 2023).</summary>
    public const string DexUsEu = "dexuseu-2023.json";

    /// <summary>The file name of a valid response object carrying no observations array.</summary>
    public const string ErrorEmpty = "error-empty.json";

    /// <summary>
    /// Reads the raw bytes of an embedded fixture.
    /// </summary>
    /// <param name="fileName">The fixture file name, for example <c>dexuseu-2023.json</c>.</param>
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
        typeof(FredFixtures).Assembly.GetManifestResourceStream(ResourcePrefix + fileName)
            ?? throw new InvalidOperationException($"Missing embedded fixture '{ResourcePrefix + fileName}'.");
}
