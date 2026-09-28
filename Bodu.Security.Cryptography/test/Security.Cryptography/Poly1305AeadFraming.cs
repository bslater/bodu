// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Poly1305AeadFraming.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

/// <summary>
/// Represents the protected <c>SealCore</c> and <c>OpenCore</c> members of <see cref="Poly1305AeadTransform" />, so
/// that the contract tests can bind them to an instance by reflection: their span parameters rule out
/// <see cref="System.Reflection.MethodBase.Invoke(object, object[])" />.
/// </summary>
/// <param name="engine">The keystream engine, positioned at block counter 0.</param>
/// <param name="associatedData">The associated data.</param>
/// <param name="input">The plaintext to seal, or the ciphertext with its tag to open.</param>
/// <param name="output">Receives the result.</param>
/// <returns>The number of bytes written.</returns>
internal delegate int Poly1305AeadFraming(
    IStreamCipher engine,
    ReadOnlySpan<byte> associatedData,
    ReadOnlySpan<byte> input,
    Span<byte> output);
