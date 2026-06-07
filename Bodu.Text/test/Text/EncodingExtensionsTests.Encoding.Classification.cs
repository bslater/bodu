// ---------------------------------------------------------------------------------------------------------------
// <copyright file="EncodingExtensionsTests.Encoding.Classification.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text;

public sealed partial class EncodingExtensionsTests
{
    /// <summary>
    /// Verifies that <see cref="EncodingExtensions.IsUtf8(System.Text.Encoding)" /> returns the expected value
    /// for each canonical encoding.
    /// </summary>
    /// <param name="codePage">The codepage under test.</param>
    /// <param name="expected">The expected return value.</param>
    [TestMethod]
    [DataRow(65001, true)]
    [DataRow(1200, false)]
    [DataRow(1201, false)]
    [DataRow(12000, false)]
    [DataRow(20127, false)]
    public void IsUtf8_ShouldRecognizeUtf8CodePage(int codePage, bool expected) => Assert.AreEqual(expected, System.Text.Encoding.GetEncoding(codePage).IsUtf8());

    /// <summary>
    /// Verifies that <see cref="EncodingExtensions.IsUtf16LittleEndian(System.Text.Encoding)" /> returns the
    /// expected value for each canonical encoding.
    /// </summary>
    /// <param name="codePage">The codepage under test.</param>
    /// <param name="expected">The expected return value.</param>
    [TestMethod]
    [DataRow(1200, true)]
    [DataRow(1201, false)]
    [DataRow(65001, false)]
    public void IsUtf16LittleEndian_ShouldRecognizeUnicodeCodePage(int codePage, bool expected) => Assert.AreEqual(expected, System.Text.Encoding.GetEncoding(codePage).IsUtf16LittleEndian());

    /// <summary>
    /// Verifies that <see cref="EncodingExtensions.IsUtf16BigEndian(System.Text.Encoding)" /> returns the
    /// expected value for each canonical encoding.
    /// </summary>
    /// <param name="codePage">The codepage under test.</param>
    /// <param name="expected">The expected return value.</param>
    [TestMethod]
    [DataRow(1201, true)]
    [DataRow(1200, false)]
    [DataRow(65001, false)]
    public void IsUtf16BigEndian_ShouldRecognizeBigEndianUnicodeCodePage(int codePage, bool expected) => Assert.AreEqual(expected, System.Text.Encoding.GetEncoding(codePage).IsUtf16BigEndian());

    /// <summary>
    /// Verifies that <see cref="EncodingExtensions.IsUtf32LittleEndian(System.Text.Encoding)" /> returns the
    /// expected value for each canonical encoding.
    /// </summary>
    /// <param name="codePage">The codepage under test.</param>
    /// <param name="expected">The expected return value.</param>
    [TestMethod]
    [DataRow(12000, true)]
    [DataRow(12001, false)]
    [DataRow(65001, false)]
    public void IsUtf32LittleEndian_ShouldRecognizeUtf32CodePage(int codePage, bool expected) => Assert.AreEqual(expected, System.Text.Encoding.GetEncoding(codePage).IsUtf32LittleEndian());

    /// <summary>
    /// Verifies that <see cref="EncodingExtensions.IsUtf32BigEndian(System.Text.Encoding)" /> returns the
    /// expected value for each canonical encoding.
    /// </summary>
    /// <param name="codePage">The codepage under test.</param>
    /// <param name="expected">The expected return value.</param>
    [TestMethod]
    [DataRow(12001, true)]
    [DataRow(12000, false)]
    [DataRow(65001, false)]
    public void IsUtf32BigEndian_ShouldRecognizeBigEndianUtf32CodePage(int codePage, bool expected) => Assert.AreEqual(expected, System.Text.Encoding.GetEncoding(codePage).IsUtf32BigEndian());

    /// <summary>
    /// Verifies that <see cref="EncodingExtensions.IsAnyUtf(System.Text.Encoding)" /> returns
    /// <see langword="true" /> for every UTF codepage and <see langword="false" /> for ASCII.
    /// </summary>
    /// <param name="codePage">The codepage under test.</param>
    /// <param name="expected">The expected return value.</param>
    [TestMethod]
    [DataRow(65001, true)]
    [DataRow(1200, true)]
    [DataRow(1201, true)]
    [DataRow(12000, true)]
    [DataRow(12001, true)]
    [DataRow(20127, false)]
    public void IsAnyUtf_ShouldRecognizeUtfFamily(int codePage, bool expected) => Assert.AreEqual(expected, System.Text.Encoding.GetEncoding(codePage).IsAnyUtf());

    /// <summary>
    /// Verifies that <see cref="EncodingExtensions.IsAscii(System.Text.Encoding)" /> returns the expected value
    /// for each canonical encoding.
    /// </summary>
    /// <param name="codePage">The codepage under test.</param>
    /// <param name="expected">The expected return value.</param>
    [TestMethod]
    [DataRow(20127, true)]
    [DataRow(65001, false)]
    [DataRow(1200, false)]
    public void IsAscii_ShouldRecognizeAsciiCodePage(int codePage, bool expected) => Assert.AreEqual(expected, System.Text.Encoding.GetEncoding(codePage).IsAscii());

