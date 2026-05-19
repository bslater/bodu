// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Cusip.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.IO.Hashing.CheckDigits;

namespace Bodu.IO.Hashing.Checksums;

/// <summary>
/// Computes the check digit of a 9-character Committee on Uniform Securities Identification Procedures (CUSIP)
/// identifier using the Luhn-style weighted modulus-10 scheme specified by the American Bankers Association. This class
/// cannot be inherited.
/// </summary>
/// <remarks>
/// <para>
/// CUSIP body characters are drawn from the decimal digits (<c>'0'</c>–<c>'9'</c>), uppercase Latin letters (<c>'A'</c>
/// –<c>'Z'</c>, with values 10–35), and the historical sentinels <c>'*'</c>=36, <c>'@'</c>=37 and <c>'#'</c>=38. The
/// body is eight characters; the ninth character is the check digit.
/// </para>
/// <para>
/// Each body character's numeric value is multiplied by a positional weight (<c>1</c> at even left-to-right indices,
/// <c>2</c> at odd indices). The resulting product is split into its tens and units digits, which are summed before
/// being added to the running total. The check digit is chosen so that the total is a multiple of ten.
/// </para>
/// <para>
/// <b>Worked example.</b> For the body <c>"03783310"</c> — Apple Inc. — the computed check digit is <c>'0'</c>, and the
/// resulting CUSIP <c>"037833100"</c> is therefore valid.
/// </para>
/// <note type="important">This algorithm is <b>not</b> cryptographically secure and should <b>not</b> be used for
/// password hashing, digital signatures, or integrity validation in security-sensitive applications.</note>
/// </remarks>
/// <example>
///<![CDATA[
/// // Apple Inc. — CUSIP body "03783310".
/// char check = Cusip.Compute("03783310");   // '0'
///
/// // Full-sequence validation.
/// bool ok = Cusip.IsValid("037833100");     // true
///
/// // Streaming use when the body is built up incrementally.
/// var algo = new Cusip();
/// algo.Append("03783310");
/// char d = algo.GetCurrentCheckDigit();     // '0'
///]]>
/// </example>
public sealed class Cusip
    : AlphanumericCheckDigitAlgorithm
{
    /// <summary>
    /// The required body length of <c>8</c> characters.
    /// </summary>
    public const int BodyLength = 8;

    /// <summary>
    /// The required full-sequence length of <c>9</c> characters.
    /// </summary>
    public const int SequenceLength = 9;
    private int _count;

    private int _sum;

    /// <summary>
    /// Initializes a new instance of the <see cref="Cusip" /> class.
    /// </summary>
    public Cusip()
    {
    }

    /// <inheritdoc />
    public override string AlgorithmName => "CUSIP";

    /// <inheritdoc />
    public override CheckDigitInputAlphabet InputAlphabet => CheckDigitInputAlphabet.AlphanumericUppercase;

    /// <inheritdoc />
    public override CheckDigitOutputAlphabet OutputAlphabet => CheckDigitOutputAlphabet.DecimalDigits;

    /// <summary>
    /// Computes the CUSIP check digit for the supplied body without allocating a streaming instance.
    /// </summary>
    /// <param name="body">The body characters. Each must be a valid CUSIP character.</param>
    /// <returns>The check digit as an ASCII character in the range <c>'0'</c> to <c>'9'</c>.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="body" /> contains any character outside the CUSIP alphabet.
    /// </exception>
    public static char Compute(ReadOnlySpan<char> body)
    {
        var sum = 0;
        for (var i = 0; i < body.Length; i++)
        {
            var value = Alphanumeric.ExpandCusip(body[i]);
            var weight = (i & 1) == 0 ? 1 : 2;
            var product = value * weight;
            sum += (product / 10) + (product % 10);
        }

        return (char)('0' + ((10 - (sum % 10)) % 10));
    }

    /// <summary>
    /// Determines whether the supplied sequence, comprising an eight-character body followed by a single-digit CUSIP
    /// check, is consistent.
    /// </summary>
    /// <param name="valueIncludingCheck">The complete nine-character CUSIP.</param>
    /// <returns>
    /// <see langword="true" /> if the sequence is empty or evaluates as valid under CUSIP; otherwise,
    /// <see langword="false" /> — including the case where the length is wrong, any body character is outside the CUSIP
    /// alphabet, or the check character is not a decimal digit.
    /// </returns>
    public static bool IsValid(ReadOnlySpan<char> valueIncludingCheck)
    {
        if (valueIncludingCheck.IsEmpty) return true;
        if (valueIncludingCheck.Length != SequenceLength) return false;

        var sum = 0;
        for (var i = 0; i < BodyLength; i++)
        {
            var ch = valueIncludingCheck[i];
            int value;
            if ((uint)(ch - '0') <= 9u) value = ch - '0';
            else if ((uint)(ch - 'A') <= 25u) value = ch - 'A' + 10;
            else if (ch == '*') value = 36;
            else if (ch == '@') value = 37;
            else if (ch == '#') value = 38;
            else return false;

            var weight = (i & 1) == 0 ? 1 : 2;
            var product = value * weight;
            sum += (product / 10) + (product % 10);
        }

        var checkChar = valueIncludingCheck[BodyLength];
        if ((uint)(checkChar - '0') > 9u) return false;
        sum += checkChar - '0';

        return sum % 10 == 0;
    }

    /// <inheritdoc />
    public override void Append(ReadOnlySpan<char> body)
    {
        var sum = _sum;
        var count = _count;

        for (var i = 0; i < body.Length; i++)
        {
            var value = Alphanumeric.ExpandCusip(body[i]);
            var weight = (count & 1) == 0 ? 1 : 2;
            var product = value * weight;
            sum += (product / 10) + (product % 10);
            count++;
        }

        _sum = sum;
        _count = count;
    }

    /// <inheritdoc />
    public override char GetCurrentCheckDigit() =>
        (char)('0' + ((10 - (_sum % 10)) % 10));

    /// <inheritdoc />
    public override void Reset()
    {
        _sum = 0;
        _count = 0;
    }
}
