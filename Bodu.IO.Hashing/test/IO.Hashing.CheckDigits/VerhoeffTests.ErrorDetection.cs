// ---------------------------------------------------------------------------------------------------------------
// <copyright file="VerhoeffTests.ErrorDetection.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Hashing.CheckDigits;

public sealed partial class VerhoeffTests
{

    private const string SingleDigitSeedBody = "1428570";

    /// <summary>
    /// Verifies that Verhoeff detects <i>every</i> adjacent-digit transposition — without exception — in the
    /// canonical seed sequence.
    /// </summary>
    [TestMethod]
    public void IsValid_WhenAdjacentDigitsAreTransposed_ShouldReturnFalse()
    {
        char check = Verhoeff.Compute(SingleDigitSeedBody.AsSpan());
        string valid = SingleDigitSeedBody + check;
        char[] buffer = valid.ToCharArray();

        for (int i = 0; i < buffer.Length - 1; i++)
        {
            if (buffer[i] == buffer[i + 1]) continue;

            (buffer[i], buffer[i + 1]) = (buffer[i + 1], buffer[i]);
            Assert.IsFalse(
                Verhoeff.IsValid(buffer),
                $"Transposition at index {i} ({new string(buffer)}) must be rejected by Verhoeff.");
            (buffer[i], buffer[i + 1]) = (buffer[i + 1], buffer[i]);
        }
    }

    /// <summary>
    /// Verifies that Verhoeff detects the <i>twin</i> error class — pairs of equal adjacent digits substituted
    /// by a pair of different equal digits, for example <c>"aa"</c> to <c>"bb"</c>.
    /// </summary>
    [TestMethod]
    public void IsValid_WhenAdjacentTwinDigitsAreSubstituted_ShouldReturnFalse()
    {
        const string twinSeedBody = "33";
        char check = Verhoeff.Compute(twinSeedBody.AsSpan());
        string valid = twinSeedBody + check;

        Assert.IsTrue(Verhoeff.IsValid(valid.AsSpan()), "Precondition: twin-seed baseline must be valid.");

        for (char c = '0'; c <= '9'; c++)
        {
            if (c == '3') continue;
            string twin = new string(c, 2) + valid[2];
            Assert.IsFalse(
                Verhoeff.IsValid(twin.AsSpan()),
                $"Twin substitution '33' -> '{c}{c}' ({twin}) must be rejected by Verhoeff.");
        }
    }

    /// <summary>
    /// Verifies that Verhoeff detects every single-digit substitution error in the canonical seed sequence.
    /// </summary>
    [TestMethod]
    public void IsValid_WhenAnySingleDigitIsSubstituted_ShouldReturnFalse()
    {
        char check = Verhoeff.Compute(SingleDigitSeedBody.AsSpan());
        string valid = SingleDigitSeedBody + check;

        Assert.IsTrue(Verhoeff.IsValid(valid.AsSpan()), "Precondition: baseline must be valid.");

        char[] buffer = valid.ToCharArray();
        for (int i = 0; i < buffer.Length; i++)
        {
            char original = buffer[i];
            for (char c = '0'; c <= '9'; c++)
            {
                if (c == original) continue;
                buffer[i] = c;
                Assert.IsFalse(
                    Verhoeff.IsValid(buffer),
                    $"Substitution of '{original}' with '{c}' at index {i} ({new string(buffer)}) must be rejected.");
            }

            buffer[i] = original;
        }
    }

}
