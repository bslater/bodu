// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PercentEncodingTests.IsValid.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Encoding;

public sealed partial class PercentEncodingTests
{
    /// <summary>
    /// Verifies that <see cref="PercentEncoding.IsValid(ReadOnlySpan{char}, PercentEncodingMode, PercentDecodingOptions)" />
    /// accepts well-formed input and rejects malformed input.
    /// </summary>
    /// <param name="input">The candidate input.</param>
    /// <param name="expected">The expected validity.</param>
    [TestMethod]
    [DataRow("abc", true)]
    [DataRow("a%2Fb", true)]
    [DataRow("%E2%80%BD", true)]
    [DataRow("", true)]
    [DataRow("%", false)]
    [DataRow("%A", false)]
    [DataRow("%GG", false)]
    [DataRow("é", false)]
    public void IsValid_ShouldMatchExpected(string input, bool expected)
    {
        Assert.AreEqual(expected, PercentEncoding.IsValid(input.AsSpan()));
    }

    /// <summary>
    /// Verifies that the relaxed option makes otherwise-invalid percent literals valid.
    /// </summary>
    [TestMethod]
    public void IsValid_WhenInvalidPercentLiteralAndRelaxed_ShouldReturnTrue()
    {
        Assert.IsTrue(PercentEncoding.IsValid("%GG".AsSpan(), PercentEncodingMode.UriComponent, PercentDecodingOptions.AllowInvalidPercentLiterals));
    }

    /// <summary>
    /// Verifies that whenever <see cref="PercentEncoding.IsValid(ReadOnlySpan{char}, PercentEncodingMode, PercentDecodingOptions)" />
    /// returns <see langword="true" /> (canonical), the lenient
    /// <see cref="PercentEncoding.TryDecode(ReadOnlySpan{char}, Span{byte}, out int, PercentEncodingMode, PercentDecodingOptions)" />
    /// also succeeds. The converse does not hold — canonical validity is strictly stronger than decodability.
    /// </summary>
    /// <param name="input">The candidate input.</param>
    [TestMethod]
    [DataRow("abc")]
    [DataRow("a%2Fb")]
    [DataRow("%GG")]
    [DataRow("a+b")]
    [DataRow("é")]
    public void IsValid_WhenTrue_ShouldImplyTryDecodeSucceeds(string input)
    {
        foreach (PercentEncodingMode mode in new[] { PercentEncodingMode.UriComponent, PercentEncodingMode.FormUrlEncoded })
        {
            foreach (PercentDecodingOptions options in new[] { PercentDecodingOptions.None, PercentDecodingOptions.AllowInvalidPercentLiterals })
            {
                if (!PercentEncoding.IsValid(input.AsSpan(), mode, options))
                    continue;

                Span<byte> destination = new byte[PercentEncoding.GetMaxDecodedLength(input.Length)];
                bool tryDecode = PercentEncoding.TryDecode(input.AsSpan(), destination, out _, mode, options);

                Assert.IsTrue(tryDecode, $"IsValid implied decodability but TryDecode failed for '{input}' (mode {mode}, options {options}).");
            }
        }
    }

    /// <summary>
    /// Verifies that canonical validation rejects a literal character the selected mode would have percent-encoded.
    /// </summary>
    /// <param name="value">The candidate input.</param>
    /// <param name="mode">The component mode.</param>
    [TestMethod]
    [DataRow("#", PercentEncodingMode.UriComponent)]
    [DataRow("a b", PercentEncodingMode.UriComponent)]
    [DataRow("+", PercentEncodingMode.UriComponent)]
    [DataRow("/", PercentEncodingMode.PathSegment)]
    [DataRow("?", PercentEncodingMode.PathSegment)]
    [DataRow("#", PercentEncodingMode.Query)]
    public void IsValid_ShouldRejectLiteralCharacters_NotAllowedByMode(string value, PercentEncodingMode mode)
    {
        Assert.IsFalse(PercentEncoding.IsValid(value.AsSpan(), mode));
    }

    /// <summary>
    /// Verifies that canonical validation accepts a literal character once the mode permits it unescaped (slash and
    /// question mark are allowed in <see cref="PercentEncodingMode.Query" />).
    /// </summary>
    [TestMethod]
    public void IsValid_ShouldAcceptLiteralCharacters_AllowedByMode()
    {
        Assert.IsTrue(PercentEncoding.IsValid("a/b?c".AsSpan(), PercentEncodingMode.Query));
        Assert.IsTrue(PercentEncoding.IsValid("a:b@c".AsSpan(), PercentEncodingMode.PathSegment));
    }

    /// <summary>
    /// Verifies that canonical validation treats <c>+</c> as the canonical space in
    /// <see cref="PercentEncodingMode.FormUrlEncoded" />.
    /// </summary>
    [TestMethod]
    public void IsValid_FormUrlEncoded_ShouldAcceptPlusAsSpace()
    {
        Assert.IsTrue(PercentEncoding.IsValid("a+b".AsSpan(), PercentEncodingMode.FormUrlEncoded));
    }

    /// <summary>
    /// Verifies that a percent-escaped octet is canonical even when the same character would be invalid as a literal:
    /// <c>%2F</c> is valid in <see cref="PercentEncodingMode.PathSegment" /> although a literal <c>/</c> is not.
    /// </summary>
    [TestMethod]
    public void IsValid_ShouldAcceptEscapedOctet_WhenLiteralWouldBeInvalid()
    {
        Assert.IsFalse(PercentEncoding.IsValid("/".AsSpan(), PercentEncodingMode.PathSegment));
        Assert.IsTrue(PercentEncoding.IsValid("%2F".AsSpan(), PercentEncodingMode.PathSegment));
    }
}
