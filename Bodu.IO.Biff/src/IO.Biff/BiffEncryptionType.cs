// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BiffEncryptionType.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Biff;

/// <summary>
/// Identifies the protection scheme a <c>FILEPASS</c> record declares.
/// </summary>
/// <remarks>
/// Values match the type word that a BIFF8 <c>FILEPASS</c> record begins with. BIFF5 has no type word — its record
/// always describes XOR obfuscation — so the reader reports <see cref="Xor" /> for every BIFF5 record. A value the
/// enumeration does not name is preserved as its raw value.
/// </remarks>
/// <seealso cref="BiffFilePassRecord.EncryptionType" />
public enum BiffEncryptionType : ushort
{
    /// <summary>
    /// XOR obfuscation: the only scheme in BIFF5, and the scheme selected by a zero type field in BIFF8.
    /// </summary>
    Xor = 0x0000,

    /// <summary>
    /// RC4 encryption (BIFF8 only), in either its standard or CryptoAPI form.
    /// </summary>
    Rc4 = 0x0001,
}
