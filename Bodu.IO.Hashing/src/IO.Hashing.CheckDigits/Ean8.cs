// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Ean8.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.IO.Hashing.Checksums;

namespace Bodu.IO.Hashing.CheckDigits;

/// <summary>
/// Computes the check digit of an 8-digit European Article Number / Global Trade Item Number-8 barcode using the EAN-8
/// weighted modulus-10 algorithm. This class cannot be inherited.
/// </summary>
/// <remarks>
/// <para>
/// EAN-8 shares its weight pattern with ISBN-13 / EAN-13 / UPC-A / GTIN-14: alternating 1 and 3 applied from the check
/// digit leftward. The static helpers on this type enforce a strict 7-digit body length (8-digit full sequence); the
/// streaming surface is length-agnostic.
/// </para>
/// <para>
/// <b>Worked example.</b> For the body <c>"7351353"</c>, the computed check digit is <c>'7'</c>, and the resulting
/// EAN-8 <c>"73513537"</c> is therefore valid.
/// </para>
/// <note type="important">This algorithm is <b>not</b> cryptographically secure and should <b>not</b> be used for
/// password hashing, digital signatures, or integrity validation in security-sensitive applications.</note>
/// </remarks>
public sealed class Ean8
    : CheckDigitAlgorithm
{
    /// <summary>
    /// The required body length of <c>7</c> decimal digits.
    /// </summary>
    public const int BodyLength = 7;

    /// <summary>
    /// The required full-sequence length of <c>8</c> decimal digits.
    /// </summary>
    public const int SequenceLength = 8;
    private int _count;

    private int _sumEvenHypothesis;
    private int _sumOddHypothesis;

    /// <summary>
    /// Initializes a new instance of the <see cref="Ean8" /> class.
    /// </summary>
    public Ean8()
    {
    }

    /// <inheritdoc />
    public override string AlgorithmName => "EAN-8";

    /// <summary>
    /// Computes the EAN-8 check digit for the supplied body of decimal digits without allocating a streaming instance.
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
    /// Determines whether the supplied sequence, comprising a seven-digit body followed by a trailing EAN-8 check
    /// digit, is consistent.
    /// </summary>
    /// <param name="digitsIncludingCheck">The complete sequence including the trailing check digit.</param>
    /// <returns>
    /// <see langword="true" /> if the sequence is exactly <see cref="SequenceLength" /> digits and evaluates as valid
    /// under EAN-8; otherwise, <see langword="false" />.
    /// </returns>
    public static bool IsValid(ReadOnlySpan<char> digitsIncludingCheck)
    {
        if (digitsIncludingCheck.IsEmpty) return true;
        if (digitsIncludingCheck.Length != SequenceLength) return false;
        return WeightedMod10.IsValidIsbn13(digitsIncludingCheck);
    }

    /// <inheritdoc />
    public override void Append(ReadOnlySpan<char> digits)
    {
        var sumEven = _sumEvenHypothesis;
        var sumOdd = _sumOddHypothesis;
        var count = _count;

        for (var i = 0; i < digits.Length; i++)
        {
            var ch = digits[i];
            if ((uint)(ch - '0') > 9u)
                ThrowHelper.ThrowIfNotAsciiDecimalDigit(ch, nameof(digits));

            var v = ch - '0';
            var tripled = v * 3;

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
        var sum = (_count & 1) == 0 ? _sumEvenHypothesis : _sumOddHypothesis;
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
