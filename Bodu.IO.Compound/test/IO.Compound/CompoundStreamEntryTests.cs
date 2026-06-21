// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CompoundStreamEntryTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Compound;

/// <summary>
/// Verifies the behavior of <see cref="CompoundStreamEntry" />.
/// </summary>
[TestClass]
public class CompoundStreamEntryTests
{
    /// <summary>
    /// Opens the <c>Workbook</c> stream of the Excel reference fixture.
    /// </summary>
    /// <param name="file">Receives the open compound file, which the caller must dispose.</param>
    /// <returns>The workbook stream entry.</returns>
    private static CompoundStreamEntry OpenWorkbook(out CompoundFile file)
    {
        file = CompoundFile.Open(CompoundFixtures.OpenReference("valid/sample1.xls"));
        return file.RootStorage.OpenStream("Workbook");
    }

    /// <summary>
    /// Verifies that <see cref="CompoundStreamEntry.Open" /> returns a readable stream of the declared length.
    /// </summary>
    [TestMethod]
    public void Open_WhenWorkbookEntry_ShouldReturnStreamOfDeclaredLength()
    {
        CompoundStreamEntry entry = OpenWorkbook(out CompoundFile file);
        using (file)
        {
            using CompoundStream stream = entry.Open();

            Assert.AreEqual(entry.Length, stream.Length);
            Assert.IsTrue(stream.CanRead);
            Assert.IsTrue(stream.CanSeek);
            Assert.IsFalse(stream.CanWrite);
        }
    }

    /// <summary>
    /// Verifies that <see cref="CompoundStreamEntry.ReadAllBytes" /> returns exactly the declared number of bytes.
    /// </summary>
    [TestMethod]
    public void ReadAllBytes_WhenWorkbookEntry_ShouldReturnDeclaredLength()
    {
        CompoundStreamEntry entry = OpenWorkbook(out CompoundFile file);
        using (file)
        {
            ReadOnlyMemory<byte> bytes = entry.ReadAllBytes();

            Assert.AreEqual(entry.Length, bytes.Length);
        }
    }

    /// <summary>
    /// Verifies that the stream entry's <see cref="CompoundStreamEntry.Stat" /> reports the stream type.
    /// </summary>
    [TestMethod]
    public void Stat_WhenWorkbookEntry_ShouldReportStreamType()
    {
        CompoundStreamEntry entry = OpenWorkbook(out CompoundFile file);
        using (file)
        {
            CompoundEntryInfo stat = entry.Stat;

            Assert.AreEqual(CompoundEntryType.Stream, stat.EntryType);
            Assert.AreEqual("Workbook", stat.Name);
            Assert.AreEqual(entry.Length, stat.Length);
        }
    }
}
