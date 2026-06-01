// ---------------------------------------------------------------------------------------------------------------
// <copyright file="EncodingExtensionsTests.Span.Decode.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Encoding;

public sealed partial class EncodingExtensionsTests
{
    /// <summary>
    /// Verifies that <see cref="EncodingExtensions.GetDecodedCharCount(ReadOnlySpan{byte}, System.Text.Encoding)" />
    /// returns the same value as <see cref="System.Text.Encoding.GetCharCount(byte[])" /> for every canonical
    /// encoding.
    /// </summary>
    /// <param name="encoding">The encoding under test.</param>
    [TestMethod]
    [DynamicData(nameof(CanonicalEncodings))]
    public void GetDecodedCharCount_WhenCalledOnSpan_ShouldMatchBclEncoding(System.Text.Encoding encoding)
    {
        var bytes = encoding.GetBytes(MultiByteText);
        var expected = encoding.GetCharCount(bytes);

        var actual = ((ReadOnlySpan<byte>)bytes).GetDecodedCharCount(encoding);

        Assert.AreEqual(expected, actual);
    }

    /// <summary>
    /// Verifies that <see cref="EncodingExtensions.GetDecodedCharCount(ReadOnlySpan{byte}, System.Text.Encoding)" />
    /// returns zero for an empty span.
    /// </summary>
    [TestMethod]
    public void GetDecodedCharCount_WhenSpanIsEmpty_ShouldReturnZero()
    {
        var actual = ReadOnlySpan<byte>.Empty.GetDecodedCharCount(System.Text.Encoding.UTF8);

        Assert.AreEqual(0, actual);
    }

