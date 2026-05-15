// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Base16Tests.TryDecode.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Encoding;

public sealed partial class Base16Tests
{

    /// <summary>
    /// Verifies that <see cref="Base16.TryDecode(ReadOnlySpan{char}, Span{byte}, out int, BaseFormatStyles)" /> with
    /// <see cref="BaseFormatStyles.AllowPrefix" /> strips a leading prefix before decoding.
    /// </summary>
    [TestMethod]
    public void TryDecode_WhenAllowPrefixAndPrefixPresent_ShouldStripPrefix()
    {
        byte[] destination = new byte[4];

        bool ok = Base16.TryDecode("0xDEADBEEF".AsSpan(), destination, out int bytesWritten, BaseFormatStyles.AllowPrefix);

        Assert.IsTrue(ok);
        Assert.AreEqual(4, bytesWritten);
        CollectionAssert.AreEqual(CanonicalBytes, destination);
    }

    /// <summary>
    /// Verifies that <see cref="Base16.TryDecode" /> succeeds when the destination is exactly the required size.
    /// </summary>
    /// <param name="charCount">The input char count.</param>
    [TestMethod]
    [DataRow(2)]
    [DataRow(4)]
    [DataRow(14)]
    [DataRow(128)]
    [DataRow(2048)]
    public void TryDecode_WhenDestinationExactlyRequiredSize_ShouldReturnTrueAndFillBuffer(int charCount)
    {
        string input = new string('a', charCount);
        byte[] destination = new byte[charCount / 2];

        bool ok = Base16.TryDecode(input.AsSpan(), destination, out int bytesWritten);

        Assert.IsTrue(ok);
        Assert.AreEqual(charCount / 2, bytesWritten);
    }

    /// <summary>
    /// Verifies that <see cref="Base16.TryDecode" /> returns <see langword="false" /> when the destination is exactly
    /// one byte short of the required size.
    /// </summary>
    /// <param name="charCount">The input char count.</param>
    [TestMethod]
    [DataRow(2)]
    [DataRow(4)]
    [DataRow(14)]
    [DataRow(128)]
    public void TryDecode_WhenDestinationOneByteShort_ShouldReturnFalseAndZeroBytesWritten(int charCount)
    {
        string input = new string('a', charCount);
        byte[] destination = new byte[(charCount / 2) - 1];

        bool ok = Base16.TryDecode(input.AsSpan(), destination, out int bytesWritten);

        Assert.IsFalse(ok);
        Assert.AreEqual(0, bytesWritten);
    }

    /// <summary>
    /// Verifies that <see cref="Base16.TryDecode(ReadOnlySpan{char}, Span{byte}, out int, BaseFormatStyles)" /> when
    /// the destination is too small returns <see langword="false" /> with <c>bytesWritten = 0</c>.
    /// </summary>
    [TestMethod]
    public void TryDecode_WhenDestinationTooSmall_ShouldReturnFalseAndZeroBytesWritten()
    {
        byte[] destination = new byte[1];

        bool ok = Base16.TryDecode(CanonicalHexLower.AsSpan(), destination, out int bytesWritten);

        Assert.IsFalse(ok);
        Assert.AreEqual(0, bytesWritten);
    }

    /// <summary>
    /// Verifies that <see cref="Base16.TryDecode(ReadOnlySpan{char}, Span{byte}, out int, BaseFormatStyles)" /> with
    /// <see cref="BaseFormatStyles.IgnoreWhitespace" /> ignores embedded whitespace.
    /// </summary>
    [TestMethod]
    public void TryDecode_WhenIgnoreWhitespace_ShouldStripWhitespaceAndDecode()
    {
        byte[] destination = new byte[4];

        bool ok = Base16.TryDecode("DE AD BE EF".AsSpan(), destination, out int bytesWritten, BaseFormatStyles.IgnoreWhitespace);

        Assert.IsTrue(ok);
        Assert.AreEqual(4, bytesWritten);
        CollectionAssert.AreEqual(CanonicalBytes, destination);
    }

    /// <summary>
    /// Verifies that lenient <see cref="Base16.TryDecode(ReadOnlySpan{char}, Span{byte}, out int, BaseFormatStyles)" />
    /// allocates a heap scratch buffer when the input exceeds the stackalloc threshold and still decodes correctly.
    /// </summary>
    [TestMethod]
    public void TryDecode_WhenInputExceedsStackallocThreshold_ShouldDecodeCorrectly()
    {
        const int byteCount = 200;
        byte[] bytes = new byte[byteCount];
        for (int i = 0; i < byteCount; i++)
        {
            bytes[i] = (byte)(i & 0xFF);
        }

        string spaced = Base16.Encode(bytes, BaseFormattingOptions.InsertSpacing);
        byte[] destination = new byte[byteCount];

        bool ok = Base16.TryDecode(spaced.AsSpan(), destination, out int bytesWritten, BaseFormatStyles.IgnoreWhitespace);

        Assert.IsTrue(ok);
        Assert.AreEqual(byteCount, bytesWritten);
        CollectionAssert.AreEqual(bytes, destination);
    }

