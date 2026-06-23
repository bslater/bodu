// ---------------------------------------------------------------------------------------------------------------
// <copyright file="EncodingExtensionsTests.Encoding.Pool.Faults.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text;

public sealed partial class EncodingExtensionsTests
{
    /// <summary>
    /// Verifies that <see cref="EncodingExtensions.GetBytesRented" /> propagates an encoder failure that occurs after
    /// the buffer is rented, exercising the pool-return cleanup path.
    /// </summary>
    [TestMethod]
    public void GetBytesRented_WhenEncodeThrowsAfterRent_ShouldPropagateException()
    {
        var encoding = new CountThenThrowEncoding();

        Assert.ThrowsExactly<InvalidOperationException>(() => _ = encoding.GetBytesRented("abc", out _));
    }

    /// <summary>
    /// Verifies that <see cref="EncodingExtensions.GetCharsRented" /> propagates a decoder failure that occurs after
    /// the buffer is rented, exercising the pool-return cleanup path.
    /// </summary>
    [TestMethod]
    public void GetCharsRented_WhenDecodeThrowsAfterRent_ShouldPropagateException()
    {
        var encoding = new CountThenThrowEncoding();

        Assert.ThrowsExactly<InvalidOperationException>(() => _ = encoding.GetCharsRented(new byte[] { 1, 2, 3 }, out _));
    }

    /// <summary>
    /// Verifies that <see cref="EncodingExtensions.GetBytesOwner" /> disposes the rented owner and propagates an
    /// encoder failure that occurs after renting.
    /// </summary>
    [TestMethod]
    public void GetBytesOwner_WhenEncodeThrowsAfterRent_ShouldPropagateException()
    {
        var encoding = new CountThenThrowEncoding();

        Assert.ThrowsExactly<InvalidOperationException>(() => _ = encoding.GetBytesOwner("abc"));
    }

    /// <summary>
    /// Verifies that <see cref="EncodingExtensions.GetCharsOwner" /> disposes the rented owner and propagates a decoder
    /// failure that occurs after renting.
    /// </summary>
    [TestMethod]
    public void GetCharsOwner_WhenDecodeThrowsAfterRent_ShouldPropagateException()
    {
        var encoding = new CountThenThrowEncoding();

        Assert.ThrowsExactly<InvalidOperationException>(() => _ = encoding.GetCharsOwner(new byte[] { 1, 2, 3 }));
    }

    /// <summary>
    /// Verifies that <see cref="EncodingExtensions.GetBytesPooled" /> disposes the builder and propagates an encoder
    /// failure that occurs after renting.
    /// </summary>
    [TestMethod]
    public void GetBytesPooled_WhenEncodeThrowsAfterRent_ShouldPropagateException()
    {
        var encoding = new CountThenThrowEncoding();

        Assert.ThrowsExactly<InvalidOperationException>(() => _ = encoding.GetBytesPooled("abc"));
    }

    /// <summary>
    /// Verifies that <see cref="EncodingExtensions.GetCharsPooled" /> disposes the builder and propagates a decoder
    /// failure that occurs after renting.
    /// </summary>
    [TestMethod]
    public void GetCharsPooled_WhenDecodeThrowsAfterRent_ShouldPropagateException()
    {
        var encoding = new CountThenThrowEncoding();

        Assert.ThrowsExactly<InvalidOperationException>(() => _ = encoding.GetCharsPooled(new byte[] { 1, 2, 3 }));
    }

    /// <summary>
    /// Verifies that <see cref="EncodingExtensions.ToOwnedBytes" /> disposes the rented owner and propagates an encoder
    /// failure that occurs after renting.
    /// </summary>
    [TestMethod]
    public void ToOwnedBytes_WhenEncodeThrowsAfterRent_ShouldPropagateException()
    {
        var encoding = new CountThenThrowEncoding();

        Assert.ThrowsExactly<InvalidOperationException>(() => _ = "abc".AsSpan().ToOwnedBytes(encoding));
    }

    /// <summary>
    /// A test encoding that reports a non-zero element count from its count methods but throws from the actual
    /// transcode methods, so the pooled and owner helpers rent a buffer and then fail mid-transcode. This drives the
    /// exception-cleanup paths that return the rented buffer to the pool or dispose the owner.
    /// </summary>
    private sealed class CountThenThrowEncoding : System.Text.Encoding
    {
        public override int GetByteCount(char[] chars, int index, int count) => count;

        public override int GetBytes(char[] chars, int charIndex, int charCount, byte[] bytes, int byteIndex) =>
            throw new InvalidOperationException("encode boom");

        public override int GetCharCount(byte[] bytes, int index, int count) => count;

        public override int GetChars(byte[] bytes, int byteIndex, int byteCount, char[] chars, int charIndex) =>
            throw new InvalidOperationException("decode boom");

        public override int GetMaxByteCount(int charCount) => charCount;

        public override int GetMaxCharCount(int byteCount) => byteCount;
    }
}
