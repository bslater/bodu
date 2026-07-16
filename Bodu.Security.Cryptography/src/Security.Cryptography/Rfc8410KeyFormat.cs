// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Rfc8410KeyFormat.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Formats.Asn1;
using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Encodes and decodes the RFC 8410 SubjectPublicKeyInfo and PKCS#8 OneAsymmetricKey structures for the 32-byte
/// Curve25519 / Edwards25519 keys, parameterized by the algorithm's object identifier (X25519 = 1.3.101.110, Ed25519 =
/// 1.3.101.112).
/// </summary>
/// <remarks>
/// RFC 8410 §4 defines the public key as a raw key encoded directly into the SubjectPublicKeyInfo BIT STRING with an
/// absent <c>parameters</c> field, and §7 defines the private key as the raw seed wrapped in a
/// <c>CurvePrivateKey ::= OCTET STRING</c> placed inside the OneAsymmetricKey <c>privateKey</c> OCTET STRING.
/// </remarks>
internal static class Rfc8410KeyFormat
{
    /// <summary>
    /// Encodes <paramref name="publicKey" /> as an RFC 8410 SubjectPublicKeyInfo for the algorithm
    /// <paramref name="oid" />.
    /// </summary>
    /// <param name="oid">The algorithm object identifier.</param>
    /// <param name="publicKey">The raw 32-byte public key.</param>
    /// <returns>The DER-encoded SubjectPublicKeyInfo.</returns>
    public static byte[] WriteSubjectPublicKeyInfo(string oid, ReadOnlySpan<byte> publicKey)
    {
        var writer = new AsnWriter(AsnEncodingRules.DER);
        using (writer.PushSequence())
        {
            using (writer.PushSequence())
                writer.WriteObjectIdentifier(oid);

            writer.WriteBitString(publicKey);
        }

        return writer.Encode();
    }

    /// <summary>
    /// Encodes <paramref name="seed" /> as an RFC 8410 PKCS#8 OneAsymmetricKey (version 1) for the algorithm
    /// <paramref name="oid" />.
    /// </summary>
    /// <param name="oid">The algorithm object identifier.</param>
    /// <param name="seed">The raw 32-byte private seed.</param>
    /// <returns>The DER-encoded PKCS#8 private key.</returns>
    public static byte[] WritePkcs8PrivateKey(string oid, ReadOnlySpan<byte> seed)
    {
        // The privateKey field carries CurvePrivateKey ::= OCTET STRING, so the seed is wrapped in an inner OCTET STRING.
        var inner = new AsnWriter(AsnEncodingRules.DER);
        inner.WriteOctetString(seed);
        byte[] innerEncoded = inner.Encode();

        try
        {
            var writer = new AsnWriter(AsnEncodingRules.DER);
            using (writer.PushSequence())
            {
                writer.WriteInteger(0);

                using (writer.PushSequence())
                    writer.WriteObjectIdentifier(oid);

                writer.WriteOctetString(innerEncoded);
            }

            return writer.Encode();
        }
        finally
        {
            CryptographyHelper.Clear(innerEncoded);
        }
    }

    /// <summary>
    /// Decodes the raw public key from an RFC 8410 SubjectPublicKeyInfo, validating the algorithm identifier.
    /// </summary>
    /// <param name="expectedOid">The algorithm object identifier the structure must declare.</param>
    /// <param name="source">The DER-encoded SubjectPublicKeyInfo, possibly with trailing data.</param>
    /// <param name="bytesRead">The number of bytes consumed from <paramref name="source" />.</param>
    /// <returns>The raw public key.</returns>
    /// <exception cref="CryptographicException">
    /// The structure is malformed or declares a different algorithm.
    /// </exception>
    public static byte[] ReadSubjectPublicKeyInfo(string expectedOid, ReadOnlySpan<byte> source, out int bytesRead)
    {
        try
        {
            var outer = new AsnReader(source.ToArray(), AsnEncodingRules.DER);
            ReadOnlyMemory<byte> encoded = outer.PeekEncodedValue();

            AsnReader spki = outer.ReadSequence();
            ReadAlgorithmIdentifier(spki, expectedOid);

            byte[] publicKey = spki.ReadBitString(out int unusedBitCount);
            if (unusedBitCount != 0)
                throw new CryptographicException(CryptoResourceStrings.Crypt_Invalid_Rfc8410KeyFormat);

            spki.ThrowIfNotEmpty();

            bytesRead = encoded.Length;
            return publicKey;
        }
        catch (AsnContentException ex)
        {
            throw new CryptographicException(CryptoResourceStrings.Crypt_Invalid_Rfc8410KeyFormat, ex);
        }
    }

