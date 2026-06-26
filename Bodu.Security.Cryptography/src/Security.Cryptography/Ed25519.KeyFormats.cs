// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Ed25519.KeyFormats.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Surfaces the Ed25519 algorithm semantics and deliberately rejects the inherited PKCS#8 / SubjectPublicKeyInfo / PEM
/// / XML key-format members, which this raw-key-only algorithm does not support.
/// </summary>
public sealed partial class Ed25519
{
    /// <summary>
    /// Gets the algorithm name <c>"Ed25519"</c>.
    /// </summary>
    /// <value>The constant string <c>"Ed25519"</c>.</value>
    public string AlgorithmName =>
        "Ed25519";

    /// <summary>
    /// Gets the approximate classical security strength of Ed25519, in bits.
    /// </summary>
    /// <value>The value 128.</value>
    public int SecurityStrengthBits =>
        128;

    /// <inheritdoc />
    public override void FromXmlString(string xmlString) =>
        throw KeyFormatNotSupported();

    /// <inheritdoc />
    public override string ToXmlString(bool includePrivateParameters) =>
        throw KeyFormatNotSupported();

    /// <inheritdoc />
    public override void ImportSubjectPublicKeyInfo(ReadOnlySpan<byte> source, out int bytesRead) =>
        throw KeyFormatNotSupported();

    /// <inheritdoc />
    public override void ImportPkcs8PrivateKey(ReadOnlySpan<byte> source, out int bytesRead) =>
        throw KeyFormatNotSupported();

    /// <inheritdoc />
    public override void ImportEncryptedPkcs8PrivateKey(ReadOnlySpan<byte> passwordBytes, ReadOnlySpan<byte> source, out int bytesRead) =>
        throw KeyFormatNotSupported();

    /// <inheritdoc />
    public override void ImportEncryptedPkcs8PrivateKey(ReadOnlySpan<char> password, ReadOnlySpan<byte> source, out int bytesRead) =>
        throw KeyFormatNotSupported();

    /// <inheritdoc />
    public override bool TryExportSubjectPublicKeyInfo(Span<byte> destination, out int bytesWritten) =>
        throw KeyFormatNotSupported();

    /// <inheritdoc />
    public override bool TryExportPkcs8PrivateKey(Span<byte> destination, out int bytesWritten) =>
        throw KeyFormatNotSupported();

    /// <inheritdoc />
    public override bool TryExportEncryptedPkcs8PrivateKey(ReadOnlySpan<byte> passwordBytes, PbeParameters pbeParameters, Span<byte> destination, out int bytesWritten) =>
        throw KeyFormatNotSupported();

    /// <inheritdoc />
    public override bool TryExportEncryptedPkcs8PrivateKey(ReadOnlySpan<char> password, PbeParameters pbeParameters, Span<byte> destination, out int bytesWritten) =>
        throw KeyFormatNotSupported();

    /// <summary>
    /// Creates the exception thrown by every unsupported inherited key-format member.
    /// </summary>
    /// <returns>A <see cref="NotSupportedException" /> describing the supported key formats.</returns>
    private static NotSupportedException KeyFormatNotSupported() =>
        new(string.Format(CultureInfo.CurrentCulture, CryptoResourceStrings.Op_NotSupported_RawKeyFormatOnly, "Ed25519"));
}
