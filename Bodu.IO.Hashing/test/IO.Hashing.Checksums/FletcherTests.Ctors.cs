// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FletcherCtorGuardTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Hashing.Checksums;

/// <summary>
/// Contains unit tests that exercise the hash-size validation in the protected <see cref="Fletcher{TSelf}" />
/// constructor. The validation guard is not reachable through the concrete <see cref="Fletcher16" /> /
/// <see cref="Fletcher32" /> / <see cref="Fletcher64" /> entry points (which each pass fixed, valid widths), so a
/// test-only subclass is required to drive invalid inputs through the base constructor.
/// </summary>
public abstract partial class FletcherTests<TTest, TAlgorithm>
{
    /// <summary>
    /// Verifies that constructing a <see cref="Fletcher{TSelf}" /> subclass with an unsupported hash size throws
    /// <see cref="ArgumentException" /> whose <c>ParamName</c> is <c>hashSize</c> and whose message begins with
    /// the literal <c>"Invalid hash size:"</c> prefix declared in the base constructor.
    /// </summary>
    /// <param name="hashSize">The invalid hash size under test.</param>
    [TestMethod]
    [DataRow(0)]
    [DataRow(1)]
    [DataRow(8)]
    [DataRow(24)]
    [DataRow(48)]
    [DataRow(128)]
    [DataRow(-1)]
    public void Ctor_WhenHashSizeIsNotSupported_ShouldThrowArgumentException(int hashSize)
    {
        var ex = Assert.ThrowsExactly<ArgumentException>(() => new TestFletcher(hashSize));
        Assert.AreEqual("hashSize", ex.ParamName);
        StringAssert.StartsWith(ex.Message, "Invalid hash size:");
    }

    /// <summary>
    /// Verifies that each supported Fletcher hash size (16, 32, 64) is accepted by the base constructor without
    /// exception and yields an instance with the matching <see cref="Fletcher{TSelf}.AlgorithmName" /> and
    /// <see cref="System.IO.Hashing.NonCryptographicHashAlgorithm.HashLengthInBytes" />.
    /// </summary>
    /// <param name="hashSize">The valid hash size under test.</param>
    /// <param name="expectedBytes">The expected <see cref="System.IO.Hashing.NonCryptographicHashAlgorithm.HashLengthInBytes" />.</param>
    [TestMethod]
    [DataRow(16, 2)]
    [DataRow(32, 4)]
    [DataRow(64, 8)]
    public void Ctor_WhenHashSizeIsSupported_ShouldProduceExpectedAlgorithmNameAndLength(int hashSize, int expectedBytes)
    {
        TestFletcher fletcher = new(hashSize);

        Assert.AreEqual(expectedBytes, fletcher.HashLengthInBytes);
        Assert.AreEqual($"Fletcher-{hashSize}", fletcher.AlgorithmName);
    }
}
