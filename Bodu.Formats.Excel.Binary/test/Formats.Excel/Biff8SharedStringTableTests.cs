// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Biff8SharedStringTableTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers.Binary;
using System.Text;
using Bodu.Formats.Excel.Biff8;

namespace Bodu.Formats.Excel;

/// <summary>
/// Verifies the BIFF8 shared-string-table decoder against hand-built record payloads.
/// </summary>
/// <remarks>
/// <para>
/// Every string cell in a worksheet is an index into this table, so a decoder that mis-reads one entry shifts every
/// subsequent string. The reference workbook exercises only the plain compressed form; the optional per-string
/// extensions - rich-text formatting runs and the Far East "extended string" block - carry trailing data that must be
/// skipped rather than decoded, and getting that wrong desynchronizes the whole table rather than corrupting one
/// entry. Those forms are built here directly because no fixture in the corpus contains them.
/// </para>
/// <para>
/// An <c>SST</c> record is followed by <c>CONTINUE</c> records, and a string may straddle the boundary between them -
/// which is also where the compression flag is re-stated. The blocks are therefore passed as a list, and the tests
/// cover both the single-block and straddling shapes.
/// </para>
/// </remarks>
[TestClass]
public class Biff8SharedStringTableTests
{
    /// <summary>The flag bit marking a string as compressed (one byte per character).</summary>
    private const byte Compressed = 0x00;

    /// <summary>The flag bit marking a string as UTF-16 (two bytes per character).</summary>
    private const byte Wide = 0x01;

    /// <summary>The flag bit marking a string as carrying an extended (Far East) block.</summary>
    private const byte HasExtended = 0x04;

    /// <summary>The flag bit marking a string as carrying rich-text formatting runs.</summary>
    private const byte HasRichRuns = 0x08;

    /// <summary>
    /// Builds a single-block shared-string table carrying the supplied encoded strings.
    /// </summary>
    /// <param name="unique">The unique-string count to declare in the header.</param>
    /// <param name="payload">The concatenated encoded strings.</param>
    /// <returns>The one-block table.</returns>
    private static ReadOnlyMemory<byte>[] Table(uint unique, params byte[] payload)
    {
        byte[] block = new byte[8 + payload.Length];
        BinaryPrimitives.WriteUInt32LittleEndian(block.AsSpan(0), unique);
        BinaryPrimitives.WriteUInt32LittleEndian(block.AsSpan(4), unique);
        payload.CopyTo(block.AsSpan(8));

        return [block];
    }

    /// <summary>
    /// Encodes one shared string in its record form.
    /// </summary>
    /// <param name="value">The string value.</param>
    /// <param name="wide">Whether to encode as UTF-16 rather than one byte per character.</param>
    /// <param name="richRuns">The number of formatting runs to declare, each contributing four trailing bytes.</param>
    /// <param name="extendedSize">The extended-block size to declare, contributing that many trailing bytes.</param>
    /// <returns>The encoded string.</returns>
    private static byte[] EncodeString(string value, bool wide = false, int richRuns = 0, int extendedSize = 0)
    {
        var bytes = new List<byte>();

        byte flags = wide ? Wide : Compressed;
        if (richRuns > 0)
            flags |= HasRichRuns;
        if (extendedSize > 0)
            flags |= HasExtended;

        byte[] length = new byte[2];
        BinaryPrimitives.WriteUInt16LittleEndian(length, (ushort)value.Length);
        bytes.AddRange(length);
        bytes.Add(flags);

        if (richRuns > 0)
        {
            byte[] runs = new byte[2];
            BinaryPrimitives.WriteUInt16LittleEndian(runs, (ushort)richRuns);
            bytes.AddRange(runs);
        }

        if (extendedSize > 0)
        {
            byte[] size = new byte[4];
            BinaryPrimitives.WriteUInt32LittleEndian(size, (uint)extendedSize);
            bytes.AddRange(size);
        }

        bytes.AddRange(wide ? Encoding.Unicode.GetBytes(value) : value.Select(c => (byte)c));

        // The trailing run and extended blocks are skipped rather than decoded, so their content is irrelevant.
        bytes.AddRange(new byte[(richRuns * 4) + extendedSize]);

        return [.. bytes];
    }

