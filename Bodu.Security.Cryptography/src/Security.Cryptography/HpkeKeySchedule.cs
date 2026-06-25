// ---------------------------------------------------------------------------------------------------------------
// <copyright file="HpkeKeySchedule.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Implements the HPKE key schedule of RFC 9180 §5.1, which combines a KEM shared secret with the establishment mode,
/// the application <c>info</c>, and an optional pre-shared key to produce the AEAD key, base nonce, and exporter secret
/// of an <see cref="HpkeContext" />.
/// </summary>
internal static class HpkeKeySchedule
{
    /// <summary>
    /// Runs the key schedule and produces a ready-to-use <see cref="HpkeContext" />.
    /// </summary>
    /// <param name="suite">The cipher suite.</param>
    /// <param name="mode">The establishment mode.</param>
    /// <param name="sharedSecret">The KEM shared secret.</param>
    /// <param name="info">The application-supplied context information.</param>
    /// <param name="psk">The pre-shared key, or empty when none is used.</param>
    /// <param name="pskId">The pre-shared key identifier, or empty when none is used.</param>
    /// <returns>The derived encryption context.</returns>
    /// <exception cref="CryptographicException">
    /// The PSK inputs are inconsistent with each other or with <paramref name="mode" />.
    /// </exception>
    public static HpkeContext Create(HpkeSuite suite, HpkeMode mode, ReadOnlySpan<byte> sharedSecret, ReadOnlySpan<byte> info, ReadOnlySpan<byte> psk, ReadOnlySpan<byte> pskId)
    {
        VerifyPskInputs(mode, psk, pskId);

        HashAlgorithmName hash = suite.KdfHashAlgorithm;
        ReadOnlySpan<byte> suiteId = suite.SuiteId;

        byte[] pskIdHash = HpkeLabeledKdf.LabeledExtract(hash, suiteId, default, "psk_id_hash"u8, pskId);
        byte[] infoHash = HpkeLabeledKdf.LabeledExtract(hash, suiteId, default, "info_hash"u8, info);

        // key_schedule_context = mode ‖ psk_id_hash ‖ info_hash
        byte[] keyScheduleContext = new byte[1 + pskIdHash.Length + infoHash.Length];
        keyScheduleContext[0] = (byte)mode;
        pskIdHash.CopyTo(keyScheduleContext, 1);
        infoHash.CopyTo(keyScheduleContext, 1 + pskIdHash.Length);

        // secret = LabeledExtract(shared_secret, "secret", psk)
        byte[] secret = HpkeLabeledKdf.LabeledExtract(hash, suiteId, sharedSecret, "secret"u8, psk);

        byte[] key = new byte[suite.AeadKeySizeInBytes];
        byte[] baseNonce = new byte[suite.AeadNonceSizeInBytes];
        byte[] exporterSecret = new byte[suite.KdfHashLengthInBytes];

        try
        {
            HpkeLabeledKdf.LabeledExpand(hash, suiteId, secret, "key"u8, keyScheduleContext, key);
            HpkeLabeledKdf.LabeledExpand(hash, suiteId, secret, "base_nonce"u8, keyScheduleContext, baseNonce);
            HpkeLabeledKdf.LabeledExpand(hash, suiteId, secret, "exp"u8, keyScheduleContext, exporterSecret);

            return new HpkeContext(suite, key, baseNonce, exporterSecret);
        }
        catch
        {
            CryptographyHelper.Clear(key);
            CryptographyHelper.Clear(baseNonce);
            CryptographyHelper.Clear(exporterSecret);
            throw;
        }
        finally
        {
            CryptographyHelper.Clear(secret);
        }
    }

    /// <summary>
    /// Validates the pre-shared key inputs against each other and the establishment mode (RFC 9180 §5.1
    /// <c>VerifyPSKInputs</c>).
    /// </summary>
    /// <param name="mode">The establishment mode.</param>
    /// <param name="psk">The pre-shared key, or empty when none is used.</param>
    /// <param name="pskId">The pre-shared key identifier, or empty when none is used.</param>
    /// <exception cref="CryptographicException">
    /// Exactly one of <paramref name="psk" /> and <paramref name="pskId" /> is supplied, or their presence does not
    /// match <paramref name="mode" />.
    /// </exception>
    private static void VerifyPskInputs(HpkeMode mode, ReadOnlySpan<byte> psk, ReadOnlySpan<byte> pskId)
    {
        bool gotPsk = !psk.IsEmpty;
        bool gotPskId = !pskId.IsEmpty;

        if (gotPsk != gotPskId)
            throw new CryptographicException(CryptoResourceStrings.Crypt_Invalid_HpkePskInputs);

        bool modeUsesPsk = mode is HpkeMode.Psk or HpkeMode.AuthPsk;
        if (gotPsk != modeUsesPsk)
            throw new CryptographicException(CryptoResourceStrings.Crypt_Invalid_HpkePskMode);
    }
}
