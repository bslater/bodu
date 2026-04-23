// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SkeinTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

/// <summary>
/// Shared test fixture for the <see cref="Skein{T}" /> family of hash algorithms, providing the keyed-hash test
/// harness with the Skein-specific relaxations required by the variable-length optional-key contract.
/// </summary>
/// <typeparam name="TTest">The concrete test class inheriting this fixture.</typeparam>
/// <typeparam name="TAlgorithm">The specific Skein variant under test (<see cref="Skein256" />, <see cref="Skein512" />, or <see cref="Skein1024" />).</typeparam>
[TestClass]
public abstract partial class SkeinTests<TTest, TAlgorithm>
    : Security.Cryptography.KeyedBlockHashAlgorithmTests<TTest, TAlgorithm, SingleTestVariant>
    where TTest : SkeinTests<TTest, TAlgorithm>, new()
    where TAlgorithm : Skein<TAlgorithm>, new()
{
    /// <summary>
    /// A deterministic 32-byte key used for all Skein-MAC round-trip tests across every variant.
    /// </summary>
    protected static readonly byte[] SkeinTestKey =
    [
        0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07,
        0x08, 0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F,
        0x10, 0x11, 0x12, 0x13, 0x14, 0x15, 0x16, 0x17,
        0x18, 0x19, 0x1A, 0x1B, 0x1C, 0x1D, 0x1E, 0x1F,
    ];

    /// <inheritdoc />
    /// <remarks>
    /// Skein reserves an empty <see cref="Skein{T}.Key" /> as the sentinel for the canonical unkeyed plain-hash
    /// profile; the framework's strict "empty key throws" negative test is therefore skipped.
    /// </remarks>
    protected override bool EmptyKeyIsValid => true;

    /// <inheritdoc />
    /// <remarks>
    /// Skein accepts any key length from zero up to <see cref="Skein{T}.MaxKeySizeBytes" />; there is no lower bound
    /// beyond "non-null". The framework's below-minimum negative test is therefore skipped, even though the Skein
    /// specification still sets <see cref="KeyedAlgorithmSpecification.MinKeyLength" /> to a MAC-friendly size so
    /// that the tests which generate a representative key via <c>GenerateUniqueKey(MinKeyLength)</c> exercise
    /// meaningful MAC behaviour rather than hashing with a one-byte key.
    /// </remarks>
    protected override bool EnforcesMinimumKeyLength => false;

    /// <inheritdoc />
    public override IEnumerable<SingleTestVariant> GetHashAlgorithmVariants() => new[]
    {
        SingleTestVariant.Default,
    };

    /// <inheritdoc />
    /// <remarks>
    /// The base default-construction helper assigns the specification's <see cref="KeyedAlgorithmSpecification.TestKey" />
    /// to <see cref="Skein{T}.Key" />. Skein's specifications publish an empty <c>TestKey</c> so that baseline fixtures
    /// exercise the canonical plain-hash profile; derived tests that need a keyed fixture supply their own key.
    /// </remarks>
    protected override TAlgorithm CreateAlgorithm() => new TAlgorithm();

    /// <summary>
    /// Verifies that supplying a non-empty <see cref="Skein{T}.Key" /> causes the digest to differ from the plain,
    /// unkeyed hash of the same input — confirming that the preliminary <c>KEY</c> UBI phase actually influences the
    /// chaining value.
    /// </summary>
    [TestMethod]
    public void ComputeHash_WhenKeyIsSet_ShouldDifferFromUnkeyedHashOfSameInput()
    {
        byte[] input = Enumerable.Range(0, 80).Select(i => (byte)((i * 31) + 7)).ToArray();

        byte[] plainHash;
        using (var plain = new TAlgorithm())
            plainHash = plain.ComputeHash(input);

        byte[] macHash;
        using (var mac = new TAlgorithm { Key = SkeinTestKey })
            macHash = mac.ComputeHash(input);

        CollectionAssert.AreNotEqual(plainHash, macHash);
    }

    /// <summary>
    /// Verifies that a MAC key longer than one Skein state block is correctly processed through the multi-block
    /// <c>KEY</c> UBI loop and produces a repeatable digest across runs.
    /// </summary>
    [TestMethod]
    public void ComputeHash_WhenKeyExceedsBlockSize_ShouldProduceStableMac()
    {
        byte[] input = Enumerable.Range(0, 50).Select(i => (byte)i).ToArray();

        using var reference = new TAlgorithm();
        int blockSize = reference.InputBlockSize;
        byte[] longKey = Enumerable.Range(0, (blockSize * 2) + 11)
            .Select(i => (byte)(i ^ 0xA5))
            .ToArray();

        byte[] first;
        using (var mac = new TAlgorithm { Key = longKey })
            first = mac.ComputeHash(input);

        byte[] second;
        using (var mac = new TAlgorithm { Key = longKey })
            second = mac.ComputeHash(input);

        CollectionAssert.AreEqual(first, second);
        Assert.AreEqual(reference.HashSize / 8, first.Length);
    }

    /// <summary>
    /// Verifies that the <see cref="Skein{T}.Key" /> getter returns a defensive copy so external callers cannot
    /// mutate the internal key material through the accessor.
    /// </summary>
    [TestMethod]
    public void Key_WhenGetterCalledTwice_ShouldReturnIndependentCopies()
    {
        using var skein = new TAlgorithm { Key = new byte[] { 1, 2, 3, 4 } };

        byte[] first = skein.Key;
        first[0] = 0xFF;

        byte[] second = skein.Key;

        CollectionAssert.AreEqual(new byte[] { 1, 2, 3, 4 }, second);
    }

    /// <summary>
    /// Verifies that assigning a key longer than <see cref="Skein{T}.MaxKeySizeBytes" /> throws
    /// <see cref="System.Security.Cryptography.CryptographicException" />.
    /// </summary>
    [TestMethod]
    public void Key_WhenAssignedLongerThanMaximum_ShouldThrowCryptographicException()
    {
        using var skein = new TAlgorithm();

        Assert.ThrowsExactly<System.Security.Cryptography.CryptographicException>(() =>
        {
            skein.Key = new byte[Skein<TAlgorithm>.MaxKeySizeBytes + 1];
        });
    }
}
