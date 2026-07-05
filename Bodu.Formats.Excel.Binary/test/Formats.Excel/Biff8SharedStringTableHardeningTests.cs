// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Biff8SharedStringTableHardeningTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers.Binary;
using Bodu.Formats.Excel.Biff8;

namespace Bodu.Formats.Excel;

/// <summary>
/// Verifies that the BIFF8 shared-string-table decoder rejects a crafted continuation layout with the documented
/// <see cref="ExcelBinaryFormatException" /> rather than letting an <see cref="IndexOutOfRangeException" /> escape.
/// </summary>
[TestClass]
public class Biff8SharedStringTableHardeningTests
{
    /// <summary>
    /// Verifies that a string that straddles into a zero-length <c>CONTINUE</c> block is rejected with
    /// <see cref="ExcelBinaryFormatException" /> instead of indexing the empty block and throwing
    /// <see cref="IndexOutOfRangeException" />.
    /// </summary>
    [TestMethod]
    [TestCategory("Regression")]
    public void Parse_WhenStringStraddlesEmptyContinueBlock_ShouldThrowExcelBinaryFormatException()
    {
        // Block 0: SST header (total, unique=1) + one compressed string declaring 2 chars but supplying only 1,
        // forcing the decoder to cross into the next block to read the second character.
        byte[] first = new byte[12];
        BinaryPrimitives.WriteUInt32LittleEndian(first.AsSpan(0), 1); // total string count
        BinaryPrimitives.WriteUInt32LittleEndian(first.AsSpan(4), 1); // unique string count
        BinaryPrimitives.WriteUInt16LittleEndian(first.AsSpan(8), 2); // charCount = 2
        first[10] = 0x00;                                             // flags: compressed (1 byte/char)
        first[11] = (byte)'A';                                        // only the first character

        // Block 1: a zero-length CONTINUE payload — the decoder re-reads the compression flag from its first byte.
        byte[] second = [];

        ReadOnlyMemory<byte>[] blocks = [first, second];

        Assert.ThrowsExactly<ExcelBinaryFormatException>(() => _ = Biff8SharedStringTable.Parse(blocks));
    }
}
