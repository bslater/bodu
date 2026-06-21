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
    /// <summary>The file name of the real-world sample compound file (an RBA exchange-rate workbook).</summary>
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

    /// <summary>
    /// Reads the raw bytes of an embedded reference-corpus fixture.
    /// </summary>
    /// <param name="relativePath">The corpus-relative path, for example <c>valid/clean.dat</c>.</param>
    /// <returns>The fixture content.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the fixture is not embedded in the test assembly.
    /// </exception>
    public static byte[] ReadReferenceBytes(string relativePath)
    {
        string resourceName = "Bodu.IO.Compound.Fixtures.Reference." + relativePath.Replace('/', '.');
        using Stream stream = typeof(CompoundFixtures).Assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Missing embedded reference fixture '{resourceName}'.");
        using MemoryStream buffer = new();
        stream.CopyTo(buffer);

        return buffer.ToArray();
    }

    /// <summary>
    /// Opens an embedded reference-corpus fixture as a seekable, read-only stream.
    /// </summary>
    /// <param name="relativePath">The corpus-relative path, for example <c>valid/clean.dat</c>.</param>
    /// <returns>A <see cref="MemoryStream" /> positioned at the start of the fixture content.</returns>
    public static MemoryStream OpenReference(string relativePath) =>
        new(ReadReferenceBytes(relativePath), writable: false);

    /// <summary>
    /// Reads the raw bytes of an embedded writer golden fixture.
    /// </summary>
    /// <param name="fileName">The golden fixture file name, for example <c>golden-v3.cfb</c>.</param>
    /// <returns>The fixture content.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the fixture is not embedded.</exception>
    public static byte[] ReadWriterGolden(string fileName)
    {
        string resourceName = "Bodu.IO.Compound.Fixtures.Writer." + fileName;
        using Stream stream = typeof(CompoundFixtures).Assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Missing embedded golden fixture '{resourceName}'.");
        using MemoryStream buffer = new();
        stream.CopyTo(buffer);

        return buffer.ToArray();
    }

    /// <summary>
    /// Reads the embedded valid-fixture manifest as a UTF-8 string.
    /// </summary>
    /// <returns>The manifest JSON text.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the manifest is not embedded.</exception>
    public static string ReadValidManifestJson()
    {
        const string resourceName = "Bodu.IO.Compound.Fixtures.Reference.manifest.valid.json";
        using Stream stream = typeof(CompoundFixtures).Assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Missing embedded manifest '{resourceName}'.");
        using StreamReader reader = new(stream);

        return reader.ReadToEnd();
    }
}