    /// <summary>
    /// Verifies that <see cref="EncodingExtensions.GetDecodedCharCount(ReadOnlySpan{byte}, System.Text.Encoding)" />
    /// throws <see cref="ArgumentNullException" /> when <c>encoding</c> is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void GetDecodedCharCount_WhenEncodingIsNull_ShouldThrowExactly()
    {
        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = ((ReadOnlySpan<byte>)[0x68]).GetDecodedCharCount(null!);
        });

        Assert.AreEqual("encoding", ex.ParamName);
    }

    /// <summary>
    /// Verifies that <see cref="EncodingExtensions.ToChars(ReadOnlySpan{byte}, System.Text.Encoding)" /> produces
    /// the same characters as the BCL <see cref="System.Text.Encoding.GetChars(byte[])" /> method for every
    /// canonical encoding.
    /// </summary>
    /// <param name="encoding">The encoding under test.</param>
    [TestMethod]
    [DynamicData(nameof(CanonicalEncodings))]
    public void ToChars_WhenCalledOnSpan_ShouldMatchBclEncoding(System.Text.Encoding encoding)
    {
        var bytes = encoding.GetBytes(MultiByteText);
        var expected = encoding.GetChars(bytes);

        var actual = ((ReadOnlySpan<byte>)bytes).ToChars(encoding);

        CollectionAssert.AreEqual(expected, actual);
    }

    /// <summary>
    /// Verifies that <see cref="EncodingExtensions.ToChars(ReadOnlySpan{byte}, System.Text.Encoding)" /> returns
    /// the same singleton empty array as <see cref="Array.Empty{T}" /> when the input is empty.
    /// </summary>
    [TestMethod]
    public void ToChars_WhenSpanIsEmpty_ShouldReturnEmptyArray()
    {
        var result = ReadOnlySpan<byte>.Empty.ToChars(System.Text.Encoding.UTF8);

        Assert.AreEqual(0, result.Length);
        Assert.AreSame([], result);
    }

    /// <summary>
    /// Verifies that <see cref="EncodingExtensions.ToChars(ReadOnlySpan{byte}, System.Text.Encoding)" /> throws
    /// <see cref="ArgumentNullException" /> when <c>encoding</c> is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void ToChars_WhenEncodingIsNull_ShouldThrowExactly()
    {
        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = ((ReadOnlySpan<byte>)[0x68]).ToChars(null!);
        });

        Assert.AreEqual("encoding", ex.ParamName);
    }

    /// <summary>
    /// Verifies that <see cref="EncodingExtensions.DecodeToString(ReadOnlySpan{byte}, System.Text.Encoding)" />
    /// produces the same string as the BCL <see cref="System.Text.Encoding.GetString(byte[])" /> method.
    /// </summary>
    /// <param name="encoding">The encoding under test.</param>
    [TestMethod]
    [DynamicData(nameof(CanonicalEncodings))]
    public void DecodeToString_WhenCalledOnSpan_ShouldMatchBclEncoding(System.Text.Encoding encoding)
    {
        var bytes = encoding.GetBytes(MultiByteText);

        var actual = ((ReadOnlySpan<byte>)bytes).DecodeToString(encoding);

        Assert.AreEqual(encoding.GetString(bytes), actual);
    }

    /// <summary>
    /// Verifies that <see cref="EncodingExtensions.DecodeToString(ReadOnlySpan{byte}, System.Text.Encoding)" />
    /// returns the empty string when the input span is empty.
    /// </summary>
    [TestMethod]
    public void DecodeToString_WhenSpanIsEmpty_ShouldReturnEmptyString()
    {
        var actual = ReadOnlySpan<byte>.Empty.DecodeToString(System.Text.Encoding.UTF8);

        Assert.AreEqual(string.Empty, actual);
    }

    /// <summary>
    /// Verifies that <see cref="EncodingExtensions.DecodeToString(ReadOnlySpan{byte}, System.Text.Encoding)" />
    /// throws <see cref="ArgumentNullException" /> when <c>encoding</c> is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void DecodeToString_WhenEncodingIsNull_ShouldThrowExactly()
    {
        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = ((ReadOnlySpan<byte>)[0x68]).DecodeToString(null!);
        });

        Assert.AreEqual("encoding", ex.ParamName);
    }

    /// <summary>
    /// Verifies that <see cref="EncodingExtensions.DecodeTo(ReadOnlySpan{byte}, System.Text.Encoding, Span{char})" />
    /// writes the decoded characters and returns the correct count when the destination is exactly the right size.
    /// </summary>
    [TestMethod]
    public void DecodeTo_WhenDestinationIsExactlySized_ShouldWriteAndReturnCount()
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(MultiByteText);
        var required = System.Text.Encoding.UTF8.GetCharCount(bytes);
        Span<char> destination = new char[required];

        var written = ((ReadOnlySpan<byte>)bytes).DecodeTo(System.Text.Encoding.UTF8, destination);

        Assert.AreEqual(required, written);
        Assert.AreEqual(MultiByteText, new string(destination));
    }

    /// <summary>
    /// Verifies that <see cref="EncodingExtensions.DecodeTo(ReadOnlySpan{byte}, System.Text.Encoding, Span{char})" />
    /// throws <see cref="ArgumentException" /> when the destination is one character too small.
    /// </summary>
    [TestMethod]
    public void DecodeTo_WhenDestinationIsOneCharTooSmall_ShouldThrowExactly()
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(MultiByteText);
        var required = System.Text.Encoding.UTF8.GetCharCount(bytes);
        var backing = new char[required - 1];

        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = ((ReadOnlySpan<byte>)bytes).DecodeTo(System.Text.Encoding.UTF8, backing);
        });
    }

    /// <summary>
    /// Verifies that <see cref="EncodingExtensions.DecodeTo(ReadOnlySpan{byte}, System.Text.Encoding, Span{char})" />
    /// throws <see cref="ArgumentNullException" /> when <c>encoding</c> is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void DecodeTo_WhenEncodingIsNull_ShouldThrowExactly()
    {
        var backing = new char[64];

        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = ((ReadOnlySpan<byte>)[0x68]).DecodeTo(null!, backing);
        });

        Assert.AreEqual("encoding", ex.ParamName);
    }

    /// <summary>
    /// Verifies that <see cref="EncodingExtensions.DecodeExactlyTo(ReadOnlySpan{byte}, System.Text.Encoding, Span{char})" />
    /// succeeds when the destination is exactly the right size.
    /// </summary>
    [TestMethod]
    public void DecodeExactlyTo_WhenDestinationIsExactlySized_ShouldWriteAndReturnCount()
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(MultiByteText);
        var required = System.Text.Encoding.UTF8.GetCharCount(bytes);
        Span<char> destination = new char[required];

        var written = ((ReadOnlySpan<byte>)bytes).DecodeExactlyTo(System.Text.Encoding.UTF8, destination);

        Assert.AreEqual(required, written);
    }

    /// <summary>
    /// Verifies that <see cref="EncodingExtensions.DecodeExactlyTo(ReadOnlySpan{byte}, System.Text.Encoding, Span{char})" />
    /// throws <see cref="ArgumentException" /> when the destination is larger than the required size.
    /// </summary>
    [TestMethod]
    public void DecodeExactlyTo_WhenDestinationIsLargerThanRequired_ShouldThrowExactly()
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(SampleText);
        var required = System.Text.Encoding.UTF8.GetCharCount(bytes);
        var backing = new char[required + 1];

        ArgumentException ex = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = ((ReadOnlySpan<byte>)bytes).DecodeExactlyTo(System.Text.Encoding.UTF8, backing);
        });

        Assert.AreEqual("destination", ex.ParamName);
    }

    /// <summary>
    /// Verifies that <see cref="EncodingExtensions.DecodeExactlyTo(ReadOnlySpan{byte}, System.Text.Encoding, Span{char})" />
    /// throws <see cref="ArgumentException" /> when the destination is smaller than the required size.
    /// </summary>
    [TestMethod]
    public void DecodeExactlyTo_WhenDestinationIsSmallerThanRequired_ShouldThrowExactly()
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(SampleText);
        var required = System.Text.Encoding.UTF8.GetCharCount(bytes);
        var backing = new char[required - 1];

        ArgumentException ex = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = ((ReadOnlySpan<byte>)bytes).DecodeExactlyTo(System.Text.Encoding.UTF8, backing);
        });

        Assert.AreEqual("destination", ex.ParamName);
    }

    /// <summary>
    /// Verifies that <see cref="EncodingExtensions.DecodeExactlyTo(ReadOnlySpan{byte}, System.Text.Encoding, Span{char})" />
    /// throws <see cref="ArgumentNullException" /> when <c>encoding</c> is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void DecodeExactlyTo_WhenEncodingIsNull_ShouldThrowExactly()
    {
        var backing = new char[64];

        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = ((ReadOnlySpan<byte>)[0x68]).DecodeExactlyTo(null!, backing);
        });

        Assert.AreEqual("encoding", ex.ParamName);
    }

    /// <summary>
    /// Verifies that <see cref="EncodingExtensions.TryDecodeTo(ReadOnlySpan{byte}, System.Text.Encoding, Span{char}, out int)" />
    /// returns <see langword="true" /> and reports the character count when the destination is exactly the right
    /// size.
    /// </summary>
    [TestMethod]
    public void TryDecodeTo_WhenDestinationFits_ShouldReturnTrueAndReportCount()
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(MultiByteText);
        var required = System.Text.Encoding.UTF8.GetCharCount(bytes);
        Span<char> destination = new char[required];

        var ok = ((ReadOnlySpan<byte>)bytes).TryDecodeTo(System.Text.Encoding.UTF8, destination, out var written);

        Assert.IsTrue(ok);
        Assert.AreEqual(required, written);
    }

    /// <summary>
    /// Verifies that <see cref="EncodingExtensions.TryDecodeTo(ReadOnlySpan{byte}, System.Text.Encoding, Span{char}, out int)" />
    /// returns <see langword="false" /> and reports zero characters written when the destination is one character
    /// too small.
    /// </summary>
    [TestMethod]
    public void TryDecodeTo_WhenDestinationIsOneCharTooSmall_ShouldReturnFalseAndZeroCharsWritten()
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(MultiByteText);
        var required = System.Text.Encoding.UTF8.GetCharCount(bytes);
        var backing = new char[required - 1];

        var ok = ((ReadOnlySpan<byte>)bytes).TryDecodeTo(System.Text.Encoding.UTF8, backing, out var written);

        Assert.IsFalse(ok);
        Assert.AreEqual(0, written);
    }

    /// <summary>
    /// Verifies that <see cref="EncodingExtensions.TryDecodeTo(ReadOnlySpan{byte}, System.Text.Encoding, Span{char}, out int)" />
    /// throws <see cref="ArgumentNullException" /> when <c>encoding</c> is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void TryDecodeTo_WhenEncodingIsNull_ShouldThrowExactly()
    {
        var backing = new char[64];

        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = ((ReadOnlySpan<byte>)[0x68]).TryDecodeTo(null!, backing, out _);
        });

        Assert.AreEqual("encoding", ex.ParamName);
    }
}
