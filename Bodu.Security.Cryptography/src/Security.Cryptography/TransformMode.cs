// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TransformMode.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography
{
    /// <summary>
    /// Defines the direction of a cryptographic transformation.
    /// </summary>
    /// <remarks>This enumeration is used to distinguish between encryption and decryption operations in cryptographic transform implementations.</remarks>
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
}