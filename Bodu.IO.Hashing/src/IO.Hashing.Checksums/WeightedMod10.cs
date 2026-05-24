// ---------------------------------------------------------------------------------------------------------------
// <copyright file="WeightedMod10.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.IO.Hashing.CheckDigits;

namespace Bodu.IO.Hashing.Checksums;

/// <summary>
/// Provides shared streaming and one-shot math for the weighted modulus-10 check-digit family used by ISBN-13, GTIN
/// (EAN / UPC-A), and ABA routing numbers.
/// </summary>
/// <remarks>
/// <para>
/// The algorithms in this family all take the form:
/// </para>
/// <para>
/// <c>sum = Σ w_i · d_i  mod 10</c>, <c>check = (10 - sum) mod 10</c>.
/// </para>
/// <para>
/// where <c>w_i</c> is a fixed-period weight pattern indexed from the <em>right-hand</em> data digit. ISBN-13 / GTIN
/// use the two-element pattern <c>{3, 1}</c> (the check position itself is implicitly weight 1); ABA routing uses the
/// three-element pattern <c>{1, 7, 3}</c> cycling from the check position.
/// </para>
/// <para>
/// For the two-weight ISBN/GTIN case a dual-hypothesis accumulator analogous to <see cref="Luhn" /> lets a streaming
/// consumer expose <see cref="AlphanumericCheckDigitAlgorithm.GetCurrentCheckDigit" /> at every prefix length without
/// buffering. The three-weight ABA case is length-locked at 8 body digits so a simple single-sum streaming accumulator
/// suffices.
/// </para>
/// </remarks>
internal static class WeightedMod10
{
    /// <summary>
    /// Computes the ABA routing-number check digit for the supplied 8-digit body using weights <c>{1, 7, 3}</c> applied
    /// cyclically from the right-hand data digit toward the left.
    /// </summary>
    /// <param name="digits">The body characters. Each must be an ASCII decimal digit.</param>
    /// <returns>The check digit as an ASCII character in the range <c>'0'</c> to <c>'9'</c>.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="digits" /> contains any character outside the range <c>'0'</c> to <c>'9'</c>.
    /// </exception>
    /// <remarks>
    /// This helper is length-agnostic; length enforcement is the responsibility of callers that treat the body length
    /// as part of their contract (for example, <see cref="AbaRoutingNumber" /> requires eight digits).
    /// </remarks>
    public static char ComputeAba(ReadOnlySpan<char> digits)
    {
        // From the right-hand data digit toward the left the ABA pattern is {1, 7, 3}. From the check position
        // itself toward the left the pattern is {3, 1, 7, 3, 1, 7, 3, 1, 7}; including the check position makes
        // the total sum a multiple of ten for a consistent sequence.
        ReadOnlySpan<int> weights = [7, 3, 1];
        var sum = 0;
        for (int i = digits.Length - 1, j = 0; i >= 0; i--, j++)
        {
            var ch = digits[i];
            if ((uint)(ch - '0') > 9u)
                ThrowHelper.ThrowIfNotAsciiDecimalDigit(ch, nameof(digits));

            sum += (ch - '0') * weights[j % 3];
        }

        return (char)('0' + ((10 - (sum % 10)) % 10));
    }

    /// <summary>
    /// Computes the ISBN-13 / GTIN check digit for the supplied body using weights <c>{3, 1}</c> applied from the
    /// right-hand data digit toward the left.
    /// </summary>
    /// <param name="digits">The body characters. Each must be an ASCII decimal digit.</param>
    /// <returns>The check digit as an ASCII character in the range <c>'0'</c> to <c>'9'</c>.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="digits" /> contains any character outside the range <c>'0'</c> to <c>'9'</c>.
    /// </exception>
    public static char ComputeIsbn13(ReadOnlySpan<char> digits)
    {
        var sum = 0;
        for (int i = digits.Length - 1, j = 0; i >= 0; i--, j++)
        {
            var ch = digits[i];
            if ((uint)(ch - '0') > 9u)
                ThrowHelper.ThrowIfNotAsciiDecimalDigit(ch, nameof(digits));

            var v = ch - '0';

            // The check position is the rightmost (j = 0) with implicit weight 1; the rightmost data digit
            // therefore receives weight 3, the next weight 1, and so on alternating 3/1 toward the left.
            sum += (j & 1) == 0 ? v * 3 : v;
        }

        return (char)('0' + ((10 - (sum % 10)) % 10));
    }

    /// <summary>
    /// Determines whether the supplied sequence, comprising a body followed by its trailing check digit, is consistent
    /// under the ABA routing-number weight pattern <c>{3, 7, 1}</c>.
    /// </summary>
    /// <param name="digitsIncludingCheck">The complete sequence including the trailing check digit.</param>
    /// <returns>
    /// <see langword="true" /> if the sequence is empty or evaluates as valid; otherwise, <see langword="false" /> —
    /// including the case where <paramref name="digitsIncludingCheck" /> contains a character outside the range
    /// <c>'0'</c> to <c>'9'</c>.
    /// </returns>
    public static bool IsValidAba(ReadOnlySpan<char> digitsIncludingCheck)
    {
        ReadOnlySpan<int> weights = [1, 7, 3];
        var sum = 0;
        for (int i = digitsIncludingCheck.Length - 1, j = 0; i >= 0; i--, j++)
        {
            var ch = digitsIncludingCheck[i];
            if ((uint)(ch - '0') > 9u) return false;

            sum += (ch - '0') * weights[j % 3];
        }

        return sum % 10 == 0;
    }

    /// <summary>
    /// Determines whether the supplied sequence, comprising a body followed by its trailing check digit, is consistent
    /// under the ISBN-13 / GTIN weight pattern <c>{3, 1}</c>.
    /// </summary>
    /// <param name="digitsIncludingCheck">The complete sequence including the trailing check digit.</param>
    /// <returns>
    /// <see langword="true" /> if the sequence is empty or evaluates as valid; otherwise, <see langword="false" /> —
    /// including the case where <paramref name="digitsIncludingCheck" /> contains a character outside the range
    /// <c>'0'</c> to <c>'9'</c>.
    /// </returns>
    public static bool IsValidIsbn13(ReadOnlySpan<char> digitsIncludingCheck)
    {
        var sum = 0;
        for (int i = digitsIncludingCheck.Length - 1, j = 0; i >= 0; i--, j++)
        {
            var ch = digitsIncludingCheck[i];
            if ((uint)(ch - '0') > 9u) return false;

            var v = ch - '0';

            // Including the check position itself, positions j = 0, 2, 4, ... carry weight 1 while j = 1, 3, 5,
            // ... carry weight 3. When the sum is a multiple of 10 the sequence is internally consistent.
            sum += (j & 1) == 0 ? v : v * 3;
        }

        return sum % 10 == 0;
    }
}
