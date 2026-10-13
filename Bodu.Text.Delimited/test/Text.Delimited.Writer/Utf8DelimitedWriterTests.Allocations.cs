// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Utf8DelimitedWriterTests.Allocations.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers;

namespace Bodu.Text.Delimited.Writer;

/// <summary>
/// Contains the allocation tests for <see cref="Utf8DelimitedWriter" />, which pin the bytes it allocates to write a
/// document, counted with <see cref="GC.GetAllocatedBytesForCurrentThread" /> after warm-up writes, so that they do not
/// depend on timing.
/// </summary>
public partial class Utf8DelimitedWriterTests
{
    /// <summary>
    /// Verifies that writing 1,000 positional records of ten fields into a buffer large enough for them allocates
    /// less than 4 KiB, rather than a string for every delimiter it writes.
    /// </summary>
    [TestMethod]
    public void WriteEndArray_WhenRecordsHaveTenFields_ShouldAllocateNothingPerDelimiter()
    {
        string[] fields = [.. Enumerable.Range(0, 10).Select(static i => "v" + i)];
        var buffer = new ArrayBufferWriter<byte>(1 << 20);

        void WriteDocument()
        {
            buffer.ResetWrittenCount();
            var writer = new Utf8DelimitedWriter(buffer);
            writer.WriteStartArray();
            for (int record = 0; record < 1000; record++)
            {
                writer.WriteStartArray();
                foreach (string field in fields)
                    writer.WriteString(field);

                writer.WriteEndArray();
            }

            writer.WriteEndArray();
        }

        for (int i = 0; i < 3; i++)
            WriteDocument();

        long before = GC.GetAllocatedBytesForCurrentThread();
        WriteDocument();
        long allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        Assert.IsLessThanOrEqualTo(4096L, allocated, $"Allocated {allocated} B; .NET {Environment.Version}.");
    }
}
