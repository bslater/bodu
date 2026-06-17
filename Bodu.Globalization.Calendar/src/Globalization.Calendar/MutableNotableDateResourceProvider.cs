// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MutableNotableDateResourceProvider.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Bodu.Globalization.Calendar;

/// <summary>
/// An <see cref="INotableDateResourceProvider" /> whose resource can be replaced at runtime, so a long-lived service
/// observes new data after a reload.
/// </summary>
/// <remarks>
/// <para>
/// The current resource is swapped atomically; a <see cref="ReloadableNotableDateService" /> built over the provider
/// rebuilds its resolution state the next time it is queried. Because a resource is itself immutable, applying new
/// override operations or refreshed data at runtime is performed by loading a fresh <see cref="NotableDateResource" />
/// and passing it to <see cref="Reload" />.
/// </para>
/// <para>
/// <strong>Logging.</strong> The constructor accepts an optional <see cref="ILogger" /> (defaulting to
/// <see cref="NullLogger.Instance" />, so logging is opt-in). When supplied, <see cref="Reload" /> records the swap at
/// <see cref="LogLevel.Information" />, naming the resource that becomes current. This level is fixed.
/// </para>
/// </remarks>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// MutableNotableDateResourceProvider provider = new(NotableDateResourceLoader.Load(initialXml));
/// INotableDateService service = new ReloadableNotableDateService(provider);
///
/// // Later, when the source document changes, swap in fresh data; the service picks it up
/// // on its next query without being rebuilt.
/// provider.Reload(NotableDateResourceLoader.Load(updatedXml));
///]]>
/// </code>
/// </example>
/// <seealso cref="ReloadableNotableDateService" /> <seealso cref="INotableDateResourceProvider" />
/// <seealso href="../guides/calendar/dependency-injection.html">Calendar dependency injection (guide)</seealso>
public sealed class MutableNotableDateResourceProvider
    : INotableDateResourceProvider
{
    /// <summary>The logger that records resource reloads.</summary>
    private readonly ILogger _logger;

    /// <summary>The resource currently in effect. Marked <see langword="volatile" /> so a swap is visible to all readers.</summary>
    private volatile NotableDateResource _current;

    /// <summary>
    /// Initializes a new instance of the <see cref="MutableNotableDateResourceProvider" /> class.
    /// </summary>
    /// <param name="resource">The initial resource.</param>
    /// <param name="logger">
    /// The logger that records resource reloads. <see langword="null" /> selects <see cref="NullLogger.Instance" />.
    /// </param>
    /// <exception cref="ArgumentNullException"><paramref name="resource" /> is <see langword="null" />.</exception>
    public MutableNotableDateResourceProvider(NotableDateResource resource, ILogger? logger = null)
    {
        ThrowHelper.ThrowIfNull(resource);

        _current = resource;
        _logger = logger ?? NullLogger.Instance;
    }

    /// <inheritdoc />
    public NotableDateResource Current =>
        _current;

    /// <summary>
    /// Replaces the resource currently in effect.
    /// </summary>
    /// <param name="resource">The resource that becomes current.</param>
    /// <exception cref="ArgumentNullException"><paramref name="resource" /> is <see langword="null" />.</exception>
    public void Reload(NotableDateResource resource)
    {
        ThrowHelper.ThrowIfNull(resource);

        _current = resource;
        Log.ResourceReloaded(_logger, resource.ResourceId);
    }
}
