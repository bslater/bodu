// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DelimitedReaderTests.ReadAsync.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Delimited;

public sealed partial class DelimitedReaderTests
{
    /// <summary>
    /// Verifies that <see cref="DelimitedReader.ReadAsync" /> yields the same rows (and header) as the synchronous
    /// <see cref="DelimitedReader.Read" /> path, including a quoted field containing an embedded newline.
    /// </summary>
    [TestMethod]
    public async Task ReadAsync_ShouldYieldSameRowsAsRead()
    {
        const string Source = "name,note\nAlice,\"line1\nline2\"\nBob,plain\n";

        List<string[]> sync = new();
        using (DelimitedReader r = new(new StringReader(Source)))
        {
            while (r.Read())
                sync.Add(r.Fields.ToArray());
        }

        List<string[]> async = new();
        using (DelimitedReader r = new(new StringReader(Source)))
        {
            while (await r.ReadAsync())
                async.Add(r.Fields.ToArray());
        }

        Assert.AreEqual(sync.Count, async.Count);
        for (int i = 0; i < sync.Count; i++)
            CollectionAssert.AreEqual(sync[i], async[i]);
    }

    /// <summary>
    /// Verifies that <see cref="DelimitedReader.ReadAsync" /> returns <see langword="false" /> on empty input.
    /// </summary>
    [TestMethod]
    public async Task ReadAsync_WhenInputIsEmpty_ShouldReturnFalse()
    {
        using DelimitedReader reader = new(new StringReader(string.Empty));

        Assert.IsFalse(await reader.ReadAsync());
    }

    /// <summary>
    /// Verifies that <see cref="DelimitedReader.ReadAsync" /> after disposal throws
    /// <see cref="ObjectDisposedException" />.
    /// </summary>
    [TestMethod]
    public async Task ReadAsync_WhenDisposed_ShouldThrowExactly()
    {
        DelimitedReader reader = new(new StringReader("a,b\n"));
        reader.Dispose();

        await Assert.ThrowsExactlyAsync<ObjectDisposedException>(async () =>
        {
            _ = await reader.ReadAsync();
        });
    }
}
