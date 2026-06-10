// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DelimitedWriterTests.Async.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Delimited;

public sealed partial class DelimitedWriterTests
{
    /// <summary>
    /// Verifies that the asynchronous header/row write methods produce byte-for-byte the same output as their
    /// synchronous counterparts, including a field that requires quoting.
    /// </summary>
    [TestMethod]
    public async Task WriteAsync_ShouldMatchSyncOutput()
    {
        StringWriter sync = new();
        using (DelimitedWriter w = new(sync))
        {
            w.WriteHeader(["name", "note"]);
            w.WriteRow(["Alice", "a,b"]);
        }

        StringWriter async = new();
        using (DelimitedWriter w = new(async))
        {
            await w.WriteHeaderAsync(["name", "note"]);
            await w.WriteRowAsync(["Alice", "a,b"]);
        }

        Assert.AreEqual(sync.ToString(), async.ToString());
    }

    /// <summary>
    /// Verifies that <see cref="DelimitedWriter.WriteRowAsync" /> counts rows the same way as
    /// <see cref="DelimitedWriter.WriteRow" />.
    /// </summary>
    [TestMethod]
    public async Task WriteRowAsync_ShouldIncrementRowsWritten()
    {
        StringWriter sw = new();
        using DelimitedWriter writer = new(sw);

        await writer.WriteRowAsync(["a"]);
        await writer.WriteRowAsync(["b"]);

        Assert.AreEqual(2, writer.RowsWritten);
    }

    /// <summary>
    /// Verifies that <see cref="DelimitedWriter.WriteRowAsync" /> rejects a row containing a <see langword="null" />
    /// element.
    /// </summary>
    [TestMethod]
    public async Task WriteRowAsync_WhenElementNull_ShouldThrowExactly()
    {
        using DelimitedWriter writer = new(new StringWriter());

        await Assert.ThrowsExactlyAsync<ArgumentException>(async () =>
        {
            await writer.WriteRowAsync(["ok", null!]);
        });
    }
}
