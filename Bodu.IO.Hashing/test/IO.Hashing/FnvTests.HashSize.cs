// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FnvTests.HashSize.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Hashing;

/// <summary>
/// Contains tests that exercise <see cref="Fnv{TSelf}" /> hash-size validation. These checks cannot be reached
/// through the production <c>Fnv1*32</c> / <c>Fnv1*64</c> derivatives because they hard-code the supported
/// widths, so a private derived type is used to drive the guard.
/// </summary>
[TestClass]
public class FnvHashSizeTests
{
    /// <summary>
    /// Verifies that supplying an unsupported hash size to <see cref="Fnv{TSelf}" /> throws
    /// <see cref="ArgumentException" /> with <c>ParamName</c> equal to <c>hashSize</c>.
    /// </summary>
    /// <param name="hashSize">The unsupported hash size in bits.</param>
    [TestMethod]
    [DataRow(0)]
    [DataRow(1)]
    [DataRow(31)]
    [DataRow(63)]
    [DataRow(128)]
    [DataRow(-1)]
    public void Ctor_WhenHashSizeIsUnsupported_ShouldThrowArgumentException(int hashSize)
    {
        var ex = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = new InvalidHashSizeFnv(hashSize);
        });
        Assert.AreEqual("hashSize", ex.ParamName);
    }

    /// <summary>
    /// A private <see cref="Fnv{TSelf}" /> subclass that allows the test to inject an arbitrary
    /// <c>hashSize</c> argument so the base-class validation guard can be exercised.
    /// </summary>
    private sealed class InvalidHashSizeFnv : Fnv<InvalidHashSizeFnv>
    {
        public InvalidHashSizeFnv()
            : this(32)
        {
        }

        public InvalidHashSizeFnv(int hashSize)
            : base(hashSize, prime: 0x01000193UL, offsetBasis: 0x811C9DC5UL, useFnv1a: true)
        {
        }
    }
}
