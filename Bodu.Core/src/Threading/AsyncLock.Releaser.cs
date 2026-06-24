// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AsyncLock.Releaser.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Threading;

public sealed partial class AsyncLock
{
    /// <summary>
    /// Represents an acquired <see cref="AsyncLock" />. Disposing the releaser releases the lock.
    /// </summary>
    /// <remarks>
    /// The releaser is a lightweight value type returned by <see cref="LockAsync()" />. It is intended to be consumed
    /// by a single <c>using</c> statement and disposed exactly once. Copying a releaser and disposing more than one
    /// copy releases the lock more than once and is undefined behavior, mirroring the contract of the framework's
    /// value-type enumerators.
    /// </remarks>
    public readonly struct Releaser : IDisposable
    {
        /// <summary>The lock to release on disposal, or <see langword="null" /> for a default releaser.</summary>
        private readonly AsyncLock? _owner;

        /// <summary>
        /// Initializes a new instance of the <see cref="Releaser" /> struct bound to the specified owner.
        /// </summary>
        /// <param name="owner">The lock that was acquired.</param>
        internal Releaser(AsyncLock owner)
        {
            _owner = owner;
        }

        /// <summary>
        /// Releases the owning <see cref="AsyncLock" />.
        /// </summary>
        public void Dispose() =>
            _owner?.Release();
    }
}
