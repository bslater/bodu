// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ExcelBinaryFixtures.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Formats.Excel.Binary;

/// <summary>
/// Loads embedded BIFF8 workbook fixtures used by the <c>Bodu.Formats.Excel.Binary</c> test suite.
/// </summary>
internal static class ExcelBinaryFixtures
{
    /// <summary>
    /// The file name of the real-world sample BIFF8 workbook (an RBA exchange-rate workbook).
    /// </summary>
    public const string SampleBiff8 = "sample-biff8.xls";

    /// <summary>
    /// Opens an embedded fixture as a seekable, read-only stream.
    /// </summary>
    /// <param name="fileName">The fixture file name, for example <c>sample-biff8.xls</c>.</param>
    /// <returns>A <see cref="MemoryStream" /> positioned at the start of the fixture content.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the fixture is not embedded in the test assembly.
    /// </exception>
    public static MemoryStream OpenStream(string fileName)
    {
        var resourceName = "Bodu.Formats.Excel.Binary.Fixtures." + fileName;
        using Stream stream = typeof(ExcelBinaryFixtures).Assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Missing embedded fixture '{resourceName}'.");
        MemoryStream buffer = new();
        stream.CopyTo(buffer);
        buffer.Position = 0;

        return buffer;
    }
}
