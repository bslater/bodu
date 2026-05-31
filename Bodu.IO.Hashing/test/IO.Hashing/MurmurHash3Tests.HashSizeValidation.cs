// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MurmurHash3Tests.HashSizeValidation.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Hashing;

/// <summary>
/// Contains unit tests that exercise the hash-size validation in the protected <see cref="MurmurHash3{T}" />
/// constructor. The validation guard is not reachable through the concrete <see cref="MurmurHash3_32" /> and
/// <see cref="MurmurHash3_128" /> entry points (which pass fixed, valid widths), so a test-only subclass is
/// required to drive invalid inputs through the base constructor.
/// </summary>
[TestClass]
public sealed class MurmurHash3HashSizeValidationTests
{

    /// <summary>
    /// Verifies that constructing a <see cref="MurmurHash3{T}" /> subclass with an unsupported hash size throws
    /// <see cref="ArgumentOutOfRangeException" /> whose <c>ParamName</c> is <c>hashSize</c> and whose message begins
    /// with the literal <c>"Invalid hash size:"</c> prefix declared in the base constructor.
    /// </summary>
    /// <param name="hashSize">The invalid hash size under test.</param>
    [TestMethod]
    [DataRow(0)]
    [DataRow(1)]
    [DataRow(8)]
    [DataRow(16)]
    [DataRow(64)]
    [DataRow(256)]
    [DataRow(-1)]
    [DataRow(int.MaxValue)]
    public void Ctor_WhenHashSizeIsNotSupported_ShouldThrowExactly(int hashSize)
    {
        ArgumentOutOfRangeException ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = new TestMurmurHash3(hashSize);
        });
        Assert.AreEqual("hashSize", ex.ParamName);
        StringAssert.StartsWith(ex.Message, "Invalid hash size:");
    }

    /// <summary>
    /// Verifies that each supported MurmurHash3 hash size (32, 128) is accepted by the base constructor without
    /// exception and yields an instance whose
    /// <see cref="System.IO.Hashing.NonCryptographicHashAlgorithm.HashLengthInBytes" /> matches the supplied size.
    /// </summary>
    /// <param name="hashSize">The valid hash size under test.</param>
    /// <param name="expectedBytes">The expected <see cref="System.IO.Hashing.NonCryptographicHashAlgorithm.HashLengthInBytes" />.</param>
    [TestMethod]
    [DataRow(32, 4)]
    [DataRow(128, 16)]
    public void Ctor_WhenHashSizeIsSupported_ShouldProduceExpectedHashLength(int hashSize, int expectedBytes)
    {
        TestMurmurHash3 algorithm = new(hashSize);

        Assert.AreEqual(expectedBytes, algorithm.HashLengthInBytes);
    }

    /// <summary>
    /// Verifies that the <see cref="MurmurHash3{T}.Seed" /> supplied to the base constructor is exposed unchanged
    /// through the <see cref="MurmurHash3{T}.Seed" /> property.
    /// </summary>
    [TestMethod]
    public void Seed_WhenSuppliedToCtor_ShouldBeExposedThroughProperty()
    {
        TestMurmurHash3 algorithm = new(32, seed: 0xDEADBEEFu);
        Assert.AreEqual(0xDEADBEEFu, algorithm.Seed);
    }

    /// <summary>
    /// Test-only <see cref="MurmurHash3{T}" /> subclass used solely to reach the protected constructor with
    /// arbitrary hash sizes. The parameterless public constructor satisfies the CRTP <c>new()</c> constraint and
    /// is never invoked directly by tests.
    /// </summary>
    private sealed class TestMurmurHash3
        : MurmurHash3<TestMurmurHash3>
    {

        public TestMurmurHash3()
            : base(32)
        {
        }

        public TestMurmurHash3(int hashSize)
            : base(hashSize)
        {
        }

        public TestMurmurHash3(int hashSize, uint seed)
            : base(hashSize, seed)
        {
        }

        protected override byte[] ComputeHashCore(ReadOnlySpan<byte> source) =>
            new byte[this.HashLengthInBytes];

    }

}
