// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SignatureFormat.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

/// <summary>
/// Specifies the wire encoding of a digital-signature value.
/// </summary>
/// <remarks>
/// <para>
/// The same mathematical signature can be serialized in incompatible ways — most notably ECDSA signatures, which
/// circulate both as ASN.1 DER <c>SEQUENCE</c> structures and as fixed-width IEEE P1363 <c>r || s</c>
/// concatenations. Carrying the format alongside the bytes prevents a signature produced in one encoding from being
/// verified, stored, or transmitted as if it were the other.
/// </para>
/// </remarks>
public enum SignatureFormat
{
    /// <summary>
    /// The encoding is unknown or unspecified.
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// The raw signature bytes of the algorithm, with no additional framing (for example an EdDSA signature).
    /// </summary>
    Raw,

    /// <summary>
    /// An ASN.1 DER-encoded structure (for example the ECDSA <c>SEQUENCE { r INTEGER, s INTEGER }</c> form used by
    /// X.509 and CMS).
    /// </summary>
    Der,

    /// <summary>
    /// The fixed-width IEEE P1363 concatenation (for example the ECDSA <c>r || s</c> form used by JOSE/COSE and the
    /// .NET <c>ECDsa.SignData</c> default).
    /// </summary>
    P1363,
}
