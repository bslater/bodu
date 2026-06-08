// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BencodeExtensionsTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Bencode;

/// <summary>
/// Behavioural tests for <see cref="BencodeExtensions" />.
/// </summary>
[TestClass]
public sealed class BencodeExtensionsTests
{
    private static readonly byte[] CanonicalEncodedInteger = "i42e"u8.ToArray();

    /// <summary>
    /// Verifies that <see cref="BencodeExtensions.ParseBencode(ReadOnlySpan{byte})" /> produces the same value as the
    /// canonical <see cref="Bencode.Parse(ReadOnlySpan{byte})" /> static method.
    /// </summary>
    [TestMethod]
    public void ParseBencode_OnReadOnlySpan_ShouldMatchStaticCanonical()
    {
        BencodedValue fromExtension = ((ReadOnlySpan<byte>)CanonicalEncodedInteger).ParseBencode();
        BencodedValue fromStatic = Bencode.Parse(CanonicalEncodedInteger);

        Assert.AreEqual(fromStatic, fromExtension);
    }

    /// <summary>
    /// Verifies that <see cref="BencodeExtensions.ParseBencode(byte[])" /> produces the same value as the canonical
    /// static method.
    /// </summary>
    [TestMethod]
    public void ParseBencode_OnByteArray_ShouldMatchStaticCanonical()
    {
        BencodedValue fromExtension = CanonicalEncodedInteger.ParseBencode();
        BencodedValue fromStatic = Bencode.Parse(CanonicalEncodedInteger);

        Assert.AreEqual(fromStatic, fromExtension);
    }

    /// <summary>
    /// Verifies that <see cref="BencodeExtensions.ParseBencode(byte[])" /> throws
    /// <see cref="ArgumentNullException" /> when the receiver is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void ParseBencode_OnByteArray_WhenSourceIsNull_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = ((byte[])null!).ParseBencode();
        });
    }

    /// <summary>
    /// Verifies that <see cref="BencodeExtensions.TryParseBencode(ReadOnlySpan{byte}, out BencodedValue?, out int)" />
    /// returns <see langword="true" /> and yields the same value and consumed-byte count as the canonical static
    /// <see cref="Bencode.TryParse(ReadOnlySpan{byte}, out BencodedValue?, out int)" /> method.
    /// </summary>
    [TestMethod]
    public void TryParseBencode_OnReadOnlySpan_ShouldMatchStaticCanonical()
    {
        var extSuccess = ((ReadOnlySpan<byte>)CanonicalEncodedInteger)
            .TryParseBencode(out BencodedValue? extValue, out var extBytesConsumed);
        var staticSuccess = Bencode.TryParse(
            CanonicalEncodedInteger, out BencodedValue? staticValue, out var staticBytesConsumed);

        Assert.AreEqual(staticSuccess, extSuccess);
        Assert.AreEqual(staticBytesConsumed, extBytesConsumed);
        Assert.AreEqual(staticValue, extValue);
    }

    /// <summary>
    /// Verifies that <see cref="BencodeExtensions.FormatBencode(BencodedValue)" /> returns the same bytes as the
    /// canonical <see cref="Bencode.Format(BencodedValue)" /> static method.
    /// </summary>
    [TestMethod]
    public void FormatBencode_OnBencodedValue_ShouldMatchStaticCanonical()
    {
        BencodedValue value = new BencodedInteger(42);

        var fromExtension = value.FormatBencode();
        var fromStatic = Bencode.Format(value);

        CollectionAssert.AreEqual(fromStatic, fromExtension);
    }

    /// <summary>
    /// Verifies that <see cref="BencodeExtensions.TryFormatBencode(BencodedValue, Span{byte}, out int)" /> matches the
    /// canonical static <see cref="Bencode.TryFormat(BencodedValue, Span{byte}, out int)" /> method.
    /// </summary>
    [TestMethod]
    public void TryFormatBencode_OnBencodedValue_ShouldMatchStaticCanonical()
    {
        BencodedValue value = new BencodedInteger(42);
        Span<byte> extBuffer = stackalloc byte[16];
        Span<byte> staticBuffer = stackalloc byte[16];

        var extSuccess = value.TryFormatBencode(extBuffer, out var extWritten);
        var staticSuccess = Bencode.TryFormat(value, staticBuffer, out var staticWritten);

        Assert.AreEqual(staticSuccess, extSuccess);
        Assert.AreEqual(staticWritten, extWritten);
        Assert.IsTrue(extBuffer[..extWritten].SequenceEqual(staticBuffer[..staticWritten]));
    }

    /// <summary>
    /// Verifies that <see cref="BencodeExtensions.GetBencodedLength(BencodedValue)" /> matches the canonical static
    /// <see cref="Bencode.GetFormattedLength(BencodedValue)" /> method.
    /// </summary>
    [TestMethod]
    public void GetBencodedLength_OnBencodedValue_ShouldMatchStaticCanonical()
    {
        BencodedValue value = new BencodedInteger(42);

        var fromExtension = value.GetBencodedLength();
        var fromStatic = Bencode.GetFormattedLength(value);

        Assert.AreEqual(fromStatic, fromExtension);
    }

    /// <summary>
    /// Verifies that <c>ParseBencode</c> followed by <c>FormatBencode</c> round-trips a value through the extension
    /// surface.
    /// </summary>
    [TestMethod]
    public void ParseBencodeThenFormatBencode_ShouldRoundTrip()
    {
        BencodedValue first = CanonicalEncodedInteger.ParseBencode();
        var emitted = first.FormatBencode();
        BencodedValue second = emitted.ParseBencode();

        Assert.AreEqual(first, second);
        CollectionAssert.AreEqual(CanonicalEncodedInteger, emitted);
    }

    /// <summary>
    /// Verifies that <see cref="BencodeExtensions.FormatBencode(BencodedValue)" /> throws
    /// <see cref="ArgumentNullException" /> when the receiver is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void FormatBencode_WhenValueIsNull_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = ((BencodedValue)null!).FormatBencode();
        });
    }
}
