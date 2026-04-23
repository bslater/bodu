// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BernsteinTests.InitialValue.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Hashing;

public partial class BernsteinTests
{
    /// <summary>
    /// Verifies that the default constructor selects <see cref="Bernstein.DefaultInitialValue" />.
    /// </summary>
    [TestMethod]
    public void InitialValue_WhenDefaultConstructed_ShouldEqualDefaultInitialValue()
    {
        Bernstein algorithm = new();
        Assert.AreEqual(Bernstein.DefaultInitialValue, algorithm.InitialValue);
    }

    /// <summary>
    /// Verifies that using different <see cref="Bernstein.InitialValue" /> seeds produces different digests for
    /// the same input.
    /// </summary>
    [TestMethod]
    public void Append_WhenInitialValuesDiffer_ShouldProduceDifferentHashes()
    {
        byte[] input = NonCryptographicHashSharedInputs.Abc;

        Bernstein a = new(initialValue: 1U, useModifiedAlgorithm: false);
        Bernstein b = new(initialValue: 2U, useModifiedAlgorithm: false);
        a.Append(input);
        b.Append(input);

        CollectionAssert.AreNotEqual(a.GetCurrentHash(), b.GetCurrentHash());
    }

    /// <summary>
    /// Verifies that an initial seed of zero is accepted and yields a 4-byte digest.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenInitialValueIsZero_ShouldProduceFourByteDigest()
    {
        Bernstein algorithm = new(initialValue: 0U, useModifiedAlgorithm: false);
        algorithm.Append(NonCryptographicHashSharedInputs.Abc);

        byte[] digest = algorithm.GetCurrentHash();
        Assert.AreEqual(4, digest.Length);
    }

    /// <summary>
    /// Verifies that an initial seed of <see cref="uint.MaxValue" /> is accepted.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenInitialValueIsUInt32MaxValue_ShouldBeAccepted()
    {
        Bernstein algorithm = new(initialValue: uint.MaxValue, useModifiedAlgorithm: false);
        algorithm.Append(NonCryptographicHashSharedInputs.Abc);

        byte[] digest = algorithm.GetCurrentHash();
        Assert.IsNotNull(digest);
    }

    /// <summary>
    /// Verifies that two independent instances constructed with the same seed produce identical digests.
    /// </summary>
    [TestMethod]
    public void Append_WhenSameInitialValue_ShouldProduceSameHash()
    {
        byte[] input = NonCryptographicHashSharedInputs.Abc;

        Bernstein a = new(initialValue: 0xABCDEFU, useModifiedAlgorithm: false);
        Bernstein b = new(initialValue: 0xABCDEFU, useModifiedAlgorithm: false);
        a.Append(input);
        b.Append(input);

        CollectionAssert.AreEqual(a.GetCurrentHash(), b.GetCurrentHash());
    }

    /// <summary>
    /// Verifies that the modified XOR variant yields a different digest from the original additive form for
    /// the same non-empty input.
    /// </summary>
    [TestMethod]
    public void Append_WhenModifiedVariant_ShouldDifferFromOriginal()
    {
        byte[] input = NonCryptographicHashSharedInputs.Abc;

        Bernstein original = new();
        Bernstein modified = new(Bernstein.DefaultInitialValue, useModifiedAlgorithm: true);
        original.Append(input);
        modified.Append(input);

        CollectionAssert.AreNotEqual(original.GetCurrentHash(), modified.GetCurrentHash());
    }
}
