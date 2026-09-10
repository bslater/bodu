// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BiffReaderTests.Allocation.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Biff;

public sealed partial class BiffReaderTests
{
    /// <summary>The number of cell records in the allocation sample.</summary>
    private const int AllocationSampleCount = 2000;

    /// <summary>
    /// Verifies that traversing a stream and decoding its numeric cells allocates nothing per record.
    /// </summary>
    [TestMethod]
    [TestCategory("Regression")]
    public void Read_WhenTraversingStream_ShouldNotAllocatePerRecord()
    {
        byte[] stream = BuildSample();
        _ = Traverse(stream);

        long before = GC.GetAllocatedBytesForCurrentThread();
        int count = Traverse(stream);
        long allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        Assert.AreEqual(AllocationSampleCount + 2, count);
        Assert.IsLessThan(256, allocated, $"Traversing {count} records allocated {allocated} bytes.");
    }

    /// <summary>
    /// Builds a sample stream of numeric and RK cells bracketed by BOF and EOF.
    /// </summary>
    /// <returns>The stream bytes.</returns>
    private static byte[] BuildSample()
    {
        var records = new List<byte[]> { BiffTestRecords.Bof8(BiffSubstreamType.Worksheet) };
        for (int i = 0; i < AllocationSampleCount; i++)
        {
            records.Add(i % 2 == 0
                ? BiffTestRecords.Number(i, 0, i * 0.5)
                : BiffTestRecords.Rk(i, 1, ((uint)i << 2) | 0x02));
        }

        records.Add(BiffTestRecords.Eof());
        return BiffTestRecords.Stream([.. records]);
    }

    /// <summary>
    /// Reads every record, decoding the numeric cells.
    /// </summary>
    /// <param name="stream">The stream bytes.</param>
    /// <returns>The number of records read.</returns>
    private static int Traverse(byte[] stream)
    {
        var reader = new BiffReader(stream);
        int count = 0;
        double sum = 0;
        while (reader.Read())
        {
            count++;
            switch (reader.RecordType)
            {
                case BiffRecordType.Number:
                    sum += reader.GetNumber().Value;
                    break;
                case BiffRecordType.Rk:
                    sum += reader.GetRk().Value;
                    break;
                default:
                    break;
            }
        }

        Assert.IsGreaterThan(0, sum);
        return count;
    }
}
