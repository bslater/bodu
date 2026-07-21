// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Gumm.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Hashing.CheckDigits;

/// <summary>
/// Computes the check digit of a decimal string using the <c>Gumm</c> algorithm. This class cannot be inherited.
/// </summary>
/// <remarks>
/// <para>
/// The Gumm algorithm was presented by H. Peter Gumm in 1985 (<i>A New Class of Check-Digit Methods for Arbitrary
/// Number Systems</i>, IEEE Transactions on Information Theory, 31(1), 102–105). Like the Verhoeff scheme it was
/// discovered independently of, it detects <b>all</b> single-digit substitution errors and <b>all</b> transpositions of
/// adjacent digits — including the <c>aa</c> ↔ <c>bb</c> twin cases — using a single decimal check digit.
/// </para>
/// <para>
/// The method operates in the dihedral group <i>D</i><sub>5</sub> (the symmetries of a regular pentagon, of order 10).
/// Each digit position carries a permutation of the group; Gumm's contribution over Verhoeff is that these alternate
/// between the identity and a single transform <c>T</c> rather than requiring position-dependent powers of a
/// permutation, which is simpler both conceptually and to implement.
/// </para>
/// <para>
/// This implementation instantiates Gumm's construction over <i>D</i><sub>5</sub> with the transform
/// <c>T(e, x) = (e, e(2 − x) + 1)</c> and applies the identity permutation at even positions (counted from the right,
/// so the trailing check digit) and <c>T</c> at odd positions. Both transform parameters are nonzero modulo 5, which is
/// the condition under which Gumm's anti-symmetry lemma holds and every adjacent transposition is detected. A sequence
/// (body followed by its check digit) is valid if and only if the accumulated group element is the identity.
/// </para>
/// <para>
/// <b>Worked example.</b> For the body <c>"236"</c>, the computed check digit is <c>'9'</c>, and the resulting sequence
/// <c>"2369"</c> is therefore valid under this instantiation.
/// </para>
/// <note type="important">This algorithm is <b>not</b> cryptographically secure and should <b>not</b> be used for
/// password hashing, digital signatures, or integrity validation in security-sensitive applications.</note>
/// </remarks>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// // Single-call computation against an in-memory body.
/// char check = Gumm.Compute("236");   // '9'
///
/// // Full-sequence validation.
/// bool ok = Gumm.IsValid("2369");     // true
///
/// // Streaming use when the body is built up incrementally.
/// var algo = new Gumm();
/// algo.Append("236");
/// char d = algo.GetCurrentCheckDigit();   // '9'
///]]>
/// </code>
/// </example>
public sealed partial class Gumm
    : CheckDigitAlgorithm
{
    /// <summary>The two parallel body-product accumulators that let the streaming <see cref="Append" /> surface compute the check digit without buffering the appended digits.</summary>
    /// <remarks>
    /// <c>_c[k]</c> holds the body product under the hypothesis that the most recently appended digit sits at
    /// right-index <c>k</c>, where right-index 0 is the rightmost body position (an odd absolute position, so transform
    /// <c>T</c> applies) and right-index 1 is the next position out (even, identity). Appending a digit demotes every
    /// earlier digit by one right-index, which flips its identity/<c>T</c> parity; the append recurrence threads that
    /// shift through the period-two index space.
    /// </remarks>
    private readonly byte[] _c = new byte[2];

    /// <summary>
    /// Initializes a new instance of the <see cref="Gumm" /> class.
    /// </summary>
    public Gumm()
    {
    }

    /// <inheritdoc />
    public override string AlgorithmName => "Gumm";

    /// <summary>
    /// Computes the Gumm check digit for the supplied body of decimal digits without allocating a streaming instance.
    /// </summary>
    /// <param name="body">The body characters. Each must be an ASCII decimal digit (<c>'0'</c> to <c>'9'</c>).</param>
    /// <returns>The check digit as an ASCII character in the range <c>'0'</c> to <c>'9'</c>.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="body" /> contains any character outside the range <c>'0'</c> to <c>'9'</c>.
    /// </exception>
    public static char Compute(ReadOnlySpan<char> body)
    {
        byte product = 0;

        // The body digit at string index i occupies absolute position (length - i) from the right, so the leftmost
        // digit carries the highest position. Folding left to right multiplies the highest-position factor first.
        for (int i = 0; i < body.Length; i++)
        {
            char ch = body[i];
            if ((uint)(ch - '0') > 9u)
                ThrowHelper.ThrowIfNotAsciiDecimalDigit(ch, nameof(body));

            product = s_d[product, Permute(body.Length - i, ch - '0')];
        }

        return (char)('0' + s_inv[product]);
    }

    /// <summary>
    /// Determines whether the supplied sequence, comprising a body followed by a trailing Gumm check digit, is valid —
    /// that is, whether the accumulated group element evaluates to the identity.
    /// </summary>
    /// <param name="digitsIncludingCheck">The complete sequence including the trailing check digit.</param>
    /// <returns>
    /// <see langword="true" /> if the sequence evaluates as valid under Gumm; otherwise, <see langword="false" /> —
    /// including the case where <paramref name="digitsIncludingCheck" /> is empty or contains a character outside the
    /// range <c>'0'</c> to <c>'9'</c>.
    /// </returns>
    public static bool IsValid(ReadOnlySpan<char> digitsIncludingCheck)
    {
        if (digitsIncludingCheck.IsEmpty) return false;

        byte product = 0;
        for (int i = 0; i < digitsIncludingCheck.Length; i++)
        {
            char ch = digitsIncludingCheck[i];
            if ((uint)(ch - '0') > 9u) return false;

            product = s_d[product, Permute(digitsIncludingCheck.Length - 1 - i, ch - '0')];
        }

        return product == 0;
    }

    /// <inheritdoc />
    public override void Append(ReadOnlySpan<char> body)
    {
        byte c0 = _c[0];
        byte c1 = _c[1];
        for (int i = 0; i < body.Length; i++)
        {
            char ch = body[i];
            if ((uint)(ch - '0') > 9u)
                ThrowHelper.ThrowIfNotAsciiDecimalDigit(ch, nameof(body));

            int v = ch - '0';

            // The new digit becomes the rightmost body factor. Under the hypothesis that it sits at right-index 0
            // (odd position, transform T) the earlier body are demoted to the index-1 product; under right-index 1
            // (even position, identity) they are demoted to the index-0 product.
            byte n0 = s_d[c1, s_t[v]];
            byte n1 = s_d[c0, v];
            c0 = n0;
            c1 = n1;
        }

        _c[0] = c0;
        _c[1] = c1;
    }

    /// <inheritdoc />
    public override char GetCurrentCheckDigit() =>
        (char)('0' + s_inv[_c[0]]);

    /// <inheritdoc />
    public override void Reset() =>
        Array.Clear(_c);

    /// <summary>
    /// Maps a digit value to its permuted group element for the given absolute position from the right: the identity at
    /// even positions and the transform <c>T</c> at odd positions.
    /// </summary>
    /// <param name="position">
    /// The absolute position from the right, where the trailing check digit is position 0.
    /// </param>
    /// <param name="value">The digit value in the range 0 to 9.</param>
    /// <returns>The group element index in the range 0 to 9.</returns>
    private static byte Permute(int position, int value) =>
        (position & 1) == 1 ? s_t[value] : (byte)value;
}
