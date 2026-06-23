// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ThrowHelperTests.ThrowIfStreamNotWritable.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.IO;

namespace Bodu;

public partial class ThrowHelperTests
{
    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfStreamNotWritable" /> does not throw for a writable stream.
    /// </summary>
    [TestMethod]
    public void ThrowIfStreamNotWritable_WhenStreamIsWritable_ShouldNotThrow()
    {
        using var stream = new MemoryStream();

        ThrowHelper.ThrowIfStreamNotWritable(stream);
    }

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfStreamNotWritable" /> throws <see cref="ArgumentNullException" />
    /// naming <c>stream</c> when the stream is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void ThrowIfStreamNotWritable_WhenStreamIsNull_ShouldThrowForStream()
    {
        Stream? stream = null;

        Assert.AreEqual(
            "stream",
            Assert.ThrowsExactly<ArgumentNullException>(() => ThrowHelper.ThrowIfStreamNotWritable(stream!)).ParamName);
    }

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfStreamNotWritable" /> throws <see cref="ArgumentException" /> naming
    /// <c>stream</c> when the stream does not support writing.
    /// </summary>
    [TestMethod]
    public void ThrowIfStreamNotWritable_WhenStreamCannotWrite_ShouldThrowForStream()
    {
        var stream = new MemoryStream(new byte[4], writable: false);

        var ex = Assert.ThrowsExactly<ArgumentException>(() => ThrowHelper.ThrowIfStreamNotWritable(stream));
        Assert.AreEqual("stream", ex.ParamName);
    }
}
