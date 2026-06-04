// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MutableNotableDateResourceProvider.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.V2;

/// <summary>
/// An <see cref="INotableDateResourceProvider" /> whose resource can be replaced at runtime, so a long-lived service
/// observes new data after a reload.
/// </summary>
/// <remarks>
/// <para>
/// The current resource is swapped atomically; a <see cref="ReloadableNotableDateService" /> built over the provider
/// rebuilds its resolution state the next time it is queried. Because a resource is itself immutable, applying new
/// override operations or refreshed data at runtime is performed by loading a fresh
/// <see cref="NotableDateResource" /> and passing it to <see cref="Reload" />.
/// </para>
/// </remarks>
public sealed class MutableNotableDateResourceProvider : INotableDateResourceProvider
{
    /// <summary>
    /// The resource currently in effect. Marked <see langword="volatile" /> so a swap is visible to all readers.
    /// </summary>
    private volatile NotableDateResource _current;

    /// <summary>
    /// Initializes a new instance of the <see cref="MutableNotableDateResourceProvider" /> class.
    /// </summary>
    /// <param name="resource">The initial resource.</param>
    /// <exception cref="ArgumentNullException"><paramref name="resource" /> is <see langword="null" />.</exception>
    public MutableNotableDateResourceProvider(NotableDateResource resource)
    {
        ThrowHelper.ThrowIfNull(resource);

        this._current = resource;
    }

    /// <inheritdoc />
    public NotableDateResource Current =>
        this._current;

    /// <summary>
    /// Replaces the resource currently in effect.
    /// </summary>
    /// <param name="resource">The resource that becomes current.</param>
    /// <exception cref="ArgumentNullException"><paramref name="resource" /> is <see langword="null" />.</exception>
    public void Reload(NotableDateResource resource)
    {
        ThrowHelper.ThrowIfNull(resource);

        this._current = resource;
    }
}
