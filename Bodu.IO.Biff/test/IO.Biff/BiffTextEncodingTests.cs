// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BiffTextEncodingTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;

namespace Bodu.IO.Biff;

/// <summary>
/// Tests for <see cref="BiffTextEncoding" />: code-page normalization and encoding resolution.
/// </summary>
[TestClass]
public sealed class BiffTextEncodingTests
{
    /// <summary>
    /// Verifies that the private BIFF markers and zero normalize to Windows code page numbers, and other values pass
    /// through.
    /// </summary>
    /// <param name="raw">The raw value.</param>
    /// <param name="expected">The normalized value.</param>
    [TestMethod]
    [DataRow(0x8000, 10000)]
    [DataRow(0x8001, 1252)]
    [DataRow(0, 1252)]
    [DataRow(1251, 1251)]
    [DataRow(1200, 1200)]
    public void Normalize_WhenRawValue_ShouldMap(int raw, int expected)
    {
        Assert.AreEqual(expected, BiffTextEncoding.Normalize(raw));
    }

    /// <summary>
    /// Verifies that legacy and Unicode code pages resolve to encodings.
    /// </summary>
    /// <param name="codePage">The code page.</param>
    /// <param name="expectedCodePage">The code page of the resolved encoding.</param>
    [TestMethod]
    [DataRow(1252, 1252)]
    [DataRow(437, 437)]
    [DataRow(932, 932)]
    [DataRow(0x8000, 10000)]
    [DataRow(1200, 1200)]
    public void GetEncoding_WhenKnownCodePage_ShouldResolve(int codePage, int expectedCodePage)
    {
        Encoding encoding = BiffTextEncoding.GetEncoding(codePage);

        Assert.AreEqual(expectedCodePage, encoding.CodePage);
    }

    /// <summary>
    /// Verifies that an unknown code page is reported as a format error.
    /// </summary>
    [TestMethod]
    public void GetEncoding_WhenUnknownCodePage_ShouldThrowBiffFormatException()
    {
        var ex = Assert.ThrowsExactly<BiffFormatException>(() => _ = BiffTextEncoding.GetEncoding(12345));

        Assert.IsNotNull(ex.InnerException);
        Assert.IsTrue(ex.Message.Contains("12345", StringComparison.Ordinal));
    }
}
