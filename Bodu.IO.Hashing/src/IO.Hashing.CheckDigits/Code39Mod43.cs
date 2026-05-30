// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Code39Mod43.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;

using Bodu.IO.Hashing.Checksums;

namespace Bodu.IO.Hashing.CheckDigits;

/// <summary>
/// Computes the check character of a Code 39 (also known as Code 3 of 9) symbol string using the
/// <c>modulo 43</c> scheme. This class cannot be inherited.
/// </summary>
/// <remarks>
/// <para>
/// The Code 39 modulo-43 check character is the optional self-checking symbol defined by the Code 39 barcode
/// symbology. Each body character is mapped to its ordinal value in the forty-three-symbol Code 39 alphabet —
/// <c>'0'</c>–<c>'9'</c> as 0–9, <c>'A'</c>–<c>'Z'</c> as 10–35, and the seven symbols <c>'-'</c>, <c>'.'</c>,
/// space, <c>'$'</c>, <c>'/'</c>, <c>'+'</c>, and <c>'%'</c> as 36–42. The values are summed, reduced modulo 43,
/// and the result is mapped back to the alphabet to produce a single trailing check character.
/// </para>
/// <para>
/// The scheme detects all single-character substitution errors. Because the underlying combiner is a commutative
/// sum, it does <b>not</b> detect every adjacent transposition; payloads that require transposition coverage
/// should prefer <see cref="Verhoeff" /> or <see cref="Damm" /> over a decimal body.
/// </para>
/// <para>
/// <b>Worked example.</b> For the body <c>"CODE39"</c> the character values are 12, 24, 13, 14, 3, and 9, summing
/// to 75; 75 mod 43 is 32, which maps to <c>'W'</c>. The resulting string <c>"CODE39W"</c> is therefore valid.
/// </para>
/// <note type="important">This algorithm is <b>not</b> cryptographically secure and should <b>not</b> be used for
/// password hashing, digital signatures, or integrity validation in security-sensitive applications.</note>
/// </remarks>
/// <example>
///<![CDATA[
/// Single-call computation against an in-memory body.
/// char check = Code39Mod43.Compute("CODE39");   // 'W'
///
/// Full-sequence validation.
/// bool ok = Code39Mod43.IsValid("CODE39W");      // true
///
/// Streaming use when the body is built up incrementally.
/// var algo = new Code39Mod43();
/// algo.Append("CODE39");
/// char d = algo.GetCurrentCheckDigit();          // 'W'
///]]>
/// </example>
public sealed class Code39Mod43
    : AlphanumericCheckDigitAlgorithm
{
    /// <summary>
    /// The Code 39 alphabet, indexed by character value (<c>0</c> to <c>42</c>).
    /// </summary>
    private const string Alphabet = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ-. $/+%";

    /// <summary>
    /// The modulus applied to the running character-value sum.
    /// </summary>
    private const int Modulus = 43;

    private int _sum;

    /// <summary>
    /// Initializes a new instance of the <see cref="Code39Mod43" /> class.
    /// </summary>
    public Code39Mod43()
    {
    }

    /// <inheritdoc />
    public override string AlgorithmName => "Code 39 Mod 43";

    /// <inheritdoc />
    public override CheckDigitInputAlphabet InputAlphabet => CheckDigitInputAlphabet.Code39;

    /// <inheritdoc />
    public override CheckDigitOutputAlphabet OutputAlphabet => CheckDigitOutputAlphabet.Code39;

    /// <summary>
    /// Computes the Code 39 modulo-43 check character for the supplied body without allocating a streaming instance.
    /// </summary>
    /// <param name="body">The body characters. Each must belong to the Code 39 alphabet.</param>
    /// <returns>The check character drawn from the Code 39 alphabet.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="body" /> contains any character outside the Code 39 alphabet.
    /// </exception>
    public static char Compute(ReadOnlySpan<char> body)
    {
        var sum = 0;
        for (var i = 0; i < body.Length; i++)
            sum += ValueOf(body[i], nameof(body));

        return Alphabet[sum % Modulus];
    }

    /// <summary>
    /// Determines whether the supplied sequence, comprising a body followed by a trailing Code 39 check character,
    /// is consistent.
    /// </summary>
    /// <param name="valueIncludingCheck">The complete sequence including the trailing check character.</param>
    /// <returns>
    /// <see langword="true" /> if the sequence evaluates as valid under the Code 39 modulo-43 scheme; otherwise,
    /// <see langword="false" /> — including the case where <paramref name="valueIncludingCheck" /> is empty or
    /// contains any character outside the Code 39 alphabet.
    /// </returns>
    public static bool IsValid(ReadOnlySpan<char> valueIncludingCheck)
    {
        if (valueIncludingCheck.IsEmpty) return false;

        var sum = 0;
        for (var i = 0; i < valueIncludingCheck.Length - 1; i++)
        {
            var value = TryValueOf(valueIncludingCheck[i]);
            if (value < 0) return false;

            sum += value;
        }

        var check = TryValueOf(valueIncludingCheck[^1]);
        if (check < 0) return false;

        return check == sum % Modulus;
    }

    /// <inheritdoc />
    public override void Append(ReadOnlySpan<char> body)
    {
        var sum = _sum;
        for (var i = 0; i < body.Length; i++)
            sum = (sum + ValueOf(body[i], nameof(body))) % Modulus;

        _sum = sum;
    }

    /// <inheritdoc />
    public override char GetCurrentCheckDigit() =>
        Alphabet[_sum];

    /// <inheritdoc />
    public override void Reset() =>
        _sum = 0;

    /// <summary>
    /// Maps a Code 39 character to its ordinal value, or <c>-1</c> when the character is outside the alphabet.
    /// </summary>
    /// <param name="ch">The character to map.</param>
    /// <returns>The value in the range 0 to 42, or <c>-1</c> when <paramref name="ch" /> is not a Code 39 character.</returns>
    private static int TryValueOf(char ch)
    {
        if ((uint)(ch - '0') <= 9u) return ch - '0';
        if ((uint)(ch - 'A') <= 25u) return ch - 'A' + 10;

        return ch switch
        {
            '-' => 36,
            '.' => 37,
            ' ' => 38,
            '$' => 39,
            '/' => 40,
            '+' => 41,
            '%' => 42,
            _ => -1,
        };
    }

    /// <summary>
    /// Maps a Code 39 character to its ordinal value, throwing when the character is outside the alphabet.
    /// </summary>
    /// <param name="ch">The character to map.</param>
    /// <param name="paramName">The parameter name reported in the exception.</param>
    /// <returns>The value in the range 0 to 42.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="ch" /> is not a Code 39 character.</exception>
    private static int ValueOf(char ch, string paramName)
    {
        var value = TryValueOf(ch);
        if (value < 0)
            throw new ArgumentOutOfRangeException(
                paramName,
                ch,
                string.Format(
                    CultureInfo.InvariantCulture,
                    HashingResourceStrings.Arg_OutOfRange_InvalidCharacterForCharacterSet,
                    ch,
                    (int)ch,
                    "Code 39",
                    "'0'-'9', 'A'-'Z', '-', '.', space, '$', '/', '+', or '%'"));

        return value;
    }
}
