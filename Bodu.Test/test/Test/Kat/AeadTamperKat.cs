// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AeadTamperKat.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Test.Kat;

/// <summary>
/// Identifies how an <see cref="AeadTamperKat" /> row mutates the base AEAD vector to drive a single
/// authentication-failure scenario.
/// </summary>
public enum AeadTamperKind
{
    /// <summary>Flip one bit of the ciphertext. Decrypt must reject the input.</summary>
    Ciphertext,

    /// <summary>Flip one bit of the tag. Decrypt must reject the input.</summary>
    Tag,

    /// <summary>Flip one bit of the associated data. Decrypt must reject the input.</summary>
    AssociatedData,

    /// <summary>Flip one bit of the nonce. Decrypt must reject the input.</summary>
    Nonce,

    /// <summary>Truncate the tag by one byte. Decrypt must reject the input.</summary>
    TruncatedTag,
}

/// <summary>
/// Represents a single AEAD tamper-test row built from a base <see cref="AeadKat" /> and a mutation
/// indicator. The contract test runner applies the mutation to the base vector and verifies that the
/// decryption rejects the tampered input.
/// </summary>
/// <param name="Name">The short label that identifies the row in failure diagnostics.</param>
/// <param name="BaseCase">The clean AEAD vector to be mutated.</param>
/// <param name="TamperKind">Which component of <see cref="BaseCase" /> the runner should mutate.</param>
public sealed record AeadTamperKat(
    string Name,
    AeadKat BaseCase,
    AeadTamperKind TamperKind) : IKat;
