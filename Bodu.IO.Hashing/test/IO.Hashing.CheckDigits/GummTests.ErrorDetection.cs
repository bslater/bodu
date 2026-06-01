// ---------------------------------------------------------------------------------------------------------------
// <copyright file="GummTests.ErrorDetection.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;

namespace Bodu.IO.Hashing.CheckDigits;

public sealed partial class GummTests
{
    /// <summary>
    /// Verifies the defining Gumm guarantee that every single-digit substitution error is detected: for every body
    /// of length 1 to 4, replacing any single position of the checked sequence (body or check digit) with a
    /// different digit causes <see cref="Gumm.IsValid(ReadOnlySpan{char})" /> to return <see langword="false" />.
    /// </summary>
    [TestMethod]
    [TestCategory("Regression")]
    public void IsValid_WhenAnySingleDigitIsSubstituted_ShouldReturnFalse()
    {
        foreach (var body in EnumerateBodies(1, 4))
        {
            var full = (body + Gumm.Compute(body.AsSpan())).ToCharArray();

            for (var i = 0; i < full.Length; i++)
            {
                var original = full[i];
                for (var d = '0'; d <= '9'; d++)
                {
                    if (d == original) continue;

                    full[i] = d;
                    Assert.IsFalse(
                        Gumm.IsValid(full),
                        $"Undetected single-digit substitution at index {i}: '{new string(full)}'.");
                }

                full[i] = original;
            }
        }
    }

    /// <summary>
    /// Verifies the defining Gumm guarantee that every transposition of two adjacent digits is detected: for every
    /// body of length 1 to 4, swapping any adjacent pair of distinct characters in the checked sequence causes
    /// <see cref="Gumm.IsValid(ReadOnlySpan{char})" /> to return <see langword="false" />.
    /// </summary>
    [TestMethod]
    [TestCategory("Regression")]
    public void IsValid_WhenAnyAdjacentPairIsTransposed_ShouldReturnFalse()
    {
        foreach (var body in EnumerateBodies(1, 4))
        {
            var full = (body + Gumm.Compute(body.AsSpan())).ToCharArray();

            for (var i = 0; i < full.Length - 1; i++)
            {
                if (full[i] == full[i + 1]) continue;

                (full[i], full[i + 1]) = (full[i + 1], full[i]);
                Assert.IsFalse(
                    Gumm.IsValid(full),
                    $"Undetected adjacent transposition at index {i}: '{new string(full)}'.");

                (full[i], full[i + 1]) = (full[i + 1], full[i]);
            }
        }
    }

    /// <summary>
    /// Enumerates every decimal body whose length falls within the inclusive range
    /// [<paramref name="minLength" />, <paramref name="maxLength" />].
    /// </summary>
    /// <param name="minLength">The smallest body length to generate.</param>
    /// <param name="maxLength">The largest body length to generate.</param>
    /// <returns>A sequence of decimal-digit strings.</returns>
    private static IEnumerable<string> EnumerateBodies(int minLength, int maxLength)
    {
        for (var length = minLength; length <= maxLength; length++)
        {
            var count = (int)Math.Pow(10, length);
            for (var n = 0; n < count; n++)
                yield return n.ToString(CultureInfo.InvariantCulture).PadLeft(length, '0');
        }
    }
}
