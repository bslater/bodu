// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Iso7064Mod11_2.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.IO.Hashing.CheckDigits;

namespace Bodu.IO.Hashing.Checksums;

/// <summary>
/// Computes the single-character check of a decimal string using the ISO 7064 <c>MOD 11-2</c> pure algorithm. This
/// class cannot be inherited.
/// </summary>
/// <remarks>
/// <para>
/// <c>MOD 11-2</c> is the pure ISO 7064 system that uses modulus eleven with radix two. It operates on an arbitrary
/// sequence of decimal digits and emits a single check character drawn from the alphabet <c>'0'</c>–<c>'9'</c> plus the
/// sentinel <c>'X'</c> used to represent the value ten.
/// </para>
/// <para>
/// The working digit is initialized to zero, and for each body digit <c>a</c> it is updated as
/// <c>p ← ((p + a) × 2) mod 11</c>. The check character is chosen so that <c>(p + c) mod 11 = 1</c>, i.e.
/// <c>c = (12 - p) mod 11</c>; the sequence <c>body + c</c> is valid when the same identity holds.
/// </para>
/// <para>
/// <b>Worked example.</b> For the body <c>"0794"</c>, the computed check character is <c>'0'</c>, and the resulting
/// sequence <c>"07940"</c> is therefore valid under <c>MOD 11-2</c>.
/// </para>
/// <note type="important">This algorithm is <b>not</b> cryptographically secure and should <b>not</b> be used for
/// password hashing, digital signatures, or integrity validation in security-sensitive applications.</note>
/// </remarks>
/// <example>
///<![CDATA[
/// // Single-call computation against a decimal body.
/// char check = Iso7064Mod11_2.Compute("0794");   // '0'
///
/// // Full-sequence validation.
/// bool ok = Iso7064Mod11_2.IsValid("07940");     // true
///
/// // Streaming use when the body is built up incrementally.
/// var algo = new Iso7064Mod11_2();
/// algo.Append("0794");
/// char d = algo.GetCurrentCheckDigit();          // '0'
///]]>
/// </example>
public sealed class Iso7064Mod11_2
    : AlphanumericCheckDigitAlgorithm
{
    private int _p;

    /// <summary>
    /// Initializes a new instance of the <see cref="Iso7064Mod11_2" /> class.
    /// </summary>
    public Iso7064Mod11_2()
    {
    }

    /// <inheritdoc />
    public override string AlgorithmName => "ISO 7064 MOD 11-2";

    /// <inheritdoc />
    public override CheckDigitInputAlphabet InputAlphabet => CheckDigitInputAlphabet.DecimalDigits;

    /// <inheritdoc />
    public override CheckDigitOutputAlphabet OutputAlphabet => CheckDigitOutputAlphabet.DecimalDigitsOrX;

    /// <summary>
    /// Computes the ISO 7064 MOD 11-2 check character for the supplied body of decimal digits without allocating a
    /// streaming instance.
    /// </summary>
    /// <param name="digits">
    /// The body characters. Each must be an ASCII decimal digit (<c>'0'</c> to <c>'9'</c>).
    /// </param>
    /// <returns>The check character, either an ASCII decimal digit or the sentinel <c>'X'</c>.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="digits" /> contains any character outside the range <c>'0'</c> to <c>'9'</c>.
    /// </exception>
    public static char Compute(ReadOnlySpan<char> digits)
    {
        var p = 0;
        for (var i = 0; i < digits.Length; i++)
        {
            var ch = digits[i];
            if ((uint)(ch - '0') > 9u)
                ThrowHelper.ThrowIfNotAsciiDecimalDigit(ch, nameof(digits));

            p = ((p + (ch - '0')) * 2) % 11;
        }

        var c = (12 - p) % 11;
        return c == 10 ? 'X' : (char)('0' + c);
    }

    /// <summary>
    /// Determines whether the supplied sequence, comprising a body followed by a trailing ISO 7064 MOD 11-2 check
    /// character, is consistent.
    /// </summary>
    /// <param name="valueIncludingCheck">The complete sequence including the trailing check character.</param>
    /// <returns>
    /// <see langword="true" /> if the sequence is empty or evaluates as valid under MOD 11-2; otherwise,
    /// <see langword="false" /> — including the case where any non-final character is not a decimal digit or the final
    /// character is neither a decimal digit nor <c>'X'</c>.
    /// </returns>
    public static bool IsValid(ReadOnlySpan<char> valueIncludingCheck)
    {
        if (valueIncludingCheck.IsEmpty) return true;

        var last = valueIncludingCheck.Length - 1;
        var p = 0;
        for (var i = 0; i < last; i++)
        {
            var ch = valueIncludingCheck[i];
            if ((uint)(ch - '0') > 9u) return false;

            p = ((p + (ch - '0')) * 2) % 11;
        }

        var checkChar = valueIncludingCheck[last];
        int checkValue;
        if ((uint)(checkChar - '0') <= 9u) checkValue = checkChar - '0';
        else if (checkChar == 'X') checkValue = 10;
        else return false;

        return (p + checkValue) % 11 == 1;
    }

    /// <inheritdoc />
    public override void Append(ReadOnlySpan<char> body)
    {
        var p = _p;
        for (var i = 0; i < body.Length; i++)
        {
            var ch = body[i];
            if ((uint)(ch - '0') > 9u)
                ThrowHelper.ThrowIfNotAsciiDecimalDigit(ch, nameof(body));

            p = ((p + (ch - '0')) * 2) % 11;
        }

        _p = p;
    }

    /// <inheritdoc />
    public override char GetCurrentCheckDigit()
    {
        var c = (12 - _p) % 11;
        return c == 10 ? 'X' : (char)('0' + c);
    }

    /// <inheritdoc />
    public override void Reset() =>
        _p = 0;
}
