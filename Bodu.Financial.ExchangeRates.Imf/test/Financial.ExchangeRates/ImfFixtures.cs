// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ImfFixtures.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates;

/// <summary>
/// Loads embedded IMF fixtures used by the <c>Bodu.Financial.ExchangeRates.Imf</c> test suite.
/// </summary>
internal static class ImfFixtures
{
    /// <summary>The prefix under which fixtures are embedded.</summary>
    private const string ResourcePrefix = "Bodu.Financial.ExchangeRates.Imf.Fixtures.";

    /// <summary>The file name of the sample April 2026 Representative Exchange Rates report.</summary>
    public const string Rep202604 = "rep-2026-04.tsv";

    /// <summary>The file name of a malformed report that carries no recognizable header row.</summary>
    public const string ErrorNoTsv = "error-notsv.txt";

    /// <summary>
    /// Reads the raw bytes of an embedded fixture.
    /// </summary>
    /// <param name="fileName">The fixture file name, for example <c>rep-2026-04.tsv</c>.</param>
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
    /// Reads the text of an embedded fixture.
    /// </summary>
    /// <param name="fileName">The fixture file name, for example <c>rep-2026-04.tsv</c>.</param>
    /// <returns>The fixture text.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the fixture is not embedded in the test assembly.
    /// </exception>
    public static string ReadText(string fileName)
    {
        using Stream stream = OpenResource(fileName);
        using StreamReader reader = new(stream);

        return reader.ReadToEnd();
    }

    /// <summary>
    /// Opens the embedded resource for a fixture file name.
    /// </summary>
    /// <param name="fileName">The fixture file name.</param>
    /// <returns>The resource stream.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the fixture is not embedded.</exception>
    private static Stream OpenResource(string fileName) =>
        typeof(ImfFixtures).Assembly.GetManifestResourceStream(ResourcePrefix + fileName)
            ?? throw new InvalidOperationException($"Missing embedded fixture '{ResourcePrefix + fileName}'.");
}
