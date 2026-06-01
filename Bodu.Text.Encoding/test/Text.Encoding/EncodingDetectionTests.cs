// ---------------------------------------------------------------------------------------------------------------
// <copyright file="EncodingDetectionTests.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Encoding;

/// <summary>
/// Behavioural tests for <see cref="EncodingDetection" />. Validates BOM-based detection of UTF-8, UTF-16
/// little- and big-endian, and UTF-32 little- and big-endian preambles.
/// </summary>
[TestClass]
public sealed class EncodingDetectionTests
{
    /// <summary>
    /// Verifies that <see cref="EncodingDetection.TryDetectByPreamble" /> detects the UTF-8 preamble.
    /// </summary>
    [TestMethod]
    public void TryDetectByPreamble_WhenInputStartsWithUtf8Bom_ShouldReturnUtf8()
    {
        var bytes = new byte[] { 0xEF, 0xBB, 0xBF, 0x41 };

        var ok = EncodingDetection.TryDetectByPreamble(bytes, out System.Text.Encoding? encoding);

        Assert.IsTrue(ok);
        Assert.IsNotNull(encoding);
        Assert.AreEqual(65001, encoding.CodePage);
    }

    /// <summary>
    /// Verifies that <see cref="EncodingDetection.TryDetectByPreamble" /> detects the UTF-16 little-endian
    /// preamble when followed by text that is not <c>00 00</c>.
    /// </summary>
    [TestMethod]
    public void TryDetectByPreamble_WhenInputStartsWithUtf16LeBom_ShouldReturnUnicode()
    {
        var bytes = new byte[] { 0xFF, 0xFE, 0x41, 0x00 };

        var ok = EncodingDetection.TryDetectByPreamble(bytes, out System.Text.Encoding? encoding);

        Assert.IsTrue(ok);
        Assert.IsNotNull(encoding);
        Assert.AreEqual(1200, encoding.CodePage);
    }

    /// <summary>
    /// Verifies that <see cref="EncodingDetection.TryDetectByPreamble" /> detects the UTF-16 big-endian preamble.
    /// </summary>
    [TestMethod]
    public void TryDetectByPreamble_WhenInputStartsWithUtf16BeBom_ShouldReturnBigEndianUnicode()
    {
        var bytes = new byte[] { 0xFE, 0xFF, 0x00, 0x41 };

        var ok = EncodingDetection.TryDetectByPreamble(bytes, out System.Text.Encoding? encoding);

        Assert.IsTrue(ok);
        Assert.IsNotNull(encoding);
        Assert.AreEqual(1201, encoding.CodePage);
    }

    /// <summary>
    /// Verifies that <see cref="EncodingDetection.TryDetectByPreamble" /> distinguishes the UTF-32 little-endian
    /// preamble (<c>FF FE 00 00</c>) from a bare UTF-16 little-endian preamble.
    /// </summary>
    [TestMethod]
    public void TryDetectByPreamble_WhenInputStartsWithUtf32LeBom_ShouldReturnUtf32()
    {
        var bytes = new byte[] { 0xFF, 0xFE, 0x00, 0x00, 0x41, 0x00, 0x00, 0x00 };

        var ok = EncodingDetection.TryDetectByPreamble(bytes, out System.Text.Encoding? encoding);

        Assert.IsTrue(ok);
        Assert.IsNotNull(encoding);
        Assert.AreEqual(12000, encoding.CodePage);
    }

    /// <summary>
    /// Verifies that <see cref="EncodingDetection.TryDetectByPreamble" /> detects the UTF-32 big-endian preamble.
    /// </summary>
    [TestMethod]
    public void TryDetectByPreamble_WhenInputStartsWithUtf32BeBom_ShouldReturnBigEndianUtf32()
    {
        var bytes = new byte[] { 0x00, 0x00, 0xFE, 0xFF, 0x00, 0x00, 0x00, 0x41 };

        var ok = EncodingDetection.TryDetectByPreamble(bytes, out System.Text.Encoding? encoding);

        Assert.IsTrue(ok);
        Assert.IsNotNull(encoding);
        Assert.AreEqual(12001, encoding.CodePage);
    }

    /// <summary>
    /// Verifies that <see cref="EncodingDetection.TryDetectByPreamble" /> returns <see langword="false" /> for
    /// input that does not begin with a known preamble.
    /// </summary>
    [TestMethod]
    public void TryDetectByPreamble_WhenInputHasNoBom_ShouldReturnFalseAndNull()
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes("hello");

        var ok = EncodingDetection.TryDetectByPreamble(bytes, out System.Text.Encoding? encoding);

        Assert.IsFalse(ok);
        Assert.IsNull(encoding);
    }

    /// <summary>
    /// Verifies that <see cref="EncodingDetection.TryDetectByPreamble" /> returns <see langword="false" /> for
    /// an empty input span.
    /// </summary>
    [TestMethod]
    public void TryDetectByPreamble_WhenInputIsEmpty_ShouldReturnFalseAndNull()
    {
        var ok = EncodingDetection.TryDetectByPreamble([], out System.Text.Encoding? encoding);

        Assert.IsFalse(ok);
        Assert.IsNull(encoding);
    }

    /// <summary>
    /// Verifies that <see cref="EncodingDetection.TryDetectByPreamble" /> reports UTF-16 little endian when only
    /// two BOM bytes are available (no UTF-32 disambiguation possible).
    /// </summary>
    [TestMethod]
    public void TryDetectByPreamble_WhenOnlyTwoBytesAndMatchUtf16Le_ShouldReturnUnicode()
    {
        var bytes = new byte[] { 0xFF, 0xFE };

        var ok = EncodingDetection.TryDetectByPreamble(bytes, out System.Text.Encoding? encoding);

        Assert.IsTrue(ok);
        Assert.IsNotNull(encoding);
        Assert.AreEqual(1200, encoding.CodePage);
    }
}
