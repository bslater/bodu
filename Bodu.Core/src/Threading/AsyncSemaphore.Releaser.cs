// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AsyncSemaphore.Releaser.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Threading;

public sealed partial class AsyncSemaphore
{
    /// <summary>
    /// Represents a permit taken from an <see cref="AsyncSemaphore" />. Disposing the releaser returns the permit.
    /// </summary>
    /// <remarks>
    /// The releaser is a lightweight value type returned by <see cref="LockAsync()" />. It is intended to be consumed
    /// by a single <c>using</c> statement and disposed exactly once. Copying a releaser and disposing more than one
    /// copy returns more than one permit, which raises the permit count and may throw from <see cref="Release()" />;
    /// this is undefined behavior, mirroring the contract of the framework's value-type enumerators.
    /// </remarks>
    public readonly struct Releaser
        : IDisposable
    {
        /// <summary>The semaphore that issued the permit, or <see langword="null" /> for a default releaser.</summary>
        private readonly AsyncSemaphore? _owner;

        /// <summary>
        /// Initializes a new instance of the <see cref="Releaser" /> struct bound to the specified owner.
        /// </summary>
        /// <param name="owner">The semaphore that issued the permit.</param>
        internal Releaser(AsyncSemaphore owner)
        {
            _owner = owner;
        }

        /// <summary>
        /// Returns the permit to the owning <see cref="AsyncSemaphore" />.
        /// </summary>
        public void Dispose() =>
            _owner?.ReleaseFromReleaser();
    }
}
