// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BlockCipherTests.Ctors.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Reflection;

namespace Bodu.Security.Cryptography;

public abstract partial class BlockCipherTests<TTest, TCipher, TVariant>
{
    /// <summary>
    /// Returns test data covering invalid key lengths for each cipher variant. Always includes an empty key
    /// and a one-byte-short key; also includes a one-byte-over key for tweakable variants, whose key length
    /// is fixed by the specification.
    /// </summary>
    /// <remarks>
    /// A row is emitted for every variant regardless of whether it is tweakable, ensuring the data source
    /// is never empty. Non-tweakable variants are silently skipped inside the test method via
    /// <see cref="Assert.Inconclusive(string)" /> because their key-size exception types vary by cipher
    /// family and are validated in the concrete test class instead.
    /// </remarks>
    public static IEnumerable<object[]> GetInvalidKeySizeCtorData()
    {
        var instance = new TTest();
        foreach (TVariant variant in instance.GetBlockCipherVariants())
        {
            BlockCipherSpecification spec = instance.GetSpecification(variant);
            var validTweak = spec.TestTweak ?? (spec.TweakSize > 0 ? new byte[spec.TweakSize] : Array.Empty<byte>());

            yield return new object[] { variant, Array.Empty<byte>(), validTweak };

            if (spec.KeySize > 1)
                yield return new object[] { variant, new byte[spec.KeySize - 1], validTweak };

            // Over-length keys are only unambiguously invalid when the key length is fixed. Tweakable
            // ciphers in this suite always have a fixed key length that equals the block size, so this
            // path is safe to test for them. Variable-length ciphers (Blowfish, AES, …) may accept
            // KeySize + 1 if that length falls within their legal range, so we skip it for those.
            if (spec.IsTweakable)
                yield return new object[] { variant, new byte[spec.KeySize + 1], validTweak };
        }
    }

    /// <summary>
    /// Returns test data covering invalid tweak lengths for each cipher variant. A sentinel row is emitted
    /// for non-tweakable variants to prevent an empty-data-source error; those rows are skipped inside the
    /// test method via <see cref="Assert.Inconclusive(string)" />.
    /// </summary>
    public static IEnumerable<object[]> GetInvalidTweakSizeCtorData()
    {
        var instance = new TTest();
        foreach (TVariant variant in instance.GetBlockCipherVariants())
        {
            BlockCipherSpecification spec = instance.GetSpecification(variant);
            var validKey = spec.TestKey ?? new byte[spec.KeySize];

            if (!spec.IsTweakable)
            {
                // Sentinel row: the test method will call Assert.Inconclusive immediately.
                yield return new object[] { variant, validKey, Array.Empty<byte>() };
                continue;
            }

            yield return new object[] { variant, validKey, Array.Empty<byte>() };

            if (spec.TweakSize > 1)
                yield return new object[] { variant, validKey, new byte[spec.TweakSize - 1] };

            yield return new object[] { variant, validKey, new byte[spec.TweakSize + 1] };
        }
    }

    /// <summary>Returns a display name for invalid-key-size constructor test rows.</summary>
    public static string GetInvalidKeySizeCtorTestDisplayName(MethodInfo methodInfo, object[] data)
    {
        ArgumentNullException.ThrowIfNull(methodInfo);
        ArgumentNullException.ThrowIfNull(data);

        var variant = (TVariant)data[0];
        var key = (byte[])data[1];
        return $"Key: {key.Length} bytes (Variant: {variant})";
    }

    /// <summary>Returns a display name for invalid-tweak-size constructor test rows.</summary>
    public static string GetInvalidTweakSizeCtorTestDisplayName(MethodInfo methodInfo, object[] data)
    {
        ArgumentNullException.ThrowIfNull(methodInfo);
        ArgumentNullException.ThrowIfNull(data);

        var variant = (TVariant)data[0];
        var tweak = (byte[])data[2];
        return $"Tweak: {tweak.Length} bytes (Variant: {variant})";
    }

    /// <summary>
    /// Verifies that constructing <typeparamref name="TCipher" /> with a key whose length does not match the
    /// expected key size throws <see cref="ArgumentException" />.
    /// </summary>
    /// <remarks>
    /// This assertion applies only to tweakable ciphers, whose key sizes are always fixed and whose
    /// constructors uniformly throw <see cref="ArgumentException" /> for length mismatches. Non-tweakable
    /// ciphers (AES, Blowfish, Camellia, …) vary in both key-size semantics and exception type; their
    /// constructor validation is covered in the concrete test class and this test marks itself
    /// <see cref="Assert.Inconclusive(string)" /> for those variants.
    /// </remarks>
    /// <param name="variant">The cipher variant that determines the expected key size.</param>
    /// <param name="key">A key buffer whose length is invalid for this cipher.</param>
    /// <param name="tweak">
    /// A valid tweak buffer (ignored by non-tweakable ciphers). Provides a well-formed tweak so that the
    /// only rejection reason is the key length.
    /// </param>
    [TestMethod]
    [DynamicData(nameof(GetInvalidKeySizeCtorData), DynamicDataDisplayName = nameof(GetInvalidKeySizeCtorTestDisplayName))]
    public void Ctor_WhenKeyIsWrongSize_ShouldThrowExactly(TVariant variant, byte[] key, byte[] tweak)
    {
        BlockCipherSpecification spec = GetSpecification(variant);
        if (!spec.IsTweakable)
        {
            Assert.Inconclusive(
                $"[{variant}] Non-tweakable cipher; key-size exception type varies by cipher family " +
                "and is validated in the concrete test class.");
            return;
        }

        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = CreateBlockCipherForAnswer(new BlockCipherKnownAnswer
            {
                Name = "ctor-invalid-key",
                Plaintext = Array.Empty<byte>(),
                Ciphertext = Array.Empty<byte>(),
                Key = key,
                Tweak = tweak.Length > 0 ? tweak : null,
            });
        });
    }

    /// <summary>
    /// Verifies that constructing <typeparamref name="TCipher" /> with a tweak whose length does not match
    /// the expected tweak size throws <see cref="ArgumentException" />. Non-tweakable variants are marked
    /// inconclusive because tweak validation is not applicable to them.
    /// </summary>
    /// <param name="variant">The cipher variant that determines the expected tweak size.</param>
    /// <param name="key">A valid key buffer. Provides a well-formed key so that the only rejection reason
    /// is the tweak length.</param>
    /// <param name="tweak">A tweak buffer whose length is invalid for this cipher.</param>
    [TestMethod]
    [DynamicData(nameof(GetInvalidTweakSizeCtorData), DynamicDataDisplayName = nameof(GetInvalidTweakSizeCtorTestDisplayName))]
    public void Ctor_WhenTweakIsWrongSize_ShouldThrowExactly(TVariant variant, byte[] key, byte[] tweak)
    {
        BlockCipherSpecification spec = GetSpecification(variant);
        if (!spec.IsTweakable)
        {
            Assert.Inconclusive($"[{variant}] Non-tweakable cipher; tweak validation is not applicable.");
            return;
        }

        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = CreateBlockCipherForAnswer(new BlockCipherKnownAnswer
            {
                Name = "ctor-invalid-tweak",
                Plaintext = Array.Empty<byte>(),
                Ciphertext = Array.Empty<byte>(),
                Key = key,
                Tweak = tweak,
            });
        });
    }
}
