// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Base16Tests.GetDecodedLength.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
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
        int actual = Base16.GetDecodedLength("0xDEADBEEF".AsSpan(), BaseFormatStyles.AllowPrefix);

        Assert.AreEqual(4, actual);
    }

    /// <summary>
    /// Verifies that <see cref="Base16.GetDecodedLength(ReadOnlySpan{char}, BaseFormatStyles)" /> with
    /// <see cref="BaseFormatStyles.IgnoreWhitespace" /> strips whitespace before counting.
    /// </summary>
    [TestMethod]
    public void GetDecodedLength_WhenIgnoreWhitespace_ShouldExcludeWhitespaceFromCount()
    {
        int actual = Base16.GetDecodedLength("DE AD BE EF".AsSpan(), BaseFormatStyles.IgnoreWhitespace);

        Assert.AreEqual(4, actual);
    }

    /// <summary>
    /// Regression: verifies that <see cref="Base16.GetDecodedLength(ReadOnlySpan{char}, BaseFormatStyles)" />
    /// validates each retained character against the hexadecimal alphabet - non-hex characters like <c>'z'</c>
    /// must produce <see cref="FormatException" /> rather than a length count that disagrees with the decoder.
    /// </summary>
    /// <param name="invalidInput">An input containing a character outside the hex alphabet.</param>
    [TestMethod]
    [DataRow("zz")]
    [DataRow("gg")]
    [DataRow("ab!c")]
    [DataRow("xx")]
    [DataRow("0xZZ")]
    public void GetDecodedLength_WhenInputContainsNonHexCharacters_ShouldThrowExactly(string invalidInput)
    {
        Assert.ThrowsExactly<FormatException>(() =>
        {
            _ = Base16.GetDecodedLength(invalidInput.AsSpan());
        });
    }

    /// <summary>
    /// Regression: verifies that the length helper still returns the correct count for valid inputs after the
    /// character-validation fix - the validation must not regress the valid-input path.
    /// </summary>
    [TestMethod]
    public void GetDecodedLength_WhenInputIsValid_ShouldStillReturnExpectedCount()
    {
        Assert.AreEqual(0, Base16.GetDecodedLength([]));
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
    public void GetDecodedLength_WhenStrictEvenInput_ShouldReturnHalf() => Assert.AreEqual(4, Base16.GetDecodedLength("DEADBEEF".AsSpan()));

    /// <summary>
    /// Verifies that <see cref="Base16.GetDecodedLength(ReadOnlySpan{char}, BaseFormatStyles)" /> throws
    /// <see cref="FormatException" /> for an odd-length strict input.
    /// </summary>
    [TestMethod]
    public void GetDecodedLength_WhenStrictOddInput_ShouldThrowExactly()
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
    public void GetEncodedLength_WithoutOptionsOverload_ShouldReturnTwiceByteCount() => Assert.AreEqual(8, Base16.GetEncodedLength(4));

    /// <summary>
    /// Regression: verifies that <see cref="Base16.TryGetDecodedLength(ReadOnlySpan{char}, out int, BaseFormatStyles)" />
    /// returns <see langword="false" /> for inputs that contain non-hex characters - matching the behaviour of
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
        bool ok = Base16.TryGetDecodedLength(invalidInput.AsSpan(), out int byteCount);

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
        bool ok = Base16.TryGetDecodedLength("abc".AsSpan(), out int byteCount);

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
        bool ok = Base16.TryGetDecodedLength("DEADBEEF".AsSpan(), out int byteCount);

        Assert.IsTrue(ok);
        Assert.AreEqual(4, byteCount);
    }

    /// <summary>
    /// Verifies that <see cref="Base16.GetDecodedLength" /> returns the length
    /// <see cref="Base16.Decode(string, BaseFormatStyles)" /> produces when it succeeds, and throws
    /// <see cref="FormatException" /> when it fails, in every combination of styles.
    /// </summary>
    [TestMethod]
    public void GetDecodedLength_WhenSweptAcrossStyles_ShouldMatchDecode()
    {
        var disagreements = new List<string>();

        foreach (var input in GetValidationSweepInputs())
        {
            foreach (BaseFormatStyles styles in DecodeAgreement.AllStyles)
            {
                DecodeAgreement.TryDecode(() => Base16.Decode(input, styles), out int decodedLength);
                DecodeAgreement.TryDecode(() => new byte[Base16.GetDecodedLength(input, styles)], out int reportedLength);
                if (reportedLength != decodedLength)
                    disagreements.Add($"[{styles}] {DecodeAgreement.Quote(input)}: GetDecodedLength {reportedLength}, Decode {decodedLength} (-1: FormatException)");
            }
        }

        DecodeAgreement.AssertNone(disagreements);
    }

    /// <summary>
    /// Verifies that <see cref="Base16.TryGetDecodedLength" /> returns <see langword="true" /> exactly when
    /// <see cref="Base16.Decode(string, BaseFormatStyles)" /> succeeds, and then reports the length it produces, in
    /// every combination of styles.
    /// </summary>
    [TestMethod]
    public void TryGetDecodedLength_WhenSweptAcrossStyles_ShouldMatchDecode()
    {
        var disagreements = new List<string>();

        foreach (var input in GetValidationSweepInputs())
        {
            foreach (BaseFormatStyles styles in DecodeAgreement.AllStyles)
            {
                DecodeAgreement.TryDecode(() => Base16.Decode(input, styles), out int decodedLength);
                var reportedLength = Base16.TryGetDecodedLength(input, out int byteCount, styles) ? byteCount : -1;
                if (reportedLength != decodedLength)
                    disagreements.Add($"[{styles}] {DecodeAgreement.Quote(input)}: TryGetDecodedLength {reportedLength}, Decode {decodedLength} (-1: rejected)");
            }
        }

        DecodeAgreement.AssertNone(disagreements);
    }

}