    /// <summary>
    /// Verifies that an empty block list decodes to an empty table rather than faulting - a workbook with no string
    /// cells carries no <c>SST</c> record at all.
    /// </summary>
    [TestMethod]
    public void Parse_WhenNoBlocks_ShouldReturnEmpty()
    {
        Assert.IsEmpty(Biff8SharedStringTable.Parse([]));
    }

    /// <summary>
    /// Verifies that a table declaring no unique strings decodes to an empty result.
    /// </summary>
    [TestMethod]
    public void Parse_WhenNoUniqueStrings_ShouldReturnEmpty()
    {
        Assert.IsEmpty(Biff8SharedStringTable.Parse(Table(0)));
    }

    /// <summary>
    /// Verifies that consecutive strings decode in order, so a cell's index resolves to the string the writer put
    /// there.
    /// </summary>
    [TestMethod]
    public void Parse_WhenSeveralCompressedStrings_ShouldDecodeInOrder()
    {
        ReadOnlyMemory<byte>[] blocks = Table(3, [.. EncodeString("one"), .. EncodeString("two"), .. EncodeString("three")]);

        CollectionAssert.AreEqual(new[] { "one", "two", "three" }, Biff8SharedStringTable.Parse(blocks));
    }

    /// <summary>
    /// Verifies that a UTF-16 string decodes to the characters it encodes, rather than being read one byte per
    /// character.
    /// </summary>
    [TestMethod]
    public void Parse_WhenStringIsWide_ShouldDecodeAsUtf16()
    {
        ReadOnlyMemory<byte>[] blocks = Table(1, EncodeString("café", wide: true));

        CollectionAssert.AreEqual(new[] { "café" }, Biff8SharedStringTable.Parse(blocks));
    }

    /// <summary>
    /// Verifies that a string's rich-text formatting runs are skipped, so the next string starts where it should.
    /// The second entry is what proves it: a mis-sized skip would decode garbage there rather than fail outright.
    /// </summary>
    [TestMethod]
    public void Parse_WhenStringHasRichTextRuns_ShouldSkipThemAndKeepAlignment()
    {
        ReadOnlyMemory<byte>[] blocks = Table(2, [.. EncodeString("styled", richRuns: 3), .. EncodeString("next")]);

        CollectionAssert.AreEqual(new[] { "styled", "next" }, Biff8SharedStringTable.Parse(blocks));
    }

    /// <summary>
    /// Verifies that a string's extended (Far East phonetic) block is skipped and alignment is preserved.
    /// </summary>
    [TestMethod]
    public void Parse_WhenStringHasExtendedBlock_ShouldSkipItAndKeepAlignment()
    {
        ReadOnlyMemory<byte>[] blocks = Table(2, [.. EncodeString("phonetic", extendedSize: 10), .. EncodeString("next")]);

        CollectionAssert.AreEqual(new[] { "phonetic", "next" }, Biff8SharedStringTable.Parse(blocks));
    }

    /// <summary>
    /// Verifies that a string carrying both optional blocks skips both, in the order the record lays them out.
    /// </summary>
    [TestMethod]
    public void Parse_WhenStringHasRichRunsAndExtendedBlock_ShouldSkipBoth()
    {
        ReadOnlyMemory<byte>[] blocks = Table(2, [.. EncodeString("both", richRuns: 2, extendedSize: 6), .. EncodeString("next")]);

        CollectionAssert.AreEqual(new[] { "both", "next" }, Biff8SharedStringTable.Parse(blocks));
    }

