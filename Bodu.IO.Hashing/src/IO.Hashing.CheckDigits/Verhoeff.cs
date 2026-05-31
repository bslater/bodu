// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Verhoeff.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Hashing.CheckDigits;

/// <summary>
/// Computes the check digit of a decimal string using the <c>Verhoeff</c> algorithm. This class cannot be inherited.
/// </summary>
/// <remarks>
/// <para>
/// The Verhoeff algorithm, published by the Dutch mathematician Jacobus Verhoeff in 1969, was the first decimal-digit
/// check-sum capable of detecting all single-digit substitution errors <i>and</i> all adjacent-digit transpositions. It
/// achieves this by combining a position-dependent permutation with multiplication in the dihedral group <i>D</i><sub>5
/// </sub>, the group of symmetries of a regular pentagon.
/// </para>
/// <para>
/// In addition to single-digit and adjacent-transposition errors, Verhoeff also catches the common <i>twin</i> errors (
/// <c>aa → bb</c>) and a significant proportion of other mutation classes.
/// </para>
/// <para>
/// <b>Worked example.</b> For the body <c>"236"</c>, the computed check digit is <c>'3'</c>, and the resulting sequence
/// <c>"2363"</c> is therefore valid under Verhoeff.
/// </para>
/// <note type="important">This algorithm is <b>not</b> cryptographically secure and should <b>not</b> be used for
/// password hashing, digital signatures, or integrity validation in security-sensitive applications.</note>
/// </remarks>
/// <example>
///<![CDATA[
/// Single-call computation against an in-memory body.
/// char check = Verhoeff.Compute("236");   // '3'
///
/// Full-sequence validation.
/// bool ok = Verhoeff.IsValid("2363");     // true
///
/// Streaming use when the body is built up incrementally.
/// var algo = new Verhoeff();
/// algo.Append("236");
/// char d = algo.GetCurrentCheckDigit();   // '3'
///]]>
/// </example>
public sealed partial class Verhoeff
    : CheckDigitAlgorithm
{
    // Eight parallel accumulators allow the streaming Append surface to compute the check digit without
    // buffering digits. _c[k] holds the Verhoeff running value 'c' under the hypothesis that the most
    // recently appended digit occupies right-index k in the final sequence. Update: on each append of v,
    //   newC[k] = d[p[k, v], _c[(k + 1) & 7]]
    // because shifting in a new most-recent digit demotes every existing digit by one right-index. The
    // previous state under hypothesis (k+1) is the product of the older digits' permutation values, and
    // the new digit's permutation value p[k, v] is left-multiplied onto it — matching the left-to-right
    // accumulation of the static rtl walk c_{i+1} = d[c_i, p[j, digit]]. D5 is non-abelian, so operand
    // order is load-bearing.
    private readonly byte[] _c = new byte[8];

    /// <summary>
    /// Initializes a new instance of the <see cref="Verhoeff" /> class.
    /// </summary>
    public Verhoeff()
    {
    }

    /// <inheritdoc />
    public override string AlgorithmName => "Verhoeff";

    /// <summary>
    /// Computes the Verhoeff check digit for the supplied body of decimal digits without allocating a streaming
    /// instance.
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
        byte c = 0;
        for (int i = digits.Length - 1, j = 1; i >= 0; i--, j++)
        {
            var ch = digits[i];
            if ((uint)(ch - '0') > 9u)
                ThrowHelper.ThrowIfNotAsciiDecimalDigit(ch, nameof(digits));

            c = s_d[c, s_p[j & 7, ch - '0']];
        }

        return (char)('0' + s_inv[c]);
    }

    /// <summary>
    /// Determines whether the supplied sequence, comprising a body followed by a trailing Verhoeff check digit, is
    /// valid — that is, whether the final running value evaluates to zero.
    /// </summary>
    /// <param name="digitsIncludingCheck">The complete sequence including the trailing check digit.</param>
    /// <returns>
    /// <see langword="true" /> if the sequence evaluates as valid under Verhoeff; otherwise,
    /// <see langword="false" /> — including the case where <paramref name="digitsIncludingCheck" /> is empty or
    /// contains a character outside the range <c>'0'</c> to <c>'9'</c>.
    /// </returns>
    public static bool IsValid(ReadOnlySpan<char> digitsIncludingCheck)
    {
        if (digitsIncludingCheck.IsEmpty) return false;

        byte c = 0;
        for (int i = digitsIncludingCheck.Length - 1, j = 0; i >= 0; i--, j++)
        {
            var ch = digitsIncludingCheck[i];
            if ((uint)(ch - '0') > 9u) return false;

            c = s_d[c, s_p[j & 7, ch - '0']];
        }

        return c == 0;
    }

    /// <inheritdoc />
    public override void Append(ReadOnlySpan<char> digits)
    {
        Span<byte> next = stackalloc byte[8];
        for (var i = 0; i < digits.Length; i++)
        {
            var ch = digits[i];
            if ((uint)(ch - '0') > 9u)
                ThrowHelper.ThrowIfNotAsciiDecimalDigit(ch, nameof(digits));

            var v = ch - '0';
            for (var k = 0; k < 8; k++)
                next[k] = s_d[s_p[k, v], _c[(k + 1) & 7]];

            next.CopyTo(_c);
        }
    }

    /// <inheritdoc />
    public override char GetCurrentCheckDigit() =>
        (char)('0' + s_inv[_c[1]]);

    /// <inheritdoc />
    public override void Reset() =>
        Array.Clear(_c);
}
