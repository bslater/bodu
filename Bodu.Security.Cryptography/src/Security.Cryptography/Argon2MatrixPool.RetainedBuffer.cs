// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Argon2MatrixPool.RetainedBuffer.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

internal sealed partial class Argon2MatrixPool
{
    /// <summary>
    /// Describes one retained buffer.
    /// </summary>
    /// <param name="Address">The buffer's address.</param>
    /// <param name="Capacity">The buffer's size, in bytes.</param>
    /// <param name="ReturnedAt">
    /// The <see cref="TimeProvider.GetTimestamp" /> value when the buffer was returned.
    /// </param>
    private readonly record struct RetainedBuffer(nint Address, nuint Capacity, long ReturnedAt);
}
