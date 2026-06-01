// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CheckDigitAlgorithmTests.Guards.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Hashing.CheckDigits;

public abstract partial class CheckDigitAlgorithmTests<TTest, TAlgorithm>
{

    /// <summary>
    /// Verifies that <see cref="CheckDigitAlgorithm.Append(ReadOnlySpan{char})" /> rejects any character outside
    /// the inclusive range <c>'0'</c> to <c>'9'</c> with <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    /// <param name="invalid">The offending character to exercise.</param>
    [DataRow('/')]
    [DataRow(':')]
    [DataRow(' ')]
    [DataRow('-')]
    [DataRow('a')]
    [DataRow('Z')]
    [DataRow('\0')]
    [TestMethod]
    public void Append_WhenCharacterIsNotAnAsciiDigit_ShouldThrowExactly(char invalid)
    {
        TAlgorithm algorithm = CreateAlgorithm();

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            algorithm.Append(new[] { '1', invalid, '2' }.AsSpan());
        });
    }

    /// <summary>
    /// Verifies that the single-character Append overload also rejects non-digit input.
    /// </summary>
    [TestMethod]
    public void Append_WhenSingleCharacterIsNotAnAsciiDigit_ShouldThrowExactly()
    {
        TAlgorithm algorithm = CreateAlgorithm();

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            algorithm.Append('x');
        });
    }

}
