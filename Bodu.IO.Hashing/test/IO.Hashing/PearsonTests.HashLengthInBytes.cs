// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PearsonTests.HashLengthInBytes.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

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
}
