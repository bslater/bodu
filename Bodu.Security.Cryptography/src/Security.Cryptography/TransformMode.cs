// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TransformMode.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

/// <summary>
/// Defines the direction of a cryptographic transformation.
/// </summary>
/// <remarks>
/// <para>
/// Used by helper APIs that take a single direction argument rather than separate <c>Encrypt</c> /
/// <c>Decrypt</c> overloads. The lower-level <see cref="IBlockCipherModeTransform.Transform(System.ReadOnlySpan{byte}, System.Span{byte}, bool)"/>
/// uses a <see cref="bool"/> instead because the transform contract predates this enumeration; either
/// representation expresses the same choice.
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
