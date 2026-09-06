// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PstTableRowTests.TryOpenCellStream.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Pst;

public partial class PstTableRowTests
{
    /// <summary>
    /// Verifies that every present cell streams the same bytes its materialized value carries and reports the same
    /// length.
    /// </summary>
    [TestMethod]
    public void TryOpenCellStream_WhenCellPresent_ShouldMatchCellValue()
    {
        (PstFile file, PstTableContext context) = PstTableContextTests.OpenContextForRowTests();
        using (file)
        {
            foreach (PstTableRow row in context.EnumerateRows())
            {
                foreach (PstPropertyValue cell in row.EnumerateCells())
                {
                    Assert.IsTrue(row.TryGetCellLength(cell.PropertyId, out long length), $"Cell 0x{cell.PropertyId:X4} must report a length.");
                    Assert.AreEqual(cell.RawData.Length, length);

                    Assert.IsTrue(row.TryOpenCellStream(cell.PropertyId, out Stream? stream), $"Cell 0x{cell.PropertyId:X4} must open.");
                    using (stream)
                    {
                        var buffer = new MemoryStream();
                        stream.CopyTo(buffer);
                        CollectionAssert.AreEqual(cell.RawData.ToArray(), buffer.ToArray());
                    }
                }
            }
        }
    }

    /// <summary>
    /// Verifies that an absent cell reports <see langword="false" /> from both accessors.
    /// </summary>
    [TestMethod]
    public void TryOpenCellStream_WhenCellAbsent_ShouldReturnFalse()
    {
        (PstFile file, PstTableContext context) = PstTableContextTests.OpenContextForRowTests();
        using (file)
        {
            PstTableRow row = context.EnumerateRows().First();

            Assert.IsFalse(row.TryOpenCellStream(0x0FFF, out Stream? stream));
            Assert.IsNull(stream);
            Assert.IsFalse(row.TryGetCellLength(0x0FFF, out long length));
            Assert.AreEqual(0L, length);
        }
    }
}
