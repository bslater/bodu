// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Base32Tests.TryDecode.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Encoding;

public sealed partial class Base32Tests
{
    /// <summary>
    /// Verifies that <see cref="Base32.TryDecode" /> recovers the original bytes for a valid Standard input.
    /// </summary>
    [TestMethod]
    public void TryDecode_WhenStandardValidInput_ShouldReturnTrueAndExpectedBytes()
    {
        byte[] destination = new byte[6];

        bool ok = Base32.TryDecode("MZXW6YTBOI======".AsSpan(), destination, out int bytesWritten);

        Assert.IsTrue(ok);
        Assert.AreEqual(6, bytesWritten);
        CollectionAssert.AreEqual(Ascii("foobar"), destination);
    }

    /// <summary>
    /// Verifies that <see cref="Base32.TryDecode" /> returns <see langword="false" /> rather than throwing on
    /// invalid characters.
    /// </summary>
    [TestMethod]
    public void TryDecode_WhenInvalidCharacters_ShouldReturnFalseAndZeroBytesWritten()
    {
        byte[] destination = new byte[6];

        bool ok = Base32.TryDecode("MZXW@YTBOI======".AsSpan(), destination, out int bytesWritten);

        Assert.IsFalse(ok);
        Assert.AreEqual(0, bytesWritten);
    }

    /// <summary>
    /// Verifies that <see cref="Base32.TryDecode" /> returns <see langword="false" /> rather than throwing when
    /// padding is incorrect under strict parsing.
    /// </summary>
    [TestMethod]
    public void TryDecode_WhenStrictAndPaddingOmitted_ShouldReturnFalseAndZeroBytesWritten()
    {
        byte[] destination = new byte[6];

        bool ok = Base32.TryDecode("MZXW6YTBOI".AsSpan(), destination, out int bytesWritten);

        Assert.IsFalse(ok);
        Assert.AreEqual(0, bytesWritten);
    }

    /// <summary>
    /// Verifies that <see cref="Base32.TryDecode" /> with <see cref="BaseFormatStyles.AllowMissingPadding" /> accepts
    /// unpadded input.
    /// </summary>
    [TestMethod]
    public void TryDecode_WhenAllowMissingPadding_ShouldReturnTrueAndExpectedBytes()
    {
        byte[] destination = new byte[6];

        bool ok = Base32.TryDecode(
            "MZXW6YTBOI".AsSpan(),
            destination,
            out int bytesWritten,
            Base32Variant.Standard,
            BaseFormatStyles.AllowMissingPadding);

        Assert.IsTrue(ok);
        Assert.AreEqual(6, bytesWritten);
        CollectionAssert.AreEqual(Ascii("foobar"), destination);
    }

    /// <summary>
    /// Verifies that <see cref="Base32.TryDecode" /> for empty input returns <see langword="true" /> with
    /// <c>bytesWritten = 0</c>.
    /// </summary>
    [TestMethod]
    public void TryDecode_WhenInputIsEmpty_ShouldReturnTrueAndZeroBytesWritten()
    {
        byte[] destination = new byte[6];

        bool ok = Base32.TryDecode(ReadOnlySpan<char>.Empty, destination, out int bytesWritten);

        Assert.IsTrue(ok);
        Assert.AreEqual(0, bytesWritten);
    }

    /// <summary>
    /// Verifies that <see cref="Base32.TryDecode" /> returns <see langword="false" /> when the destination is too
    /// small.
    /// </summary>
    [TestMethod]
    public void TryDecode_WhenDestinationTooSmall_ShouldReturnFalseAndZeroBytesWritten()
    {
        byte[] destination = new byte[1];

        bool ok = Base32.TryDecode("MZXW6YTBOI======".AsSpan(), destination, out int bytesWritten);

        Assert.IsFalse(ok);
        Assert.AreEqual(0, bytesWritten);
    }

    /// <summary>
    /// Verifies that <see cref="Base32.TryDecode" /> succeeds when the destination is exactly the required size.
    /// </summary>
    /// <param name="input">The Base32 input.</param>
    /// <param name="expectedByteCount">The expected decoded byte count.</param>
    [TestMethod]
    [DataRow("MY======", 1)]
    [DataRow("MZXQ====", 2)]
    [DataRow("MZXW6===", 3)]
    [DataRow("MZXW6YQ=", 4)]
    [DataRow("MZXW6YTB", 5)]
    [DataRow("MZXW6YTBOI======", 6)]
    public void TryDecode_WhenDestinationExactlyRequiredSize_ShouldReturnTrueAndFillBuffer(string input, int expectedByteCount)
    {
        byte[] destination = new byte[expectedByteCount];

        bool ok = Base32.TryDecode(input.AsSpan(), destination, out int bytesWritten);

        Assert.IsTrue(ok);
        Assert.AreEqual(expectedByteCount, bytesWritten);
    }

    /// <summary>
    /// Verifies that <see cref="Base32.TryDecode" /> returns <see langword="false" /> rather than throwing for
    /// various malformed inputs.
    /// </summary>
    /// <param name="malformedInput">The malformed input.</param>
    [TestMethod]
    [DataRow("MZ@W6YTB")]       // invalid char
    [DataRow("MZ8W6YTB")]       // '8' not in standard alphabet
    [DataRow("MZ=XW6YTB")]      // padding in middle
    [DataRow("MZXW6YTBOI")]     // missing required padding
    [DataRow("M=======")]       // single data char with full padding (invalid)
    public void TryDecode_WhenStrictAndMalformedInput_ShouldReturnFalse(string malformedInput)
    {
        byte[] destination = new byte[16];

        bool ok = Base32.TryDecode(malformedInput.AsSpan(), destination, out int bytesWritten);

        Assert.IsFalse(ok);
        Assert.AreEqual(0, bytesWritten);
    }

    /// <summary>
    /// Verifies that <see cref="Base32.TryDecode" /> rejects an undefined variant with
    /// <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [TestMethod]
    public void TryDecode_WhenUndefinedVariant_ShouldThrowArgumentOutOfRangeException()
    {
        byte[] destination = new byte[16];

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = Base32.TryDecode("MZXW6YTB".AsSpan(), destination, out _, (Base32Variant)99);
        });
    }
}
