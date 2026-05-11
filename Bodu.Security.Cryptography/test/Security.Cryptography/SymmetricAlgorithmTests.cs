// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SymmetricAlgorithmTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

/// <summary>
/// Provides unit tests for symmetric algorithms to verify encryption, decryption, and property behaviours.
/// </summary>
/// <typeparam name="TTest">The concrete test class, used to resolve specification data for <see cref="DynamicDataAttribute" /> sources.</typeparam>
/// <typeparam name="TAlgorithm">The type of symmetric algorithm under test.</typeparam>
[TestClass]
public abstract partial class SymmetricAlgorithmTests<TTest, TAlgorithm>
    where TTest : SymmetricAlgorithmTests<TTest, TAlgorithm>, new()
    where TAlgorithm : System.Security.Cryptography.SymmetricAlgorithm
{
    /// <summary>
    /// Creates an instance of the symmetric algorithm under test.
    /// </summary>
    /// <returns>An instance of the symmetric algorithm.</returns>
    protected abstract TAlgorithm CreateAlgorithm();

    /// <summary>
    /// Returns the <see cref="SymmetricAlgorithmSpecification" /> describing the expected observable properties of
    /// <typeparamref name="TAlgorithm" />.
    /// </summary>
    /// <returns>A <see cref="SymmetricAlgorithmSpecification" /> instance populated with the expected values.</returns>
    protected abstract SymmetricAlgorithmSpecification GetSpecification();

    /// <summary>
    /// Configures <paramref name="algorithm"/> to use ECB mode. Used by base-class tests that exercise the
    /// ECB-specific null-IV contract.
    /// </summary>
    /// <param name="algorithm">The algorithm instance to reconfigure.</param>
    protected abstract void SetEcbMode(TAlgorithm algorithm);

    /// <summary>
    /// Returns one row per entry in <see cref="SymmetricAlgorithmSpecification.LegalKeySizesBits" /> for use as a
    /// <see cref="DynamicDataAttribute" /> source in parameterised tests.
    /// </summary>
    /// <returns>A sequence of single-element arrays, each containing a key size in bits.</returns>
    public static IEnumerable<object[]> LegalKeySizesBitsData() =>
        new TTest().GetSpecification().LegalKeySizesBits.Select(k => new object[] { k });
}
