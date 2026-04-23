// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DammTests.ErrorDetection.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Hashing.CheckDigits;

public sealed partial class DammTests
{
    private const string SingleDigitSeedBody = "572";

    /// <summary>
    /// Verifies that Damm detects every single-digit substitution error in the canonical seed sequence.
    /// </summary>
    [TestMethod]
    public void IsValid_WhenAnySingleDigitIsSubstituted_ShouldReturnFalse()
    {
        char check = Damm.Compute(SingleDigitSeedBody.AsSpan());
        string valid = SingleDigitSeedBody + check;

        Assert.IsTrue(Damm.IsValid(valid.AsSpan()), "Precondition: baseline must be valid.");

        char[] buffer = valid.ToCharArray();
        for (int i = 0; i < buffer.Length; i++)
        {
            char original = buffer[i];
            for (char c = '0'; c <= '9'; c++)
            {
                if (c == original) continue;
                buffer[i] = c;
                Assert.IsFalse(
                    Damm.IsValid(buffer),
                    $"Substitution of '{original}' with '{c}' at index {i} ({new string(buffer)}) must be rejected.");
            }

            buffer[i] = original;
        }
    }

    /// <summary>
    /// Verifies that Damm detects <i>every</i> adjacent-digit transposition — without exception — in the
    /// canonical seed sequence. Unlike Luhn, Damm has no known transposition blind spot.
    /// </summary>
    [TestMethod]
    public void IsValid_WhenAdjacentDigitsAreTransposed_ShouldReturnFalse()
    {
        char check = Damm.Compute(SingleDigitSeedBody.AsSpan());
        string valid = SingleDigitSeedBody + check;
        char[] buffer = valid.ToCharArray();

        for (int i = 0; i < buffer.Length - 1; i++)
        {
            if (buffer[i] == buffer[i + 1]) continue;

            (buffer[i], buffer[i + 1]) = (buffer[i + 1], buffer[i]);
            Assert.IsFalse(
                Damm.IsValid(buffer),
                $"Transposition at index {i} ({new string(buffer)}) must be rejected by Damm.");
            (buffer[i], buffer[i + 1]) = (buffer[i + 1], buffer[i]);
        }
    }
}
