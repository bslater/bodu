// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CompoundStreamAccessTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Compound;

/// <summary>
/// Verifies opening and reading a named stream through <see cref="CompoundStorage.OpenStream(string)" />.
/// </summary>
[TestClass]
public class CompoundStreamAccessTests
{
    /// <summary>
    /// Opens the <c>Workbook</c> stream of the Excel reference fixture.
    /// </summary>
    /// <param name="file">Receives the open compound file, which the caller must dispose.</param>
    /// <returns>The opened workbook stream cursor.</returns>
    private static CompoundStream OpenWorkbook(out CompoundFile file)
    {
        file = CompoundFile.Open(CompoundFixtures.OpenReference("valid/sample1.xls"));
        return file.RootStorage.OpenStream("Workbook");
    }

    /// <summary>
    /// Verifies that <see cref="CompoundStorage.OpenStream(string)" /> returns a read-only, seekable stream of the
    /// declared length.
    /// </summary>
    [TestMethod]
    public void OpenStream_WhenWorkbookEntry_ShouldReturnReadOnlySeekableStream()
    {
        using CompoundStream stream = OpenWorkbook(out CompoundFile file);
        using (file)
        {
            Assert.IsTrue(stream.CanRead);
            Assert.IsTrue(stream.CanSeek);
            Assert.IsFalse(stream.CanWrite);
            Assert.AreEqual(stream.Stat.Length, stream.Length);
            Assert.IsGreaterThan(0, stream.Length);
        }
    }

    /// <summary>
    /// Verifies that <see cref="CompoundStream.ReadAllBytes" /> returns exactly the declared number of bytes.
    /// </summary>
    [TestMethod]
    public void ReadAllBytes_WhenWorkbookEntry_ShouldReturnDeclaredLength()
    {
        using CompoundStream stream = OpenWorkbook(out CompoundFile file);
        using (file)
        {
            ReadOnlyMemory<byte> bytes = stream.ReadAllBytes();

            Assert.AreEqual(stream.Length, bytes.Length);
        }
    }

    /// <summary>
    /// Verifies that the stream's <see cref="CompoundStream.Stat" /> reports the stream type and name.
    /// </summary>
    [TestMethod]
    public void Stat_WhenWorkbookEntry_ShouldReportStreamType()
    {
        using CompoundStream stream = OpenWorkbook(out CompoundFile file);
        using (file)
        {
            CompoundEntryInfo stat = stream.Stat;

            Assert.AreEqual(CompoundEntryType.Stream, stat.EntryType);
            Assert.AreEqual("Workbook", stat.Name);
            Assert.AreEqual(stream.Length, stat.Length);
        }
    }
}
