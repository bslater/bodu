// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SymmetricAlgorithmTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

/// <summary>
/// Provides unit tests for symmetric algorithms to verify encryption, decryption, and property behaviors.
/// </summary>
/// <typeparam name="TAlgorithm">The type of symmetric algorithm under test.</typeparam>
[TestClass]
public abstract partial class SymmetricAlgorithmTests<TAlgorithm>
    where TAlgorithm : System.Security.Cryptography.SymmetricAlgorithm
{
    /// <summary>
    /// Creates an instance of the symmetric algorithm under test.
    /// </summary>
    /// <returns>An instance of the symmetric algorithm.</returns>
    protected abstract TAlgorithm CreateAlgorithm();
}
