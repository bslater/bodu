// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IStreamAeadTransform.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

/// <summary>
/// Represents a stream-cipher authenticated-encryption (AEAD) transform — an <see cref="IAeadTransform" /> backed by a
/// stream cipher and a one-time message-authentication code rather than a block-cipher mode. The extended-nonce
/// Poly1305 AEADs (<see cref="XChaCha20Poly1305" />, <see cref="XSalsa20Poly1305" />, <see cref="XSalsa20Poly1305Ietf" />
/// ) implement this surface.
/// </summary>
/// <remarks>
/// This interface distinguishes stream-cipher AEADs from block-cipher AEAD modes
/// (<see cref="IAeadBlockCipherModeTransform" />) so the two families can share <see cref="IAeadTransform" /> without
/// the stream constructions being modelled as block-cipher modes.
/// </remarks>
public interface IStreamAeadTransform
    : IAeadTransform
{
}
