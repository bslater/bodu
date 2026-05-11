// ---------------------------------------------------------------------------------------------------------------
// <copyright file="KeyedBlockHashAlgorithmTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

/// <summary>
/// Provides a reusable base class for verifying the correctness of
/// <see cref="KeyedBlockHashAlgorithm{T}" /> implementations.
/// </summary>
/// <typeparam name="TTest">The concrete test type inheriting this class.</typeparam>
/// <typeparam name="TAlgorithm">
/// The keyed block hash algorithm under test. Must derive from
/// <see cref="KeyedBlockHashAlgorithm{TAlgorithm}" /> and expose a public parameterless constructor.
/// </typeparam>
/// <typeparam name="TVariant">The enumeration type used to represent algorithm configuration variants.</typeparam>
/// <remarks>
/// Extends <see cref="BlockHashAlgorithmTests{TTest, TAlgorithm, TVariant}" /> with test logic that
/// is specific to keyed algorithms — key retention, defensive copying, legal key length boundaries,
/// and interaction between key assignment and hashing state.
/// </remarks>
public abstract partial class KeyedBlockHashAlgorithmTests<TTest, TAlgorithm, TVariant>
    : BlockHashAlgorithmTests<TTest, TAlgorithm, TVariant>
    where TTest : KeyedBlockHashAlgorithmTests<TTest, TAlgorithm, TVariant>, new()
    where TAlgorithm : KeyedBlockHashAlgorithm<TAlgorithm>, new()
    where TVariant : struct, Enum
{
    /// <summary>
    /// Creates a new instance of the hash algorithm under test, preconfigured with a deterministic key.
    /// </summary>
    /// <returns>
    /// A newly constructed instance of <typeparamref name="TAlgorithm" />, initialised with the
    /// result of <see cref="GetDeterministicKey" /> to ensure repeatable hash output across runs.
    /// </returns>
    /// <remarks>
    /// Override this method if the algorithm under test requires special construction (for example,
    /// constructor parameters, clamping, or post-initialisation setup).
    /// </remarks>
    protected override TAlgorithm CreateAlgorithm() =>
        new TAlgorithm
        {
            Key = ((KeyedAlgorithmSpecification)GetSpecification(DefaultVariant)).TestKey
        };

    /// <summary>
    /// Creates a new instance of the hash algorithm under test using the specified key.
    /// </summary>
    /// <param name="key">
    /// The key to assign to the newly constructed algorithm instance.
    /// </param>
    /// <returns>
    /// A newly constructed instance of <typeparamref name="TAlgorithm" />, initialised with
    /// <paramref name="key" />.
    /// </returns>
    /// <remarks>
    /// Override this method if the algorithm under test requires special construction, such as constructor
    /// parameters, key clamping, key normalisation, or post-initialisation setup.
    /// </remarks>
    protected virtual TAlgorithm CreateAlgorithm(byte[] key) =>
        new TAlgorithm
        {
            Key = key
        };

    /// <summary>
    /// Creates a new instance of the algorithm for the specified <paramref name="variant" />.
    /// </summary>
    /// <param name="variant">The variant to instantiate.</param>
    /// <returns>A new instance of <typeparamref name="TAlgorithm" /> configured for the given variant.</returns>
    protected virtual TAlgorithm CreateAlgorithm(TVariant variant, byte[] key)
    {
        TAlgorithm algorithm = CreateAlgorithm(variant);
        algorithm.Key = key;
        return algorithm;
    }

    /// <summary>
    /// Generates a unique, valid cryptographic key for use with the current algorithm under test.
    /// </summary>
    /// <returns>A non-null, non-empty <see cref="byte" /> array containing a randomly generated key suitable for the algorithm.</returns>
    /// <remarks>
    /// Used by test cases that verify key-dependent behaviour, such as confirming that different
    /// keys yield different hash outputs or that key isolation is preserved across instances.
    /// </remarks>
    protected virtual byte[] GenerateUniqueKey(int size)
    {
        var key = new byte[size];
        CryptoHelpers.FillWithRandomNonZeroBytes(key);
        return key;
    }
}
