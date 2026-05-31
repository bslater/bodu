// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PearsonTests.Append.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;

namespace Bodu.IO.Hashing;

public partial class PearsonTests
{

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

        var result = algorithm.GetCurrentHash();
        Assert.AreEqual(bits / 8, result.Length);
    }

}
