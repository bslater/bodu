// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PercentEncodingTests.Encode.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Encoding;

public sealed partial class PercentEncodingTests
{
    /// <summary>
    /// Verifies that unreserved characters pass through in <see cref="PercentEncodingMode.UriComponent" /> mode.
    /// </summary>
    [TestMethod]
    public void Encode_WhenUnreservedAndUriComponent_ShouldPassThrough()
    {
        string encoded = PercentEncoding.Encode(Ascii("AZaz09-._~"), PercentEncodingMode.UriComponent);

        Assert.AreEqual("AZaz09-._~", encoded);
    }

    /// <summary>
    /// Verifies the documented <see cref="PercentEncodingMode.UriComponent" /> encodings of reserved input.
    /// </summary>
    /// <param name="input">The ASCII input.</param>
    /// <param name="expected">The expected percent-encoded output.</param>
    [TestMethod]
    [DataRow("hello world", "hello%20world")]
    [DataRow("a/b?c=d", "a%2Fb%3Fc%3Dd")]
    [DataRow("100%", "100%25")]
    public void Encode_WhenUriComponent_ShouldMatchExpected(string input, string expected)
    {
        Assert.AreEqual(expected, PercentEncoding.Encode(Ascii(input), PercentEncodingMode.UriComponent));
    }

    /// <summary>
    /// Verifies that a space encodes as <c>%20</c> outside form mode and the percent sign as <c>%25</c>.
    /// </summary>
    /// <param name="mode">The component mode.</param>
    [TestMethod]
    [DataRow(PercentEncodingMode.UriComponent)]
    [DataRow(PercentEncodingMode.PathSegment)]
    [DataRow(PercentEncodingMode.Query)]
    public void Encode_WhenSpaceOrPercentOutsideFormMode_ShouldUsePercentEscapes(PercentEncodingMode mode)
    {
        Assert.AreEqual("%20", PercentEncoding.Encode(Ascii(" "), mode));
        Assert.AreEqual("%25", PercentEncoding.Encode(Ascii("%"), mode));
    }

    /// <summary>
    /// Verifies that slash is encoded in <see cref="PercentEncodingMode.UriComponent" /> and
    /// <see cref="PercentEncodingMode.PathSegment" /> modes but passes through in <see cref="PercentEncodingMode.Query" />.
    /// </summary>
    [TestMethod]
    public void Encode_WhenSlash_ShouldEncodePerMode()
    {
        Assert.AreEqual("%2F", PercentEncoding.Encode(Ascii("/"), PercentEncodingMode.UriComponent));
        Assert.AreEqual("%2F", PercentEncoding.Encode(Ascii("/"), PercentEncodingMode.PathSegment));
        Assert.AreEqual("/", PercentEncoding.Encode(Ascii("/"), PercentEncodingMode.Query));
    }

    /// <summary>
    /// Verifies that the fragment marker <c>#</c> is encoded in every component mode.
    /// </summary>
    /// <param name="mode">The component mode.</param>
    [TestMethod]
    [DataRow(PercentEncodingMode.UriComponent)]
    [DataRow(PercentEncodingMode.PathSegment)]
    [DataRow(PercentEncodingMode.Query)]
    public void Encode_WhenHash_ShouldEncodeInAllComponentModes(PercentEncodingMode mode)
    {
        Assert.AreEqual("%23", PercentEncoding.Encode(Ascii("#"), mode));
    }

    /// <summary>
    /// Verifies that <see cref="PercentEncodingMode.PathSegment" /> passes through sub-delimiters, colon, and at-sign.
    /// </summary>
    [TestMethod]
    public void Encode_WhenPathSegment_ShouldPassThroughSubDelimitersColonAt()
    {
        string encoded = PercentEncoding.Encode(Ascii("a:b@c!$&'()*+,;="), PercentEncodingMode.PathSegment);

        Assert.AreEqual("a:b@c!$&'()*+,;=", encoded);
    }

    /// <summary>
    /// Verifies that <see cref="PercentEncodingMode.Query" /> passes through slash and question mark.
    /// </summary>
    [TestMethod]
    public void Encode_WhenQuery_ShouldPassThroughSlashAndQuestion()
    {
        Assert.AreEqual("a/b?c", PercentEncoding.Encode(Ascii("a/b?c"), PercentEncodingMode.Query));
    }

    /// <summary>
    /// Verifies the WHATWG <see cref="PercentEncodingMode.FormUrlEncoded" /> rules: space becomes <c>+</c>, plus and
    /// percent are escaped, and the pass-through set excludes <c>~</c>.
    /// </summary>
    [TestMethod]
    public void Encode_WhenFormUrlEncoded_ShouldApplyWhatwgRules()
    {
        Assert.AreEqual("a+b", PercentEncoding.Encode(Ascii("a b"), PercentEncodingMode.FormUrlEncoded));
        Assert.AreEqual("%2B", PercentEncoding.Encode(Ascii("+"), PercentEncodingMode.FormUrlEncoded));
        Assert.AreEqual("%25", PercentEncoding.Encode(Ascii("%"), PercentEncodingMode.FormUrlEncoded));
        Assert.AreEqual("%7E", PercentEncoding.Encode(Ascii("~"), PercentEncodingMode.FormUrlEncoded));
        Assert.AreEqual("Az09*-._", PercentEncoding.Encode(Ascii("Az09*-._"), PercentEncodingMode.FormUrlEncoded));
    }

    /// <summary>
    /// Verifies that encoding emits uppercase hexadecimal digits.
    /// </summary>
    [TestMethod]
    public void Encode_ShouldEmitUppercaseHex()
    {
        Assert.AreEqual("%E2%80%BD", PercentEncoding.Encode(new byte[] { 0xE2, 0x80, 0xBD }, PercentEncodingMode.UriComponent));
    }

    /// <summary>
    /// Verifies that an undefined mode is rejected.
    /// </summary>
    [TestMethod]
    public void Encode_WhenModeUndefined_ShouldThrowArgumentOutOfRangeException()
    {
        ArgumentOutOfRangeException ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = PercentEncoding.Encode(Ascii("test"), (PercentEncodingMode)99);
        });

        Assert.AreEqual("mode", ex.ParamName);
    }

    /// <summary>
    /// Verifies that empty input encodes to an empty string.
    /// </summary>
    [TestMethod]
    public void Encode_WhenEmpty_ShouldReturnEmptyString()
    {
        Assert.AreEqual(string.Empty, PercentEncoding.Encode(ReadOnlySpan<byte>.Empty));
    }

    /// <summary>
    /// Verifies that <see cref="PercentEncoding.TryEncode(ReadOnlySpan{byte}, Span{char}, out int, PercentEncodingMode)" />
    /// returns <see langword="false" /> and writes zero when the destination is too small.
    /// </summary>
    [TestMethod]
    public void TryEncode_WhenDestinationTooSmall_ShouldReturnFalseAndWriteZero()
    {
        byte[] bytes = Ascii("hello world");
        Span<char> destination = new char[bytes.Length]; // shorter than "hello%20world"

        bool success = PercentEncoding.TryEncode(bytes, destination, out int charsWritten);

        Assert.IsFalse(success);
        Assert.AreEqual(0, charsWritten);
    }
}
