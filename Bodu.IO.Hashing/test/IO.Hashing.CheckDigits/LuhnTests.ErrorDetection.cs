// ---------------------------------------------------------------------------------------------------------------
// <copyright file="LuhnTests.ErrorDetection.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Hashing.CheckDigits;

public sealed partial class LuhnTests
{

    private const string SingleDigitSeedBody = "7992739871";

    /// <summary>
    /// Verifies that Luhn detects every adjacent-digit transposition error in the seed sequence, <i>except</i>
    /// the well-documented exception in which the two digits sum to nine and differ by nine — namely the
    /// swap of <c>'0'</c> and <c>'9'</c>.
    /// </summary>
    [TestMethod]
    public void IsValid_WhenAdjacentDigitsAreTransposed_ShouldReturnFalseExceptFor09Transposition()
    {
        var check = Luhn.Compute(SingleDigitSeedBody.AsSpan());
        var valid = SingleDigitSeedBody + check;
        var buffer = valid.ToCharArray();

        for (var i = 0; i < buffer.Length - 1; i++)
        {
            if (buffer[i] == buffer[i + 1]) continue;

            (buffer[i], buffer[i + 1]) = (buffer[i + 1], buffer[i]);

            var isValid = Luhn.IsValid(buffer);
            var isZeroNineSwap = (buffer[i] == '0' && buffer[i + 1] == '9') || (buffer[i] == '9' && buffer[i + 1] == '0');

            if (isZeroNineSwap)
                Assert.IsTrue(isValid, $"The 0<->9 transposition at index {i} ({new string(buffer)}) is the documented Luhn exception and should remain valid.");
            else
                Assert.IsFalse(isValid, $"Transposition at index {i} ({new string(buffer)}) must be rejected.");

            (buffer[i], buffer[i + 1]) = (buffer[i + 1], buffer[i]);
        }
    }

    /// <summary>
    /// Verifies that Luhn detects every single-digit substitution error — for each position in the body, every
    /// distinct replacement digit invalidates the resulting sequence.
    /// </summary>
    [TestMethod]
    public void IsValid_WhenAnySingleDigitIsSubstituted_ShouldReturnFalse()
    {
        var check = Luhn.Compute(SingleDigitSeedBody.AsSpan());
        var valid = SingleDigitSeedBody + check;

        Assert.IsTrue(Luhn.IsValid(valid.AsSpan()), "Precondition: baseline must be valid.");

        var buffer = valid.ToCharArray();
        for (var i = 0; i < buffer.Length; i++)
        {
            var original = buffer[i];
            for (var c = '0'; c <= '9'; c++)
            {
                if (c == original) continue;
                buffer[i] = c;
                Assert.IsFalse(
                    Luhn.IsValid(buffer),
                    $"Substitution of '{original}' with '{c}' at index {i} ({new string(buffer)}) must be rejected.");
            }

            buffer[i] = original;
        }
    }

    /// <summary>
    /// Verifies the documented Luhn blind-spot: swapping adjacent <c>'0'</c> and <c>'9'</c> preserves the
    /// weighted digit sum, so the resulting transposition is not detected.
    /// </summary>
    [TestMethod]
    public void IsValid_WhenZeroAndNineAreTransposed_ShouldRemainValid()
    {
        Assert.IsTrue(Luhn.IsValid("091".AsSpan()), "Precondition: '091' is a valid Luhn sequence.");
        Assert.IsTrue(Luhn.IsValid("901".AsSpan()), "Transposing the adjacent 0 and 9 of '091' must remain valid under Luhn — the documented blind spot.");
    }

}
