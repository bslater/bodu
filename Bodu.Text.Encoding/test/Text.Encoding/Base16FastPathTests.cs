// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Base16FastPathTests.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Encoding;

/// <summary>
/// Regression tests for the BCL fast-path delegation in <see cref="Base16" />. The unformatted upper case encode
/// path routes through <see cref="System.Convert.ToHexString(ReadOnlySpan{byte})" />, so the Bodu output must
/// agree with the BCL byte-for-byte to maintain consumer expectations. The lower case path matches
/// <c>Convert.ToHexString(bytes).ToLowerInvariant()</c> regardless of framework.
/// </summary>
[TestClass]
public sealed class Base16FastPathTests
{

    /// <summary>
    /// Verifies that the default lower-case encode result matches the BCL upper case conversion lowered to
    /// lower case (the canonical lower-hex form).
    /// </summary>
    [TestMethod]
    public void Encode_DefaultLowerCase_ShouldMatchBclCanonicalLowerHex()
    {
        var bytes = new byte[256];
        for (var i = 0; i < 256; i++)
        {
            bytes[i] = (byte)i;
        }

        var boduResult = Base16.Encode(bytes);
        var bclResult = System.Convert.ToHexString(bytes).ToLowerInvariant();

        Assert.AreEqual(bclResult, boduResult);
    }

    /// <summary>
    /// Verifies parity with the BCL across various input sizes including edge cases (empty, single byte, large).
    /// </summary>
    /// <param name="size">The input size under test.</param>
    [TestMethod]
    [DataRow(0)]
    [DataRow(1)]
    [DataRow(2)]
    [DataRow(15)]
    [DataRow(16)]
    [DataRow(64)]
    [DataRow(1023)]
    [DataRow(1024)]
    [DataRow(4096)]
    public void Encode_ShouldMatchBclAcrossSizes(int size)
    {
        var bytes = new byte[size];
        new Random(0xC0FFEE).NextBytes(bytes);

        Assert.AreEqual(System.Convert.ToHexString(bytes).ToLowerInvariant(), Base16.Encode(bytes));
        Assert.AreEqual(System.Convert.ToHexString(bytes), Base16.Encode(bytes, BaseFormattingOptions.UpperCase));
    }

    /// <summary>
    /// Verifies that the decorated paths (prefix / spacing / line breaks / mixed) still produce the expected output
    /// after the unformatted fast path delegates to the BCL — the formatting overload is unrelated and must remain
    /// independent.
    /// </summary>
    [TestMethod]
    public void Encode_WhenDecorationsRequested_ShouldStillUseLocalFormattingPath()
    {
        var bytes = new byte[] { 0xDE, 0xAD, 0xBE, 0xEF };

        Assert.AreEqual("0xDEADBEEF",
            Base16.Encode(bytes, BaseFormattingOptions.UpperCase | BaseFormattingOptions.IncludePrefix));

        Assert.AreEqual("DE AD BE EF",
            Base16.Encode(bytes, BaseFormattingOptions.UpperCase | BaseFormattingOptions.InsertSpacing));
    }

    /// <summary>
    /// Verifies that the upper-case encode result matches <see cref="System.Convert.ToHexString(byte[])" />.
    /// </summary>
    [TestMethod]
    public void Encode_WithUpperCaseFlag_ShouldMatchBclConvertToHexString()
    {
        var bytes = new byte[256];
        for (var i = 0; i < 256; i++)
        {
            bytes[i] = (byte)i;
        }

        var boduResult = Base16.Encode(bytes, BaseFormattingOptions.UpperCase);
        var bclResult = System.Convert.ToHexString(bytes);

        Assert.AreEqual(bclResult, boduResult);
    }

}
