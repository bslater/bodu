// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TigerTests.ComputeHash.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------


namespace Bodu.Security.Cryptography;

public partial class TigerTests
{
    /// <summary>
    /// Verifies that <see cref="Tiger.ComputeHash" />, when VariantIsDifferent, ProduceDifferentHash.
    /// </summary>
    [TestMethod]
    public void ComputeHash_WhenVariantIsDifferent_ShouldProduceDifferentHash()
    {
        TigerHashingVariant[] variants = Enum.GetValues<Bodu.Security.Cryptography.TigerHashingVariant>().ToArray();
        if (variants.Length < 2)
            Assert.Inconclusive("Not enough variants to test.");

        var input = new byte[0];
        var actual = new List<byte[]>();
        foreach (TigerHashingVariant variant in variants)
        {
            using Tiger algorithm = CreateAlgorithm();
            algorithm.Variant = variant;

            actual.Add(algorithm.ComputeHash(input));
        }

        CollectionAssert.AllItemsAreUnique(actual, "Hash results should be unique for different variants.");
    }
}
