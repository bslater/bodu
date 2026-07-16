// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FnvTests{T,T}.HashSizeValidation.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Hashing;

/// <summary>
/// Contains unit tests that exercise the hash-size validation in the protected <see cref="Fnv{TSelf}" />
/// constructor. The validation guard is not reachable through the concrete <see cref="Fnv132" />, <see cref="Fnv164" />,
/// <see cref="Fnv1a32" />, or <see cref="Fnv1a64" /> entry points (which each pass fixed, valid widths), so a
/// test-only subclass is required to drive invalid inputs through the base constructor.
/// </summary>
[TestClass]
public sealed class FnvHashSizeValidationTests
{

    /// <summary>
    /// Verifies that constructing a <see cref="Fnv{TSelf}" /> subclass with an unsupported hash size throws
    /// <see cref="ArgumentException" /> whose <c>ParamName</c> is <c>hashSize</c> and whose message begins with
    /// the literal <c>"Invalid hash size:"</c> prefix declared in the base constructor.
    /// </summary>
    /// <param name="hashSize">The invalid hash size under test.</param>
    [TestMethod]
    [DataRow(0)]
    [DataRow(1)]
    [DataRow(8)]
    [DataRow(16)]
    [DataRow(128)]
    [DataRow(-1)]
    [DataRow(int.MaxValue)]
    public void Ctor_WhenHashSizeIsNotSupported_ShouldThrowExactly(int hashSize)
    {
        ArgumentException ex = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = new TestFnv(hashSize);
        });
        Assert.AreEqual("hashSize", ex.ParamName);
        StringAssert.StartsWith(ex.Message, "Invalid hash size:");
    }

    /// <summary>
    /// Verifies that each supported FNV hash size (32, 64) is accepted by the base constructor without exception
    /// and yields an instance whose <see cref="System.IO.Hashing.NonCryptographicHashAlgorithm.HashLengthInBytes" />
    /// matches the supplied size.
    /// </summary>
    /// <param name="hashSize">The valid hash size under test.</param>
    /// <param name="expectedBytes">The expected <see cref="System.IO.Hashing.NonCryptographicHashAlgorithm.HashLengthInBytes" />.</param>
    [TestMethod]
    [DataRow(32, 4)]
    [DataRow(64, 8)]
    public void Ctor_WhenHashSizeIsSupported_ShouldProduceExpectedHashLength(int hashSize, int expectedBytes)
    {
        TestFnv algorithm = new(hashSize);

        Assert.AreEqual(expectedBytes, algorithm.HashLengthInBytes);
        StringAssert.StartsWith(algorithm.AlgorithmName, "FNV-");
    }

    /// <summary>
    /// Test-only <see cref="Fnv{TSelf}" /> subclass used solely to reach the protected constructor with arbitrary
    /// hash sizes. The parameterless public constructor satisfies the CRTP <c>new()</c> constraint and is never
    /// invoked directly by tests.
    /// </summary>
    private sealed class TestFnv
        : Fnv
    {

        public TestFnv()
            : base(32, prime: 0x01000193UL, offsetBasis: 0x811C9DC5UL, useFnv1a: false)
        {
        }

        public TestFnv(int hashSize)
            : base(hashSize, prime: 0x01000193UL, offsetBasis: 0x811C9DC5UL, useFnv1a: false)
        {
        }

    }

}
