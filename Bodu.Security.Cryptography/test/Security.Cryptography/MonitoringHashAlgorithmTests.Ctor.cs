// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MonitoringHashAlgorithmTests.Ctor.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public partial class MonitoringHashAlgorithmTests
{
    // <summary>
    /// Verifies that the constructor sets <see cref="HashSize"/> to 32 bits (4 bytes). </summary>
    /// <summary>
    /// Verifies that <see cref="MonitoringHashAlgorithm.Ctor" /> returns the expected value.
    /// </summary>
    [TestMethod]
    public void Ctor_ShouldSetHashSizeTo32Bits()
    {
        using var algorithm = new MonitoringHashAlgorithm();
        Assert.AreEqual(32, algorithm.HashSize);
    }

    /// <summary>
    /// Verifies that the constructor initializes <see cref="MonitoringHashAlgorithm.BytesProcessed" /> to zero.
    /// </summary>
    [TestMethod]
    public void Ctor_ShouldSetBytesProcessedToZero()
    {
        using var algorithm = new MonitoringHashAlgorithm();
        Assert.AreEqual(0, algorithm.BytesProcessed);
    }

    /// <summary>
    /// Verifies that the initial hash result after construction is the 4-byte zero array.
    /// </summary>
    [TestMethod]
    public void Ctor_ShouldReturnZeroHash_WhenNoInputProcessed()
    {
        using var algorithm = new MonitoringHashAlgorithm();
        var result = algorithm.ComputeHash(Array.Empty<byte>());
        CollectionAssert.AreEqual(new byte[] { 0, 0, 0, 0 }, result);
    }
}
