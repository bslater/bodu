// ---------------------------------------------------------------------------------------------------------------
// <copyright file="StringEncodingExtensionsTests.GetUtf8BytesPooled.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Buffers;

namespace Bodu.Text;

public sealed partial class StringEncodingExtensionsTests
{
    /// <summary>
    /// Verifies that <see cref="StringEncodingExtensions.GetUtf8BytesPooled(string)" /> returns a builder whose
    /// written span equals the UTF-8 encoded bytes exactly.
    /// </summary>
    [TestMethod]
    public void GetUtf8BytesPooled_WhenInvoked_ShouldReturnBuilderWhoseWrittenSpanMatchesUtf8()
    {
        var expected = System.Text.Encoding.UTF8.GetBytes(MultiByteText);

        using PooledBufferBuilder<byte> builder = MultiByteText.GetUtf8BytesPooled();

        Assert.AreEqual(expected.Length, builder.WrittenCount);
        CollectionAssert.AreEqual(expected, builder.WrittenSpan.ToArray());
    }

    /// <summary>
    /// Verifies that <see cref="StringEncodingExtensions.GetUtf8BytesPooled(string)" /> returns a builder with
    /// zero written bytes when the input string is empty.
    /// </summary>
    [TestMethod]
    public void GetUtf8BytesPooled_WhenStringIsEmpty_ShouldReturnEmptyBuilder()
    {
        using PooledBufferBuilder<byte> builder = string.Empty.GetUtf8BytesPooled();

        Assert.AreEqual(0, builder.WrittenCount);
        Assert.IsTrue(builder.IsEmpty);
    }

    /// <summary>
    /// Verifies that <see cref="StringEncodingExtensions.GetUtf8BytesPooled(string)" /> throws
    /// <see cref="ArgumentNullException" /> when <c>text</c> is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void GetUtf8BytesPooled_WhenTextIsNull_ShouldThrowExactly()
    {
        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = StringEncodingExtensions.GetUtf8BytesPooled(null!);
        });

        Assert.AreEqual("text", ex.ParamName);
    }
}
