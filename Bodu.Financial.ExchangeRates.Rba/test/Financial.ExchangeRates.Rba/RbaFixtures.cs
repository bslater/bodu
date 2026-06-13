// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RbaFixtures.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Linq;

namespace Bodu.Financial.ExchangeRates.Rba;

/// <summary>
/// Loads embedded RBA fixtures used by the <c>Bodu.Financial.ExchangeRates.Rba</c> test suite.
/// </summary>
internal static class RbaFixtures
{
    /// <summary>
    /// The prefix under which fixtures are embedded.
    /// </summary>
    private const string ResourcePrefix = "Bodu.Financial.ExchangeRates.Rba.Fixtures.";

    /// <summary>
    /// The file name of the real-world sample RBA workbook (the 2023-to-current era). The fixture is embedded under the
    /// RBA's own file name so it matches both <see cref="RbaEra.FileName" /> and a known-answer row's source file.
    /// </summary>
    public const string Sample = "2023-current.xls";

    /// <summary>
    /// The file name of the embedded known-answer data set.
    /// </summary>
    public const string KnownAnswerData = "rba-known-answers.json";

    /// <summary>
    /// Reads the raw bytes of an embedded fixture.
    /// </summary>
    /// <param name="fileName">The fixture file name, for example <c>2023-current.xls</c>.</param>
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
    /// <param name="fileName">The fixture file name, for example <c>2023-current.xls</c>.</param>
    /// <returns>A <see cref="MemoryStream" /> positioned at the start of the fixture content.</returns>
    public static MemoryStream OpenStream(string fileName) =>
        new(ReadBytes(fileName), writable: false);

    /// <summary>
    /// Reads the text of an embedded fixture, stripping any byte-order mark.
    /// </summary>
    /// <param name="fileName">The fixture file name, for example <c>rba-known-answers.json</c>.</param>
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
    /// Lists the workbook (<c>.xls</c>) fixtures currently embedded in the test assembly.
    /// </summary>
    /// <returns>The set of embedded workbook file names, compared without case sensitivity.</returns>
    public static IReadOnlySet<string> EmbeddedWorkbookFileNames() =>
        typeof(RbaFixtures).Assembly.GetManifestResourceNames()
            .Where(name => name.StartsWith(ResourcePrefix, StringComparison.Ordinal)
                && name.EndsWith(".xls", StringComparison.OrdinalIgnoreCase))
            .Select(name => name[ResourcePrefix.Length..])
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Opens the embedded resource for a fixture file name.
    /// </summary>
    /// <param name="fileName">The fixture file name.</param>
    /// <returns>The resource stream.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the fixture is not embedded.</exception>
    private static Stream OpenResource(string fileName) =>
        typeof(RbaFixtures).Assembly.GetManifestResourceStream(ResourcePrefix + fileName)
            ?? throw new InvalidOperationException($"Missing embedded fixture '{ResourcePrefix + fileName}'.");
}
