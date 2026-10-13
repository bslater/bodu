// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DelimitedSerializerTests.Allocations.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers;

namespace Bodu.Text.Delimited;

/// <summary>
/// Contains the allocation tests for <see cref="DelimitedSerializer" />, which pin the bytes a call allocates, counted
/// with <see cref="GC.GetAllocatedBytesForCurrentThread" /> after warm-up calls, so that they do not depend on timing.
/// </summary>
public partial class DelimitedSerializerTests
{
    /// <summary>The number of records each allocation measurement writes or reads.</summary>
    private const int AllocationRecordCount = 1000;

    /// <summary>
    /// Verifies that serializing 1,000 records of ten columns from an asynchronous sequence allocates no more than
    /// serializing them from a list into a buffer, within 192 KiB for the batch buffer the stream is written from,
    /// rather than a writer and its field lists for every record.
    /// </summary>
    [TestMethod]
    public void SerializeAsync_WhenRecordsArriveAsAnAsynchronousSequence_ShouldAllocateLittleMoreThanForAList()
    {
        List<TenNumberRecord> records = CreateTenNumberRecords();
        var buffer = new ArrayBufferWriter<byte>(1 << 20);
        using var stream = new MemoryStream(1 << 20);

        long listBytes = MeasureAllocations(() =>
        {
            buffer.ResetWrittenCount();
            DelimitedSerializer.Serialize(buffer, records);
        });
        long sequenceBytes = MeasureAllocations(() =>
        {
            stream.Position = 0;
            DelimitedSerializer.SerializeAsync(stream, ToSynchronousAsyncEnumerable(records)).AsTask().GetAwaiter().GetResult();
        });

        Assert.IsLessThanOrEqualTo(listBytes + (192 * 1024), sequenceBytes, $"Sequence: {sequenceBytes} B; list: {listBytes} B; .NET {Environment.Version}.");
    }

    /// <summary>
    /// Verifies that deserializing 1,000 rows of ten fields into <see cref="string" /> arrays allocates little more
    /// than the rows themselves, at most 424 bytes a row for its ten field strings and its array, within 64 KiB for the
    /// document, rather than a list for every row it collects.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenRowsHaveTenFields_ShouldAllocateLittleMoreThanTheRows()
    {
        byte[] document = System.Text.Encoding.UTF8.GetBytes(DelimitedSerializer.Serialize(CreateTenNumberRecords()));

        long allocated = MeasureAllocations(() => DelimitedSerializer.Deserialize<string[]>(document.AsSpan()));

        Assert.IsLessThanOrEqualTo((AllocationRecordCount * 424L) + (64 * 1024), allocated, $"Allocated {allocated} B; .NET {Environment.Version}.");
    }

    /// <summary>
    /// Creates the records the allocation tests write.
    /// </summary>
    /// <returns><see cref="AllocationRecordCount" /> records, each with its index in its first column.</returns>
    private static List<TenNumberRecord> CreateTenNumberRecords() =>
        [.. Enumerable.Range(0, AllocationRecordCount).Select(static index => new TenNumberRecord { A = index })];

    /// <summary>
    /// Calls an action three times to warm up and once measured, and returns the bytes the measured call allocated on
    /// the current thread.
    /// </summary>
    /// <param name="action">The action to measure.</param>
    /// <returns>The bytes allocated by the measured call.</returns>
    /// <remarks>
    /// Reflection generates the code of a property's getter or setter on its second call, so the warm-up calls each
    /// accessor more than once and the measured call sees only the steady state.
    /// </remarks>
    private static long MeasureAllocations(Action action)
    {
        for (int i = 0; i < 3; i++)
            action();

        long before = GC.GetAllocatedBytesForCurrentThread();
        action();

        return GC.GetAllocatedBytesForCurrentThread() - before;
    }

    /// <summary>
    /// Yields records as an asynchronous sequence whose every step completes synchronously, so that a call over it
    /// runs on the calling thread and its allocations can be counted there.
    /// </summary>
    /// <typeparam name="TRecord">The record type.</typeparam>
    /// <param name="records">The records to yield.</param>
    /// <returns>An asynchronous sequence of the records.</returns>
    private static async IAsyncEnumerable<TRecord> ToSynchronousAsyncEnumerable<TRecord>(IEnumerable<TRecord> records)
    {
        foreach (TRecord record in records)
            yield return record;

        await Task.CompletedTask.ConfigureAwait(false);
    }
}
