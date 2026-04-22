// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SymmetricAlgorithmExtensions.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography.Extensions;

/// <summary>
/// Provides a set of <see langword="static" /> ( <see langword="Shared" /> in Visual Basic) methods that extend the
/// <see cref="System.Security.Cryptography.SymmetricAlgorithm" /> class.
/// </summary>
public static partial class SymmetricAlgorithmExtensions
{
    /// <summary>
    /// The default buffer size, in bytes, used when reading from or writing to streams during encryption and decryption operations.
    /// </summary>
    public const int DefaultBufferSize = 81920;
}
