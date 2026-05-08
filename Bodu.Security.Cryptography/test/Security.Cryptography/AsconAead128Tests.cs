// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AsconAead128Tests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Contains unit tests for the <see cref="AsconAead128" /> authenticated encryption algorithm.
/// Tests are partitioned across the following partial files:
/// <list type="bullet">
/// <item><description><c>AsconAead128Tests.Encrypt.cs</c> — constructor validation, <see cref="AsconAead128.Encrypt" /> argument handling, and output-size verification.</description></item>
/// <item><description><c>AsconAead128Tests.Decrypt.cs</c> — <see cref="AsconAead128.Decrypt" /> argument handling, tag-mismatch detection, and round-trip correctness.</description></item>
/// </list>
/// </summary>
[TestClass]
public partial class AsconAead128Tests
{
    private static readonly byte[] ValidKey = new byte[AsconAead128.KeyBytes];
    private static readonly byte[] ValidNonce = new byte[AsconAead128.NonceBytes];

    static AsconAead128Tests()
    {
        for (int i = 0; i < ValidKey.Length; i++) ValidKey[i] = (byte)i;
        for (int i = 0; i < ValidNonce.Length; i++) ValidNonce[i] = (byte)(i + 0x10);
    }

    private static readonly string[] DisposableFieldExclusions =
    [
        // Disposal state flags are allowed to remain non-default.
        "_disposed",

        // The completion flag is expected to become true once the instance is no longer reusable.
        "_completed",

        // AAD lifecycle bookkeeping is not sensitive key/state material.
        "_aadProcessed"
    ];

    /// <summary>
    /// Enumerates the private instance fields that should be zeroed or reset after disposal.
    /// </summary>
    /// <returns>
    /// A sequence of fields to inspect after <see cref="AsconAead128.Dispose" /> has completed.
    /// </returns>
    public static IEnumerable<object[]> GetDisposableFields() =>
        TestHelpers.GetFieldInfoForType<AsconAead128>(
            excludeReadOnly: false,
            excludeFileds: DisposableFieldExclusions);

    /// <summary>
    /// Creates a fresh <see cref="AsconAead128" /> instance using the shared test key and nonce,
    /// with associated data already processed. Most tests use this helper to obtain an
    /// encryption-ready instance.
    /// </summary>
    /// <param name="aad">Optional associated data. Defaults to an empty span.</param>
    /// <returns>An <see cref="AsconAead128" /> instance ready for encryption or decryption.</returns>
    private static AsconAead128 MakeInstance(ReadOnlySpan<byte> aad = default)
    {
        AsconAead128 instance = new AsconAead128(ValidKey, ValidNonce);
        instance.ProcessAssociatedData(aad);
        return instance;
    }
}
