// ---------------------------------------------------------------------------------------------------------------
// <copyright file="StreamExtensionsTests.WriteAllBytes.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public partial class StreamExtensionsTests
{
    /// <summary>
    /// Verifies that <c>WriteAllBytes</c> writes the entire span to the stream.
    /// </summary>
    [TestMethod]
    public void WriteAllBytes_WhenInvoked_ShouldWriteAllBytes()
    {
        byte[] content = [1, 2, 3, 4, 5];
        using MemoryStream stream = new();

        stream.WriteAllBytes(content);

        CollectionAssert.AreEqual(content, stream.ToArray());
    }

    /// <summary>
    /// Verifies that <c>WriteAllBytes</c> writes nothing when the span is empty.
    /// </summary>
    [TestMethod]
    public void WriteAllBytes_WhenSpanIsEmpty_ShouldWriteNothing()
    {
        using MemoryStream stream = new();

        stream.WriteAllBytes(ReadOnlySpan<byte>.Empty);

        Assert.AreEqual(0L, stream.Length);
    }

    /// <summary>
    /// Verifies that <c>WriteAllBytes</c> throws <see cref="ArgumentNullException" /> when the stream is
    /// <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void WriteAllBytes_WhenStreamIsNull_ShouldThrowExactly()
    {
        Stream stream = null!;

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            stream.WriteAllBytes([1, 2, 3]);
        });
    }
}
