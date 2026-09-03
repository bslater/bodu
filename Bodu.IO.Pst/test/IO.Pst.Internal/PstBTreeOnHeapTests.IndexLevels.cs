// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PstBTreeOnHeapTests.IndexLevels.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.IO.Pst;

namespace Bodu.IO.Pst.Internal;

public partial class PstBTreeOnHeapTests
{
    /// <summary>
    /// Verifies that a header declaring more index levels than any MS-PST tree can hold is rejected at parse time,
    /// so a crafted level count cannot drive an exponential enumeration.
    /// </summary>
    [TestMethod]
    public void ReadHeader_WhenIndexLevelsExceedSpecMaximum_ShouldThrowPstFileFormatException()
    {
        var ltp = new PstLtpFixtureBuilder { ClientSignature = 0xB5 };
        uint leaf = ltp.AddBthLeafItem(4, 4, (0x10, [1, 0, 0, 0]));
        uint headerHid = ltp.AddBthHeaderItem(4, 4, indexLevels: 9, leaf);

        (PstFile file, PstHeapNode heap) = Open(ltp);
        using (file)
        {
            var ex = Assert.ThrowsExactly<PstFileFormatException>(() => _ = PstBTreeOnHeap.ReadHeader(heap, headerHid));

            Assert.AreEqual(PstFileError.InvalidHeap, ex.Error);
        }
    }

    /// <summary>
    /// Verifies that an index item naming itself as its own child is rejected during enumeration rather than being
    /// re-entered once per declared level — a two-entry self-reference would otherwise yield 2^levels records.
    /// </summary>
    [TestMethod]
    public void EnumerateRecords_WhenIndexItemReferencesItself_ShouldThrowPstFileFormatException()
    {
        var ltp = new PstLtpFixtureBuilder { ClientSignature = 0xB5 };

        // The first item of the first block resolves under HID(0, 1); the index item's two entries both name it.
        uint selfHid = PstLtpFixtureBuilder.Hid(0, 1);
        uint index = ltp.AddBthIndexItem(4, (0x10, selfHid), (0x20, selfHid));
        Assert.AreEqual(selfHid, index, "The fixture must place the index item at the identifier it references.");
        uint headerHid = ltp.AddBthHeaderItem(4, 4, indexLevels: 3, index);

        (PstFile file, PstHeapNode heap) = Open(ltp);
        using (file)
        {
            PstBthHeader header = PstBTreeOnHeap.ReadHeader(heap, headerHid);

            var ex = Assert.ThrowsExactly<PstFileFormatException>(() =>
            {
                _ = PstBTreeOnHeap.EnumerateRecords(heap, header, PstValidationLevel.Compatible).ToList();
            });

            Assert.AreEqual(PstFileError.InvalidHeap, ex.Error);
        }
    }

    /// <summary>
    /// Verifies that a malformed tree header reports the heap error category, so callers can branch on the failure
    /// without parsing its message.
    /// </summary>
    [TestMethod]
    public void ReadHeader_WhenHeaderIsMalformed_ShouldReportInvalidHeapCategory()
    {
        var ltp = new PstLtpFixtureBuilder { ClientSignature = 0xB5 };
        uint headerHid = ltp.AddItem([0xB4, 2, 6, 0, 0, 0, 0, 0]);

        (PstFile file, PstHeapNode heap) = Open(ltp);
        using (file)
        {
            var ex = Assert.ThrowsExactly<PstFileFormatException>(() => _ = PstBTreeOnHeap.ReadHeader(heap, headerHid));

            Assert.AreEqual(PstFileError.InvalidHeap, ex.Error);
        }
    }
}
