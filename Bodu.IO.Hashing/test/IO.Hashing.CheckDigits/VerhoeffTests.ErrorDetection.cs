// ---------------------------------------------------------------------------------------------------------------
// <copyright file="VerhoeffTests.ErrorDetection.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
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
        var check = Verhoeff.Compute(SingleDigitSeedBody.AsSpan());
        var valid = SingleDigitSeedBody + check;
        var buffer = valid.ToCharArray();

        for (var i = 0; i < buffer.Length - 1; i++)
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
        var check = Verhoeff.Compute(twinSeedBody.AsSpan());
        var valid = twinSeedBody + check;

        Assert.IsTrue(Verhoeff.IsValid(valid.AsSpan()), "Precondition: twin-seed baseline must be valid.");

        for (var c = '0'; c <= '9'; c++)
        {
            if (c == '3') continue;
            var twin = new string(c, 2) + valid[2];
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
        var check = Verhoeff.Compute(SingleDigitSeedBody.AsSpan());
        var valid = SingleDigitSeedBody + check;

        Assert.IsTrue(Verhoeff.IsValid(valid.AsSpan()), "Precondition: baseline must be valid.");

        var buffer = valid.ToCharArray();
        for (var i = 0; i < buffer.Length; i++)
        {
            var original = buffer[i];
            for (var c = '0'; c <= '9'; c++)
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
