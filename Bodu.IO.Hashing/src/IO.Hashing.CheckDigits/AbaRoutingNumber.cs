// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AbaRoutingNumber.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Hashing.CheckDigits;

/// <summary>
/// Computes the check digit of a 9-digit American Bankers Association (ABA) routing transit number using the weighted
/// modulus-10 scheme specified by the US Federal Reserve. This class cannot be inherited.
/// </summary>
/// <remarks>
/// <para>
/// The ABA scheme applies the cyclic weight pattern <c>{3, 7, 1}</c> to the first eight digits (weights
/// <c>3, 7, 1, 3, 7, 1, 3, 7</c> from left to right). The check digit is chosen so that the full nine-digit sequence
/// yields a weighted sum that is a multiple of ten.
/// </para>
/// <para>
/// The static helpers on this type enforce a strict 8-digit body length (9-digit full sequence) to match the ABA
/// specification; the streaming surface is length-agnostic to permit chunked input.
/// </para>
/// <para>
/// <b>Worked example.</b> For the body <c>"01100001"</c> — the leading 8 digits of the Federal Reserve Bank of Boston's
/// routing number — the computed check digit is <c>'5'</c>, and the resulting sequence <c>"011000015"</c> is therefore
/// valid.
/// </para>
/// <note type="important">This algorithm is <b>not</b> cryptographically secure and should <b>not</b> be used for
/// password hashing, digital signatures, or integrity validation in security-sensitive applications.</note>
/// </remarks>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// // Single-call computation against the 8-digit body.
/// char check = AbaRoutingNumber.Compute("01100001");   // '5'
///
/// // Full-sequence validation.
/// bool ok = AbaRoutingNumber.IsValid("011000015");     // true
///
/// // Streaming use when the body is built up incrementally.
/// var algo = new AbaRoutingNumber();
/// algo.Append("01100001");
/// char d = algo.GetCurrentCheckDigit();                // '5'
///]]>
/// </code>
/// </example>
public sealed class AbaRoutingNumber
    : CheckDigitAlgorithm
{
    /// <summary>The required body length of <c>8</c> decimal digits.</summary>
    public const int BodyLength = 8;

    /// <summary>The required full-sequence length of <c>9</c> decimal digits.</summary>
    public const int SequenceLength = 9;

    /// <summary>The running list of appended body digit values, retained so the streaming surface can recompute the weighted sum on demand.</summary>
    /// <remarks>
    /// The ABA weights (<c>3</c>, <c>7</c>, <c>1</c>, repeating, applied from the left of the body toward the right)
    /// have a cycle period of three, so the dual-hypothesis trick used by the ISBN-13 family does not apply: a
    /// streaming consumer cannot weigh incoming digits until the final body length is known. To preserve the streaming
    /// contract (<see cref="Append" /> / <see cref="GetCurrentCheckDigit" /> at any point), the implementation buffers
    /// the digit values (at most eight) and recomputes the weighted sum on each <see cref="GetCurrentCheckDigit" />
    /// call, which is acceptably cheap given the tiny fixed body length.
    /// </remarks>
    private readonly List<int> _digits = new(BodyLength);

    /// <summary>
    /// Initializes a new instance of the <see cref="AbaRoutingNumber" /> class.
    /// </summary>
    public AbaRoutingNumber()
    {
    }

    /// <inheritdoc />
    public override string AlgorithmName => "ABA";

    /// <summary>
    /// Computes the ABA routing-number check digit for the supplied body of decimal digits without allocating a
    /// streaming instance.
    /// </summary>
    /// <param name="body">The body characters. Each must be an ASCII decimal digit (<c>'0'</c> to <c>'9'</c>).</param>
    /// <returns>The check digit as an ASCII character in the range <c>'0'</c> to <c>'9'</c>.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="body" /> contains any character outside the range <c>'0'</c> to <c>'9'</c>.
    /// </exception>
    /// <remarks>
    /// This helper is length-tolerant to support streaming and partial-body use;
    /// <see cref="IsValid(ReadOnlySpan{char})" /> enforces the strict <see cref="SequenceLength" /> domain contract for
    /// full validation.
    /// </remarks>
    public static char Compute(ReadOnlySpan<char> body) =>
        WeightedMod10.ComputeAba(body);

    /// <summary>
    /// Determines whether the supplied sequence, comprising an eight-digit body followed by a trailing ABA check digit,
    /// is consistent.
    /// </summary>
    /// <param name="digitsIncludingCheck">The complete sequence including the trailing check digit.</param>
    /// <returns>
    /// <see langword="true" /> if the sequence is exactly <see cref="SequenceLength" /> digits and evaluates as valid
    /// under the ABA scheme; otherwise, <see langword="false" /> — including the case where
    /// <paramref name="digitsIncludingCheck" /> is empty, the length is wrong, or a non-digit character is present.
    /// </returns>
    public static bool IsValid(ReadOnlySpan<char> digitsIncludingCheck) =>
        digitsIncludingCheck.Length == SequenceLength && WeightedMod10.IsValidAba(digitsIncludingCheck);

    /// <inheritdoc />
    public override void Append(ReadOnlySpan<char> body)
    {
        for (int i = 0; i < body.Length; i++)
        {
            char ch = body[i];
            if ((uint)(ch - '0') > 9u)
                ThrowHelper.ThrowIfNotAsciiDecimalDigit(ch, nameof(body));

            _digits.Add(ch - '0');
        }
    }

    /// <inheritdoc />
    public override char GetCurrentCheckDigit()
    {
        ReadOnlySpan<int> weights = [7, 3, 1];
        int count = _digits.Count;
        int sum = 0;
        for (int i = count - 1, j = 0; i >= 0; i--, j++)
            sum += _digits[i] * weights[j % 3];

        return (char)('0' + ((10 - (sum % 10)) % 10));
    }

    /// <inheritdoc />
    public override void Reset() =>
        _digits.Clear();
}
