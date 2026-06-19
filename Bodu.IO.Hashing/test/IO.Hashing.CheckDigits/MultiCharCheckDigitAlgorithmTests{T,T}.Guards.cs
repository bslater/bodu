// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MultiCharCheckDigitAlgorithmTests{T,T}.Guards.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.IO.Hashing.Checksums;

namespace Bodu.IO.Hashing.CheckDigits;

public abstract partial class MultiCharCheckDigitAlgorithmTests<TTest, TAlgorithm>
{

    /// <summary>
    /// Verifies that <c>Append</c> rejects characters that fall outside the declared input alphabet with
    /// <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [TestMethod]
    public void Append_WhenCharacterIsOutsideInputAlphabet_ShouldThrowExactly()
    {
        MultiCharCheckDigitAlgorithmSpecification spec = GetSpecification();
        char invalid = spec.InputAlphabet == CheckDigitInputAlphabet.DecimalDigits ? 'A' : '!';

        TAlgorithm algorithm = CreateAlgorithm();

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            algorithm.Append(new[] { '1', invalid, '2' }.AsSpan());
        });
    }

    /// <summary>
    /// Verifies that the single-character <c>Append</c> overload also rejects input outside the declared input
    /// alphabet.
    /// </summary>
    [TestMethod]
    public void Append_WhenSingleCharacterIsOutsideInputAlphabet_ShouldThrowExactly()
    {
        MultiCharCheckDigitAlgorithmSpecification spec = GetSpecification();
        char invalid = spec.InputAlphabet == CheckDigitInputAlphabet.DecimalDigits ? 'A' : '!';

        TAlgorithm algorithm = CreateAlgorithm();

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            algorithm.Append(invalid);
        });
    }

    /// <summary>
    /// Verifies that <c>GetCurrentCheckDigits</c> throws <see cref="ArgumentException" /> when the destination
    /// span is shorter than <c>CheckLength</c>.
    /// </summary>
    [TestMethod]
    public void GetCurrentCheckDigits_WhenDestinationTooSmall_ShouldThrowExactly()
    {
        TAlgorithm algorithm = CreateAlgorithm();
        char[] tooSmall = new char[algorithm.CheckLength - 1];

        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            algorithm.GetCurrentCheckDigits(tooSmall.AsSpan());
        });
    }

}
