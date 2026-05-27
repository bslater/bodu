// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AeadKat.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Test.Kat;

/// <summary>
/// Represents a single authenticated-encryption known-answer test (KAT) row — the inputs (key, nonce, associated data,
/// plaintext) and the expected outputs (ciphertext and authentication tag) for one AEAD trial.
/// </summary>
/// <param name="Name">The short label that identifies the row in failure diagnostics.</param>
/// <param name="Key">The encryption key.</param>
/// <param name="Nonce">The nonce (also called IV) used for this trial.</param>
/// <param name="AssociatedData">The associated data; empty when the trial has no AD.</param>
/// <param name="Plaintext">The plaintext input; empty when the trial has no payload.</param>
/// <param name="Ciphertext">The expected ciphertext, excluding the authentication tag.</param>
/// <param name="Tag">The expected authentication tag.</param>
public sealed record AeadKat(
    string Name,
    byte[] Key,
    byte[] Nonce,
    byte[] AssociatedData,
    byte[] Plaintext,
    byte[] Ciphertext,
    byte[] Tag) : IKat;
