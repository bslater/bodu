// ---------------------------------------------------------------------------------------------------------------
// <copyright file="KatSourceKind.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography.Infrastructure;

/// <summary>
/// Identifies the provenance of a cryptographic known-answer test vector so that test names and reports do not overstate
/// the authority of a vector — for example, distinguishing a published standard from an Internet-Draft, a value taken
/// from a reference implementation, or an in-tree regression baseline.
/// </summary>
/// <remarks>
/// This is the single provenance taxonomy shared by every known-answer family (digest, cipher, AEAD, key-derivation, and
/// the asymmetric operations). It supersedes the family-specific <c>AeadKatSourceKind</c> and the free-form
/// <c>Profile</c> / <c>Source</c> string tags that earlier KAT records carried.
/// </remarks>
public enum KatSourceKind
{
    /// <summary>
    /// The vector is published in a formal standard such as a FIPS publication, a NIST special publication, or an ISO
    /// standard.
    /// </summary>
    Standard,

    /// <summary>
    /// The vector is published in an RFC.
    /// </summary>
    Rfc,

    /// <summary>
    /// The vector is published in an Internet-Draft.
    /// </summary>
    InternetDraft,

    /// <summary>
    /// The vector is published by a reference implementation or library test suite.
    /// </summary>
    ReferenceImplementation,

    /// <summary>
    /// The vector is produced by composing independently-tested primitives or constructions rather than taken from an
    /// external published source.
    /// </summary>
    DerivedOracle,

    /// <summary>
    /// The vector is an in-tree regression baseline captured from this library's own output, retained to detect
    /// unintended changes until an authoritative external vector is sourced.
    /// </summary>
    InternalRegression,
}
