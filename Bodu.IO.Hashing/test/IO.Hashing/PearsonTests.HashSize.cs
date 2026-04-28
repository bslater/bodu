// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PearsonTests.HashSize.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;

namespace Bodu.IO.Hashing;

public partial class PearsonTests
{
    /// <summary>
    /// Verifies that the parameterless constructor selects an 8-bit hash size.
    /// </summary>
    [TestMethod]
    public void HashLengthInBytes_WhenDefaultConstructed_ShouldBeOneByte()
    {
        Pearson algorithm = new();
        Assert.AreEqual(1, algorithm.HashLengthInBytes);
    }

    /// <summary>
    /// Verifies that constructing with a supported hash size yields a digest of the expected length.
    /// </summary>
    /// <param name="bits">The hash size in bits to request.</param>
    [TestMethod]
    [DataRow(8)]
    [DataRow(64)]
    [DataRow(128)]
    [DataRow(512)]
    [DataRow(2048)]
    public void Append_WhenHashSizeSet_ShouldReturnExpectedByteLength(int bits)
    {
        Pearson algorithm = new(bits, Pearson.PearsonTableType.Pearson);
        algorithm.Append(Encoding.ASCII.GetBytes("abc"));

        byte[] result = algorithm.GetCurrentHash();
        Assert.AreEqual(bits / 8, result.Length);
    }

    /// <summary>
    /// Verifies that constructing with a hash size outside [8, 2048] or not divisible by 8 throws
    /// <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    /// <param name="bits">The unsupported hash size, in bits.</param>
    [TestMethod]
    [DataRow(0)]
    [DataRow(7)]
    [DataRow(9)]
    [DataRow(2056)]
    [DataRow(-8)]
    public void Ctor_WhenHashSizeIsOutOfRange_ShouldThrow(int bits)
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
            _ = new Pearson(bits, Pearson.PearsonTableType.Pearson));
    }
}
