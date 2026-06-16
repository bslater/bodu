// ---------------------------------------------------------------------------------------------------------------
// <copyright file="StringEncodingExtensionsTests.WriteTo.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers;
using Bodu.Buffers;

namespace Bodu.Text;

public sealed partial class StringEncodingExtensionsTests
{
    /// <summary>
    /// Verifies that
    /// <see cref="StringEncodingExtensions.WriteTo(string, System.Text.Encoding, IBufferWriter{byte})" />
    /// writes the encoded bytes into the supplied writer.
    /// </summary>
    [TestMethod]
    public void WriteTo_WhenInvoked_ShouldWriteEncodedBytesIntoWriter()
    {
        byte[] expected = System.Text.Encoding.UTF8.GetBytes(MultiByteText);
        using var writer = new PooledBufferBuilder<byte>(16);

        MultiByteText.WriteTo(System.Text.Encoding.UTF8, writer);

        CollectionAssert.AreEqual(expected, writer.WrittenSpan.ToArray());
    }

    /// <summary>
    /// Verifies that
    /// <see cref="StringEncodingExtensions.WriteTo(string, System.Text.Encoding, IBufferWriter{byte})" /> writes
    /// nothing when the input string is empty.
    /// </summary>
    [TestMethod]
    public void WriteTo_WhenStringIsEmpty_ShouldNotWriteAnyBytes()
    {
        using var writer = new PooledBufferBuilder<byte>(16);

        string.Empty.WriteTo(System.Text.Encoding.UTF8, writer);

        Assert.AreEqual(0, writer.WrittenCount);
    }

    /// <summary>
    /// Verifies that
    /// <see cref="StringEncodingExtensions.WriteTo(string, System.Text.Encoding, IBufferWriter{byte})" /> throws
    /// <see cref="ArgumentNullException" /> when <c>text</c> is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void WriteTo_WhenTextIsNull_ShouldThrowExactly()
    {
        using var writer = new PooledBufferBuilder<byte>(16);

        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            StringEncodingExtensions.WriteTo(null!, System.Text.Encoding.UTF8, writer);
        });

        Assert.AreEqual("text", ex.ParamName);
    }

    /// <summary>
    /// Verifies that
    /// <see cref="StringEncodingExtensions.WriteTo(string, System.Text.Encoding, IBufferWriter{byte})" /> throws
    /// <see cref="ArgumentNullException" /> when <c>encoding</c> is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void WriteTo_WhenEncodingIsNull_ShouldThrowExactly()
    {
        using var writer = new PooledBufferBuilder<byte>(16);

        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            SampleText.WriteTo(null!, writer);
        });

        Assert.AreEqual("encoding", ex.ParamName);
    }

    /// <summary>
    /// Verifies that
    /// <see cref="StringEncodingExtensions.WriteTo(string, System.Text.Encoding, IBufferWriter{byte})" /> throws
    /// <see cref="ArgumentNullException" /> when <c>writer</c> is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void WriteTo_WhenWriterIsNull_ShouldThrowExactly()
    {
        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            SampleText.WriteTo(System.Text.Encoding.UTF8, null!);
        });

        Assert.AreEqual("writer", ex.ParamName);
    }
}
