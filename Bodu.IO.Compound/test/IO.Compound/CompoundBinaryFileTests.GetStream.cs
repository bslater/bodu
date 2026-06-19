// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CompoundBinaryFileTests.GetStream.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Compound;

public partial class CompoundBinaryFileTests
{
    /// <summary>
    /// Verifies that opening the <c>Workbook</c> stream returns a stream of the declared length whose first bytes are
    /// the expected BIFF beginning-of-file record.
    /// </summary>
    [TestMethod]
    public void GetStream_WhenWorkbookRequested_ShouldReturnStreamOfDeclaredLength()
    {
        using CompoundBinaryFile file = OpenSample();

        using CompoundStream workbook = file.GetStream("Workbook");

        Assert.AreEqual(461504L, workbook.Length);

        byte[] header = new byte[2];
        _ = workbook.Read(header, 0, 2);

        // The BIFF8 workbook globals substream opens with a BOF record (record id 0x0809, little-endian).
        Assert.AreEqual(0x09, header[0]);
        Assert.AreEqual(0x08, header[1]);
    }

    /// <summary>
    /// Verifies that <see cref="CompoundBinaryFile.GetStream(string)" /> throws
    /// <see cref="CompoundStreamNotFoundException" /> for a stream that does not exist, capturing the requested name.
    /// </summary>
    [TestMethod]
    public void GetStream_WhenStreamMissing_ShouldThrowCompoundStreamNotFoundException()
    {
        using CompoundBinaryFile file = OpenSample();

        var ex = Assert.ThrowsExactly<CompoundStreamNotFoundException>(() =>
        {
            using CompoundStream stream = file.GetStream("DoesNotExist");
        });

        Assert.AreEqual("DoesNotExist", ex.StreamName);
    }

    /// <summary>
    /// Verifies that the materialized <c>Workbook</c> stream is seekable and reports a read-only, non-writable surface.
    /// </summary>
    [TestMethod]
    public void GetStream_WhenWorkbookRequested_ShouldBeReadOnlyAndSeekable()
    {
        using CompoundBinaryFile file = OpenSample();

        using CompoundStream workbook = file.GetStream("Workbook");

        Assert.IsTrue(workbook.CanRead);
        Assert.IsTrue(workbook.CanSeek);
        Assert.IsFalse(workbook.CanWrite);

        workbook.Seek(0, SeekOrigin.End);
        Assert.AreEqual(workbook.Length, workbook.Position);
    }
}
