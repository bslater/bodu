// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Isin.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Hashing.Checksums;

/// <summary>
/// Computes the check digit of a 12-character International Securities Identification Number (ISIN) using the
/// Luhn-based scheme defined by ISO 6166. This class cannot be inherited.
/// </summary>
/// <remarks>
/// <para>
/// An ISIN comprises a two-letter country code, a nine-character national security identifier, and a single
/// trailing check digit. To compute the check, every body character is expanded to its numeric value (digits as
/// themselves, <c>'A'</c>–<c>'Z'</c> to 10–35) and the concatenated string of resulting digits is fed to the
/// standard Luhn algorithm.
/// </para>
/// <para>
/// Because each letter contributes two digits while each numeral contributes one, the effective digit stream
/// length varies by country-code and body composition. The streaming surface nevertheless preserves
/// prefix-equivalence with <see cref="Compute(ReadOnlySpan{char})" />.
/// </para>
/// <para>
/// <b>Worked example.</b> For the body <c>"US037833100"</c> — Apple Inc. — the computed check digit is <c>'5'</c>,
/// and the resulting ISIN <c>"US0378331005"</c> is therefore valid.
/// </para>
/// <note type="important">This algorithm is <b>not</b> cryptographically secure and should <b>not</b> be used for
/// password hashing, digital signatures, or integrity validation in security-sensitive applications.</note>
/// </remarks>
public sealed class Isin
    : AlphanumericCheckDigitAlgorithm
{
    /// <summary>The required body length of <c>11</c> characters.</summary>
    public const int BodyLength = 11;

    /// <summary>The required full-sequence length of <c>12</c> characters.</summary>
    public const int SequenceLength = 12;

    private readonly Luhn _luhn = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="Isin" /> class.
    /// </summary>
    public Isin()
    {
    }

    /// <inheritdoc />
    public override string AlgorithmName => "ISIN";

    /// <inheritdoc />
    public override CheckDigitInputAlphabet InputAlphabet => CheckDigitInputAlphabet.AlphanumericUppercase;

    /// <inheritdoc />
    public override CheckDigitOutputAlphabet OutputAlphabet => CheckDigitOutputAlphabet.DecimalDigits;

    /// <inheritdoc />
    public override void Append(ReadOnlySpan<char> body)
    {
        for (int i = 0; i < body.Length; i++)
        {
            char ch = body[i];
            if ((uint)(ch - '0') <= 9u)
            {
                _luhn.Append(ch);
            }
            else if ((uint)(ch - 'A') <= 25u)
            {
                int value = ch - 'A' + 10;
                _luhn.Append((char)('0' + (value / 10)));
                _luhn.Append((char)('0' + (value % 10)));
            }
            else
            {
                ThrowHelper.ThrowIfNotAsciiAlphanumericUppercase(ch, nameof(body));
            }
        }
    }

    /// <inheritdoc />
    public override void Reset() =>
        _luhn.Reset();

    /// <inheritdoc />
    public override char GetCurrentCheckDigit() =>
        _luhn.GetCurrentCheckDigit();

    /// <summary>
    /// Computes the ISIN check digit for the supplied body without allocating a streaming instance.
    /// </summary>
    /// <param name="body">The body characters. Each must be an ASCII decimal digit or uppercase Latin letter.</param>
    /// <returns>The check digit as an ASCII character in the range <c>'0'</c> to <c>'9'</c>.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="body" /> contains any character outside <c>'0'</c>–<c>'9'</c> and
    /// <c>'A'</c>–<c>'Z'</c>.
    /// </exception>
    public static char Compute(ReadOnlySpan<char> body)
    {
        Isin isin = new();
        isin.Append(body);
        return isin.GetCurrentCheckDigit();
    }

    /// <summary>
    /// Determines whether the supplied sequence, comprising an eleven-character body followed by a single-digit
    /// ISIN check, is consistent.
    /// </summary>
    /// <param name="valueIncludingCheck">The complete twelve-character ISIN.</param>
    /// <returns>
    /// <see langword="true" /> if the sequence is empty or evaluates as valid under ISIN; otherwise,
    /// <see langword="false" /> — including the case where the length is wrong, any body character is outside
    /// the alphanumeric uppercase alphabet, or the check character is not a decimal digit.
    /// </returns>
    public static bool IsValid(ReadOnlySpan<char> valueIncludingCheck)
    {
        if (valueIncludingCheck.IsEmpty) return true;
        if (valueIncludingCheck.Length != SequenceLength) return false;

        // Short-circuit obvious rejections before allocating.
        for (int i = 0; i < SequenceLength; i++)
        {
            char ch = valueIncludingCheck[i];
            if ((uint)(ch - '0') > 9u && (uint)(ch - 'A') > 25u) return false;
        }

        if ((uint)(valueIncludingCheck[SequenceLength - 1] - '0') > 9u) return false;

        char computed = Compute(valueIncludingCheck[..^1]);
        return computed == valueIncludingCheck[SequenceLength - 1];
    }
}
