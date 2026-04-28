// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MurmurHash3Tests.HashSize.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Hashing;

/// <summary>
/// Contains tests that exercise <see cref="MurmurHash3{T}" /> hash-size validation. These checks cannot be
/// reached through the production <c>MurmurHash3_32</c> / <c>MurmurHash3_128</c> derivatives because they
/// hard-code the supported widths, so a private derived type is used to drive the guard.
/// </summary>
[TestClass]
public class MurmurHash3HashSizeTests
{
    /// <summary>
    /// Verifies that supplying an unsupported hash size to <see cref="MurmurHash3{T}" /> throws
    /// <see cref="ArgumentOutOfRangeException" /> with <c>ParamName</c> equal to <c>hashSize</c>.
    /// </summary>
    /// <param name="hashSize">The unsupported hash size in bits.</param>
    [TestMethod]
    [DataRow(0)]
    [DataRow(8)]
    [DataRow(16)]
    [DataRow(64)]
    [DataRow(256)]
    [DataRow(-1)]
    public void Ctor_WhenHashSizeIsUnsupported_ShouldThrowArgumentOutOfRangeException(int hashSize)
    {
        var ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = new InvalidHashSizeMurmur(hashSize);
        });
        Assert.AreEqual("hashSize", ex.ParamName);
    }

    /// <summary>
    /// A private <see cref="MurmurHash3{T}" /> subclass that allows the test to inject an arbitrary
    /// <c>hashSize</c> argument so the base-class validation guard can be exercised.
    /// </summary>
    private sealed class InvalidHashSizeMurmur : MurmurHash3<InvalidHashSizeMurmur>
    {
        public InvalidHashSizeMurmur()
            : this(32)
        {
        }

        public InvalidHashSizeMurmur(int hashSize)
            : base(hashSize)
        {
        }

        protected override byte[] ComputeHashCore(ReadOnlySpan<byte> source) =>
            new byte[this.HashLengthInBytes];
    }
}
