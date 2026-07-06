// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BlowfishBlockCipherTests.KnownAnswers.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Security.Cryptography.Infrastructure;
namespace Bodu.Security.Cryptography;

/// <summary>
/// <see cref="BlowfishBlockCipher" /> known-answer test vectors, loaded dynamically from Eric Young's published
/// Blowfish reference test set (shipped as an embedded resource). This is the canonical reference for the 16-round
/// Blowfish ECB cipher, covering the variable-key triple table and the growing-key set-key sequence.
/// </summary>
/// <remarks>
/// All vectors target standard 16-round Blowfish in raw single-block ECB mode. The variable-key table uses 64-bit
/// keys; the set-key sequence grows the key one byte at a time from the fixed <c>FEDCBA9876543210</c> plaintext. Eric
/// Young's <c>k[1]</c>–<c>k[3]</c> rows use 1–3 byte keys, which are below Blowfish's 32-bit specification minimum that
/// <see cref="BlowfishBlockCipher" /> enforces, so those three rows are skipped by the reader.
/// </remarks>
/// <seealso href="https://www.schneier.com/wp-content/uploads/2015/12/vectors-2.txt">vectors-2.txt — Eric Young Blowfish vectors</seealso>
internal sealed partial class BlowfishBlockCipherTests
{
    private static readonly KatProvenance ProfileSchneierVectors = KatProvenance.ReferenceImplementation("Eric Young / Schneier Blowfish vectors");

    private const string BlowfishEricYoungResourceName = "Bodu.Security.Cryptography.Blowfish.eric-young-vectors.txt";

    private static readonly BlockCipherKnownAnswer[] DefaultKnownAnswers = LoadKnownAnswers();

    /// <summary>
    /// Returns the KAT vectors for <paramref name="variant" />. Blowfish exposes a single configuration in this test
    /// suite (variable key, 64-bit block), so the variant parameter is reserved for parity with other cipher families
    /// and always yields the full Eric Young set.
    /// </summary>
    /// <param name="variant">The (single) cipher variant.</param>
    /// <returns>Every loaded Eric Young vector.</returns>
    private static IReadOnlyList<BlockCipherKnownAnswer> KnownAnswersFor(SingleTestVariant variant) => DefaultKnownAnswers;

    /// <summary>Loads Eric Young's Blowfish vectors from the embedded reference file.</summary>
    /// <returns>The variable-key and in-range set-key ECB vectors.</returns>
    /// <exception cref="InvalidOperationException">The embedded resource cannot be located.</exception>
    private static BlockCipherKnownAnswer[] LoadKnownAnswers()
    {
        using Stream stream = typeof(BlowfishBlockCipherTests).Assembly.GetManifestResourceStream(BlowfishEricYoungResourceName)
            ?? throw new InvalidOperationException($"Embedded resource '{BlowfishEricYoungResourceName}' is missing.");

        return [.. BlowfishEricYoungKatReader.Read(stream, ProfileSchneierVectors)];
    }
}
