// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TransformMode.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

/// <summary>
/// Defines the direction of a cryptographic transformation.
/// </summary>
/// <remarks>
/// <para>
/// Reserved: no public member of this library consumes it. The transform contracts express the direction otherwise -
/// <see cref="IBlockCipherModeTransform.Transform(System.ReadOnlySpan{byte}, System.Span{byte}, bool)" /> takes a
/// <see cref="bool" />, and the stream ciphers are self-inverse.
/// </para>
/// </remarks>
public enum TransformMode
{
    /// <summary>
    /// Indicates that the transform should encrypt plaintext into ciphertext.
    /// </summary>
    Encrypt,

    /// <summary>
    /// Indicates that the transform should decrypt ciphertext into plaintext.
    /// </summary>
    Decrypt,
}
