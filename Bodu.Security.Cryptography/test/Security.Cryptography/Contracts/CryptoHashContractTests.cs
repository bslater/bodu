// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CryptoHashContractTests.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Security.Cryptography.Infrastructure;

namespace Bodu.Security.Cryptography.Contracts;

/// <summary>
/// Reusable behavioural contract test base for a cryptographic hash, keyed hash, or extendable-output
/// function (XOF). Concrete subclasses override <see cref="ComputeHash" /> and <see cref="KnownAnswers" />
/// to plug their algorithm into the inherited tests.
/// </summary>
/// <typeparam name="THash">The hash type under test.</typeparam>
public abstract class CryptoHashContractTests<THash>
{
    /// <summary>
    /// Computes the algorithm's digest over <paramref name="input" />, requesting
    /// <paramref name="outputLengthBytes" /> bytes of output (for XOFs) and optionally consuming a key and
    /// customisation string.
    /// </summary>
    /// <param name="input">The raw byte payload.</param>
    /// <param name="outputLengthBytes">The requested digest length in bytes.</param>
    /// <param name="key">An optional key for keyed-hash variants.</param>
    /// <param name="customization">An optional personalisation / customisation string.</param>
    /// <returns>The computed digest bytes.</returns>
    protected abstract byte[] ComputeHash(byte[] input, int outputLengthBytes, byte[]? key, byte[]? customization);

    /// <summary>
    /// Returns the known-answer vectors for the algorithm under test.
    /// </summary>
    protected abstract IReadOnlyList<CryptoHashKat> KnownAnswers { get; }

    /// <summary>
    /// Verifies that every <see cref="CryptoHashKat" /> row reproduces its expected digest.
    /// </summary>
    [TestMethod]
    public void HashData_WhenKnownAnswer_ShouldMatchDigest()
    {
        foreach (CryptoHashKat kat in KnownAnswers)
        {
            byte[] actual = ComputeHash(kat.Input, kat.OutputLengthBytes, kat.Key, kat.Customization);

            CollectionAssert.AreEqual(
                kat.ExpectedDigest,
                actual,
                $"KAT '{kat.Name}': hash produced unexpected digest.");
        }
    }

    /// <summary>
    /// Verifies that the digest length matches the requested output length for every
    /// <see cref="CryptoHashKat" /> row. Documents that XOFs and variable-length hashes honour their
    /// requested output size.
    /// </summary>
    [TestMethod]
    public void HashData_WhenOutputLengthRequested_ShouldReturnExpectedLength()
    {
        foreach (CryptoHashKat kat in KnownAnswers)
        {
            byte[] actual = ComputeHash(kat.Input, kat.OutputLengthBytes, kat.Key, kat.Customization);

            Assert.AreEqual(
                kat.OutputLengthBytes,
                actual.Length,
                $"KAT '{kat.Name}': digest length differs from requested OutputLengthBytes.");
        }
    }
}
