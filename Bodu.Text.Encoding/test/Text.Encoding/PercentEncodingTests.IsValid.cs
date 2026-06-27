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
    /// Verifies that <see cref="PercentEncoding.IsValid(ReadOnlySpan{char}, PercentEncodingMode, PercentDecodingOptions)" />
    /// agrees with <see cref="PercentEncoding.TryDecode(ReadOnlySpan{char}, Span{byte}, out int, PercentEncodingMode, PercentDecodingOptions)" />.
    /// </summary>
    /// <param name="input">The candidate input.</param>
    [TestMethod]
    [DataRow("abc")]
    [DataRow("a%2Fb")]
    [DataRow("%GG")]
    [DataRow("a+b")]
    [DataRow("é")]
    public void IsValid_ShouldAgreeWithTryDecode(string input)
    {
        foreach (PercentEncodingMode mode in new[] { PercentEncodingMode.UriComponent, PercentEncodingMode.FormUrlEncoded })
        {
            foreach (PercentDecodingOptions options in new[] { PercentDecodingOptions.None, PercentDecodingOptions.AllowInvalidPercentLiterals })
            {
                Span<byte> destination = new byte[PercentEncoding.GetMaxDecodedLength(input.Length)];
                bool tryDecode = PercentEncoding.TryDecode(input.AsSpan(), destination, out _, mode, options);

                bool isValid = PercentEncoding.IsValid(input.AsSpan(), mode, options);

                Assert.AreEqual(isValid, tryDecode, $"Disagreement for '{input}' (mode {mode}, options {options}).");
            }
        }
    }
}
