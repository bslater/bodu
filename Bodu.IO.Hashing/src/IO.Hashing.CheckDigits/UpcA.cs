// ---------------------------------------------------------------------------------------------------------------
// <copyright file="UpcA.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.IO.Hashing.Checksums;

namespace Bodu.IO.Hashing.CheckDigits;

/// <summary>
/// Computes the check digit of a 12-digit Universal Product Code (UPC-A) barcode using the UPC-A weighted
/// modulus-10 algorithm. This class cannot be inherited.
/// </summary>
/// <remarks>
/// <para>
/// UPC-A shares its weight pattern with EAN-13 and ISBN-13; a UPC-A value is exactly an EAN-13 whose country
/// prefix is a leading zero. The static helpers on this type enforce a strict 11-digit body length (12-digit
/// full sequence); the streaming surface is length-agnostic.
/// </para>
/// <para>
/// <b>Worked example.</b> For the body <c>"03600029145"</c>, the computed check digit is <c>'2'</c>, and the
/// resulting UPC-A <c>"036000291452"</c> is therefore valid.
/// </para>
/// <note type="important">This algorithm is <b>not</b> cryptographically secure and should <b>not</b> be used for
/// password hashing, digital signatures, or integrity validation in security-sensitive applications.</note>
/// </remarks>
public sealed class UpcA
    : CheckDigitAlgorithm
{
    /// <summary>The required body length of <c>11</c> decimal digits.</summary>
    public const int BodyLength = 11;

    /// <summary>The required full-sequence length of <c>12</c> decimal digits.</summary>
    public const int SequenceLength = 12;

    private int sumEvenHypothesis;
    private int sumOddHypothesis;
    private int count;

    /// <summary>
    /// Initializes a new instance of the <see cref="UpcA" /> class.
    /// </summary>
    public UpcA()
    {
    }

    /// <inheritdoc />
    public override string AlgorithmName => "UPC-A";

    /// <inheritdoc />
    public override void Append(ReadOnlySpan<char> digits)
    {
        int sumEven = sumEvenHypothesis;
        int sumOdd = sumOddHypothesis;
        int count = this.count;

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

        sumEvenHypothesis = sumEven;
        sumOddHypothesis = sumOdd;
        this.count = count;
    }

    /// <inheritdoc />
    public override void Reset()
    {
        sumEvenHypothesis = 0;
        sumOddHypothesis = 0;
        count = 0;
    }

    /// <inheritdoc />
    public override char GetCurrentCheckDigit()
    {
        int sum = (count & 1) == 0 ? sumEvenHypothesis : sumOddHypothesis;
        return (char)('0' + ((10 - (sum % 10)) % 10));
    }

    /// <summary>
    /// Computes the UPC-A check digit for the supplied body of decimal digits without allocating a streaming
    /// instance.
    /// </summary>
    /// <param name="digits">The body characters. Each must be an ASCII decimal digit (<c>'0'</c> to <c>'9'</c>).</param>
    /// <returns>The check digit as an ASCII character in the range <c>'0'</c> to <c>'9'</c>.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="digits" /> contains any character outside the range <c>'0'</c> to <c>'9'</c>.
    /// </exception>
    /// <remarks>
    /// This helper is length-tolerant to support streaming and partial-body use; <see cref="IsValid(ReadOnlySpan{char})" />
    /// enforces the strict <see cref="SequenceLength" /> domain contract for full validation.
    /// </remarks>
    public static char Compute(ReadOnlySpan<char> digits) =>
        WeightedMod10.ComputeIsbn13(digits);

    /// <summary>
    /// Determines whether the supplied sequence, comprising an eleven-digit body followed by a trailing UPC-A
    /// check digit, is consistent.
    /// </summary>
    /// <param name="digitsIncludingCheck">The complete sequence including the trailing check digit.</param>
    /// <returns>
    /// <see langword="true" /> if the sequence is exactly <see cref="SequenceLength" /> digits and evaluates as
    /// valid under UPC-A; otherwise, <see langword="false" />.
    /// </returns>
    public static bool IsValid(ReadOnlySpan<char> digitsIncludingCheck)
    {
        if (digitsIncludingCheck.IsEmpty) return true;
        if (digitsIncludingCheck.Length != SequenceLength) return false;
        return WeightedMod10.IsValidIsbn13(digitsIncludingCheck);
    }
}
