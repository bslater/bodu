// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Base64Tests.TryDecode.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Encoding;

public sealed partial class Base64Tests
{

    /// <summary>
    /// Verifies that <see cref="Base64.TryDecode" /> with <see cref="BaseFormatStyles.AllowMissingPadding" /> accepts
    /// unpadded input.
    /// </summary>
    [TestMethod]
    public void TryDecode_WhenAllowMissingPadding_ShouldReturnTrueAndExpectedBytes()
    {
        var destination = new byte[5];

        var ok = Base64.TryDecode(
            "Zm9vYmE".AsSpan(),
            destination,
            out var bytesWritten,
            Base64Variant.Standard,
            BaseFormatStyles.AllowMissingPadding);

        Assert.IsTrue(ok);
        Assert.AreEqual(5, bytesWritten);
        CollectionAssert.AreEqual(Ascii("fooba"), destination);
    }

    /// <summary>
    /// Verifies that <see cref="Base64.TryDecode" /> succeeds when the destination is exactly the required size.
    /// </summary>
    /// <param name="input">The Base64 input.</param>
    /// <param name="expectedByteCount">The expected decoded byte count.</param>
    [TestMethod]
    [DataRow("Zg==", 1)]
    [DataRow("Zm8=", 2)]
    [DataRow("Zm9v", 3)]
    [DataRow("Zm9vYg==", 4)]
    [DataRow("Zm9vYmE=", 5)]
    [DataRow("Zm9vYmFy", 6)]
    public void TryDecode_WhenDestinationExactlyRequiredSize_ShouldReturnTrueAndFillBuffer(string input, int expectedByteCount)
    {
        var destination = new byte[expectedByteCount];

        var ok = Base64.TryDecode(input.AsSpan(), destination, out var bytesWritten);

        Assert.IsTrue(ok);
        Assert.AreEqual(expectedByteCount, bytesWritten);
    }

    /// <summary>
    /// Verifies that <see cref="Base64.TryDecode" /> for empty input returns <see langword="true" /> with
    /// <c>bytesWritten = 0</c>.
    /// </summary>
    [TestMethod]
    public void TryDecode_WhenInputIsEmpty_ShouldReturnTrueAndZeroBytesWritten()
    {
        var destination = new byte[6];

        var ok = Base64.TryDecode(ReadOnlySpan<char>.Empty, destination, out var bytesWritten);

        Assert.IsTrue(ok);
        Assert.AreEqual(0, bytesWritten);
    }

    /// <summary>
    /// Verifies that <see cref="Base64.TryDecode" /> returns <see langword="false" /> on invalid input rather than
    /// throwing.
    /// </summary>
    [TestMethod]
    public void TryDecode_WhenInvalidCharacters_ShouldReturnFalseAndZeroBytesWritten()
    {
        var destination = new byte[6];

        var ok = Base64.TryDecode("Zm9vYm!y".AsSpan(), destination, out var bytesWritten);

        Assert.IsFalse(ok);
        Assert.AreEqual(0, bytesWritten);
    }
    /// <summary>
    /// Verifies that <see cref="Base64.TryDecode" /> recovers the original bytes for a valid Standard input.
    /// </summary>
    [TestMethod]
    public void TryDecode_WhenStandardValidInput_ShouldReturnTrueAndExpectedBytes()
    {
        var destination = new byte[6];

        var ok = Base64.TryDecode("Zm9vYmFy".AsSpan(), destination, out var bytesWritten);

        Assert.IsTrue(ok);
        Assert.AreEqual(6, bytesWritten);
        CollectionAssert.AreEqual(Ascii("foobar"), destination);
    }

    /// <summary>
    /// Verifies that <see cref="Base64.TryDecode" /> rejects various malformed inputs without throwing.
    /// </summary>
    /// <param name="malformedInput">The malformed input.</param>
    [TestMethod]
    [DataRow("Zm@v")]
    [DataRow("Zm-v")]   // URL-safe char in Standard
    [DataRow("Zm_v")]   // URL-safe char in Standard
    [DataRow("Z=m9")]   // padding in middle
    [DataRow("Z")]      // one data char with no padding
    public void TryDecode_WhenStrictAndMalformedInput_ShouldReturnFalse(string malformedInput)
    {
        var destination = new byte[16];

        var ok = Base64.TryDecode(malformedInput.AsSpan(), destination, out var bytesWritten);

        Assert.IsFalse(ok);
        Assert.AreEqual(0, bytesWritten);
    }

    /// <summary>
    /// Verifies that <see cref="Base64.TryDecode" /> returns <see langword="false" /> on a truncated input in strict
    /// mode.
    /// </summary>
    [TestMethod]
    public void TryDecode_WhenStrictAndPaddingOmitted_ShouldReturnFalseAndZeroBytesWritten()
    {
        var destination = new byte[5];

        var ok = Base64.TryDecode("Zm9vYmE".AsSpan(), destination, out var bytesWritten);

        Assert.IsFalse(ok);
        Assert.AreEqual(0, bytesWritten);
    }

    /// <summary>
    /// Verifies that <see cref="Base64.TryDecode" /> rejects an undefined variant.
    /// </summary>
    [TestMethod]
    public void TryDecode_WhenUndefinedVariant_ShouldThrowExactly()
    {
        var destination = new byte[16];

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = Base64.TryDecode("Zm9v".AsSpan(), destination, out _, (Base64Variant)99);
        });
    }

}
