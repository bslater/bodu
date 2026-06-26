// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Ed25519.KeyFormats.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Surfaces the Ed25519 algorithm semantics, implements the RFC 8410 PKCS#8 / SubjectPublicKeyInfo (and PEM) key
/// formats over the inherited members, and deliberately rejects the unsupported XML and encrypted-PKCS#8 formats.
/// </summary>
public sealed partial class Ed25519
{
    /// <summary>The RFC 8410 object identifier for Ed25519.</summary>
    private const string KeyFormatOid = "1.3.101.112";

    /// <summary>
    /// Gets the algorithm name <c>"Ed25519"</c>.
    /// </summary>
    /// <value>The constant string <c>"Ed25519"</c>.</value>
    [SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "Instance property for parity with the parameter-set algorithms, where the value is instance-dependent.")]
    public string AlgorithmName =>
        "Ed25519";

    /// <summary>
    /// Gets the approximate classical security strength of Ed25519, in bits.
    /// </summary>
    /// <value>The value 128.</value>
    [SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "Instance property for parity with the parameter-set algorithms, where the value is instance-dependent.")]
    public int SecurityStrengthBits =>
        128;

    /// <inheritdoc />
    public override void ImportSubjectPublicKeyInfo(ReadOnlySpan<byte> source, out int bytesRead)
    {
        ThrowIfDisposed();
        byte[] publicKey = Rfc8410KeyFormat.ReadSubjectPublicKeyInfo(KeyFormatOid, source, out bytesRead);

        if (publicKey.Length != PublicKeySizeInBytes)
            throw new CryptographicException(CryptoResourceStrings.Crypt_Invalid_Rfc8410KeyFormat);

        ImportPublicKey(publicKey);
    }

    /// <inheritdoc />
    public override void ImportPkcs8PrivateKey(ReadOnlySpan<byte> source, out int bytesRead)
    {
        ThrowIfDisposed();
        byte[] seed = Rfc8410KeyFormat.ReadPkcs8PrivateKey(KeyFormatOid, source, out bytesRead);

        try
        {
            if (seed.Length != PrivateKeySizeInBytes)
                throw new CryptographicException(CryptoResourceStrings.Crypt_Invalid_Rfc8410KeyFormat);

            ImportPrivateKey(seed);
        }
        finally
        {
            CryptographyHelper.Clear(seed);
        }
    }

    /// <inheritdoc />
    public override bool TryExportSubjectPublicKeyInfo(Span<byte> destination, out int bytesWritten)
    {
        byte[] subjectPublicKeyInfo = Rfc8410KeyFormat.WriteSubjectPublicKeyInfo(KeyFormatOid, ExportPublicKey());
        return TryCopyTo(subjectPublicKeyInfo, destination, out bytesWritten);
    }

    /// <inheritdoc />
    public override bool TryExportPkcs8PrivateKey(Span<byte> destination, out int bytesWritten)
    {
        byte[] seed = ExportPrivateKey();
        byte[] pkcs8 = Rfc8410KeyFormat.WritePkcs8PrivateKey(KeyFormatOid, seed);

        try
        {
            return TryCopyTo(pkcs8, destination, out bytesWritten);
        }
        finally
        {
            CryptographyHelper.Clear(seed);
            CryptographyHelper.Clear(pkcs8);
        }
    }

    /// <inheritdoc />
    public override void FromXmlString(string xmlString) =>
        throw KeyFormatNotSupported();

    /// <inheritdoc />
    public override string ToXmlString(bool includePrivateParameters) =>
        throw KeyFormatNotSupported();

    /// <inheritdoc />
    public override void ImportEncryptedPkcs8PrivateKey(ReadOnlySpan<byte> passwordBytes, ReadOnlySpan<byte> source, out int bytesRead) =>
        throw KeyFormatNotSupported();

    /// <inheritdoc />
    public override void ImportEncryptedPkcs8PrivateKey(ReadOnlySpan<char> password, ReadOnlySpan<byte> source, out int bytesRead) =>
        throw KeyFormatNotSupported();

    /// <inheritdoc />
    public override bool TryExportEncryptedPkcs8PrivateKey(ReadOnlySpan<byte> passwordBytes, PbeParameters pbeParameters, Span<byte> destination, out int bytesWritten) =>
        throw KeyFormatNotSupported();

    /// <inheritdoc />
    public override bool TryExportEncryptedPkcs8PrivateKey(ReadOnlySpan<char> password, PbeParameters pbeParameters, Span<byte> destination, out int bytesWritten) =>
        throw KeyFormatNotSupported();

    /// <summary>
    /// Copies <paramref name="source" /> into <paramref name="destination" /> when it fits, following the BCL
    /// try-export buffer-probing convention.
    /// </summary>
    /// <param name="source">The encoded key bytes.</param>
    /// <param name="destination">The caller's destination span.</param>
    /// <param name="bytesWritten">The number of bytes written, or 0 when the destination is too small.</param>
    /// <returns><see langword="true" /> when the bytes were written; otherwise, <see langword="false" />.</returns>
    private static bool TryCopyTo(byte[] source, Span<byte> destination, out int bytesWritten)
    {
        if (destination.Length < source.Length)
        {
            bytesWritten = 0;
            return false;
        }

        source.CopyTo(destination);
        bytesWritten = source.Length;
        return true;
    }

    /// <summary>
    /// Creates the exception thrown by the unsupported XML and encrypted-PKCS#8 key-format members.
    /// </summary>
    /// <returns>A <see cref="NotSupportedException" /> naming the supported formats.</returns>
    private static NotSupportedException KeyFormatNotSupported() =>
        new(string.Format(CultureInfo.CurrentCulture, CryptoResourceStrings.Op_NotSupported_XmlAndEncryptedKeyFormat, "Ed25519"));
}
