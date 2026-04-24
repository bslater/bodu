// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CheckDigitAlgorithmTests.Guards.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.IO.CheckDigits;

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
    [DataTestMethod]
    public void Append_WhenCharacterIsNotAnAsciiDigit_ShouldThrowArgumentOutOfRangeException(char invalid)
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
    public void Append_WhenSingleCharacterIsNotAnAsciiDigit_ShouldThrowArgumentOutOfRangeException()
    {
        TAlgorithm algorithm = CreateAlgorithm();

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            algorithm.Append('x');
        });
    }
}
