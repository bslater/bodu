// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SymmetricAlgorithmExtensionTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography.Extensions;

/// <summary>
/// Tests covering the <see cref="SymmetricAlgorithmExtensions" /> surface — including the
/// synchronous and asynchronous <c>Encrypt</c>/<c>Decrypt</c> families and the
/// <c>TryCreateEncryptor</c>/<c>TryCreateDecryptor</c> helpers. Tests for each method live in
/// their own partial file.
/// </summary>
/// <remarks>
/// <para>
/// Tests use <see cref="SimpleReversingSymmetricAlgorithm" />, a deterministic test-infrastructure
/// algorithm whose "encryption" simply reverses the input byte stream. This keeps the fixtures
/// independent of any real cipher implementation while still exercising the extension layer's
/// buffer, stream, and padding paths.
/// </para>
/// <para>
/// This file holds only shared fixtures and helpers. All test methods are declared in per-method
/// partial files (for example <c>SymmetricAlgorithmExtensionTests_EncryptAsync.cs</c>).
/// </para>
/// </remarks>
[TestClass]
public partial class SymmetricAlgorithmExtensionTests
{
    /// <summary>
    /// Provides a set of <see cref="SymmetricAlgorithm" /> instances for data-driven tests that
    /// need to verify behaviour across multiple concrete algorithms.
    /// </summary>
    /// <returns>
    /// A sequence of single-element object arrays, each containing a fresh
    /// <see cref="SymmetricAlgorithm" /> instance.
    /// </returns>
    /// <remarks>
    /// Use via <c>[DynamicData(nameof(SymmetricAlgorithmTestData))]</c> on tests that should run
    /// against both the in-house reversing algorithm and a real one (AES).
    /// </remarks>
    public static IEnumerable<object[]> SymmetricAlgorithmTestData()
    {
        yield return new object[] { SimpleReversingSymmetricAlgorithm.Create() };
        yield return new object[] { Aes.Create() };
    }

    /// <summary>
    /// Creates a fresh <see cref="SimpleReversingSymmetricAlgorithm" /> instance for a single
    /// test.
    /// </summary>
    /// <returns>A new <see cref="SymmetricAlgorithm" /> with default key/IV.</returns>
    private static SymmetricAlgorithm CreateAlgorithm() => new SimpleReversingSymmetricAlgorithm();
}
