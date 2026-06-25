// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Isbn13.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Hashing.CheckDigits;

/// <summary>
/// Computes the check digit of a 13-digit International Standard Book Number using the ISBN-13 weighted modulus-10
/// algorithm. This class cannot be inherited.
/// </summary>
/// <remarks>
/// <para>
/// The ISBN-13 check scheme, introduced in 2007 by the International ISBN Agency, alternates weights of 1 and 3 across
/// the twelve body digits — the rightmost data digit receives weight 3 — and the check digit is chosen so that the
/// resulting weighted sum is a multiple of ten.
/// </para>
/// <para>
/// The same algorithm underpins the EAN-13, UPC-A, GTIN-8, and GTIN-14 barcode families — see <see cref="Ean13" /> and
/// its siblings for strict-length variants.
/// </para>
/// <para>
/// <b>Worked example.</b> For the body <c>"978030640615"</c>, the computed check digit is <c>'7'</c>, and the resulting
/// ISBN-13 <c>"9780306406157"</c> is therefore valid.
/// </para>
/// <note type="important">This algorithm is <b>not</b> cryptographically secure and should <b>not</b> be used for
/// password hashing, digital signatures, or integrity validation in security-sensitive applications.</note>
/// </remarks>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// // Single-call computation against the 12-digit body.
/// char check = Isbn13.Compute("978030640615");   // '7'
///
/// // Full-sequence validation.
/// bool ok = Isbn13.IsValid("9780306406157");     // true
///
/// // Streaming use when the body is built up incrementally.
/// var algo = new Isbn13();
/// algo.Append("978030640615");
/// char d = algo.GetCurrentCheckDigit();          // '7'
///]]>
/// </code>
/// </example>
public sealed class Isbn13
    : CheckDigitAlgorithm
{
    /// <summary>The number of body digits appended so far.</summary>
    private int _count;

    /// <summary>The running weighted sum computed under the hypothesis that the final body length is even.</summary>
    private int _sumEvenHypothesis;

    /// <summary>The running weighted sum computed under the hypothesis that the final body length is odd.</summary>
    private int _sumOddHypothesis;

    /// <summary>
    /// Initializes a new instance of the <see cref="Isbn13" /> class.
    /// </summary>
    public Isbn13()
    {
    }

    /// <inheritdoc />
    public override string AlgorithmName => "ISBN-13";

    /// <summary>
    /// Computes the ISBN-13 check digit for the supplied body of decimal digits without allocating a streaming
    /// instance.
    /// </summary>
    /// <param name="digits">
    /// The body characters. Each must be an ASCII decimal digit (<c>'0'</c> to <c>'9'</c>).
    /// </param>
    /// <returns>The check digit as an ASCII character in the range <c>'0'</c> to <c>'9'</c>.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="digits" /> contains any character outside the range <c>'0'</c> to <c>'9'</c>.
    /// </exception>
    public static char Compute(ReadOnlySpan<char> digits) =>
        WeightedMod10.ComputeIsbn13(digits);

    /// <summary>
    /// Determines whether the supplied sequence, comprising a twelve-digit body followed by a trailing ISBN-13 check
    /// digit, is consistent — that is, whether the weighted sum evaluates to a multiple of ten.
    /// </summary>
    /// <param name="digitsIncludingCheck">The complete sequence including the trailing check digit.</param>
    /// <returns>
    /// <see langword="true" /> if the sequence evaluates as valid under ISBN-13; otherwise, <see langword="false" /> —
    /// including the case where <paramref name="digitsIncludingCheck" /> is empty or contains a character outside the
    /// range <c>'0'</c> to <c>'9'</c>.
    /// </returns>
    public static bool IsValid(ReadOnlySpan<char> digitsIncludingCheck) =>
        WeightedMod10.IsValidIsbn13(digitsIncludingCheck);

    /// <inheritdoc />
    public override void Append(ReadOnlySpan<char> digits)
    {
        int sumEven = _sumEvenHypothesis;
        int sumOdd = _sumOddHypothesis;
        int count = _count;

        for (int i = 0; i < digits.Length; i++)
        {
            char ch = digits[i];
            if ((uint)(ch - '0') > 9u)
                ThrowHelper.ThrowIfNotAsciiDecimalDigit(ch, nameof(digits));

            int v = ch - '0';
            int tripled = v * 3;

            // Dual-hypothesis accumulator. The rightmost data digit always carries weight 3, so whether the
            // digit at body-index i carries weight 1 or weight 3 depends on whether the final body length N
            // is even or odd. _sumEvenHypothesis assumes N is even; _sumOddHypothesis assumes N is odd.
            if ((count & 1) == 0)
            {
                sumEven += v;
                sumOdd += tripled;
            }
            else
            {
                sumEven += tripled;
                sumOdd += v;
            }

            count++;
        }

        _sumEvenHypothesis = sumEven;
        _sumOddHypothesis = sumOdd;
        _count = count;
    }

    /// <inheritdoc />
    public override char GetCurrentCheckDigit()
    {
        int sum = (_count & 1) == 0 ? _sumEvenHypothesis : _sumOddHypothesis;
        return (char)('0' + ((10 - (sum % 10)) % 10));
    }

    /// <inheritdoc />
    public override void Reset()
    {
        _sumEvenHypothesis = 0;
        _sumOddHypothesis = 0;
        _count = 0;
    }
}
