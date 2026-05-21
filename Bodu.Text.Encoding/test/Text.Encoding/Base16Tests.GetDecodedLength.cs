// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Base16Tests.GetDecodedLength.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Encoding;

public sealed partial class Base16Tests
{

    /// <summary>
    /// Verifies that <see cref="Base16.GetDecodedLength(ReadOnlySpan{char}, BaseFormatStyles)" /> with
    /// <see cref="BaseFormatStyles.AllowPrefix" /> strips the prefix before counting.
    /// </summary>
    [TestMethod]
    public void GetDecodedLength_WhenAllowPrefix_ShouldExcludePrefixFromCount()
    {
        var actual = Base16.GetDecodedLength("0xDEADBEEF".AsSpan(), BaseFormatStyles.AllowPrefix);

        Assert.AreEqual(4, actual);
    }

    /// <summary>
    /// Verifies that <see cref="Base16.GetDecodedLength(ReadOnlySpan{char}, BaseFormatStyles)" /> with
    /// <see cref="BaseFormatStyles.IgnoreWhitespace" /> strips whitespace before counting.
    /// </summary>
    [TestMethod]
    public void GetDecodedLength_WhenIgnoreWhitespace_ShouldExcludeWhitespaceFromCount()
    {
        var actual = Base16.GetDecodedLength("DE AD BE EF".AsSpan(), BaseFormatStyles.IgnoreWhitespace);

        Assert.AreEqual(4, actual);
    }

    /// <summary>
    /// Regression: verifies that <see cref="Base16.GetDecodedLength(ReadOnlySpan{char}, BaseFormatStyles)" />
    /// validates each retained character against the hexadecimal alphabet — non-hex characters like <c>'z'</c>
    /// must produce <see cref="FormatException" /> rather than a length count that disagrees with the decoder.
    /// </summary>
    /// <param name="invalidInput">An input containing a character outside the hex alphabet.</param>
    [TestMethod]
    [DataRow("zz")]
    [DataRow("gg")]
    [DataRow("ab!c")]
    [DataRow("xx")]
    [DataRow("0xZZ")]
    public void GetDecodedLength_WhenInputContainsNonHexCharacters_ShouldThrowFormatException(string invalidInput)
    {
        Assert.ThrowsExactly<FormatException>(() =>
        {
            _ = Base16.GetDecodedLength(invalidInput.AsSpan());
        });
    }

    /// <summary>
    /// Regression: verifies that the length helper still returns the correct count for valid inputs after the
    /// character-validation fix — the validation must not regress the valid-input path.
    /// </summary>
    [TestMethod]
    public void GetDecodedLength_WhenInputIsValid_ShouldStillReturnExpectedCount()
    {
        Assert.AreEqual(0, Base16.GetDecodedLength(ReadOnlySpan<char>.Empty));
        Assert.AreEqual(4, Base16.GetDecodedLength("DEADBEEF".AsSpan()));
        Assert.AreEqual(4, Base16.GetDecodedLength("deadbeef".AsSpan()));
        Assert.AreEqual(4, Base16.GetDecodedLength("0xDEADBEEF".AsSpan(), BaseFormatStyles.AllowPrefix));
        Assert.AreEqual(4, Base16.GetDecodedLength("DE AD BE EF".AsSpan(), BaseFormatStyles.IgnoreWhitespace));
    }
    /// <summary>
    /// Verifies that <see cref="Base16.GetDecodedLength(ReadOnlySpan{char}, BaseFormatStyles)" /> returns half of the
    /// input length for strict input.
    /// </summary>
    [TestMethod]
    public void GetDecodedLength_WhenStrictEvenInput_ShouldReturnHalf()
    {
        Assert.AreEqual(4, Base16.GetDecodedLength("DEADBEEF".AsSpan()));
    }

    /// <summary>
    /// Verifies that <see cref="Base16.GetDecodedLength(ReadOnlySpan{char}, BaseFormatStyles)" /> throws
    /// <see cref="FormatException" /> for an odd-length strict input.
    /// </summary>
    [TestMethod]
    public void GetDecodedLength_WhenStrictOddInput_ShouldThrowFormatException()
    {
        Assert.ThrowsExactly<FormatException>(() =>
        {
            _ = Base16.GetDecodedLength("abc".AsSpan());
        });
    }

    /// <summary>
    /// Verifies that <see cref="Base16.GetEncodedLength(int)" /> (the no-options overload) returns twice the byte
    /// count.
    /// </summary>
    [TestMethod]
    public void GetEncodedLength_WithoutOptionsOverload_ShouldReturnTwiceByteCount()
    {
        Assert.AreEqual(8, Base16.GetEncodedLength(4));
    }

    /// <summary>
    /// Regression: verifies that <see cref="Base16.TryGetDecodedLength(ReadOnlySpan{char}, out int, BaseFormatStyles)" />
    /// returns <see langword="false" /> for inputs that contain non-hex characters — matching the behaviour of
    /// <see cref="Base16.Decode(string, BaseFormatStyles)" /> rather than silently reporting a length the decoder
    /// would reject.
    /// </summary>
    /// <param name="invalidInput">An input containing a character outside the hex alphabet.</param>
    [TestMethod]
    [DataRow("zz")]
    [DataRow("gg")]
    [DataRow("ab!c")]
    [DataRow("xx")]
    public void TryGetDecodedLength_WhenInputContainsNonHexCharacters_ShouldReturnFalse(string invalidInput)
    {
        var ok = Base16.TryGetDecodedLength(invalidInput.AsSpan(), out var byteCount);

        Assert.IsFalse(ok, $"TryGetDecodedLength should return false for: {invalidInput}");
        Assert.AreEqual(0, byteCount);
    }

    /// <summary>
    /// Verifies that <see cref="Base16.TryGetDecodedLength(ReadOnlySpan{char}, out int, BaseFormatStyles)" /> returns
    /// <see langword="false" /> rather than throwing on odd-length input.
    /// </summary>
    [TestMethod]
    public void TryGetDecodedLength_WhenOddInput_ShouldReturnFalseAndZero()
    {
        var ok = Base16.TryGetDecodedLength("abc".AsSpan(), out var byteCount);

        Assert.IsFalse(ok);
        Assert.AreEqual(0, byteCount);
    }

    /// <summary>
    /// Verifies that <see cref="Base16.TryGetDecodedLength(ReadOnlySpan{char}, out int, BaseFormatStyles)" /> returns
    /// <see langword="true" /> for valid input and reports the byte count.
    /// </summary>
    [TestMethod]
    public void TryGetDecodedLength_WhenStrictEvenInput_ShouldReturnTrueAndCount()
    {
        var ok = Base16.TryGetDecodedLength("DEADBEEF".AsSpan(), out var byteCount);

        Assert.IsTrue(ok);
        Assert.AreEqual(4, byteCount);
    }

}
