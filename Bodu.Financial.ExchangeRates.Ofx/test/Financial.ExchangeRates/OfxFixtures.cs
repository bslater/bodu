// ---------------------------------------------------------------------------------------------------------------
// <copyright file="OfxFixtures.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates;

/// <summary>
/// Loads embedded OFX fixtures used by the <c>Bodu.Financial.ExchangeRates.Ofx</c> test suite.
/// </summary>
internal static class OfxFixtures
{
    /// <summary>The prefix under which fixtures are embedded.</summary>
    private const string ResourcePrefix = "Bodu.Financial.ExchangeRates.Ofx.Fixtures.";

    /// <summary>The file name of the sample AUD/USD spot-rate-history response (early January 2023).</summary>
    public const string AudUsd = "audusd-ofx.json";

    /// <summary>
    /// Reads the raw bytes of an embedded fixture.
    /// </summary>
    /// <param name="fileName">The fixture file name, for example <c>audusd-ofx.json</c>.</param>
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
        typeof(OfxFixtures).Assembly.GetManifestResourceStream(ResourcePrefix + fileName)
            ?? throw new InvalidOperationException($"Missing embedded fixture '{ResourcePrefix + fileName}'.");
}
