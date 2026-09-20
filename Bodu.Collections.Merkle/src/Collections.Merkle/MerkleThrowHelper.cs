// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MerkleThrowHelper.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Collections.Merkle;

/// <summary>
/// Provides the argument and state guards shared across this package's Merkle surface.
/// </summary>
internal static class MerkleThrowHelper
{
    /// <summary>
    /// Throws a <see cref="CryptographicException" /> when a hash algorithm reported that it could not write its
    /// digest into the destination buffer.
    /// </summary>
    /// <param name="success">The value returned by the algorithm's try-compute call.</param>
    /// <exception cref="CryptographicException"><paramref name="success" /> is <see langword="false" />.</exception>
    /// <remarks>
    /// The destination is always sized from the algorithm's own reported digest length, so a failure here means the
    /// algorithm contradicted itself rather than that the caller supplied anything wrong.
    /// </remarks>
    internal static void ThrowIfHashAlgorithmDestinationTooSmall(bool success)
    {
        if (!success)
            throw new CryptographicException(MerkleResourceStrings.Crypt_Invalid_HashDestinationTooSmall);
    }
}
