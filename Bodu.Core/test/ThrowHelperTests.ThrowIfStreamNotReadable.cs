// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ThrowHelperTests.ThrowIfStreamNotReadable.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu;

public partial class ThrowHelperTests
{
    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfStreamNotReadable" /> does not throw for a readable stream.
    /// </summary>
    [TestMethod]
    public void ThrowIfStreamNotReadable_WhenStreamIsReadable_ShouldNotThrow()
    {
        using var stream = new MemoryStream();

        ThrowHelper.ThrowIfStreamNotReadable(stream);
    }

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfStreamNotReadable" /> throws <see cref="ArgumentNullException" />
    /// naming <c>stream</c> when the stream is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void ThrowIfStreamNotReadable_WhenStreamIsNull_ShouldThrowForStream()
    {
        Stream? stream = null;

        Assert.AreEqual(
            "stream",
            Assert.ThrowsExactly<ArgumentNullException>(() => ThrowHelper.ThrowIfStreamNotReadable(stream!)).ParamName);
    }

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfStreamNotReadable" /> throws <see cref="ArgumentException" /> naming
    /// <c>stream</c> when the stream does not support reading.
    /// </summary>
    [TestMethod]
    public void ThrowIfStreamNotReadable_WhenStreamCannotRead_ShouldThrowForStream()
    {
        var stream = new MemoryStream();
        stream.Dispose();

        ArgumentException ex = Assert.ThrowsExactly<ArgumentException>(() => ThrowHelper.ThrowIfStreamNotReadable(stream));
        Assert.AreEqual("stream", ex.ParamName);
    }
}
