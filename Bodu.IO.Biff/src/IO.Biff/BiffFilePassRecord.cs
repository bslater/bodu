// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BiffFilePassRecord.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Biff;

/// <summary>
/// Represents a decoded <c>FILEPASS</c> record: the protection scheme applied to the records that follow it. The codec
/// reports the scheme and the XOR parameters; it does not decrypt.
/// </summary>
/// <remarks>
/// BIFF5 carries only the XOR key and hash. BIFF8 prefixes a type word: zero selects the same XOR pair, one selects
/// RC4, whose header the codec leaves uninterpreted.
/// </remarks>
/// <param name="EncryptionType">The protection scheme.</param>
/// <param name="XorKey">The XOR obfuscation key; zero for RC4.</param>
/// <param name="XorHash">The XOR password verifier; zero for RC4.</param>
public readonly record struct BiffFilePassRecord(BiffEncryptionType EncryptionType, ushort XorKey, ushort XorHash)
{
    /// <summary>
    /// Decodes a <c>FILEPASS</c> payload.
    /// </summary>
    /// <param name="payload">The record payload.</param>
    /// <param name="version">The stream version, which selects the layout.</param>
    /// <returns>The decoded record.</returns>
    /// <exception cref="BiffFormatException">Thrown when the payload is malformed.</exception>
    internal static BiffFilePassRecord Read(ReadOnlySpan<byte> payload, BiffVersion version)
    {
        const BiffRecordType type = BiffRecordType.FilePass;

        if (version != BiffVersion.Biff8)
        {
            return new BiffFilePassRecord(
                BiffEncryptionType.Xor,
                BiffPayload.ReadUInt16(payload, 0, type),
                BiffPayload.ReadUInt16(payload, 2, type));
        }

        var encryption = (BiffEncryptionType)BiffPayload.ReadUInt16(payload, 0, type);
        if (encryption == BiffEncryptionType.Xor)
        {
            return new BiffFilePassRecord(
                encryption,
                BiffPayload.ReadUInt16(payload, 2, type),
                BiffPayload.ReadUInt16(payload, 4, type));
        }

        return new BiffFilePassRecord(encryption, 0, 0);
    }
}
