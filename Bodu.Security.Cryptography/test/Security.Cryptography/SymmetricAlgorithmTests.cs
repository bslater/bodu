// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SymmetricAlgorithmTests.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Provides unit tests for symmetric algorithms to verify encryption, decryption, and property behaviours.
/// </summary>
/// <typeparam name="TTest">The concrete test class, used to resolve specification data for <see cref="DynamicDataAttribute" /> sources.</typeparam>
/// <typeparam name="TAlgorithm">The type of symmetric algorithm under test.</typeparam>
[TestClass]
public abstract partial class SymmetricAlgorithmTests<TTest, TAlgorithm>
    where TTest : SymmetricAlgorithmTests<TTest, TAlgorithm>, new()
    where TAlgorithm : SymmetricAlgorithm, new()
{
    /// <summary>
    /// Creates an instance of the symmetric algorithm under test.
    /// </summary>
    /// <returns>An instance of the symmetric algorithm.</returns>
    protected virtual TAlgorithm CreateAlgorithm() => new();

    /// <summary>
    /// Returns the <see cref="SymmetricAlgorithmSpecification" /> describing the expected observable properties of
    /// <typeparamref name="TAlgorithm" />.
    /// </summary>
    /// <returns>A <see cref="SymmetricAlgorithmSpecification" /> instance populated with the expected values.</returns>
    protected abstract SymmetricAlgorithmSpecification GetSpecification();

    /// <summary>
    /// Configures <paramref name="algorithm"/> to use the supplied <paramref name="mode"/>. Used by base-class
    /// tests that need to switch the algorithm's cipher block mode without depending on a property surface that
    /// only exists on concrete cipher types.
    /// </summary>
    /// <param name="algorithm">The algorithm instance to reconfigure.</param>
    /// <param name="mode">The cipher block mode to apply.</param>
    protected abstract void SetBlockMode(TAlgorithm algorithm, CipherModeKind mode);

    /// <summary>
    /// Reads the current cipher block mode from <paramref name="algorithm"/>. Mirrors
    /// <see cref="SetBlockMode" /> on the read side so base-class tests can assert
    /// <see cref="CipherModeKind" /> values without depending on a property surface that only exists on
    /// concrete cipher types.
    /// </summary>
    /// <param name="algorithm">The algorithm instance to read from.</param>
    /// <returns>The current <see cref="CipherModeKind" /> value.</returns>
    protected abstract CipherModeKind GetBlockMode(TAlgorithm algorithm);

    /// <summary>
    /// Convenience wrapper around <see cref="SetBlockMode(TAlgorithm, CipherModeKind)" /> that applies
    /// <see cref="CipherModeKind.ECB" />.
    /// </summary>
    /// <param name="algorithm">The algorithm instance to reconfigure.</param>
    protected void SetEcbMode(TAlgorithm algorithm) => SetBlockMode(algorithm, CipherModeKind.ECB);

    /// <summary>
    /// Returns one row per entry in <see cref="SymmetricAlgorithmSpecification.LegalKeySizesBits" /> for use as a
    /// <see cref="DynamicDataAttribute" /> source in parameterised tests.
    /// </summary>
    /// <returns>A sequence of single-element arrays, each containing a key size in bits.</returns>
    public static IEnumerable<object[]> LegalKeySizeBitsData() =>
        new TTest().GetSpecification().LegalKeySizesBits
            .Select(keySize => new object[] { keySize });

    /// <summary>
    /// Returns one row per key-size value, in bits, that <see cref="SymmetricAlgorithm.KeySize" />
    /// must reject. Values are derived from each legal key size and filtered to exclude any size
    /// that happens to be legal for the algorithm under test.
    /// </summary>
    /// <returns>
    /// A sequence of single-element arrays, each containing an invalid key size in bits.
    /// </returns>
    public static IEnumerable<object[]> InvalidKeySizeBitsData()
    {
        SymmetricAlgorithmSpecification spec = new TTest().GetSpecification();
        HashSet<int> legal = [.. spec.LegalKeySizesBits];
        IEnumerable<int> candidates =
            new[] { -1, 0 }.Concat(
                legal.SelectMany(size => new[]
                {
                    size - 1,
                    size + 1,
                    size * 2,
                    size / 2
                }));

        foreach (var candidate in candidates.Distinct().OrderBy(size => size))
        {
            if (!legal.Contains(candidate))
                yield return new object[] { candidate };
        }
    }

    /// <summary>
    /// Returns one row per key-size value, in bytes, that <see cref="SymmetricAlgorithm.KeySize" />
    /// must reject. Values are derived from each legal key size and filtered to exclude any size
    /// that happens to be legal for the algorithm under test.
    /// </summary>
    /// <returns>
    /// A sequence of single-element arrays, each containing an invalid key size in bytes.
    /// </returns>
    public static IEnumerable<object[]> InvalidKeySizeBytesData()
    {
        SymmetricAlgorithmSpecification spec = new TTest().GetSpecification();
        HashSet<int> legal = [.. spec.LegalKeySizesBits.Select(size => size / 8)];
        IEnumerable<int> candidates =
            new[] { -1, 0 }.Concat(
                legal.SelectMany(size => new[]
                {
                    size - 1,
                    size + 1,
                    size * 2,
                    size / 2
                }));

        foreach (var candidate in candidates.Distinct().OrderBy(size => size))
        {
            if (!legal.Contains(candidate))
                yield return new object[] { candidate };
        }
    }

    /// <summary>
    /// Returns one row per bloclk-size value, in bytes, that <see cref="SymmetricAlgorithm.BlockSize" />
    /// must reject. Values are derived from each legal key size and filtered to exclude any size
    /// that happens to be legal for the algorithm under test.
    /// </summary>
    /// <returns>
    /// A sequence of single-element arrays, each containing an invalid block size in bytes.
    /// </returns>
    public static IEnumerable<object[]> InvalidBlockSizeBytesData()
    {
        SymmetricAlgorithmSpecification spec = new TTest().GetSpecification();
        var blockSize = spec.BlockSizeBits / 8;
        foreach (var candidate in new[] { 0, -1, blockSize - 1, blockSize + 1, blockSize * 2, blockSize / 2 })
        {
            if (candidate != blockSize)
                yield return new object[] { candidate };
        }
    }
}
