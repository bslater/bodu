// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PercentEncodingTests.Length.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Encoding;

public sealed partial class PercentEncodingTests
{
    /// <summary>
    /// Verifies that <see cref="PercentEncoding.GetEncodedLength(ReadOnlySpan{byte}, PercentEncodingMode)" /> equals
    /// the characters written by the encoder across modes and content shapes.
    /// </summary>
    /// <param name="input">The ASCII source text.</param>
    /// <param name="mode">The component mode.</param>
    [TestMethod]
    [DataRow("hello world", PercentEncodingMode.UriComponent)]
    [DataRow("a/b?c=d#e", PercentEncodingMode.Query)]
    [DataRow("a b+c%~", PercentEncodingMode.FormUrlEncoded)]
    [DataRow("path:seg@ment", PercentEncodingMode.PathSegment)]
    [DataRow("", PercentEncodingMode.UriComponent)]
    public void GetEncodedLength_ShouldEqualTryEncodeCharsWritten(string input, PercentEncodingMode mode)
    {
        byte[] bytes = Ascii(input);

        int reported = PercentEncoding.GetEncodedLength(bytes, mode);

        Span<char> destination = new char[PercentEncoding.GetMaxEncodedLength(bytes.Length)];
        PercentEncoding.TryEncode(bytes, destination, out int charsWritten, mode);

        Assert.AreEqual(charsWritten, reported);
    }

    /// <summary>
    /// Verifies that <see cref="PercentEncoding.GetMaxEncodedLength(int)" /> returns three characters per byte.
    /// </summary>
    [TestMethod]
    public void GetMaxEncodedLength_ShouldReturnThreePerByte()
    {
        Assert.AreEqual(30, PercentEncoding.GetMaxEncodedLength(10));
    }

    /// <summary>
    /// Verifies that <see cref="PercentEncoding.TryGetDecodedLength(ReadOnlySpan{char}, out int, PercentEncodingMode, PercentDecodingOptions)" />
    /// equals the bytes written by the decoder.
    /// </summary>
    /// <param name="encoded">The percent-encoded input.</param>
    /// <param name="mode">The component mode.</param>
    [TestMethod]
    [DataRow("a%2Fb", PercentEncodingMode.UriComponent)]
    [DataRow("a+b", PercentEncodingMode.FormUrlEncoded)]
    [DataRow("%E2%80%BD", PercentEncodingMode.UriComponent)]
    public void TryGetDecodedLength_ShouldEqualTryDecodeBytesWritten(string encoded, PercentEncodingMode mode)
    {
        bool gotLength = PercentEncoding.TryGetDecodedLength(encoded.AsSpan(), out int reported, mode);

        Span<byte> destination = new byte[PercentEncoding.GetMaxDecodedLength(encoded.Length)];
        bool decoded = PercentEncoding.TryDecode(encoded.AsSpan(), destination, out int bytesWritten, mode);

        Assert.IsTrue(gotLength);
        Assert.IsTrue(decoded);
        Assert.AreEqual(bytesWritten, reported);
    }

    /// <summary>
    /// Verifies that <see cref="PercentEncoding.GetMaxDecodedLength(int)" /> returns the input character count.
    /// </summary>
    [TestMethod]
    public void GetMaxDecodedLength_ShouldReturnCharCount()
    {
        Assert.AreEqual(42, PercentEncoding.GetMaxDecodedLength(42));
    }
}
