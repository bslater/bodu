// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PstCryptMethod.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Pst;

/// <summary>
/// Identifies how a file's external block data is encoded (the header's <c>bCryptMethod</c> values).
/// </summary>
/// <remarks>
/// The permute and cyclic methods are byte-substitution obfuscation defined by MS-PST §5.1 and §5.2, not cryptography;
/// the reader decodes both transparently. The Windows Information Protection method is real encryption and is not
/// supported.
/// </remarks>
public enum PstCryptMethod
{
    /// <summary>
    /// Block data is stored verbatim (<c>NDB_CRYPT_NONE</c>).
    /// </summary>
    None = 0x00,

    /// <summary>
    /// Block data is permuted through a fixed substitution table (<c>NDB_CRYPT_PERMUTE</c>).
    /// </summary>
    Permute = 0x01,

    /// <summary>
    /// Block data is encoded with a block-keyed rolling substitution (<c>NDB_CRYPT_CYCLIC</c>).
    /// </summary>
    Cyclic = 0x02,

    /// <summary>
    /// Block data is encrypted by Windows Information Protection (<c>NDB_CRYPT_EDPCRYPTED</c>); unsupported.
    /// </summary>
    WindowsInformationProtection = 0x10,
}
