// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Iso7064Mod97_10.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.IO.Hashing.CheckDigits;

namespace Bodu.IO.Hashing.Checksums;

/// <summary>
/// Computes the 2-character check code of an alphanumeric string using the ISO 7064 <c>MOD 97-10</c> pure algorithm.
/// This class cannot be inherited.
/// </summary>
/// <remarks>
/// <para>
/// <c>MOD 97-10</c> is the pure ISO 7064 system that uses modulus ninety-seven with radix ten. It underpins the
/// International Bank Account Number (IBAN, ISO 13616), the Legal Entity Identifier (LEI, ISO 17442), and the Committee
/// on Uniform Securities Identification Procedures (CUSIP) check computation used for Legal-Entity Identifier issuance.
/// Letters in the body are expanded by value <c>'A'</c>=10 … <c>'Z'</c>=35 before being absorbed.
/// </para>
/// <para>
/// The running remainder <c>r</c> is initialized to zero. Each body character is absorbed as
/// <c>r ← (r·10 + a) mod 97</c> for a decimal digit or <c>r ← (r·100 + a) mod 97</c> for an uppercase letter (whose
/// value is two decimal digits). The two-digit check code is <c>(98 - (r · 100) mod 97) mod 97</c>, formatted as two
/// ASCII decimal digits; the complete sequence <c>body + check</c> is valid when its running remainder, including the
/// check code, equals <c>1</c>.
/// </para>
/// <para>
/// <b>Worked example.</b> For the body <c>"794"</c>, the computed check code is <c>"44"</c>, and the resulting sequence
/// <c>"79444"</c> is therefore valid under MOD 97-10.
/// </para>
/// <note type="important">This algorithm is <b>not</b> cryptographically secure and should <b>not</b> be used for
/// password hashing, digital signatures, or integrity validation in security-sensitive applications.</note>
/// </remarks>
/// <example>
///<![CDATA[
/// // Single-call computation — returns the two-digit check as a string.
/// string check = Iso7064Mod97_10.Compute("794");   // "44"
///
/// // Full-sequence validation.
/// bool ok = Iso7064Mod97_10.IsValid("79444");      // true
///
/// // Streaming use when the body is built up incrementally.
/// var algo = new Iso7064Mod97_10();
/// algo.Append("794");
/// string code = algo.GetCurrentCheckDigits();      // "44"
///]]>
/// </example>
public sealed class Iso7064Mod97_10
    : MultiCharCheckDigitAlgorithm
{
    /// <summary>
    /// The fixed check-code length of <c>2</c> decimal digits.
    /// </summary>
    public const int CheckDigits = 2;

    private int _r;

    /// <summary>
    /// Initializes a new instance of the <see cref="Iso7064Mod97_10" /> class.
    /// </summary>
    public Iso7064Mod97_10()
    {
    }

    /// <inheritdoc />
    public override string AlgorithmName => "ISO 7064 MOD 97-10";

    /// <inheritdoc />
    public override int CheckLength => CheckDigits;

    /// <inheritdoc />
    public override CheckDigitInputAlphabet InputAlphabet => CheckDigitInputAlphabet.AlphanumericUppercase;

    /// <summary>
    /// Computes the ISO 7064 MOD 97-10 check code for the supplied body without allocating a streaming instance.
    /// </summary>
    /// <param name="body">The body characters. Each must be an ASCII decimal digit or uppercase Latin letter.</param>
    /// <returns>The check code as a string of two ASCII decimal digits.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="body" /> contains any character outside the alphanumeric uppercase alphabet.
    /// </exception>
    public static string Compute(ReadOnlySpan<char> body)
    {
        var engine = new Iso7064Mod97_10();
        engine.Append(body);
        return engine.GetCurrentCheckDigits();
    }

    /// <summary>
    /// Determines whether the supplied sequence, comprising a body followed by a two-digit MOD 97-10 check code, is
    /// consistent — that is, whether its running remainder (with letters expanded and the check code absorbed) equals
    /// <c>1</c>.
    /// </summary>
    /// <param name="valueIncludingCheck">The complete sequence including the trailing check code.</param>
    /// <returns>
    /// <see langword="true" /> if the sequence is empty or evaluates as valid under MOD 97-10; otherwise,
    /// <see langword="false" /> — including the case where any character is outside the alphanumeric uppercase
    /// alphabet.
    /// </returns>
    public static bool IsValid(ReadOnlySpan<char> valueIncludingCheck)
    {
        if (valueIncludingCheck.IsEmpty) return true;

        var r = 0;
        for (var i = 0; i < valueIncludingCheck.Length; i++)
        {
            var ch = valueIncludingCheck[i];
            if ((uint)(ch - '0') <= 9u)
            {
                r = ((r * 10) + (ch - '0')) % 97;
            }
            else if ((uint)(ch - 'A') <= 25u)
            {
                r = ((r * 100) + (ch - 'A' + 10)) % 97;
            }
            else
            {
                return false;
            }
        }

        return r == 1;
    }

    /// <inheritdoc />
    public override void Append(ReadOnlySpan<char> body)
    {
        var r = this._r;
        for (var i = 0; i < body.Length; i++)
        {
            var ch = body[i];
            if ((uint)(ch - '0') <= 9u)
            {
                r = ((r * 10) + (ch - '0')) % 97;
            }
            else if ((uint)(ch - 'A') <= 25u)
            {
                r = ((r * 100) + (ch - 'A' + 10)) % 97;
            }
            else
            {
                ThrowHelper.ThrowIfNotAsciiAlphanumericUppercase(ch, nameof(body));
            }
        }

        this._r = r;
    }

    /// <inheritdoc />
    public override int GetCurrentCheckDigits(Span<char> destination)
    {
        HashingThrowHelper.ThrowIfDestinationSpanTooShort(destination.Length, CheckLength, nameof(destination));

        var full = (_r * 100) % 97;
        var check = (98 - full) % 97;
        destination[0] = (char)('0' + (check / 10));
        destination[1] = (char)('0' + (check % 10));
        return CheckLength;
    }

    /// <inheritdoc />
    public override void Reset() =>
        _r = 0;
}
