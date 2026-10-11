// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CompoundFixtures.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.IO.Compound.Builders;
using Bodu.IO.Compound.PropertySets;

namespace Bodu.IO.Compound;

/// <summary>
/// Builds the sample compound file and loads the embedded compound-file fixtures used by the <c>Bodu.IO.Compound</c>
/// test suite.
/// </summary>
internal static class CompoundFixtures
{
    /// <summary>
    /// Builds the sample compound file: a small container shaped like an Office document, holding a <c>Workbook</c>
    /// stream in the regular sectors, a summary information property set in the mini stream, and a storage with a
    /// stream of its own.
    /// </summary>
    /// <returns>The bytes of the container.</returns>
    /// <remarks>
    /// The tests build the sample rather than commit a real document, so that every byte of it has a known origin.
    /// </remarks>
    public static byte[] BuildSample()
    {
        var root = CompoundStorageBuilder.CreateRoot();
        _ = root.AddStream("Workbook", CompoundContainerBytes.Pattern(6000));
        _ = root.AddStream(SummaryInformation.StreamName, new SummaryInformationBuilder { Title = "Sample", Author = "Bodu" }.ToArray());
        _ = root.AddStorage("Storage").AddStream("Stream", CompoundContainerBytes.Pattern(100));
        return root.ToArray();
    }

    /// <summary>
    /// Opens the sample compound file as a seekable, read-only stream.
    /// </summary>
    /// <returns>A <see cref="MemoryStream" /> positioned at the start of the sample.</returns>
    public static MemoryStream OpenSample() =>
        new(BuildSample(), writable: false);

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
