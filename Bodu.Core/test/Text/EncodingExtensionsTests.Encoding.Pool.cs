// ---------------------------------------------------------------------------------------------------------------
// <copyright file="EncodingExtensionsTests.Encoding.Pool.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers;
using Bodu.Buffers;

namespace Bodu.Text;

public sealed partial class EncodingExtensionsTests
{
    /// <summary>
    /// Verifies that <see cref="EncodingExtensions.GetBytesRented(System.Text.Encoding, ReadOnlySpan{char}, out int)" />
    /// returns a buffer containing the encoded bytes and reports the exact byte count.
    /// </summary>
    [TestMethod]
    public void GetBytesRented_WhenInvoked_ShouldReportExactCountAndContainEncodedContent()
    {
        byte[] expected = System.Text.Encoding.UTF8.GetBytes(MultiByteText);

        byte[] rented = System.Text.Encoding.UTF8.GetBytesRented(MultiByteText, out int written);
        try
        {
            Assert.AreEqual(expected.Length, written);
            CollectionAssert.AreEqual(expected, rented[..written]);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(rented);
        }
    }

    /// <summary>
    /// Verifies that <see cref="EncodingExtensions.GetBytesRented(System.Text.Encoding, ReadOnlySpan{char}, out int)" />
    /// returns the singleton empty array and reports zero written when the input span is empty.
    /// </summary>
    [TestMethod]
    public void GetBytesRented_WhenSpanIsEmpty_ShouldReturnEmptyArrayWithZeroWritten()
    {
        byte[] rented = System.Text.Encoding.UTF8.GetBytesRented([], out int written);

        Assert.AreEqual(0, written);
        Assert.AreSame([], rented);
    }

