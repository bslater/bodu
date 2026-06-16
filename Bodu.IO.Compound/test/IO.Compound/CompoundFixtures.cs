// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CompoundFixtures.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Compound;

/// <summary>
/// Loads embedded compound-file fixtures used by the <c>Bodu.IO.Compound</c> test suite.
/// </summary>
internal static class CompoundFixtures
{
    /// <summary>
    /// The file name of the real-world sample compound file (an RBA exchange-rate workbook).
    /// </summary>
    public const string SampleCompound = "sample-compound.xls";

    /// <summary>
    /// Reads the raw bytes of an embedded fixture.
    /// </summary>
    /// <param name="fileName">The fixture file name, for example <c>sample-compound.xls</c>.</param>
    /// <returns>The fixture content.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the fixture is not embedded in the test assembly.
    /// </exception>
    public static byte[] ReadBytes(string fileName)
    {
        string resourceName = "Bodu.IO.Compound.Fixtures." + fileName;
        using Stream stream = typeof(CompoundFixtures).Assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Missing embedded fixture '{resourceName}'.");
        using MemoryStream buffer = new();
        stream.CopyTo(buffer);

        return buffer.ToArray();
    }

    /// <summary>
    /// Opens an embedded fixture as a seekable, read-only stream.
    /// </summary>
    /// <param name="fileName">The fixture file name, for example <c>sample-compound.xls</c>.</param>
    /// <returns>A <see cref="MemoryStream" /> positioned at the start of the fixture content.</returns>
    public static MemoryStream OpenStream(string fileName) =>
        new(ReadBytes(fileName), writable: false);
}
