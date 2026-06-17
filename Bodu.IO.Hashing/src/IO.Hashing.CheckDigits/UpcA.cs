// ---------------------------------------------------------------------------------------------------------------
// <copyright file="UpcA.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------


namespace Bodu.IO.Hashing.CheckDigits;

/// <summary>
/// Computes the check digit of a 12-digit Universal Product Code (UPC-A) barcode using the UPC-A weighted modulus-10
/// algorithm. This class cannot be inherited.
/// </summary>
/// <remarks>
/// <para>
/// UPC-A shares its weight pattern with EAN-13 and ISBN-13; a UPC-A value is exactly an EAN-13 whose country prefix is
/// a leading zero. The static helpers on this type enforce a strict 11-digit body length (12-digit full sequence); the
/// streaming surface is length-agnostic.
/// </para>
/// <para>
/// <b>Worked example.</b> For the body <c>"03600029145"</c>, the computed check digit is <c>'2'</c>, and the resulting
/// UPC-A <c>"036000291452"</c> is therefore valid.
/// </para>
/// <note type="important">This algorithm is <b>not</b> cryptographically secure and should <b>not</b> be used for
/// password hashing, digital signatures, or integrity validation in security-sensitive applications.</note>
/// </remarks>
/// <example>
///<![CDATA[
/// // Single-call computation against the 11-digit body.
/// char check = UpcA.Compute("03600029145");   // '2'
///
/// // Full-sequence validation.
/// bool ok = UpcA.IsValid("036000291452");     // true
///
/// // Streaming use when the body is built up incrementally.
/// var algo = new UpcA();
/// algo.Append("03600029145");
/// char d = algo.GetCurrentCheckDigit();       // '2'
///]]>
/// </example>
public sealed class UpcA
    : CheckDigitAlgorithm
{
    /// <summary>
    /// The required body length of <c>11</c> decimal digits.
    /// </summary>
    public const int BodyLength = 11;

    /// <summary>
    /// The required full-sequence length of <c>12</c> decimal digits.
    /// </summary>
    public const int SequenceLength = 12;

    /// <summary>
    /// The number of body digits appended so far.
    /// </summary>
    private int _count;

    /// <summary>
    /// The running weighted sum computed under the hypothesis that the final body length is even.
    /// </summary>
    private int _sumEvenHypothesis;

    /// <summary>
    /// The running weighted sum computed under the hypothesis that the final body length is odd.
    /// </summary>
    private int _sumOddHypothesis;

    /// <summary>
    /// Initializes a new instance of the <see cref="UpcA" /> class.
    /// </summary>
    public UpcA()
    {
    }

    /// <inheritdoc />
    public override string AlgorithmName => "UPC-A";

    /// <summary>
    /// Computes the UPC-A check digit for the supplied body of decimal digits without allocating a streaming instance.
    /// </summary>
    /// <param name="digits">
    /// The body characters. Each must be an ASCII decimal digit (<c>'0'</c> to <c>'9'</c>).
    /// </param>
    /// <returns>The check digit as an ASCII character in the range <c>'0'</c> to <c>'9'</c>.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="digits" /> contains any character outside the range <c>'0'</c> to <c>'9'</c>.
    /// </exception>
    /// <remarks>
    /// This helper is length-tolerant to support streaming and partial-body use;
    /// <see cref="IsValid(ReadOnlySpan{char})" /> enforces the strict <see cref="SequenceLength" /> domain contract for
    /// full validation.
    /// </remarks>
    public static char Compute(ReadOnlySpan<char> digits) =>
        WeightedMod10.ComputeIsbn13(digits);

    /// <summary>
    /// Determines whether the supplied sequence, comprising an eleven-digit body followed by a trailing UPC-A check
    /// digit, is consistent.
    /// </summary>
    /// <param name="digitsIncludingCheck">The complete sequence including the trailing check digit.</param>
    /// <returns>
    /// <see langword="true" /> if the sequence is exactly <see cref="SequenceLength" /> digits and evaluates as valid
    /// under UPC-A; otherwise, <see langword="false" /> — including the case where
    /// <paramref name="digitsIncludingCheck" /> is empty.
    /// </returns>
    public static bool IsValid(ReadOnlySpan<char> digitsIncludingCheck) =>
        digitsIncludingCheck.Length == SequenceLength && WeightedMod10.IsValidIsbn13(digitsIncludingCheck);

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