    /// <summary>
    /// Verifies that <see cref="EncodingExtensions.GetBytesRented(System.Text.Encoding, ReadOnlySpan{char}, out int)" />
    /// throws <see cref="ArgumentNullException" /> when <c>encoding</c> is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void GetBytesRented_WhenEncodingIsNull_ShouldThrowExactly()
    {
        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = EncodingExtensions.GetBytesRented(null!, SampleText, out _);
        });

        Assert.AreEqual("encoding", ex.ParamName);
    }

    /// <summary>
    /// Verifies that <see cref="EncodingExtensions.GetCharsRented(System.Text.Encoding, ReadOnlySpan{byte}, out int)" />
    /// returns a buffer containing the decoded characters and reports the exact character count.
    /// </summary>
    [TestMethod]
    public void GetCharsRented_WhenInvoked_ShouldReportExactCountAndContainDecodedContent()
    {
        byte[] bytes = System.Text.Encoding.UTF8.GetBytes(MultiByteText);

        char[] rented = System.Text.Encoding.UTF8.GetCharsRented(bytes, out int written);
        try
        {
            Assert.AreEqual(MultiByteText.Length, written);
            Assert.AreEqual(MultiByteText, new string(rented, 0, written));
        }
        finally
        {
            ArrayPool<char>.Shared.Return(rented);
        }
    }

    /// <summary>
    /// Verifies that <see cref="EncodingExtensions.GetCharsRented(System.Text.Encoding, ReadOnlySpan{byte}, out int)" />
    /// returns the singleton empty array and reports zero written when the input span is empty.
    /// </summary>
    [TestMethod]
    public void GetCharsRented_WhenSpanIsEmpty_ShouldReturnEmptyArrayWithZeroWritten()
    {
        char[] rented = System.Text.Encoding.UTF8.GetCharsRented([], out int written);

        Assert.AreEqual(0, written);
        Assert.AreSame([], rented);
    }

    /// <summary>
    /// Verifies that <see cref="EncodingExtensions.GetCharsRented(System.Text.Encoding, ReadOnlySpan{byte}, out int)" />
    /// throws <see cref="ArgumentNullException" /> when <c>encoding</c> is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void GetCharsRented_WhenEncodingIsNull_ShouldThrowExactly()
    {
        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = EncodingExtensions.GetCharsRented(null!, [0x68], out _);
        });

        Assert.AreEqual("encoding", ex.ParamName);
    }

    /// <summary>
    /// Verifies that <see cref="EncodingExtensions.GetBytesOwner(System.Text.Encoding, ReadOnlySpan{char}, MemoryPool{byte})" />
    /// returns an owner whose memory length and content match the encoded bytes.
    /// </summary>
    [TestMethod]
    public void GetBytesOwner_WhenInvoked_ShouldReturnExactSizedOwnerWithEncodedContent()
    {
        byte[] expected = System.Text.Encoding.UTF8.GetBytes(MultiByteText);

        using IMemoryOwner<byte> owner = System.Text.Encoding.UTF8.GetBytesOwner(MultiByteText);

        Assert.AreEqual(expected.Length, owner.Memory.Length);
        CollectionAssert.AreEqual(expected, owner.Memory.ToArray());
    }

    /// <summary>
    /// Verifies that <see cref="EncodingExtensions.GetBytesOwner(System.Text.Encoding, ReadOnlySpan{char}, MemoryPool{byte})" />
    /// throws <see cref="ArgumentNullException" /> when <c>encoding</c> is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void GetBytesOwner_WhenEncodingIsNull_ShouldThrowExactly()
    {
        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = EncodingExtensions.GetBytesOwner(null!, SampleText);
        });

        Assert.AreEqual("encoding", ex.ParamName);
    }

    /// <summary>
    /// Verifies that <see cref="EncodingExtensions.GetCharsOwner(System.Text.Encoding, ReadOnlySpan{byte}, MemoryPool{char})" />
    /// returns an owner whose memory length and content match the decoded characters.
    /// </summary>
    [TestMethod]
    public void GetCharsOwner_WhenInvoked_ShouldReturnExactSizedOwnerWithDecodedContent()
    {
        byte[] bytes = System.Text.Encoding.UTF8.GetBytes(MultiByteText);

        using IMemoryOwner<char> owner = System.Text.Encoding.UTF8.GetCharsOwner(bytes);

        Assert.AreEqual(MultiByteText.Length, owner.Memory.Length);
        Assert.AreEqual(MultiByteText, new string(owner.Memory.Span));
    }

    /// <summary>
    /// Verifies that <see cref="EncodingExtensions.GetCharsOwner(System.Text.Encoding, ReadOnlySpan{byte}, MemoryPool{char})" />
    /// throws <see cref="ArgumentNullException" /> when <c>encoding</c> is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void GetCharsOwner_WhenEncodingIsNull_ShouldThrowExactly()
    {
        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = EncodingExtensions.GetCharsOwner(null!, [0x68]);
        });

        Assert.AreEqual("encoding", ex.ParamName);
    }

    /// <summary>
    /// Verifies that <see cref="EncodingExtensions.GetBytesPooled(System.Text.Encoding, ReadOnlySpan{char})" />
    /// returns a <see cref="PooledBufferBuilder{T}" /> whose written span equals the encoded bytes exactly.
    /// </summary>
    [TestMethod]
    public void GetBytesPooled_WhenInvoked_ShouldReturnBuilderWhoseWrittenSpanMatchesEncoded()
    {
        byte[] expected = System.Text.Encoding.UTF8.GetBytes(MultiByteText);

        using PooledBufferBuilder<byte> builder = System.Text.Encoding.UTF8.GetBytesPooled(MultiByteText);

        Assert.AreEqual(expected.Length, builder.WrittenCount);
        CollectionAssert.AreEqual(expected, builder.WrittenSpan.ToArray());
    }

    /// <summary>
    /// Verifies that <see cref="EncodingExtensions.GetBytesPooled(System.Text.Encoding, ReadOnlySpan{char})" />
    /// returns a builder with zero written bytes when the input span is empty.
    /// </summary>
    [TestMethod]
    public void GetBytesPooled_WhenSpanIsEmpty_ShouldReturnEmptyBuilder()
    {
        using PooledBufferBuilder<byte> builder = System.Text.Encoding.UTF8.GetBytesPooled([]);

        Assert.AreEqual(0, builder.WrittenCount);
        Assert.IsTrue(builder.IsEmpty);
    }

    /// <summary>
    /// Verifies that <see cref="EncodingExtensions.GetBytesPooled(System.Text.Encoding, ReadOnlySpan{char})" />
    /// throws <see cref="ArgumentNullException" /> when <c>encoding</c> is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void GetBytesPooled_WhenEncodingIsNull_ShouldThrowExactly()
    {
        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = EncodingExtensions.GetBytesPooled(null!, SampleText);
        });

        Assert.AreEqual("encoding", ex.ParamName);
    }

    /// <summary>
    /// Verifies that <see cref="EncodingExtensions.GetCharsPooled(System.Text.Encoding, ReadOnlySpan{byte})" />
    /// returns a <see cref="PooledBufferBuilder{T}" /> whose written span equals the decoded characters exactly.
    /// </summary>
    [TestMethod]
    public void GetCharsPooled_WhenInvoked_ShouldReturnBuilderWhoseWrittenSpanMatchesDecoded()
    {
        byte[] bytes = System.Text.Encoding.UTF8.GetBytes(MultiByteText);

        using PooledBufferBuilder<char> builder = System.Text.Encoding.UTF8.GetCharsPooled(bytes);

        Assert.AreEqual(MultiByteText.Length, builder.WrittenCount);
        Assert.AreEqual(MultiByteText, new string(builder.WrittenSpan));
    }

    /// <summary>
    /// Verifies that <see cref="EncodingExtensions.GetCharsPooled(System.Text.Encoding, ReadOnlySpan{byte})" />
    /// returns a builder with zero written characters when the input span is empty.
    /// </summary>
    [TestMethod]
    public void GetCharsPooled_WhenSpanIsEmpty_ShouldReturnEmptyBuilder()
    {
        using PooledBufferBuilder<char> builder = System.Text.Encoding.UTF8.GetCharsPooled([]);

        Assert.AreEqual(0, builder.WrittenCount);
        Assert.IsTrue(builder.IsEmpty);
    }

    /// <summary>
    /// Verifies that <see cref="EncodingExtensions.GetCharsPooled(System.Text.Encoding, ReadOnlySpan{byte})" />
    /// throws <see cref="ArgumentNullException" /> when <c>encoding</c> is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void GetCharsPooled_WhenEncodingIsNull_ShouldThrowExactly()
    {
        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = EncodingExtensions.GetCharsPooled(null!, [0x68]);
        });

        Assert.AreEqual("encoding", ex.ParamName);
    }
}
