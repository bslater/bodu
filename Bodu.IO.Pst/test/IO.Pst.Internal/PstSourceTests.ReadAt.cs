// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PstSourceTests.ReadAt.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers.Binary;
using Bodu.IO.Pst;

namespace Bodu.IO.Pst.Internal;

public partial class PstSourceTests
{
    /// <summary>
    /// Verifies that a page reference whose offset sits near the top of the signed 64-bit range is rejected as a
    /// format error: the range check must not overflow when the read length is added to the offset.
    /// </summary>
    [TestMethod]
    public void ReadAt_WhenOffsetNearInt64Maximum_ShouldThrowPstFileFormatException()
    {
        byte[] file = BuildSingleNode(out _, [1, 2, 3], out _);
        BinaryPrimitives.WriteUInt64LittleEndian(file.AsSpan(224), 0x7FFF_FFFF_FFFF_FF00UL);
        PstFixtureBuilder.RepairHeaderChecksum(file);

        var ex = Assert.ThrowsExactly<PstFileFormatException>(() => _ = ReadNodePayload(file, PstValidationLevel.Compatible));

        Assert.AreEqual(PstFileError.InvalidBlock, ex.Error);
    }
}
