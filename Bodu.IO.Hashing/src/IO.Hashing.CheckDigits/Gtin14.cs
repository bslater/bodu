// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Gtin14.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.IO.Hashing.Checksums;

namespace Bodu.IO.Hashing.CheckDigits;

/// <summary>
/// Computes the check digit of a 14-digit Global Trade Item Number (GTIN-14) barcode using the GTIN-14 weighted
/// modulus-10 algorithm. This class cannot be inherited.
/// </summary>
/// <remarks>
/// <para>
/// GTIN-14 — the logistics-tier GS1 identifier derived by prefixing an EAN-13 with a single indicator digit —
/// shares its weight pattern with EAN-13, UPC-A, and ISBN-13. The static helpers on this type enforce a strict
/// 13-digit body length (14-digit full sequence); the streaming surface is length-agnostic.
/// </para>
/// <para>
/// <b>Worked example.</b> For the body <c>"1061414100041"</c>, the computed check digit is <c>'5'</c>, and the
/// resulting GTIN-14 <c>"10614141000415"</c> is therefore valid.
/// </para>
/// <note type="important">This algorithm is <b>not</b> cryptographically secure and should <b>not</b> be used for
/// password hashing, digital signatures, or integrity validation in security-sensitive applications.</note>
/// </remarks>
public sealed class Gtin14
    : CheckDigitAlgorithm
{
    /// <summary>The required body length of <c>13</c> decimal digits.</summary>
    public const int BodyLength = 13;

    /// <summary>The required full-sequence length of <c>14</c> decimal digits.</summary>
    public const int SequenceLength = 14;

    private int _sumEvenHypothesis;
    private int _sumOddHypothesis;
    private int _count;

    /// <summary>
    /// Initializes a new instance of the <see cref="Gtin14" /> class.
    /// </summary>
    public Gtin14()
    {
    }

    /// <inheritdoc />
    public override string AlgorithmName => "GTIN-14";

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
    public override void Reset()
    {
        _sumEvenHypothesis = 0;
        _sumOddHypothesis = 0;
        _count = 0;
    }

    /// <inheritdoc />
    public override char GetCurrentCheckDigit()
    {
        int sum = (_count & 1) == 0 ? _sumEvenHypothesis : _sumOddHypothesis;
        return (char)('0' + ((10 - (sum % 10)) % 10));
    }

    /// <summary>
    /// Computes the GTIN-14 check digit for the supplied body of decimal digits without allocating a streaming
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
    /// Determines whether the supplied sequence, comprising a thirteen-digit body followed by a trailing
    /// GTIN-14 check digit, is consistent.
    /// </summary>
    /// <param name="digitsIncludingCheck">The complete sequence including the trailing check digit.</param>
    /// <returns>
    /// <see langword="true" /> if the sequence is exactly <see cref="SequenceLength" /> digits and evaluates as
    /// valid under GTIN-14; otherwise, <see langword="false" />.
    /// </returns>
    public static bool IsValid(ReadOnlySpan<char> digitsIncludingCheck)
    {
        if (digitsIncludingCheck.IsEmpty) return true;
        if (digitsIncludingCheck.Length != SequenceLength) return false;
        return WeightedMod10.IsValidIsbn13(digitsIncludingCheck);
    }
}
