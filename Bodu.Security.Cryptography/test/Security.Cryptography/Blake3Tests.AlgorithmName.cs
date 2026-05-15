// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Blake3Tests.AlgorithmName.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public partial class Blake3Tests
{
    /// <summary>
    /// Verifies that <see cref="Blake3.AlgorithmName" /> returns the literal <c>"BLAKE3"</c> on a fresh instance.
    /// </summary>
    [TestMethod]
    public void AlgorithmName_WhenInstanceIsFresh_ShouldReturnLiteralBlake3()
    {
        using var algorithm = new Blake3();

        Assert.AreEqual("BLAKE3", algorithm.AlgorithmName);
    }
}
