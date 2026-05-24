// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CheckDigitAlgorithmTests.GetCurrentCheckDigit.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Hashing.CheckDigits;

public abstract partial class CheckDigitAlgorithmTests<TTest, TAlgorithm>
{

    /// <summary>
    /// Verifies that reading the current check digit twice in succession — with no intervening appends — yields
    /// the same value, proving the getter is non-destructive.
    /// </summary>
    [TestMethod]
    public void GetCurrentCheckDigit_WhenReadRepeatedly_ShouldReturnSameValue()
    {
        TAlgorithm algorithm = CreateAlgorithm();
        algorithm.Append("12345".AsSpan());

        var first = algorithm.GetCurrentCheckDigit();
        var second = algorithm.GetCurrentCheckDigit();
        var third = algorithm.GetCurrentCheckDigit();

        Assert.AreEqual(first, second);
        Assert.AreEqual(second, third);
    }

}
