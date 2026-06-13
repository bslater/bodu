// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RbaFixtures.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates.Rba;

/// <summary>
/// Loads embedded RBA workbook fixtures used by the <c>Bodu.Financial.ExchangeRates.Rba</c> test suite.
/// </summary>
internal static class RbaFixtures
{
    /// <summary>
    /// The file name of the real-world sample RBA workbook (the 2023-to-current era).
    /// </summary>
    public const string Sample = "rba-2023-current.xls";

    /// <summary>
    /// Reads the raw bytes of an embedded fixture.
    /// </summary>
    /// <param name="fileName">The fixture file name, for example <c>rba-2023-current.xls</c>.</param>
    /// <returns>The fixture content.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the fixture is not embedded in the test assembly.
    /// </exception>
    public static byte[] ReadBytes(string fileName)
    {
        var resourceName = "Bodu.Financial.ExchangeRates.Rba.Fixtures." + fileName;
        using Stream stream = typeof(RbaFixtures).Assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Missing embedded fixture '{resourceName}'.");
        using MemoryStream buffer = new();
        stream.CopyTo(buffer);

        return buffer.ToArray();
    }

    /// <summary>
    /// Opens an embedded fixture as a seekable, read-only stream.
    /// </summary>
    /// <param name="fileName">The fixture file name, for example <c>rba-2023-current.xls</c>.</param>
    /// <returns>A <see cref="MemoryStream" /> positioned at the start of the fixture content.</returns>
    public static MemoryStream OpenStream(string fileName) =>
        new(ReadBytes(fileName), writable: false);
}
