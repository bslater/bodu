// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AlphanumericTests.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Hashing.Checksums;

/// <summary>
/// Contains direct unit tests for the internal <see cref="Alphanumeric" /> helpers. Although several of these
/// helpers are exercised indirectly through <see cref="Cusip" />, <see cref="Sedol" />, and <see cref="Isin" />,
/// most public consumers short-circuit on invalid characters before reaching the helper, so the failure paths
/// require direct invocation through <see cref="System.Runtime.CompilerServices.InternalsVisibleToAttribute" />.
/// </summary>
[TestClass]
public sealed class AlphanumericTests
{

    // ─── ExpandCusip ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Verifies that <see cref="Alphanumeric.ExpandCusip(char)" /> maps each decimal digit to its numeric value.
    /// </summary>
    [TestMethod]
    public void ExpandCusip_WhenCharacterIsDigit_ShouldReturnDigitValue()
    {
        for (var c = '0'; c <= '9'; c++)
        {
            Assert.AreEqual(c - '0', Alphanumeric.ExpandCusip(c));
        }
    }

    /// <summary>
    /// Verifies that <see cref="Alphanumeric.ExpandCusip(char)" /> throws <see cref="ArgumentOutOfRangeException" />
    /// when the supplied character is outside the CUSIP alphabet.
    /// </summary>
    [DataRow(' ')]
    [DataRow('-')]
    [DataRow('a')]
    [DataRow('z')]
    [DataRow('!')]
    [DataRow('$')]
    [DataRow('/')]
    [DataRow(':')]
    [DataRow('`')]
    [DataRow('{')]
    [DataRow('é')]
    [TestMethod]
    public void ExpandCusip_WhenCharacterIsOutsideCusipAlphabet_ShouldThrowExactly(char invalid)
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = Alphanumeric.ExpandCusip(invalid);
        });
    }

    /// <summary>
    /// Verifies that <see cref="Alphanumeric.ExpandCusip(char)" /> maps each CUSIP punctuation sentinel to its
    /// specified numeric value (<c>'*'</c>=36, <c>'@'</c>=37, <c>'#'</c>=38).
    /// </summary>
    [DataRow('*', 36)]
    [DataRow('@', 37)]
    [DataRow('#', 38)]
    [TestMethod]
    public void ExpandCusip_WhenCharacterIsPunctuationSentinel_ShouldReturnAssignedValue(char sentinel, int expected) => Assert.AreEqual(expected, Alphanumeric.ExpandCusip(sentinel));

    /// <summary>
    /// Verifies that <see cref="Alphanumeric.ExpandCusip(char)" /> maps each uppercase Latin letter to the ISO
    /// 7064 weighted value (<c>'A'</c>=10 … <c>'Z'</c>=35).
    /// </summary>
    [TestMethod]
    public void ExpandCusip_WhenCharacterIsUppercaseLetter_ShouldReturnTenPlusOrdinal()
    {
        for (var c = 'A'; c <= 'Z'; c++)
        {
            Assert.AreEqual(c - 'A' + 10, Alphanumeric.ExpandCusip(c));
        }
    }

    // ─── ExpandLetterDigit (non-ASCII branch) ─────────────────────────────────────────────────

    /// <summary>
    /// Verifies that <see cref="Alphanumeric.ExpandLetterDigit(char)" /> maps each decimal digit to its numeric
    /// value.
    /// </summary>
    [TestMethod]
    public void ExpandLetterDigit_WhenCharacterIsDigit_ShouldReturnDigitValue()
    {
        for (var c = '0'; c <= '9'; c++)
        {
            Assert.AreEqual(c - '0', Alphanumeric.ExpandLetterDigit(c));
        }
    }

    /// <summary>
    /// Verifies that <see cref="Alphanumeric.ExpandLetterDigit(char)" /> throws
    /// <see cref="ArgumentOutOfRangeException" /> when the supplied character is not an ASCII decimal digit or
    /// uppercase Latin letter — covering the <c>ThrowHelper.ThrowIfNotAsciiAlphanumericUppercase</c> branch.
    /// </summary>
    /// <param name="invalid">The disallowed character under test.</param>
    [DataRow(' ')]
    [DataRow('-')]
    [DataRow('a')]
    [DataRow('z')]
    [DataRow('!')]
    [DataRow('@')]
    [DataRow('#')]
    [DataRow('*')]
    [DataRow('/')]
    [DataRow(':')]
    [DataRow('`')]
    [DataRow('{')]
    [DataRow('é')]
    [DataRow('\0')]
    [TestMethod]
    public void ExpandLetterDigit_WhenCharacterIsOutsideAlphanumericUppercase_ShouldThrowExactly(char invalid)
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = Alphanumeric.ExpandLetterDigit(invalid);
        });
    }

    /// <summary>
    /// Verifies that <see cref="Alphanumeric.ExpandLetterDigit(char)" /> maps each uppercase Latin letter to the
    /// ISO 7064 weighted value (<c>'A'</c>=10 … <c>'Z'</c>=35).
    /// </summary>
    [TestMethod]
    public void ExpandLetterDigit_WhenCharacterIsUppercaseLetter_ShouldReturnTenPlusOrdinal()
    {
        for (var c = 'A'; c <= 'Z'; c++)
        {
            Assert.AreEqual(c - 'A' + 10, Alphanumeric.ExpandLetterDigit(c));
        }
    }
    // ─── ValidateAlphanumeric ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// Verifies that <see cref="Alphanumeric.ValidateAlphanumeric(ReadOnlySpan{char}, string)" /> accepts every
    /// digit and every uppercase Latin letter without throwing.
    /// </summary>
    [TestMethod]
    public void ValidateAlphanumeric_WhenAllCharactersAreValid_ShouldNotThrow() => Alphanumeric.ValidateAlphanumeric("0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ".AsSpan(), "value");

    /// <summary>
    /// Verifies that <see cref="Alphanumeric.ValidateAlphanumeric(ReadOnlySpan{char}, string)" /> rejects any
    /// character outside <c>'0'</c>–<c>'9'</c> and <c>'A'</c>–<c>'Z'</c>, regardless of position in the span, and
    /// surfaces the supplied <paramref name="paramName" /> on the resulting exception.
    /// </summary>
    /// <param name="invalid">The disallowed character placed at position 2 of the input span.</param>
    [DataRow(' ')]
    [DataRow('-')]
    [DataRow('a')]
    [DataRow('z')]
    [DataRow('!')]
    [DataRow('@')]
    [DataRow('/')]
    [DataRow(':')]
    [DataRow('`')]
    [DataRow('{')]
    [DataRow('é')]
    [TestMethod]
    public void ValidateAlphanumeric_WhenCharacterIsOutsideAllowedRange_ShouldThrowExactly(char invalid)
    {
        var sequence = "A1" + invalid + "2B";

        ArgumentOutOfRangeException ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            Alphanumeric.ValidateAlphanumeric(sequence.AsSpan(), "value");
        });
        Assert.AreEqual("value", ex.ParamName);
    }

    /// <summary>
    /// Verifies that <see cref="Alphanumeric.ValidateAlphanumeric(ReadOnlySpan{char}, string)" /> accepts an
    /// empty span without throwing.
    /// </summary>
    [TestMethod]
    public void ValidateAlphanumeric_WhenSpanIsEmpty_ShouldNotThrow() => Alphanumeric.ValidateAlphanumeric([], "value");

    // ─── ValidateCusip ────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Verifies that <see cref="Alphanumeric.ValidateCusip(ReadOnlySpan{char}, string)" /> accepts every CUSIP
    /// alphabet character — digits, uppercase letters, and the three punctuation sentinels.
    /// </summary>
    [TestMethod]
    public void ValidateCusip_WhenAllCharactersAreValid_ShouldNotThrow() => Alphanumeric.ValidateCusip("0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ*@#".AsSpan(), "value");

    /// <summary>
    /// Verifies that <see cref="Alphanumeric.ValidateCusip(ReadOnlySpan{char}, string)" /> rejects any character
    /// outside <c>'0'</c>–<c>'9'</c>, <c>'A'</c>–<c>'Z'</c>, and the punctuation sentinels (<c>'*'</c>, <c>'@'</c>,
    /// <c>'#'</c>), and surfaces the supplied <paramref name="paramName" />.
    /// </summary>
    /// <param name="invalid">The disallowed character placed at position 2 of the input span.</param>
    [DataRow(' ')]
    [DataRow('-')]
    [DataRow('a')]
    [DataRow('z')]
    [DataRow('!')]
    [DataRow('$')]
    [DataRow('/')]
    [DataRow(':')]
    [DataRow('é')]
    [TestMethod]
    public void ValidateCusip_WhenCharacterIsOutsideCusipAlphabet_ShouldThrowExactly(char invalid)
    {
        var sequence = "A1" + invalid + "2#";

        ArgumentOutOfRangeException ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            Alphanumeric.ValidateCusip(sequence.AsSpan(), "value");
        });
        Assert.AreEqual("value", ex.ParamName);
    }

    /// <summary>
    /// Verifies that <see cref="Alphanumeric.ValidateCusip(ReadOnlySpan{char}, string)" /> accepts an empty span
    /// without throwing.
    /// </summary>
    [TestMethod]
    public void ValidateCusip_WhenSpanIsEmpty_ShouldNotThrow() => Alphanumeric.ValidateCusip([], "value");

}
