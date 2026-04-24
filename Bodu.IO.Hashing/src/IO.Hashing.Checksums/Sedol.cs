// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Sedol.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Hashing.Checksums;

/// <summary>
/// Computes the check digit of a 7-character Stock Exchange Daily Official List (SEDOL) identifier using the
/// weighted modulus-10 scheme defined by the London Stock Exchange. This class cannot be inherited.
/// </summary>
/// <remarks>
/// <para>
/// SEDOL identifiers comprise six body characters followed by a single numeric check digit. Body characters are
/// drawn from the decimal digits and the uppercase Latin consonants — vowels (<c>A</c>, <c>E</c>, <c>I</c>,
/// <c>O</c>, <c>U</c>) are <b>not</b> permitted in any body position and will cause input-validation exceptions.
/// </para>
/// <para>
/// The weight pattern <c>{1, 3, 1, 7, 3, 9}</c> is applied from left to right across the six body characters;
/// each character's numeric value (digits: <c>0</c>–<c>9</c>; letters: <c>A</c>=10 … <c>Z</c>=35) is multiplied
/// by its position weight. The check digit is chosen so the weighted sum is a multiple of ten.
/// </para>
/// <para>
/// <b>Worked example.</b> For the body <c>"B0WNLY"</c>, the computed check digit is <c>'7'</c>, and the
/// resulting SEDOL <c>"B0WNLY7"</c> is therefore valid.
/// </para>
/// <note type="important">This algorithm is <b>not</b> cryptographically secure and should <b>not</b> be used for
/// password hashing, digital signatures, or integrity validation in security-sensitive applications.</note>
/// </remarks>
public sealed class Sedol
    : AlphanumericCheckDigitAlgorithm
{
    /// <summary>The required body length of <c>6</c> characters.</summary>
    public const int BodyLength = 6;

    /// <summary>The required full-sequence length of <c>7</c> characters.</summary>
    public const int SequenceLength = 7;

    private static readonly int[] Weights = [1, 3, 1, 7, 3, 9];

    private int _sum;
    private int _count;

    /// <summary>
    /// Initializes a new instance of the <see cref="Sedol" /> class.
    /// </summary>
    public Sedol()
    {
    }

    /// <inheritdoc />
    public override string AlgorithmName => "SEDOL";

    /// <inheritdoc />
    public override CheckDigitInputAlphabet InputAlphabet => CheckDigitInputAlphabet.AlphanumericUppercase;

    /// <inheritdoc />
    public override CheckDigitOutputAlphabet OutputAlphabet => CheckDigitOutputAlphabet.DecimalDigits;

    /// <inheritdoc />
    public override void Append(ReadOnlySpan<char> body)
    {
        int sum = _sum;
        int count = _count;

        for (int i = 0; i < body.Length; i++)
        {
            char ch = body[i];
            if (Alphanumeric.IsVowel(ch) || ((uint)(ch - '0') > 9u && (uint)(ch - 'A') > 25u))
                throw new ArgumentOutOfRangeException(
                    nameof(body),
                    ch,
                    $"Character '{ch}' (U+{(int)ch:X4}) is not a valid SEDOL character ('0'-'9' or uppercase consonant; vowels A/E/I/O/U are not permitted).");

            int weight = count < Weights.Length ? Weights[count] : 1;
            sum += Alphanumeric.ExpandLetterDigit(ch) * weight;
            count++;
        }

        _sum = sum;
        _count = count;
    }

    /// <inheritdoc />
    public override void Reset()
    {
        _sum = 0;
        _count = 0;
    }

    /// <inheritdoc />
    public override char GetCurrentCheckDigit() =>
        (char)('0' + ((10 - (_sum % 10)) % 10));

    /// <summary>
    /// Computes the SEDOL check digit for the supplied body without allocating a streaming instance.
    /// </summary>
    /// <param name="body">The body characters. Each must be an ASCII decimal digit or uppercase consonant.</param>
    /// <returns>The check digit as an ASCII character in the range <c>'0'</c> to <c>'9'</c>.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="body" /> contains a vowel or a character outside the SEDOL alphabet.
    /// </exception>
    public static char Compute(ReadOnlySpan<char> body)
    {
        Alphanumeric.ValidateSedol(body, nameof(body));

        int sum = 0;
        for (int i = 0; i < body.Length; i++)
        {
            int weight = i < Weights.Length ? Weights[i] : 1;
            sum += Alphanumeric.ExpandLetterDigit(body[i]) * weight;
        }

        return (char)('0' + ((10 - (sum % 10)) % 10));
    }

    /// <summary>
    /// Determines whether the supplied sequence, comprising a six-character body followed by a single-digit
    /// SEDOL check, is consistent.
    /// </summary>
    /// <param name="valueIncludingCheck">The complete seven-character SEDOL.</param>
    /// <returns>
    /// <see langword="true" /> if the sequence is empty or evaluates as valid under SEDOL; otherwise,
    /// <see langword="false" /> — including the case where the length is wrong, any body character is a vowel or
    /// non-alphanumeric, or the check character is not a decimal digit.
    /// </returns>
    public static bool IsValid(ReadOnlySpan<char> valueIncludingCheck)
    {
        if (valueIncludingCheck.IsEmpty) return true;
        if (valueIncludingCheck.Length != SequenceLength) return false;

        int sum = 0;
        for (int i = 0; i < BodyLength; i++)
        {
            char ch = valueIncludingCheck[i];
            if (Alphanumeric.IsVowel(ch)) return false;
            int v;
            if ((uint)(ch - '0') <= 9u) v = ch - '0';
            else if ((uint)(ch - 'A') <= 25u) v = ch - 'A' + 10;
            else return false;

            sum += v * Weights[i];
        }

        char checkChar = valueIncludingCheck[BodyLength];
        if ((uint)(checkChar - '0') > 9u) return false;
        sum += checkChar - '0';

        return sum % 10 == 0;
    }
}
