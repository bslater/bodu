// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Crockford32.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;


namespace Bodu.IO.Hashing.CheckDigits;

/// <summary>
/// Computes the check symbol of a Crockford Base32 encoded value using the <c>modulo 37</c> scheme defined by Douglas
/// Crockford. This class cannot be inherited.
/// </summary>
/// <remarks>
/// <para>
/// Crockford's Base32 specification defines an optional trailing check symbol equal to the encoded integer value taken
/// modulo 37. The body is decoded as a Base32 number using the thirty-two-symbol Crockford alphabet (<c>'0'</c>–
/// <c>'9'</c> and <c>'A'</c>–<c>'Z'</c> excluding <c>'I'</c>, <c>'L'</c>, <c>'O'</c>, and <c>'U'</c>); decoding is
/// case-insensitive and treats <c>'I'</c>/<c>'L'</c> as <c>1</c> and <c>'O'</c> as <c>0</c>. The running value is
/// reduced modulo 37 by Horner's method, so arbitrarily long inputs are processed without arbitrary-precision
/// arithmetic.
/// </para>
/// <para>
/// Because the modulus 37 is prime and exceeds the alphabet size, five additional check-only symbols represent the
/// values 32 through 36: <c>'*'</c>, <c>'~'</c>, <c>'$'</c>, <c>'='</c>, and <c>'U'</c>. This is the check scheme
/// commonly paired with ULID and other Crockford Base32 identifiers.
/// </para>
/// <para>
/// <b>Worked example.</b> The body <c>"16J"</c> decodes to 1234; 1234 mod 37 is 13, which maps to <c>'D'</c>. The
/// resulting string <c>"16JD"</c> is therefore valid.
/// </para>
/// <note type="important">This algorithm is <b>not</b> cryptographically secure and should <b>not</b> be used for
/// password hashing, digital signatures, or integrity validation in security-sensitive applications.</note>
/// </remarks>
/// <example>
///<![CDATA[
/// // Single-call computation against an in-memory body.
/// char check = Crockford32.Compute("16J");   // 'D'
///
/// // Full-sequence validation.
/// bool ok = Crockford32.IsValid("16JD");      // true
///
/// // Streaming use when the body is built up incrementally.
/// var algo = new Crockford32();
/// algo.Append("16J");
/// char d = algo.GetCurrentCheckDigit();       // 'D'
///]]>
/// </example>
public sealed class Crockford32
    : AlphanumericCheckDigitAlgorithm
{
    /// <summary>
    /// The Crockford Base32 check alphabet, indexed by check value (<c>0</c> to <c>36</c>). The first thirty-two
    /// entries are the encoding symbols; the last five are the check-only symbols.
    /// </summary>
    private const string CheckSymbols = "0123456789ABCDEFGHJKMNPQRSTVWXYZ*~$=U";

    /// <summary>
    /// The radix of the Crockford Base32 encoding.
    /// </summary>
    private const int Radix = 32;

    /// <summary>
    /// The modulus applied to the decoded value.
    /// </summary>
    private const int Modulus = 37;

    private int _value;

    /// <summary>
    /// Initializes a new instance of the <see cref="Crockford32" /> class.
    /// </summary>
    public Crockford32()
    {
    }

    /// <inheritdoc />
    public override string AlgorithmName => "Crockford Base32";

    /// <inheritdoc />
    public override CheckDigitInputAlphabet InputAlphabet => CheckDigitInputAlphabet.CrockfordBase32;

    /// <inheritdoc />
    public override CheckDigitOutputAlphabet OutputAlphabet => CheckDigitOutputAlphabet.CrockfordBase32Check;

    /// <summary>
    /// Computes the Crockford Base32 check symbol for the supplied body without allocating a streaming instance.
    /// </summary>
    /// <param name="body">The body characters. Each must belong to the Crockford Base32 alphabet.</param>
    /// <returns>The check symbol drawn from the Crockford Base32 check alphabet.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="body" /> contains any character outside the Crockford Base32 alphabet.
    /// </exception>
    public static char Compute(ReadOnlySpan<char> body)
    {
        var value = 0;
        for (var i = 0; i < body.Length; i++)
            value = (value * Radix + DecodeBody(body[i], nameof(body))) % Modulus;

        return CheckSymbols[value];
    }

    /// <summary>
    /// Determines whether the supplied sequence, comprising a body followed by a trailing Crockford Base32 check
    /// symbol, is consistent.
    /// </summary>
    /// <param name="valueIncludingCheck">The complete sequence including the trailing check symbol.</param>
    /// <returns>
    /// <see langword="true" /> if the sequence evaluates as valid under the Crockford Base32 modulo-37 scheme;
    /// otherwise, <see langword="false" /> — including the case where <paramref name="valueIncludingCheck" /> is empty,
    /// contains a body character outside the Crockford Base32 alphabet, or ends with an unrecognized check symbol.
    /// </returns>
    public static bool IsValid(ReadOnlySpan<char> valueIncludingCheck)
    {
        if (valueIncludingCheck.IsEmpty) return false;

        var value = 0;
        for (var i = 0; i < valueIncludingCheck.Length - 1; i++)
        {
            var digit = TryDecodeBody(valueIncludingCheck[i]);
            if (digit < 0) return false;

            value = (value * Radix + digit) % Modulus;
        }

        var check = TryDecodeCheck(valueIncludingCheck[^1]);
        if (check < 0) return false;

        return check == value;
    }

    /// <inheritdoc />
    public override void Append(ReadOnlySpan<char> body)
    {
        var value = _value;
        for (var i = 0; i < body.Length; i++)
            value = (value * Radix + DecodeBody(body[i], nameof(body))) % Modulus;

        _value = value;
    }

    /// <inheritdoc />
    public override char GetCurrentCheckDigit() =>
        CheckSymbols[_value];

    /// <inheritdoc />
    public override void Reset() =>
        _value = 0;

    /// <summary>
    /// Decodes a Crockford Base32 body character to its value, or <c>-1</c> when it is outside the alphabet.
    /// </summary>
    /// <param name="ch">The character to decode. Decoding is case-insensitive.</param>
    /// <returns>
    /// The value in the range 0 to 31, or <c>-1</c> when <paramref name="ch" /> is not a body symbol.
    /// </returns>
    private static int TryDecodeBody(char ch)
    {
        if ((uint)(ch - '0') <= 9u) return ch - '0';

        var u = char.ToUpperInvariant(ch);
        return u switch
        {
            'O' => 0,
            'I' or 'L' => 1,
            >= 'A' and <= 'H' => u - 'A' + 10,
            'J' => 18,
            'K' => 19,
            'M' => 20,
            'N' => 21,
            'P' => 22,
            'Q' => 23,
            'R' => 24,
            'S' => 25,
            'T' => 26,
            'V' => 27,
            'W' => 28,
            'X' => 29,
            'Y' => 30,
            'Z' => 31,
            _ => -1,
        };
    }

    /// <summary>
    /// Decodes a Crockford Base32 check symbol to its value, accepting the body symbols (0 to 31) plus the five
    /// check-only symbols (32 to 36), or <c>-1</c> when it is unrecognized.
    /// </summary>
    /// <param name="ch">The character to decode. Decoding is case-insensitive.</param>
    /// <returns>
    /// The value in the range 0 to 36, or <c>-1</c> when <paramref name="ch" /> is not a check symbol.
    /// </returns>
    private static int TryDecodeCheck(char ch)
    {
        var value = TryDecodeBody(ch);
        if (value >= 0) return value;

        return char.ToUpperInvariant(ch) switch
        {
            '*' => 32,
            '~' => 33,
            '$' => 34,
            '=' => 35,
            'U' => 36,
            _ => -1,
        };
    }

    /// <summary>
    /// Decodes a Crockford Base32 body character to its value, throwing when it is outside the alphabet.
    /// </summary>
    /// <param name="ch">The character to decode.</param>
    /// <param name="paramName">The parameter name reported in the exception.</param>
    /// <returns>The value in the range 0 to 31.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="ch" /> is not a body symbol.
    /// </exception>
    private static int DecodeBody(char ch, string paramName)
    {
        var value = TryDecodeBody(ch);
        if (value < 0)
            throw new ArgumentOutOfRangeException(
                paramName,
                ch,
                string.Format(
                    CultureInfo.CurrentCulture,
                    HashingResourceStrings.Arg_OutOfRange_InvalidCharacterForCharacterSet,
                    ch,
                    (int)ch,
                    "Crockford Base32",
                    "'0'-'9' or 'A'-'Z' excluding 'U' (case-insensitive; 'I'/'L' = 1, 'O' = 0)"));

        return value;
    }
}
