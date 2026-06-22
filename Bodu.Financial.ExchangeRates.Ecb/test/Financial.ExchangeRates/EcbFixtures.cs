// ---------------------------------------------------------------------------------------------------------------
// <copyright file="EcbFixtures.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates;

/// <summary>
/// Loads embedded ECB fixtures used by the <c>Bodu.Financial.ExchangeRates.Ecb</c> test suite.
/// </summary>
internal static class EcbFixtures
{
    /// <summary>The prefix under which fixtures are embedded.</summary>
    private const string ResourcePrefix = "Bodu.Financial.ExchangeRates.Ecb.Fixtures.";

    /// <summary>The file name of the sample <c>eurofxref</c> history feed, embedded under the ECB's own file name.</summary>
    public const string Sample = "eurofxref-hist.xml";

    /// <summary>
    /// Reads the raw bytes of an embedded fixture.
    /// </summary>
    /// <param name="fileName">The fixture file name, for example <c>eurofxref-hist.xml</c>.</param>
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
    /// Opens an embedded fixture as a seekable, read-only stream.
    /// </summary>
    /// <param name="fileName">The fixture file name, for example <c>eurofxref-hist.xml</c>.</param>
    /// <returns>A <see cref="MemoryStream" /> positioned at the start of the fixture content.</returns>
    public static MemoryStream OpenStream(string fileName) =>
        new(ReadBytes(fileName), writable: false);

    /// <summary>
    /// Reads the text of an embedded fixture.
    /// </summary>
    /// <param name="fileName">The fixture file name, for example <c>eurofxref-hist.xml</c>.</param>
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
        typeof(EcbFixtures).Assembly.GetManifestResourceStream(ResourcePrefix + fileName)
            ?? throw new InvalidOperationException($"Missing embedded fixture '{ResourcePrefix + fileName}'.");
}
