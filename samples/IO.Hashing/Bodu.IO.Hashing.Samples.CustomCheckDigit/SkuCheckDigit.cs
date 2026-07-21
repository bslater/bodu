// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SkuCheckDigit.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.IO.Hashing.CheckDigits;

namespace Bodu.IO.Hashing.Samples.CustomCheckDigit;

/// <summary>
/// A custom weighted mod-10 check-digit scheme for internal SKU numbers, implementing the
/// library's <see cref="CheckDigitAlgorithm" /> contract. Digits are weighted with the classic
/// repeating <c>7, 3, 1</c> cycle from the left, summed, and the check digit is the value that
/// brings the total to a multiple of ten — the shape used by several transport and inventory
/// schemes, and deliberately different from Luhn's double-and-fold.
/// </summary>
/// <remarks>
/// Implementing the four abstract members (<see cref="AlgorithmName" />, <see cref="Append" />,
/// <see cref="GetCurrentCheckDigit" />, <see cref="Reset" />) is the whole contract; the static
/// <c>Compute</c>/<c>IsValid</c> pair mirrors the convenience surface every built-in scheme
/// exposes. The companion test project proves the implementation against the library's shared
/// <c>CheckDigitContractTests&lt;T&gt;</c> base.
/// </remarks>
public sealed class SkuCheckDigit
    : CheckDigitAlgorithm
{
    private static readonly int[] Weights = [7, 3, 1];

    private int _position;
    private int _sum;

    /// <inheritdoc />
    public override string AlgorithmName => "SKU-731";

    /// <summary>
    /// Computes the check digit for the supplied payload digits.
    /// </summary>
    /// <param name="digits">The payload digits, excluding the check digit.</param>
    /// <returns>The check digit character.</returns>
    public static char Compute(ReadOnlySpan<char> digits)
    {
        var algorithm = new SkuCheckDigit();
        algorithm.Append(digits);
        return algorithm.GetCurrentCheckDigit();
    }

    /// <summary>
    /// Determines whether the supplied value's trailing check digit is consistent with its payload.
    /// </summary>
    /// <param name="digitsIncludingCheck">The full value, ending with the check digit.</param>
    /// <returns><see langword="true" /> when the check digit verifies; otherwise, <see langword="false" />.</returns>
    public static bool IsValid(ReadOnlySpan<char> digitsIncludingCheck)
    {
        if (digitsIncludingCheck.Length < 2)
            return false;

        foreach (var ch in digitsIncludingCheck)
        {
            if ((uint)(ch - '0') > 9u)
                return false;
        }

        return Compute(digitsIncludingCheck[..^1]) == digitsIncludingCheck[^1];
    }

    /// <inheritdoc />
    public override void Append(ReadOnlySpan<char> digits)
    {
        foreach (var ch in digits)
        {
            if ((uint)(ch - '0') > 9u)
                throw new ArgumentException($"'{ch}' is not an ASCII decimal digit.", nameof(digits));

            _sum += (ch - '0') * Weights[_position % Weights.Length];
            _position++;
        }
    }

    /// <inheritdoc />
    public override char GetCurrentCheckDigit() =>
        (char)('0' + ((10 - (_sum % 10)) % 10));

    /// <inheritdoc />
    public override void Reset()
    {
        _position = 0;
        _sum = 0;
    }
}
