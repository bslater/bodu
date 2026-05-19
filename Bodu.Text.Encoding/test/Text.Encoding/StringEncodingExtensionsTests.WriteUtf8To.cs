// ---------------------------------------------------------------------------------------------------------------
// <copyright file="StringEncodingExtensionsTests.WriteUtf8To.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers;
using Bodu.Buffers;

namespace Bodu.Text.Encoding;

public sealed partial class StringEncodingExtensionsTests
{
    /// <summary>
    /// Verifies that <see cref="StringEncodingExtensions.WriteUtf8To(string, IBufferWriter{byte})" /> writes the
    /// UTF-8 encoded bytes into the supplied writer.
    /// </summary>
    [TestMethod]
    public void WriteUtf8To_WhenInvoked_ShouldWriteUtf8BytesIntoWriter()
    {
        byte[] expected = System.Text.Encoding.UTF8.GetBytes(MultiByteText);
        using PooledBufferBuilder<byte> writer = new PooledBufferBuilder<byte>(16);

        MultiByteText.WriteUtf8To(writer);

        CollectionAssert.AreEqual(expected, writer.WrittenSpan.ToArray());
    }

    /// <summary>
    /// Verifies that <see cref="StringEncodingExtensions.WriteUtf8To(string, IBufferWriter{byte})" /> throws
    /// <see cref="ArgumentNullException" /> when <c>text</c> is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void WriteUtf8To_WhenTextIsNull_ShouldThrowArgumentNullException()
    {
        using PooledBufferBuilder<byte> writer = new PooledBufferBuilder<byte>(16);

        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            StringEncodingExtensions.WriteUtf8To(null!, writer);
        });

        Assert.AreEqual("text", ex.ParamName);
    }

    /// <summary>
    /// Verifies that <see cref="StringEncodingExtensions.WriteUtf8To(string, IBufferWriter{byte})" /> throws
    /// <see cref="ArgumentNullException" /> when <c>writer</c> is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void WriteUtf8To_WhenWriterIsNull_ShouldThrowArgumentNullException()
    {
        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            SampleText.WriteUtf8To(null!);
        });

        Assert.AreEqual("writer", ex.ParamName);
    }
}
