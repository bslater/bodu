// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SkeinTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

/// <summary>
/// Shared test fixture for the <see cref="Skein{T}" /> family of hash algorithms, parameterised by a Skein-specific
/// variant enum that encodes both the requested output size and the operating mode (plain hash vs Skein-MAC) so a
/// single test class can drive every (state, output, mode) combination supported by its variant.
/// </summary>
/// <typeparam name="TTest">The concrete test class inheriting this fixture.</typeparam>
/// <typeparam name="TAlgorithm">The specific Skein variant under test (<see cref="Skein256" />, <see cref="Skein512" />, or <see cref="Skein1024" />).</typeparam>
/// <typeparam name="TVariant">
/// The per-class variant enum encoding (output size, mode) — for example <see cref="Skein256TestVariant" />. The
/// enum's name convention is <c>Hash_<i>n</i></c> for the plain-hash profile and <c>Mac_<i>n</i></c> for the keyed
/// Skein-MAC profile, where <i>n</i> is the digest size in bits.
/// </typeparam>
[TestClass]
public abstract partial class SkeinTests<TTest, TAlgorithm, TVariant>
    : Security.Cryptography.KeyedBlockHashAlgorithmTests<TTest, TAlgorithm, TVariant>
    where TTest : SkeinTests<TTest, TAlgorithm, TVariant>, new()
    where TAlgorithm : Skein<TAlgorithm>, new()
    where TVariant : struct, Enum
{
    /// <summary>
    /// A deterministic 32-byte key used as the variant default for every Skein-MAC profile across every Skein
    /// variant. Per-row keys carried by <see cref="KeyedHashAlgorithmKnownAnswer.Key" /> override this default for
    /// keyed known-answer runs.
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
    /// Skein accepts any key length from zero up to <see cref="Skein{T}.MaxKeySize" /> / 8 bytes; there is no lower bound
    /// beyond "non-null". The framework's below-minimum negative test is therefore skipped, even though the Skein
    /// specification still sets <see cref="KeyedAlgorithmSpecification.MinKeyLength" /> to a MAC-friendly size so
    /// that the tests which generate a representative key via <c>GenerateUniqueKey(MinKeyLength)</c> exercise
    /// meaningful MAC behaviour rather than hashing with a one-byte key.
    /// </remarks>
    protected override bool EnforcesMinimumKeyLength => false;

    /// <inheritdoc />
    /// <remarks>
    /// The base default-construction helper assigns the specification's <see cref="KeyedAlgorithmSpecification.TestKey" />
    /// to <see cref="Skein{T}.Key" />. For the unkeyed default variant Skein's specifications publish an empty
    /// <c>TestKey</c> so baseline fixtures exercise the canonical plain-hash profile; keyed variants supply the
    /// shared <see cref="SkeinTestKey" />.
    /// </remarks>
    protected override TAlgorithm CreateAlgorithm() => CreateAlgorithm(DefaultVariant);

    /// <summary>
    /// Returns <see langword="true" /> when <paramref name="variant" />'s name begins with <c>"Mac_"</c>, indicating
    /// the keyed Skein-MAC profile. Concrete test classes follow the <c>Hash_<i>n</i></c> / <c>Mac_<i>n</i></c>
    /// naming convention so this single helper covers every Skein variant enum.
    /// </summary>
    /// <param name="variant">The variant under test.</param>
    /// <returns><see langword="true" /> if the variant selects the keyed MAC profile; otherwise <see langword="false" />.</returns>
    protected static bool IsMacVariant(TVariant variant) =>
        variant.ToString().StartsWith("Mac_", StringComparison.Ordinal);

    /// <summary>
    /// Verifies that a MAC key longer than one Skein state block is correctly processed through the multi-block
    /// <c>KEY</c> UBI loop and produces a repeatable digest across runs.
    /// </summary>
    [TestMethod]
    public void ComputeHash_WhenKeyExceedsBlockSize_ShouldProduceStableMac()
    {
        var input = Enumerable.Range(0, 50).Select(i => (byte)i).ToArray();

        using var reference = new TAlgorithm();
        var blockSize = reference.InputBlockSize;
        var longKey = Enumerable.Range(0, (blockSize * 2) + 11)
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
}
