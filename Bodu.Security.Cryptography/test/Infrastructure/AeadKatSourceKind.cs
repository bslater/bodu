// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AeadKatSourceKind.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography.Infrastructure;

/// <summary>
/// Identifies the provenance of an AEAD known-answer-test vector so that test names and labels do not overstate the
/// authority of a vector — for example, distinguishing a published RFC vector from an Internet-Draft vector or a value
/// derived by composing independently-tested primitives.
/// </summary>
public enum AeadKatSourceKind
{
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
}
