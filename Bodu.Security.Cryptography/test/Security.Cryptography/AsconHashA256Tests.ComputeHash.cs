// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AsconHashA256Tests.ComputeHash.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

public partial class AsconHashA256Tests
{
    /// <summary>
    /// Verifies that two consecutive <see cref="HashAlgorithm.ComputeHash(byte[])" /> calls
    /// against the same <see cref="AsconHashA256" /> instance produce identical digests for
    /// identical input — guarding against state leakage across reuse.
    /// </summary>
    [TestMethod]
    public void ComputeHash_WhenInvokedRepeatedly_ShouldYieldIdenticalDigest()
    {
        using var algorithm = new AsconHashA256();
        byte[] input = Enumerable.Range(0, 256).Select(i => (byte)i).ToArray();

        byte[] first = algorithm.ComputeHash(input);
        byte[] second = algorithm.ComputeHash(input);

        CollectionAssert.AreEqual(first, second);
    }
}
