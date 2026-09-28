// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Poly1305AeadTransform.KeyAndNonceBuffer.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Runtime.CompilerServices;

namespace Bodu.Security.Cryptography;

public abstract partial class Poly1305AeadTransform
{
    /// <summary>
    /// Holds the 32-byte key followed by the 24-byte nonce inline, inside the transform, so that constructing one
    /// allocates nothing beyond the instance itself.
    /// </summary>
    [InlineArray(KeyBytes + NonceBytes)]
    private struct KeyAndNonceBuffer
    {
        /// <summary>The first byte; the <see cref="InlineArrayAttribute" /> expands it to 56.</summary>
        private byte _byte0;
    }
}