    /// <summary>
    /// Verifies that <see cref="Base16.TryDecode(ReadOnlySpan{char}, Span{byte}, out int, BaseFormatStyles)" /> on
    /// empty input returns <see langword="true" /> with <c>bytesWritten = 0</c> instead of throwing.
    /// </summary>
    [TestMethod]
    public void TryDecode_WhenInputIsEmpty_ShouldReturnTrueAndZeroBytesWritten()
    {
        byte[] destination = new byte[4];

        bool ok = Base16.TryDecode(ReadOnlySpan<char>.Empty, destination, out int bytesWritten);

        Assert.IsTrue(ok);
        Assert.AreEqual(0, bytesWritten);
    }

    /// <summary>
    /// Verifies that <see cref="Base16.TryDecode(ReadOnlySpan{char}, Span{byte}, out int, BaseFormatStyles)" /> on
    /// non-hex input returns <see langword="false" /> rather than throwing.
    /// </summary>
    [TestMethod]
    public void TryDecode_WhenInvalidCharacters_ShouldReturnFalseAndZeroBytesWritten()
    {
        byte[] destination = new byte[4];

        bool ok = Base16.TryDecode("xy".AsSpan(), destination, out int bytesWritten);

        Assert.IsFalse(ok);
        Assert.AreEqual(0, bytesWritten);
    }

    /// <summary>
    /// Verifies that lenient <see cref="Base16.TryDecode(ReadOnlySpan{char}, Span{byte}, out int, BaseFormatStyles)" />
    /// returns <see langword="false" /> when the digit count after stripping is odd.
    /// </summary>
    [TestMethod]
    public void TryDecode_WhenLenientYieldsOddDigitCount_ShouldReturnFalse()
    {
        byte[] destination = new byte[4];

        bool ok = Base16.TryDecode(
            "0x ABC".AsSpan(),
            destination,
            out int bytesWritten,
            BaseFormatStyles.AllowPrefix | BaseFormatStyles.IgnoreWhitespace);

        Assert.IsFalse(ok);
        Assert.AreEqual(0, bytesWritten);
    }

    /// <summary>
    /// Verifies that <see cref="Base16.TryDecode(ReadOnlySpan{char}, Span{byte}, out int, BaseFormatStyles)" /> on
    /// odd-length input returns <see langword="false" /> rather than throwing.
    /// </summary>
    [TestMethod]
    public void TryDecode_WhenOddLengthInput_ShouldReturnFalseAndZeroBytesWritten()
    {
        byte[] destination = new byte[4];

        bool ok = Base16.TryDecode("abc".AsSpan(), destination, out int bytesWritten);

        Assert.IsFalse(ok);
        Assert.AreEqual(0, bytesWritten);
    }

    /// <summary>
    /// Verifies that <see cref="Base16.TryDecode" /> rejects various malformed inputs without throwing.
    /// </summary>
    /// <param name="malformedInput">The malformed input.</param>
    [TestMethod]
    [DataRow("g0")]
    [DataRow("0g")]
    [DataRow("a")]      // odd length
    [DataRow("abc")]    // odd length
    [DataRow("ab/cd")]  // invalid separator
    [DataRow(" ab")]    // whitespace in strict mode
    public void TryDecode_WhenStrictAndMalformedInput_ShouldReturnFalse(string malformedInput)
    {
        byte[] destination = new byte[16];

        bool ok = Base16.TryDecode(malformedInput.AsSpan(), destination, out int bytesWritten);

        Assert.IsFalse(ok);
        Assert.AreEqual(0, bytesWritten);
    }
    /// <summary>
    /// Verifies that <see cref="Base16.TryDecode(ReadOnlySpan{char}, Span{byte}, out int, BaseFormatStyles)" /> on
    /// strict valid input returns <see langword="true" /> with the expected bytes.
    /// </summary>
    [TestMethod]
    public void TryDecode_WhenStrictValidInput_ShouldReturnTrueAndExpectedBytes()
    {
        byte[] destination = new byte[4];

        bool ok = Base16.TryDecode(CanonicalHexLower.AsSpan(), destination, out int bytesWritten);

        Assert.IsTrue(ok);
        Assert.AreEqual(4, bytesWritten);
        CollectionAssert.AreEqual(CanonicalBytes, destination);
    }

}
