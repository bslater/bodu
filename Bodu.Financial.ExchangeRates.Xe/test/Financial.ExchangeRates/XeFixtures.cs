// ---------------------------------------------------------------------------------------------------------------
// <copyright file="XeFixtures.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;

namespace Bodu.Financial.ExchangeRates;

/// <summary>
/// Loads embedded XE fixtures used by the <c>Bodu.Financial.ExchangeRates.Xe</c> test suite.
/// </summary>
internal static class XeFixtures
{
    /// <summary>The prefix under which fixtures are embedded.</summary>
    private const string ResourcePrefix = "Bodu.Financial.ExchangeRates.Xe.Fixtures.";

    /// <summary>The file name of the sample AUD/USD charting-rates response (early January 2023).</summary>
    public const string AudUsd = "audusd-charting-rates.json";

    /// <summary>The file name of a response whose <c>batchList</c> array is empty.</summary>
    public const string EmptyBatchList = "empty-batchlist.json";

    /// <summary>The file name of the bootstrap page that names the current XE application script chunk.</summary>
    public const string Bootstrap = "xe-bootstrap.html";

    /// <summary>The file name of the application script chunk that embeds the authorization credential.</summary>
    public const string AppChunk = "xe-app-chunk.js";

    /// <summary>
    /// Reads the raw bytes of an embedded fixture.
    /// </summary>
    /// <param name="fileName">The fixture file name, for example <c>audusd-charting-rates.json</c>.</param>
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
    /// Reads the UTF-8 text of an embedded fixture.
    /// </summary>
    /// <param name="fileName">The fixture file name.</param>
    /// <returns>The fixture content as text.</returns>
    public static string ReadText(string fileName) =>
        Encoding.UTF8.GetString(ReadBytes(fileName));

    /// <summary>
    /// Opens the embedded resource for a fixture file name.
    /// </summary>
    /// <param name="fileName">The fixture file name.</param>
    /// <returns>The resource stream.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the fixture is not embedded.</exception>
    private static Stream OpenResource(string fileName) =>
        typeof(XeFixtures).Assembly.GetManifestResourceStream(ResourcePrefix + fileName)
            ?? throw new InvalidOperationException($"Missing embedded fixture '{ResourcePrefix + fileName}'.");
}
