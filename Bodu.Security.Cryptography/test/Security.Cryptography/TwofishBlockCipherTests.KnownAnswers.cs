// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TwofishBlockCipherTests.KnownAnswers.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Security.Cryptography.Infrastructure;

namespace Bodu.Security.Cryptography;

/// <summary>
/// <see cref="TwofishBlockCipher" /> known-answer test vectors, loaded dynamically from the official Twofish AES
/// submission ECB known-answer test files shipped as embedded resources: the variable-key (<c>ecb_vk.txt</c>),
/// variable-text (<c>ecb_vt.txt</c>), and tables (<c>ecb_tbl.txt</c>) suites. Together these exercise every key bit,
/// every plaintext bit, and the chained MDS / permutation-table sequence across all three key sizes.
/// </summary>
/// <remarks>
/// All vectors target single-block Twofish ECB encryption with no padding or IV. Each row carries its own key,
/// plaintext, and expected ciphertext, so the harness constructs a fresh engine per row. Vectors are partitioned by
/// key size (128 / 192 / 256 bits) to feed the corresponding <see cref="BlockCipherKeyVariant" />.
/// </remarks>
/// <seealso href="https://www.schneier.com/wp-content/uploads/2015/12/twofish-1.zip">Twofish AES submission test-vector diskette (ecb_vk / ecb_vt / ecb_tbl)</seealso>
internal sealed partial class TwofishBlockCipherTests
{
    private static readonly KatProvenance ProfileAesSubmission = KatProvenance.ReferenceImplementation("Twofish AES submission ECB KAT (ecb_vk/ecb_vt/ecb_tbl)");

    private static readonly BlockCipherKnownAnswer[] AllKnownAnswers = LoadAllKnownAnswers();

    private static readonly BlockCipherKnownAnswer[] Key128KnownAnswers = FilterByKeyLength(16);

    private static readonly BlockCipherKnownAnswer[] Key192KnownAnswers = FilterByKeyLength(24);

    private static readonly BlockCipherKnownAnswer[] Key256KnownAnswers = FilterByKeyLength(32);

    /// <summary>
    /// Returns the curated KAT vectors for <paramref name="variant" /> — the full official AES-submission ECB suite
    /// for that key size.
    /// </summary>
    /// <param name="variant">The key-size variant to source vectors for.</param>
    /// <returns>The vectors whose key length matches <paramref name="variant" />.</returns>
    private static IReadOnlyList<BlockCipherKnownAnswer> KnownAnswersFor(BlockCipherKeyVariant variant) => variant switch
    {
        BlockCipherKeyVariant.Key128 => Key128KnownAnswers,
        BlockCipherKeyVariant.Key192 => Key192KnownAnswers,
        BlockCipherKeyVariant.Key256 => Key256KnownAnswers,
        _ => throw new ArgumentOutOfRangeException(nameof(variant), variant, null),
    };

    /// <summary>Loads and concatenates the three embedded AES-submission ECB KAT files.</summary>
    /// <returns>Every Twofish ECB vector across the variable-key, variable-text, and tables suites.</returns>
    private static BlockCipherKnownAnswer[] LoadAllKnownAnswers()
    {
        (string Resource, string Label)[] files =
        [
            ("Bodu.Security.Cryptography.Twofish.ecb_vk.txt", "VK"),
            ("Bodu.Security.Cryptography.Twofish.ecb_vt.txt", "VT"),
            ("Bodu.Security.Cryptography.Twofish.ecb_tbl.txt", "TBL"),
        ];

        var vectors = new List<BlockCipherKnownAnswer>();
        foreach ((string resource, string label) in files)
        {
            using Stream stream = typeof(TwofishBlockCipherTests).Assembly.GetManifestResourceStream(resource)
                ?? throw new InvalidOperationException($"Embedded resource '{resource}' is missing.");

            vectors.AddRange(AesSubmissionKatReader.Read(stream, ProfileAesSubmission, label));
        }

        return [.. vectors];
    }

    /// <summary>Filters <see cref="AllKnownAnswers" /> to the rows whose key is <paramref name="keyLength" /> bytes.</summary>
    /// <param name="keyLength">The key length, in bytes, to select.</param>
    /// <returns>The matching vectors.</returns>
    private static BlockCipherKnownAnswer[] FilterByKeyLength(int keyLength) =>
        [.. AllKnownAnswers.Where(answer => answer.Key!.Length == keyLength)];
}
