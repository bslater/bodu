// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AbaRoutingNumber.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Hashing.CheckDigits;

using Bodu.IO.Hashing.Checksums;

/// <summary>
/// Computes the check digit of a 9-digit American Bankers Association (ABA) routing transit number using the
/// weighted modulus-10 scheme specified by the US Federal Reserve. This class cannot be inherited.
/// </summary>
/// <remarks>
/// <para>
/// The ABA scheme applies the cyclic weight pattern <c>{3, 7, 1}</c> to the first eight digits (weights
/// <c>3, 7, 1, 3, 7, 1, 3, 7</c> from left to right). The check digit is chosen so that the full nine-digit
/// sequence yields a weighted sum that is a multiple of ten.
/// </para>
/// <para>
/// The static helpers on this type enforce a strict 8-digit body length (9-digit full sequence) to match the
/// ABA specification; the streaming surface is length-agnostic to permit chunked input.
/// </para>
/// <para>
/// <b>Worked example.</b> For the body <c>"01100001"</c> — the leading 8 digits of the Federal Reserve Bank of
/// Boston's routing number — the computed check digit is <c>'5'</c>, and the resulting sequence
/// <c>"011000015"</c> is therefore valid.
/// </para>
/// <note type="important">This algorithm is <b>not</b> cryptographically secure and should <b>not</b> be used for
/// password hashing, digital signatures, or integrity validation in security-sensitive applications.</note>
/// </remarks>
public sealed class AbaRoutingNumber
    : CheckDigitAlgorithm
{
    /// <summary>The required body length of <c>8</c> decimal digits.</summary>
    public const int BodyLength = 8;

    /// <summary>The required full-sequence length of <c>9</c> decimal digits.</summary>
    public const int SequenceLength = 9;

    // Weights applied from the left of the body toward the right. Position 0 weight 3, position 1 weight 7,
    // position 2 weight 1, repeating. The dual-hypothesis trick used by the ISBN-13 family does not apply here
    // — the cycle period is 3, so a streaming consumer cannot weigh incoming digits until the final body
    // length is known. To preserve the streaming contract (Append / GetCurrentCheckDigit at any point), the
    // implementation buffers the running list of digit values (at most 8) and recomputes the weighted sum on
    // each GetCurrentCheckDigit call. This is acceptably cheap given the tiny fixed body length.
    private readonly List<int> _digits = new List<int>(BodyLength);

    /// <summary>
    /// Initializes a new instance of the <see cref="AbaRoutingNumber" /> class.
    /// </summary>
    public AbaRoutingNumber()
    {
    }

    /// <inheritdoc />
    public override string AlgorithmName => "ABA";

    /// <inheritdoc />
    public override void Append(ReadOnlySpan<char> digits)
    {
        for (int i = 0; i < digits.Length; i++)
        {
            char ch = digits[i];
            if ((uint)(ch - '0') > 9u)
                ThrowHelper.ThrowIfNotAsciiDecimalDigit(ch, nameof(digits));

            this._digits.Add(ch - '0');
        }
    }

    /// <inheritdoc />
    public override void Reset() =>
        _digits.Clear();

    /// <inheritdoc />
    public override char GetCurrentCheckDigit()
    {
        ReadOnlySpan<int> weights = new int[] { 7, 3, 1 };
        int count = _digits.Count;
        int sum = 0;
        for (int i = count - 1, j = 0; i >= 0; i--, j++)
            sum += _digits[i] * weights[j % 3];

        return (char)('0' + ((10 - (sum % 10)) % 10));
    }

    /// <summary>
    /// Computes the ABA routing-number check digit for the supplied body of decimal digits without allocating a
    /// streaming instance.
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
        WeightedMod10.ComputeAba(digits);

    /// <summary>
    /// Determines whether the supplied sequence, comprising an eight-digit body followed by a trailing ABA
    /// check digit, is consistent.
    /// </summary>
    /// <param name="digitsIncludingCheck">The complete sequence including the trailing check digit.</param>
    /// <returns>
    /// <see langword="true" /> if the sequence is exactly <see cref="SequenceLength" /> digits and evaluates as
    /// valid under the ABA scheme; otherwise, <see langword="false" /> — including the case where the length is
    /// wrong or a non-digit character is present.
    /// </returns>
    public static bool IsValid(ReadOnlySpan<char> digitsIncludingCheck)
    {
        if (digitsIncludingCheck.IsEmpty) return true;
        if (digitsIncludingCheck.Length != SequenceLength) return false;
        return WeightedMod10.IsValidAba(digitsIncludingCheck);
    }
}
