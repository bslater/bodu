// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Luhn.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Hashing.CheckDigits;

/// <summary>
/// Computes the check digit of a decimal string using the <c>Luhn</c> (<c>mod 10</c>) algorithm. This class cannot be
/// inherited.
/// </summary>
/// <remarks>
/// <para>
/// The Luhn algorithm — sometimes called the <i>modulus 10</i> or <i>mod 10</i> algorithm — was designed by Hans Peter
/// Luhn at IBM in 1954 and entered the public domain shortly afterwards. It is the check-digit scheme used by most
/// credit card numbers, many national identification numbers, IMEI numbers, and numerous other serial encodings.
/// </para>
/// <para>
/// Working right-to-left over the sequence, every second digit (starting with the digit immediately to the left of the
/// check digit) is doubled. When doubling produces a two-digit value, the digits are summed — equivalently one
/// subtracts nine. The check digit is the value that makes the total sum divisible by ten.
/// </para>
/// <para>
/// Luhn catches all single-digit substitution errors and all adjacent-digit transpositions <i>except</i> the
/// transposition <c>09 ↔ 90</c>, which preserves the digit sum. It does not reliably detect other mutations such as
/// twin errors (<c>aa → bb</c>).
/// </para>
/// <para>
/// <b>Worked example.</b> For the body <c>"7992739871"</c>, the computed check digit is <c>'3'</c>, and the resulting
/// sequence <c>"79927398713"</c> is therefore valid under Luhn.
/// </para>
/// <note type="important">This algorithm is <b>not</b> cryptographically secure and should <b>not</b> be used for
/// password hashing, digital signatures, or integrity validation in security-sensitive applications.</note>
/// </remarks>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// // Single-call computation against an in-memory body.
/// char check = Luhn.Compute("7992739871");   // '3'
///
/// // Full-sequence validation.
/// bool ok = Luhn.IsValid("79927398713");     // true
///
/// // Streaming use when the body is built up incrementally.
/// var algo = new Luhn();
/// algo.Append("7992739871");
/// char d = algo.GetCurrentCheckDigit();      // '3'
///]]>
/// </code>
/// </example>
public sealed class Luhn
    : CheckDigitAlgorithm
{
    /// <summary>The number of body digits appended so far.</summary>
    private int _count;

    /// <summary>The running Luhn sum computed under the hypothesis that the final body length is even.</summary>
    private int _sumEvenHypothesis;

    /// <summary>The running Luhn sum computed under the hypothesis that the final body length is odd.</summary>
    private int _sumOddHypothesis;

    /// <summary>
    /// Initializes a new instance of the <see cref="Luhn" /> class.
    /// </summary>
    public Luhn()
    {
    }

    /// <inheritdoc />
    public override string AlgorithmName => "Luhn";

    /// <summary>
    /// Computes the Luhn check digit for the supplied body of decimal digits without allocating a streaming instance.
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
        int sum = 0;
        for (int i = digits.Length - 1, j = 2; i >= 0; i--, j++)
        {
            char ch = digits[i];
            if ((uint)(ch - '0') > 9u)
                ThrowHelper.ThrowIfNotAsciiDecimalDigit(ch, nameof(digits));

            int v = ch - '0';
            if ((j & 1) == 0)
            {
                v *= 2;
                if (v > 9) v -= 9;
            }

            sum += v;
        }

        return (char)('0' + ((10 - (sum % 10)) % 10));
    }

    /// <summary>
    /// Determines whether the supplied sequence, comprising a body followed by a trailing Luhn check digit, is
    /// consistent — that is, whether the digit sum evaluates to a multiple of ten.
    /// </summary>
    /// <param name="digitsIncludingCheck">The complete sequence including the trailing check digit.</param>
    /// <returns>
    /// <see langword="true" /> if the sequence evaluates as valid under Luhn; otherwise, <see langword="false" /> —
    /// including the case where <paramref name="digitsIncludingCheck" /> is empty or contains a character outside the
    /// range <c>'0'</c> to <c>'9'</c>.
    /// </returns>
    public static bool IsValid(ReadOnlySpan<char> digitsIncludingCheck)
    {
        if (digitsIncludingCheck.IsEmpty) return false;

        int sum = 0;
        for (int i = digitsIncludingCheck.Length - 1, j = 1; i >= 0; i--, j++)
        {
            char ch = digitsIncludingCheck[i];
            if ((uint)(ch - '0') > 9u) return false;

            int v = ch - '0';
            if ((j & 1) == 0)
            {
                v *= 2;
                if (v > 9) v -= 9;
            }

            sum += v;
        }

        return sum % 10 == 0;
    }

    /// <inheritdoc />
    public override void Append(ReadOnlySpan<char> digits)
    {
        int sumEven = _sumEvenHypothesis;
        int sumOdd = _sumOddHypothesis;
        int count = _count;

        for (int i = 0; i < digits.Length; i++)
        {
            char ch = digits[i];
            if ((uint)(ch - '0') > 9u)
                ThrowHelper.ThrowIfNotAsciiDecimalDigit(ch, nameof(digits));

            int v = ch - '0';
            int doubled = v * 2;
            if (doubled > 9) doubled -= 9;

            // _sumEvenHypothesis (body length N will be even): body[i] is doubled iff i is odd.
            // _sumOddHypothesis  (body length N will be odd):  body[i] is doubled iff i is even.
            if ((count & 1) == 0)
            {
                sumEven += v;
                sumOdd += doubled;
            }
            else
            {
                sumEven += doubled;
                sumOdd += v;
            }

            count++;
        }

        _sumEvenHypothesis = sumEven;
        _sumOddHypothesis = sumOdd;
        _count = count;
    }

    /// <inheritdoc />
    public override char GetCurrentCheckDigit()
    {
        int sum = (_count & 1) == 0 ? _sumEvenHypothesis : _sumOddHypothesis;
        return (char)('0' + ((10 - (sum % 10)) % 10));
    }

    /// <inheritdoc />
    public override void Reset()
    {
        _sumEvenHypothesis = 0;
        _sumOddHypothesis = 0;
        _count = 0;
    }
}
