// ---------------------------------------------------------------------------------------------------------------
// <copyright file="StringEncodingExtensionsTests.GetBytesPooled.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Buffers;

namespace Bodu.Text.Encoding;

public sealed partial class StringEncodingExtensionsTests
{
    /// <summary>
    /// Verifies that <see cref="StringEncodingExtensions.GetBytesPooled(string, System.Text.Encoding)" />
    /// returns a builder whose written span equals the encoded bytes exactly.
    /// </summary>
    [TestMethod]
    public void GetBytesPooled_WhenInvoked_ShouldReturnBuilderWhoseWrittenSpanMatchesEncoded()
    {
        byte[] expected = System.Text.Encoding.UTF8.GetBytes(MultiByteText);

        using PooledBufferBuilder<byte> builder = MultiByteText.GetBytesPooled(System.Text.Encoding.UTF8);

        Assert.AreEqual(expected.Length, builder.WrittenCount);
        CollectionAssert.AreEqual(expected, builder.WrittenSpan.ToArray());
    }

    /// <summary>
    /// Verifies that <see cref="StringEncodingExtensions.GetBytesPooled(string, System.Text.Encoding)" />
    /// returns a builder with zero written bytes when the input string is empty.
    /// </summary>
    [TestMethod]
    public void GetBytesPooled_WhenStringIsEmpty_ShouldReturnEmptyBuilder()
    {
        using PooledBufferBuilder<byte> builder = string.Empty.GetBytesPooled(System.Text.Encoding.UTF8);

        Assert.AreEqual(0, builder.WrittenCount);
        Assert.IsTrue(builder.IsEmpty);
    }

    /// <summary>
    /// Verifies that <see cref="StringEncodingExtensions.GetBytesPooled(string, System.Text.Encoding)" /> throws
    /// <see cref="ArgumentNullException" /> when <c>text</c> is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void GetBytesPooled_WhenTextIsNull_ShouldThrowArgumentNullException()
    {
        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = StringEncodingExtensions.GetBytesPooled(null!, System.Text.Encoding.UTF8);
        });

        Assert.AreEqual("text", ex.ParamName);
    }

    /// <summary>
    /// Verifies that <see cref="StringEncodingExtensions.GetBytesPooled(string, System.Text.Encoding)" /> throws
    /// <see cref="ArgumentNullException" /> when <c>encoding</c> is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void GetBytesPooled_WhenEncodingIsNull_ShouldThrowArgumentNullException()
    {
        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = SampleText.GetBytesPooled(null!);
        });

        Assert.AreEqual("encoding", ex.ParamName);
    }
}
