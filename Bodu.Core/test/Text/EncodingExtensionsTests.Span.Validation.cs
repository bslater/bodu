// ---------------------------------------------------------------------------------------------------------------
// <copyright file="EncodingExtensionsTests.Span.Validation.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text;

public sealed partial class EncodingExtensionsTests
{
    /// <summary>
    /// Verifies that <see cref="EncodingExtensions.ThrowIfTooSmallForEncoding(Span{byte}, ReadOnlySpan{char}, System.Text.Encoding, string)" />
    /// does not throw when the destination is exactly the required size.
    /// </summary>
    [TestMethod]
    public void ThrowIfTooSmallForEncoding_WhenDestinationFitsExactly_ShouldNotThrow()
    {
        var required = System.Text.Encoding.UTF8.GetByteCount(MultiByteText);
        Span<byte> destination = new byte[required];

        destination.ThrowIfTooSmallForEncoding(MultiByteText.AsSpan(), System.Text.Encoding.UTF8);
    }

    /// <summary>
    /// Verifies that <see cref="EncodingExtensions.ThrowIfTooSmallForEncoding(Span{byte}, ReadOnlySpan{char}, System.Text.Encoding, string)" />
    /// does not throw when the destination has more than enough capacity.
    /// </summary>
    [TestMethod]
    public void ThrowIfTooSmallForEncoding_WhenDestinationIsOversized_ShouldNotThrow()
    {
        var required = System.Text.Encoding.UTF8.GetByteCount(MultiByteText);
        Span<byte> destination = new byte[required + 64];

        destination.ThrowIfTooSmallForEncoding(MultiByteText.AsSpan(), System.Text.Encoding.UTF8);
    }

    /// <summary>
    /// Verifies that <see cref="EncodingExtensions.ThrowIfTooSmallForEncoding(Span{byte}, ReadOnlySpan{char}, System.Text.Encoding, string)" />
    /// throws <see cref="ArgumentException" /> when the destination is one byte too small.
    /// </summary>
    [TestMethod]
    public void ThrowIfTooSmallForEncoding_WhenDestinationIsOneByteTooSmall_ShouldThrowExactly()
    {
        var required = System.Text.Encoding.UTF8.GetByteCount(MultiByteText);
        var backing = new byte[required - 1];

        ArgumentException ex = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            Span<byte> destination = backing;
            destination.ThrowIfTooSmallForEncoding(MultiByteText.AsSpan(), System.Text.Encoding.UTF8);
        });

        Assert.AreEqual("destination", ex.ParamName);
    }

    /// <summary>
    /// Verifies that <see cref="EncodingExtensions.ThrowIfTooSmallForEncoding(Span{byte}, ReadOnlySpan{char}, System.Text.Encoding, string)" />
    /// reports the supplied parameter name when provided.
    /// </summary>
    [TestMethod]
    public void ThrowIfTooSmallForEncoding_WhenParamNameSupplied_ShouldUseIt()
    {
        var backing = new byte[1];

        ArgumentException ex = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            Span<byte> destination = backing;
            destination.ThrowIfTooSmallForEncoding(MultiByteText.AsSpan(), System.Text.Encoding.UTF8, "myBuffer");
        });

        Assert.AreEqual("myBuffer", ex.ParamName);
    }

    /// <summary>
    /// Verifies that <see cref="EncodingExtensions.ThrowIfTooSmallForEncoding(Span{byte}, ReadOnlySpan{char}, System.Text.Encoding, string)" />
    /// throws <see cref="ArgumentNullException" /> when <c>encoding</c> is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void ThrowIfTooSmallForEncoding_WhenEncodingIsNull_ShouldThrowExactly()
    {
        var backing = new byte[64];

        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            Span<byte> destination = backing;
            destination.ThrowIfTooSmallForEncoding(SampleText.AsSpan(), null!);
        });

        Assert.AreEqual("encoding", ex.ParamName);
    }

    /// <summary>
    /// Verifies that <see cref="EncodingExtensions.ThrowIfTooSmallForDecoding(Span{char}, ReadOnlySpan{byte}, System.Text.Encoding, string)" />
    /// does not throw when the destination is exactly the required size.
    /// </summary>
    [TestMethod]
    public void ThrowIfTooSmallForDecoding_WhenDestinationFitsExactly_ShouldNotThrow()
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(MultiByteText);
        var required = System.Text.Encoding.UTF8.GetCharCount(bytes);
        Span<char> destination = new char[required];

        destination.ThrowIfTooSmallForDecoding(bytes, System.Text.Encoding.UTF8);
    }

    /// <summary>
    /// Verifies that <see cref="EncodingExtensions.ThrowIfTooSmallForDecoding(Span{char}, ReadOnlySpan{byte}, System.Text.Encoding, string)" />
    /// does not throw when the destination has more than enough capacity.
    /// </summary>
    [TestMethod]
    public void ThrowIfTooSmallForDecoding_WhenDestinationIsOversized_ShouldNotThrow()
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(MultiByteText);
        var required = System.Text.Encoding.UTF8.GetCharCount(bytes);
        Span<char> destination = new char[required + 32];

        destination.ThrowIfTooSmallForDecoding(bytes, System.Text.Encoding.UTF8);
    }

    /// <summary>
    /// Verifies that <see cref="EncodingExtensions.ThrowIfTooSmallForDecoding(Span{char}, ReadOnlySpan{byte}, System.Text.Encoding, string)" />
    /// throws <see cref="ArgumentException" /> when the destination is one character too small.
    /// </summary>
    [TestMethod]
    public void ThrowIfTooSmallForDecoding_WhenDestinationIsOneCharTooSmall_ShouldThrowExactly()
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(MultiByteText);
        var required = System.Text.Encoding.UTF8.GetCharCount(bytes);
        var backing = new char[required - 1];

        ArgumentException ex = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            Span<char> destination = backing;
            destination.ThrowIfTooSmallForDecoding(bytes, System.Text.Encoding.UTF8);
        });

        Assert.AreEqual("destination", ex.ParamName);
    }

    /// <summary>
    /// Verifies that <see cref="EncodingExtensions.ThrowIfTooSmallForDecoding(Span{char}, ReadOnlySpan{byte}, System.Text.Encoding, string)" />
    /// throws <see cref="ArgumentNullException" /> when <c>encoding</c> is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void ThrowIfTooSmallForDecoding_WhenEncodingIsNull_ShouldThrowExactly()
    {
        var backing = new char[64];

        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            Span<char> destination = backing;
            destination.ThrowIfTooSmallForDecoding([0x68], null!);
        });

        Assert.AreEqual("encoding", ex.ParamName);
    }
}
