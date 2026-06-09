// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Isbn10.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------


namespace Bodu.IO.Hashing.CheckDigits;

/// <summary>
/// Computes the check character of a 10-character International Standard Book Number using the ISBN-10 weighted
/// modulus-11 algorithm. This class cannot be inherited.
/// </summary>
/// <remarks>
/// <para>
/// ISBN-10, in use prior to the global migration to ISBN-13 in 2007, applies weights <c>10, 9, 8, 7, 6, 5, 4, 3, 2</c>
/// to the nine body digits from left to right. The weighted sum is taken modulo eleven; the check character is chosen
/// so that the complete weighted sum over ten positions (including the check at weight one) is a multiple of eleven.
/// When the required check value is ten, the sentinel <c>'X'</c> is emitted.
/// </para>
/// <para>
/// <b>Worked examples.</b> For the body <c>"030640615"</c>, the computed check character is <c>'2'</c> (
/// <c>0306406152</c>). For the body <c>"043942089"</c>, the computed check character is <c>'X'</c> (<c>043942089X</c>),
/// demonstrating the <c>'X'</c>-for-ten sentinel.
/// </para>
/// <note type="important">This algorithm is <b>not</b> cryptographically secure and should <b>not</b> be used for
/// password hashing, digital signatures, or integrity validation in security-sensitive applications.</note>
/// </remarks>
/// <example>
///<![CDATA[
/// // Two contrasting bodies — the second exercises the 'X' sentinel.
/// char check    = Isbn10.Compute("030640615");   // '2'
/// char checkX   = Isbn10.Compute("043942089");   // 'X'  (sentinel for value ten)
///
/// // Full-sequence validation.
/// bool ok = Isbn10.IsValid("043942089X");        // true
///
/// // Streaming use when the body is built up incrementally.
/// var algo = new Isbn10();
/// algo.Append("030640615");
/// char d = algo.GetCurrentCheckDigit();          // '2'
///]]>
/// </example>
public sealed class Isbn10
    : AlphanumericCheckDigitAlgorithm
{
    /// <summary>
    /// The required body length of <c>9</c> decimal digits.
    /// </summary>
    public const int BodyLength = 9;

    /// <summary>
    /// The required full-sequence length of <c>10</c> characters.
    /// </summary>
    public const int SequenceLength = 10;
    private int _count;

    private int _sum;

    /// <summary>
    /// Initializes a new instance of the <see cref="Isbn10" /> class.
    /// </summary>
    public Isbn10()
    {
    }

    /// <inheritdoc />
    public override string AlgorithmName => "ISBN-10";

    /// <inheritdoc />
    public override CheckDigitInputAlphabet InputAlphabet => CheckDigitInputAlphabet.DecimalDigits;

    /// <inheritdoc />
    public override CheckDigitOutputAlphabet OutputAlphabet => CheckDigitOutputAlphabet.DecimalDigitsOrX;

    /// <summary>
    /// Computes the ISBN-10 check character for the supplied body of decimal digits without allocating a streaming
    /// instance.
    /// </summary>
    /// <param name="digits">
    /// The body characters. Each must be an ASCII decimal digit (<c>'0'</c> to <c>'9'</c>).
    /// </param>
    /// <returns>
    /// The check character as an ASCII digit <c>'0'</c> to <c>'9'</c>, or the sentinel <c>'X'</c> for the value ten.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="digits" /> contains any character outside the range <c>'0'</c> to <c>'9'</c>.
    /// </exception>
    /// <remarks>
    /// This helper is length-tolerant to support streaming and partial-body use;
    /// <see cref="IsValid(ReadOnlySpan{char})" /> enforces the strict <see cref="SequenceLength" /> domain contract for
    /// full validation.
    /// </remarks>
    public static char Compute(ReadOnlySpan<char> digits)
    {
        var sum = 0;
        for (var i = 0; i < digits.Length; i++)
        {
            var ch = digits[i];
            if ((uint)(ch - '0') > 9u)
                ThrowHelper.ThrowIfNotAsciiDecimalDigit(ch, nameof(digits));

            sum += (10 - i) * (ch - '0');
        }

        var check = (11 - (((sum % 11) + 11) % 11)) % 11;
        return check == 10 ? 'X' : (char)('0' + check);
    }

    /// <summary>
    /// Determines whether the supplied sequence, comprising a nine-digit body followed by a trailing ISBN-10 check
    /// character, is consistent.
    /// </summary>
    /// <param name="valueIncludingCheck">The complete ten-character sequence.</param>
    /// <returns>
    /// <see langword="true" /> if the sequence evaluates as valid under ISBN-10; otherwise, <see langword="false" /> —
    /// including the case where <paramref name="valueIncludingCheck" /> is empty, has the wrong length, or contains an
    /// unrecognized character.
    /// </returns>
    public static bool IsValid(ReadOnlySpan<char> valueIncludingCheck)
    {
        if (valueIncludingCheck.Length != SequenceLength) return false;

        var sum = 0;
        for (var i = 0; i < SequenceLength; i++)
        {
            var ch = valueIncludingCheck[i];
            int value;
            if ((uint)(ch - '0') <= 9u)
            {
                value = ch - '0';
            }
            else if (i == SequenceLength - 1 && ch == 'X')
            {
                value = 10;
            }
            else
            {
                return false;
            }

            sum += (10 - i) * value;
        }

        return sum % 11 == 0;
    }

    /// <inheritdoc />
    public override void Append(ReadOnlySpan<char> body)
    {
        var sum = _sum;
        var count = _count;
        for (var i = 0; i < body.Length; i++)
        {
            var ch = body[i];
            if ((uint)(ch - '0') > 9u)
                ThrowHelper.ThrowIfNotAsciiDecimalDigit(ch, nameof(body));

            // ISBN-10 weight for the digit at zero-based left-to-right position k is (10 - k). At k = 9 the
            // weight would be 1 (the check position), which this streaming API does not reach; beyond k = 9 the
            // weight becomes non-positive and the algorithm's guarantees no longer hold. Callers invoking with
            // bodies longer than 9 digits do so outside the ISBN-10 specification.
            sum += (10 - count) * (ch - '0');
            count++;
        }

        _sum = sum;
        _count = count;
    }

    /// <inheritdoc />
    public override char GetCurrentCheckDigit()
    {
        var check = (11 - (((_sum % 11) + 11) % 11)) % 11;
        return check == 10 ? 'X' : (char)('0' + check);
    }

    /// <inheritdoc />
    public override void Reset()
    {
        _sum = 0;
        _count = 0;
    }
}
