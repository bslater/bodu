// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BiffEncryptionType.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Biff;

/// <summary>
/// Identifies the protection scheme a <c>FILEPASS</c> record declares.
/// </summary>
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
