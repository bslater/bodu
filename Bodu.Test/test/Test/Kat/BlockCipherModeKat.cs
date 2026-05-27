// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BlockCipherModeKat.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Test.Kat;

/// <summary>
/// Represents a single known-answer test row for a symmetric block cipher operating in a given mode of operation with a
/// given padding scheme. Carries every parameter required to reproduce the ciphertext from the key and the
/// initialisation vector.
/// </summary>
/// <param name="Name">The short label that identifies the row in failure diagnostics.</param>
/// <param name="Algorithm">The cipher identifier (for example <c>"Threefish-256"</c>, <c>"Blowfish"</c>).</param>
/// <param name="Mode">The block cipher mode of operation.</param>
/// <param name="Padding">The padding scheme applied to the final block.</param>
/// <param name="Key">The encryption key.</param>
/// <param name="IV">The initialisation vector (or nonce) for modes that require one.</param>
/// <param name="Plaintext">The plaintext bytes supplied to the encryptor.</param>
/// <param name="Ciphertext">The expected ciphertext bytes the encryptor must produce.</param>
public sealed record BlockCipherModeKat(
    string Name,
    string Algorithm,
    CipherMode Mode,
    PaddingMode Padding,
    byte[] Key,
    byte[] IV,
    byte[] Plaintext,
    byte[] Ciphertext) : IKat;