    /// <summary>
    /// Decodes the raw private seed from an RFC 8410 PKCS#8 OneAsymmetricKey, validating the algorithm identifier.
    /// </summary>
    /// <param name="expectedOid">The algorithm object identifier the structure must declare.</param>
    /// <param name="source">The DER-encoded PKCS#8 private key, possibly with trailing data.</param>
    /// <param name="bytesRead">The number of bytes consumed from <paramref name="source" />.</param>
    /// <returns>The raw private seed.</returns>
    /// <exception cref="CryptographicException">
    /// The structure is malformed or declares a different algorithm.
    /// </exception>
    public static byte[] ReadPkcs8PrivateKey(string expectedOid, ReadOnlySpan<byte> source, out int bytesRead)
    {
        // Copy the source so the ASN.1 reader has a stable buffer; this copy embeds the private seed, so it is
        // zeroed in the finally rather than abandoned to the garbage collector.
        byte[] sourceCopy = source.ToArray();
        try
        {
            var outer = new AsnReader(sourceCopy, AsnEncodingRules.DER);
            ReadOnlyMemory<byte> encoded = outer.PeekEncodedValue();

            AsnReader pkcs8 = outer.ReadSequence();

            // version is 0 (v1) or 1 (v2, which additionally permits an attached public key); both are accepted.
            if (!pkcs8.TryReadInt32(out int version) || version is not (0 or 1))
                throw new CryptographicException(CryptoResourceStrings.Crypt_Invalid_Rfc8410KeyFormat);

            ReadAlgorithmIdentifier(pkcs8, expectedOid);

            byte[] curvePrivateKey = pkcs8.ReadOctetString();
            try
            {
                var inner = new AsnReader(curvePrivateKey, AsnEncodingRules.DER);
                byte[] seed = inner.ReadOctetString();
                inner.ThrowIfNotEmpty();

                bytesRead = encoded.Length;
                return seed;
            }
            finally
            {
                CryptographyHelper.Clear(curvePrivateKey);
            }
        }
        catch (AsnContentException ex)
        {
            throw new CryptographicException(CryptoResourceStrings.Crypt_Invalid_Rfc8410KeyFormat, ex);
        }
        finally
        {
            CryptographyHelper.Clear(sourceCopy);
        }
    }

    /// <summary>
    /// Reads and validates the AlgorithmIdentifier SEQUENCE, requiring the expected OID and an absent parameters field.
    /// </summary>
    /// <param name="parent">The reader positioned at the AlgorithmIdentifier SEQUENCE.</param>
    /// <param name="expectedOid">The object identifier the AlgorithmIdentifier must declare.</param>
    /// <exception cref="CryptographicException">
    /// The algorithm identifier is absent, has parameters, or differs.
    /// </exception>
    private static void ReadAlgorithmIdentifier(AsnReader parent, string expectedOid)
    {
        AsnReader algorithm = parent.ReadSequence();
        string oid = algorithm.ReadObjectIdentifier();

        // RFC 8410 §3: the parameters field is absent for these algorithms.
        if (!string.Equals(oid, expectedOid, StringComparison.Ordinal) || algorithm.HasData)
            throw new CryptographicException(CryptoResourceStrings.Crypt_Invalid_Rfc8410KeyFormat);
    }
}
