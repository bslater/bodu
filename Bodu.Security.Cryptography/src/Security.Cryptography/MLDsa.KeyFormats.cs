// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MLDsa.KeyFormats.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Surfaces the ML-DSA parameter-set semantics and deliberately rejects the inherited PKCS#8 / SubjectPublicKeyInfo /
/// PEM / XML key-format members. ML-DSA exposes only its FIPS 204 raw key encodings; the standardized ASN.1 containers
/// are intentionally out of scope for this version.
/// </summary>
public abstract partial class MLDsa
{
    /// <summary>
    /// Gets the FIPS 204 parameter-set name, such as <c>"ML-DSA-65"</c>.
    /// </summary>
    /// <value>The parameter-set name selected when the instance was created.</value>
    /// <remarks>
    /// Unlike the inherited <see cref="AsymmetricAlgorithm.KeySize" />, which reports a fixed parameter-set designator
    /// rather than a meaningful bit length for a post-quantum scheme, this property names the parameter set directly.
    /// </remarks>
    public string AlgorithmName =>
        _parameters.Name;

    /// <summary>
    /// Gets the claimed security strength of the selected parameter set, in bits (128, 192, or 256 for ML-DSA-44,
    /// ML-DSA-65, and ML-DSA-87 respectively, corresponding to NIST security categories 2, 3, and 5).
    /// </summary>
    /// <value>The security strength in bits.</value>
    public int SecurityStrengthBits =>
        _parameters.Lambda;

    /// <summary>
    /// Not supported: ML-DSA keys have no XML key format. This member always throws.
    /// </summary>
    /// <inheritdoc path="/param" />
    /// <exception cref="NotSupportedException">Always.</exception>
    public override void FromXmlString(string xmlString) =>
        throw KeyFormatNotSupported();

    /// <summary>
    /// Not supported: ML-DSA keys have no XML key format. This member always throws.
    /// </summary>
    /// <inheritdoc path="/param" />
    /// <returns>This member does not return.</returns>
    /// <exception cref="NotSupportedException">Always.</exception>
    public override string ToXmlString(bool includePrivateParameters) =>
        throw KeyFormatNotSupported();

    /// <summary>
    /// Not supported: ML-DSA keys are exchanged only in their raw FIPS 204 encodings, not as SubjectPublicKeyInfo. This
    /// member always throws.
    /// </summary>
    /// <inheritdoc path="/param" />
    /// <exception cref="NotSupportedException">Always.</exception>
    public override void ImportSubjectPublicKeyInfo(ReadOnlySpan<byte> source, out int bytesRead) =>
        throw KeyFormatNotSupported();

    /// <summary>
    /// Not supported: ML-DSA keys are exchanged only in their raw FIPS 204 encodings, not as PKCS#8. This member always
    /// throws.
    /// </summary>
    /// <inheritdoc path="/param" />
    /// <exception cref="NotSupportedException">Always.</exception>
    public override void ImportPkcs8PrivateKey(ReadOnlySpan<byte> source, out int bytesRead) =>
        throw KeyFormatNotSupported();

    /// <summary>
    /// Not supported: encrypted PKCS#8 is out of scope for this library. This member always throws.
    /// </summary>
    /// <inheritdoc path="/param" />
    /// <exception cref="NotSupportedException">Always.</exception>
    public override void ImportEncryptedPkcs8PrivateKey(ReadOnlySpan<byte> passwordBytes, ReadOnlySpan<byte> source, out int bytesRead) =>
        throw KeyFormatNotSupported();

    /// <summary>
    /// Not supported: encrypted PKCS#8 is out of scope for this library. This member always throws.
    /// </summary>
    /// <inheritdoc path="/param" />
    /// <exception cref="NotSupportedException">Always.</exception>
    public override void ImportEncryptedPkcs8PrivateKey(ReadOnlySpan<char> password, ReadOnlySpan<byte> source, out int bytesRead) =>
        throw KeyFormatNotSupported();

    /// <summary>
    /// Not supported: ML-DSA keys are exchanged only in their raw FIPS 204 encodings, not as SubjectPublicKeyInfo. This
    /// member always throws.
    /// </summary>
    /// <inheritdoc path="/param" />
    /// <returns>This member does not return.</returns>
    /// <exception cref="NotSupportedException">Always.</exception>
    public override bool TryExportSubjectPublicKeyInfo(Span<byte> destination, out int bytesWritten) =>
        throw KeyFormatNotSupported();

    /// <summary>
    /// Not supported: ML-DSA keys are exchanged only in their raw FIPS 204 encodings, not as PKCS#8. This member always
    /// throws.
    /// </summary>
    /// <inheritdoc path="/param" />
    /// <returns>This member does not return.</returns>
    /// <exception cref="NotSupportedException">Always.</exception>
    public override bool TryExportPkcs8PrivateKey(Span<byte> destination, out int bytesWritten) =>
        throw KeyFormatNotSupported();

    /// <summary>
    /// Not supported: encrypted PKCS#8 is out of scope for this library. This member always throws.
    /// </summary>
    /// <inheritdoc path="/param" />
    /// <returns>This member does not return.</returns>
    /// <exception cref="NotSupportedException">Always.</exception>
    public override bool TryExportEncryptedPkcs8PrivateKey(ReadOnlySpan<byte> passwordBytes, PbeParameters pbeParameters, Span<byte> destination, out int bytesWritten) =>
        throw KeyFormatNotSupported();

    /// <summary>
    /// Not supported: encrypted PKCS#8 is out of scope for this library. This member always throws.
    /// </summary>
    /// <inheritdoc path="/param" />
    /// <returns>This member does not return.</returns>
    /// <exception cref="NotSupportedException">Always.</exception>
    public override bool TryExportEncryptedPkcs8PrivateKey(ReadOnlySpan<char> password, PbeParameters pbeParameters, Span<byte> destination, out int bytesWritten) =>
        throw KeyFormatNotSupported();

    /// <summary>
    /// Creates the exception thrown by every unsupported inherited key-format member.
    /// </summary>
    /// <returns>A <see cref="NotSupportedException" /> describing the supported key formats.</returns>
    private NotSupportedException KeyFormatNotSupported() =>
        new(string.Format(CultureInfo.CurrentCulture, CryptoResourceStrings.Op_NotSupported_RawKeyFormatOnly, _parameters.Name));
}
