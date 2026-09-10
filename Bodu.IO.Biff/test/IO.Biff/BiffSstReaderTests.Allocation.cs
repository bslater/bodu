// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BiffSstReaderTests.Allocation.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Biff;

public sealed partial class BiffSstReaderTests
{
    /// <summary>The number of strings in the allocation samples.</summary>
    private const int AllocationSampleCount = 2000;

    /// <summary>
    /// Verifies that reading a table of contiguous strings and copying each into a caller's buffer allocates
    /// nothing per string.
    /// </summary>
    [TestMethod]
    [TestCategory("Regression")]
    public void Read_WhenStringsAreContiguous_ShouldNotAllocatePerString()
    {
        byte[] stream = ContiguousSample();
        char[] buffer = new char[16];
        _ = CopyAll(stream, buffer);

        long before = GC.GetAllocatedBytesForCurrentThread();
        int count = CopyAll(stream, buffer);
        long allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        Assert.AreEqual(AllocationSampleCount, count);
        Assert.IsLessThan(256, allocated, $"Reading {count} strings allocated {allocated} bytes.");
    }

    /// <summary>
    /// Verifies that the scratch buffer used for fragmented strings is allocated once and reused for later
    /// fragments that fit it.
    /// </summary>
    [TestMethod]
    [TestCategory("Regression")]
    public void Read_WhenSeveralStringsAreFragmented_ShouldReuseScratchBuffer()
    {
        byte[] stream = FragmentedSample();
        char[] buffer = new char[64];
        _ = CopyAll(stream, buffer);

        long before = GC.GetAllocatedBytesForCurrentThread();
        int count = CopyAll(stream, buffer);
        long allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        Assert.AreEqual(3, count);

        // One scratch array sized for the longest fragment (256 chars minimum) is the only allocation.
        Assert.IsLessThan(1024, allocated, $"Reading {count} fragmented strings allocated {allocated} bytes.");
    }

    /// <summary>
    /// Builds a table of short contiguous strings spanning several continuation records.
    /// </summary>
    /// <returns>The stream bytes.</returns>
    private static byte[] ContiguousSample()
    {
        var body = new List<byte>();
        for (int i = 0; i < AllocationSampleCount; i++)
            body.AddRange(BiffTestRecords.UnicodeString($"s{i:D5}"));

        // Split at string boundaries (each string is 9 bytes) so no string straddles a record.
        const int perRecord = 800 * 9;
        var records = new List<byte[]> { BiffTestRecords.Sst(AllocationSampleCount, AllocationSampleCount, [.. body.Take(perRecord)]) };
        for (int offset = perRecord; offset < body.Count; offset += perRecord)
            records.Add(BiffTestRecords.Continue([.. body.Skip(offset).Take(perRecord)]));

        return Table([.. records]);
    }

    /// <summary>
    /// Builds a table of three strings that each straddle a continuation boundary.
    /// </summary>
    /// <returns>The stream bytes.</returns>
    private static byte[] FragmentedSample() =>
        Table(
            BiffTestRecords.Sst(3, 3, [0x04, 0x00, Compressed, (byte)'a', (byte)'b']),
            BiffTestRecords.Continue(Compressed, (byte)'c', (byte)'d', 0x03, 0x00, Compressed, (byte)'e'),
            BiffTestRecords.Continue(Compressed, (byte)'f', (byte)'g', 0x02, 0x00, Compressed, (byte)'h'),
            BiffTestRecords.Continue(Compressed, (byte)'i'));

    /// <summary>
    /// Reads every string of the table into the buffer.
    /// </summary>
    /// <param name="stream">The stream bytes.</param>
    /// <param name="buffer">The destination buffer.</param>
    /// <returns>The number of strings read.</returns>
    private static int CopyAll(byte[] stream, char[] buffer)
    {
        var reader = new BiffReader(stream);
        while (reader.Read() && reader.RecordType != BiffRecordType.Sst)
        {
        }

        var strings = new BiffSstReader(ref reader);
        int count = 0;
        int chars = 0;
        while (strings.Read(ref reader))
        {
            count++;
            chars += strings.CopyTo(buffer);
        }

        Assert.IsGreaterThan(0, chars);
        return count;
    }
}
