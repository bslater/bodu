// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Damm.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Hashing.CheckDigits;

/// <summary>
/// Computes the check digit of a decimal string using the <c>Damm</c> algorithm. This class cannot be inherited.
/// </summary>
/// <remarks>
/// <para>
/// The Damm algorithm was presented by H. Michael Damm in 2004. It evaluates a running <i>interim</i> digit by
/// repeatedly indexing into a carefully chosen 10×10 <i>totally antisymmetric</i> quasigroup — a table with no fixed
/// points on its diagonal and no partial idempotent entries. The final interim is the check digit.
/// </para>
/// <para>
/// Damm detects <b>all</b> single-digit substitution errors and <b>all</b> adjacent-digit transpositions — the latter
/// without the <c>09 ↔ 90</c> exception that Luhn suffers from. Validation is especially clean: a sequence (body
/// followed by its check digit) is valid if and only if its final interim is zero.
/// </para>
/// <para>
/// <b>Worked example.</b> For the body <c>"572"</c>, the computed check digit is <c>'4'</c>, and the resulting sequence
/// <c>"5724"</c> is therefore valid under Damm.
/// </para>
/// <note type="important">This algorithm is <b>not</b> cryptographically secure and should <b>not</b> be used for
/// password hashing, digital signatures, or integrity validation in security-sensitive applications.</note>
/// </remarks>
/// <example>
///<![CDATA[
/// // Single-call computation against an in-memory body.
/// char check = Damm.Compute("572");   // '4'
///
/// // Full-sequence validation — equivalent to checking that the final interim is zero.
/// bool ok = Damm.IsValid("5724");     // true
///
/// // Streaming use when the body is built up incrementally.
/// var algo = new Damm();
/// algo.Append("572");
/// char d = algo.GetCurrentCheckDigit(); // '4'
///]]>
/// </example>
public sealed partial class Damm
    : CheckDigitAlgorithm
{
    private byte _interim;

    /// <summary>
    /// Initializes a new instance of the <see cref="Damm" /> class.
    /// </summary>
    public Damm()
    {
    }

    /// <inheritdoc />
    public override string AlgorithmName => "Damm";

    /// <summary>
    /// Computes the Damm check digit for the supplied body of decimal digits without allocating a streaming instance.
    /// </summary>
    /// <param name="digits">
    /// The body characters. Each must be an ASCII decimal digit (<c>'0'</c> to <c>'9'</c>).
    /// </param>
    /// <returns>The check digit as an ASCII character in the range <c>'0'</c> to <c>'9'</c>.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="digits" /> contains any character outside the range <c>'0'</c> to <c>'9'</c>.
    /// </exception>
    public static char Compute(ReadOnlySpan<char> digits)
    {
        byte interim = 0;
        for (var i = 0; i < digits.Length; i++)
        {
            var ch = digits[i];
            if ((uint)(ch - '0') > 9u)
                ThrowHelper.ThrowIfNotAsciiDecimalDigit(ch, nameof(digits));

            interim = s_table[interim, ch - '0'];
        }

        return (char)('0' + interim);
    }

    /// <summary>
    /// Determines whether the supplied sequence, comprising a body followed by a trailing Damm check digit, is valid —
    /// that is, whether the final interim evaluates to zero.
    /// </summary>
    /// <param name="digitsIncludingCheck">The complete sequence including the trailing check digit.</param>
    /// <returns>
    /// <see langword="true" /> if the sequence evaluates as valid under Damm; otherwise, <see langword="false" /> —
    /// including the case where <paramref name="digitsIncludingCheck" /> is empty or contains a character outside the
    /// range <c>'0'</c> to <c>'9'</c>.
    /// </returns>
    public static bool IsValid(ReadOnlySpan<char> digitsIncludingCheck)
    {
        if (digitsIncludingCheck.IsEmpty) return false;

        byte interim = 0;
        for (var i = 0; i < digitsIncludingCheck.Length; i++)
        {
            var ch = digitsIncludingCheck[i];
            if ((uint)(ch - '0') > 9u) return false;

            interim = s_table[interim, ch - '0'];
        }

        return interim == 0;
    }

    /// <inheritdoc />
    public override void Append(ReadOnlySpan<char> digits)
    {
        var interim = _interim;
        for (var i = 0; i < digits.Length; i++)
        {
            var ch = digits[i];
            if ((uint)(ch - '0') > 9u)
                ThrowHelper.ThrowIfNotAsciiDecimalDigit(ch, nameof(digits));

            interim = s_table[interim, ch - '0'];
        }

        _interim = interim;
    }

    /// <inheritdoc />
    public override char GetCurrentCheckDigit() =>
        (char)('0' + _interim);

    /// <inheritdoc />
    public override void Reset() =>
        _interim = 0;
}
