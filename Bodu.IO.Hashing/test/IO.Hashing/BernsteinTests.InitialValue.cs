// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BernsteinTests.InitialValue.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

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
    /// Verifies that two independent instances constructed with the same seed produce identical digests.
    /// </summary>
    [TestMethod]
    public void InitialValue_WhenSameValue_ShouldProduceSameHash()
    {
        var input = NonCryptographicHashSharedInputs.Abc;

        Bernstein a = new() { InitialValue = 0xABCDEFU };
        Bernstein b = new() { InitialValue = 0xABCDEFU };
        a.Append(input);
        b.Append(input);

        CollectionAssert.AreEqual(a.GetCurrentHash(), b.GetCurrentHash());
    }

    /// <summary>
    /// Verifies that setting <see cref="Bernstein.InitialValue" /> after input has been consumed throws
    /// <see cref="CryptographicUnexpectedOperationException" />.
    /// </summary>
    [TestMethod]
    public void InitialValue_WhenSetAfterHashingStarted_ShouldThrowExactly()
    {
        Bernstein algorithm = new();
        algorithm.Append(new byte[] { 1, 2, 3 });

        Assert.ThrowsExactly<CryptographicUnexpectedOperationException>(() => algorithm.InitialValue = 42U);
    }

    /// <summary>
    /// Verifies that <see cref="Bernstein.InitialValue" /> can be reconfigured after <see cref="Bernstein.Reset" />.
    /// </summary>
    [TestMethod]
    public void InitialValue_WhenSetAfterReset_ShouldBeAccepted()
    {
        Bernstein algorithm = new();
        algorithm.Append(new byte[] { 1, 2, 3 });
        algorithm.Reset();
        algorithm.InitialValue = 42U;

        Assert.AreEqual(42U, algorithm.InitialValue);
    }

    /// <summary>
    /// Verifies that changing <see cref="Bernstein.InitialValue" /> before any input has been consumed
    /// updates the accumulator so that two instances with distinct seeds produce distinct digests.
    /// </summary>
    [TestMethod]
    public void InitialValue_WhenSetBeforeHashing_ShouldAffectResult()
    {
        var input = NonCryptographicHashSharedInputs.Abc;

        Bernstein a = new() { InitialValue = 1U };
        Bernstein b = new() { InitialValue = 2U };
        a.Append(input);
        b.Append(input);

        CollectionAssert.AreNotEqual(a.GetCurrentHash(), b.GetCurrentHash());
    }

    /// <summary>
    /// Verifies that an initial seed of <see cref="uint.MaxValue" /> is accepted.
    /// </summary>
    [TestMethod]
    public void InitialValue_WhenSetToMaxValue_ShouldBeAccepted()
    {
        Bernstein algorithm = new() { InitialValue = uint.MaxValue };
        algorithm.Append(NonCryptographicHashSharedInputs.Abc);

        var digest = algorithm.GetCurrentHash();
        Assert.IsNotNull(digest);
    }

    /// <summary>
    /// Verifies that an initial seed of zero is accepted and yields a 4-byte digest.
    /// </summary>
    [TestMethod]
    public void InitialValue_WhenSetToZero_ShouldBeAccepted()
    {
        Bernstein algorithm = new() { InitialValue = 0U };
        algorithm.Append(NonCryptographicHashSharedInputs.Abc);

        var digest = algorithm.GetCurrentHash();
        Assert.AreEqual(4, digest.Length);
    }

}