    /// <summary>
    /// Verifies that a header shorter than the eight-byte count pair is rejected rather than read past its end.
    /// </summary>
    [TestMethod]
    public void Parse_WhenHeaderIsTruncated_ShouldThrowExcelBinaryFormatException()
    {
        ReadOnlyMemory<byte>[] blocks = [new byte[4]];

        _ = Assert.ThrowsExactly<ExcelBinaryFormatException>(() => _ = Biff8SharedStringTable.Parse(blocks));
    }

    /// <summary>
    /// Verifies that a unique-string count larger than the bytes available is rejected before any allocation is
    /// sized from it, so a crafted count cannot drive an unbounded allocation.
    /// </summary>
    [TestMethod]
    public void Parse_WhenUniqueCountExceedsAvailableBytes_ShouldThrowExcelBinaryFormatException()
    {
        byte[] block = new byte[12];
        BinaryPrimitives.WriteUInt32LittleEndian(block.AsSpan(4), uint.MaxValue);

        _ = Assert.ThrowsExactly<ExcelBinaryFormatException>(() => _ = Biff8SharedStringTable.Parse([block]));
    }

    /// <summary>
    /// Verifies that a table promising more strings than it carries is rejected once the blocks run out, rather than
    /// returning a short table that would shift every later cell reference.
    /// </summary>
    [TestMethod]
    public void Parse_WhenStringsRunOutBeforeTheDeclaredCount_ShouldThrowExcelBinaryFormatException()
    {
        ReadOnlyMemory<byte>[] blocks = Table(2, EncodeString("only-one"));

        _ = Assert.ThrowsExactly<ExcelBinaryFormatException>(() => _ = Biff8SharedStringTable.Parse(blocks));
    }

    /// <summary>
    /// Verifies that a string header truncated mid-way through its own fields is rejected, whichever optional field
    /// the payload stops short of.
    /// </summary>
    /// <param name="flags">The string flags declaring which optional fields follow.</param>
    /// <param name="headerBytes">The number of header bytes supplied after the length word.</param>
    [TestMethod]
    [DataRow((byte)Compressed, 0, DisplayName = "no room for the flag byte")]
    [DataRow((byte)HasRichRuns, 1, DisplayName = "no room for the run count")]
    [DataRow((byte)HasExtended, 1, DisplayName = "no room for the extended size")]
    public void Parse_WhenStringHeaderIsTruncated_ShouldThrowExcelBinaryFormatException(byte flags, int headerBytes)
    {
        var payload = new List<byte>();
        byte[] length = new byte[2];
        BinaryPrimitives.WriteUInt16LittleEndian(length, 1);
        payload.AddRange(length);

        if (headerBytes > 0)
            payload.Add(flags);

        _ = Assert.ThrowsExactly<ExcelBinaryFormatException>(() => _ = Biff8SharedStringTable.Parse(Table(1, [.. payload])));
    }

    /// <summary>
    /// Verifies that a string spanning an <c>SST</c> record and its <c>CONTINUE</c> block decodes as one value, and
    /// that the continuation's leading compression flag is consumed rather than decoded as a character.
    /// </summary>
    [TestMethod]
    public void Parse_WhenStringStraddlesAContinueBlock_ShouldDecodeAcrossTheBoundary()
    {
        byte[] first = new byte[12];
        BinaryPrimitives.WriteUInt32LittleEndian(first.AsSpan(0), 1);
        BinaryPrimitives.WriteUInt32LittleEndian(first.AsSpan(4), 1);
        BinaryPrimitives.WriteUInt16LittleEndian(first.AsSpan(8), 4);
        first[10] = Compressed;
        first[11] = (byte)'A';

        // The CONTINUE payload restates the compression flag before its share of the characters.
        byte[] second = [Compressed, (byte)'B', (byte)'C', (byte)'D'];

        CollectionAssert.AreEqual(new[] { "ABCD" }, Biff8SharedStringTable.Parse([first, second]));
    }
}