    /// <summary>
    /// Verifies that <see cref="EncodingExtensions.GetDisplayName(System.Text.Encoding)" /> appends
    /// <c>-BOM</c> to UTF variants that emit a preamble and uses the canonical short form otherwise.
    /// </summary>
    [TestMethod]
    public void GetDisplayName_ForUtf8WithBom_ShouldAppendBomSuffix() => Assert.AreEqual("UTF-8-BOM", new System.Text.UTF8Encoding(true).GetDisplayName());

    /// <summary>
    /// Verifies that <see cref="EncodingExtensions.GetDisplayName(System.Text.Encoding)" /> returns the plain
    /// <c>"UTF-8"</c> form for an encoding constructed without a preamble.
    /// </summary>
    [TestMethod]
    public void GetDisplayName_ForUtf8WithoutBom_ShouldOmitBomSuffix() => Assert.AreEqual("UTF-8", new System.Text.UTF8Encoding(false).GetDisplayName());

    /// <summary>
    /// Verifies that <see cref="EncodingExtensions.GetDisplayName(System.Text.Encoding)" /> returns the WebName
    /// for an encoding that is neither a UTF variant nor ASCII.
    /// </summary>
    [TestMethod]
    public void GetDisplayName_ForNonUtfNonAsciiCodePage_ShouldReturnWebName()
    {
        System.Text.Encoding encoding = new NonUtfNonAsciiTestEncoding();

        var actual = encoding.GetDisplayName();

        Assert.AreEqual(encoding.WebName, actual);
    }

    /// <summary>
    /// A minimal <see cref="System.Text.Encoding" /> subclass that reports a non-UTF, non-ASCII codepage so the
    /// fallback branch of <see cref="EncodingExtensions.GetDisplayName(System.Text.Encoding)" /> can be
    /// exercised without depending on <c>System.Text.CodePagesEncodingProvider</c>.
    /// </summary>
    private sealed class NonUtfNonAsciiTestEncoding : System.Text.Encoding
    {
        /// <inheritdoc />
        public override int CodePage => 1252;

        /// <inheritdoc />
        public override string WebName => "windows-1252";

        /// <inheritdoc />
        public override int GetByteCount(char[] chars, int index, int count) => count;

        /// <inheritdoc />
        public override int GetBytes(char[] chars, int charIndex, int charCount, byte[] bytes, int byteIndex)
        {
            for (var i = 0; i < charCount; i++)
                bytes[byteIndex + i] = (byte)chars[charIndex + i];
            return charCount;
        }

        /// <inheritdoc />
        public override int GetCharCount(byte[] bytes, int index, int count) => count;

        /// <inheritdoc />
        public override int GetChars(byte[] bytes, int byteIndex, int byteCount, char[] chars, int charIndex)
        {
            for (var i = 0; i < byteCount; i++)
                chars[charIndex + i] = (char)bytes[byteIndex + i];
            return byteCount;
        }

        /// <inheritdoc />
        public override int GetMaxByteCount(int charCount) => charCount;

        /// <inheritdoc />
        public override int GetMaxCharCount(int byteCount) => byteCount;
    }

    /// <summary>
    /// Verifies that <see cref="EncodingExtensions.GetDisplayName(System.Text.Encoding)" /> returns
    /// <c>"US-ASCII"</c> for the ASCII encoding.
    /// </summary>
    [TestMethod]
    public void GetDisplayName_ForAscii_ShouldReturnUsAscii() => Assert.AreEqual("US-ASCII", System.Text.Encoding.ASCII.GetDisplayName());

    /// <summary>
    /// Verifies that all classification methods throw <see cref="ArgumentNullException" /> when
    /// <c>encoding</c> is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void ClassificationMethods_WhenEncodingIsNull_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() => _ = EncodingExtensions.IsUtf8(null!));
        Assert.ThrowsExactly<ArgumentNullException>(() => _ = EncodingExtensions.IsUtf16LittleEndian(null!));
        Assert.ThrowsExactly<ArgumentNullException>(() => _ = EncodingExtensions.IsUtf16BigEndian(null!));
        Assert.ThrowsExactly<ArgumentNullException>(() => _ = EncodingExtensions.IsUtf32LittleEndian(null!));
        Assert.ThrowsExactly<ArgumentNullException>(() => _ = EncodingExtensions.IsUtf32BigEndian(null!));
        Assert.ThrowsExactly<ArgumentNullException>(() => _ = EncodingExtensions.IsAnyUtf(null!));
        Assert.ThrowsExactly<ArgumentNullException>(() => _ = EncodingExtensions.IsAscii(null!));
        Assert.ThrowsExactly<ArgumentNullException>(() => _ = EncodingExtensions.GetDisplayName(null!));
    }
}
