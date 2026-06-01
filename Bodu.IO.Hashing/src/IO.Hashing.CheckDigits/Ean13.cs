// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Ean13.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.IO.Hashing.Checksums;

namespace Bodu.IO.Hashing.CheckDigits;

/// <summary>
/// Computes the check digit of a 13-digit European Article Number / Global Trade Item Number-13 barcode using the
/// EAN-13 weighted modulus-10 algorithm. This class cannot be inherited.
/// </summary>
/// <remarks>
/// <para>
/// EAN-13 and ISBN-13 share the same weight pattern — alternating 1 and 3 over the twelve body digits with the
/// rightmost data digit carrying weight 3 — so a 13-digit ISBN is also a valid EAN-13. The static helpers on this type
/// enforce a strict 12-digit body length (13-digit full sequence) to make downstream callers' intent explicit; the
/// streaming surface is length-agnostic.
/// </para>
/// <para>
/// <b>Worked example.</b> For the body <c>"501234567890"</c>, the computed check digit is <c>'0'</c>, and the resulting
/// EAN-13 <c>"5012345678900"</c> is therefore valid.
/// </para>
/// <note type="important">This algorithm is <b>not</b> cryptographically secure and should <b>not</b> be used for
/// password hashing, digital signatures, or integrity validation in security-sensitive applications.</note>
/// </remarks>
/// <example>
///<![CDATA[
/// Single-call computation against the 12-digit body.
/// char check = Ean13.Compute("501234567890");   // '0'
///
/// Full-sequence validation.
/// bool ok = Ean13.IsValid("5012345678900");     // true
///
/// Streaming use when the body is built up incrementally.
/// var algo = new Ean13();
/// algo.Append("501234567890");
/// char d = algo.GetCurrentCheckDigit();         // '0'
///]]>
/// </example>
public sealed class Ean13
    : CheckDigitAlgorithm
{
    /// <summary>
    /// The required body length of <c>12</c> decimal digits.
    /// </summary>
    public const int BodyLength = 12;

    /// <summary>
    /// The required full-sequence length of <c>13</c> decimal digits.
    /// </summary>
    public const int SequenceLength = 13;
    private int _count;

    private int _sumEvenHypothesis;
    private int _sumOddHypothesis;

    /// <summary>
    /// Initializes a new instance of the <see cref="Ean13" /> class.
    /// </summary>
    public Ean13()
    {
    }

    /// <inheritdoc />
    public override string AlgorithmName => "EAN-13";

    /// <summary>
    /// Computes the EAN-13 check digit for the supplied body of decimal digits without allocating a streaming instance.
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
    /// Determines whether the supplied sequence, comprising a twelve-digit body followed by a trailing EAN-13 check
    /// digit, is consistent.
    /// </summary>
    /// <param name="digitsIncludingCheck">The complete sequence including the trailing check digit.</param>
    /// <returns>
    /// <see langword="true" /> if the sequence is exactly <see cref="SequenceLength" /> digits and evaluates as valid
    /// under EAN-13; otherwise, <see langword="false" /> — including the case where
    /// <paramref name="digitsIncludingCheck" /> is empty, has the wrong length, or contains a non-digit character.
    /// </returns>
    public static bool IsValid(ReadOnlySpan<char> digitsIncludingCheck) =>
        digitsIncludingCheck.Length == SequenceLength && WeightedMod10.IsValidIsbn13(digitsIncludingCheck);

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
