// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ReloadableNotableDateService.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Bodu.Globalization.Calendar;

/// <summary>
/// An <see cref="INotableDateService" /> that resolves against the resource currently supplied by an
/// <see cref="INotableDateResourceProvider" />, rebuilding its resolution state whenever that resource is reloaded.
/// </summary>
/// <remarks>
/// <para>
/// This service is a drop-in replacement for <see cref="NotableDateService" /> in scenarios where the underlying data
/// is reloaded at runtime — for example a dependency-injection singleton whose source document changes. Each query
/// reads the provider's current resource; when it differs from the one the inner service was built from, a fresh inner
/// service is constructed. Construction is cheap, so reloads are inexpensive.
/// </para>
/// <para>
/// <strong>Logging.</strong> The constructors accept an optional <see cref="ILogger" /> (defaulting to
/// <see cref="NullLogger.Instance" />, so logging is opt-in). When supplied, the service records each rebuild of its
/// resolution state after observing a reloaded resource at <see cref="LogLevel.Debug" />. This level is fixed.
/// </para>
/// </remarks>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// MutableNotableDateResourceProvider provider = new(NotableDateResourceLoader.Load(initialXml));
/// INotableDateService service = new ReloadableNotableDateService(provider);
///
/// IReadOnlyList<NotableDate> before = service.Resolve(2026, "US");
/// provider.Reload(NotableDateResourceLoader.Load(updatedXml));
/// IReadOnlyList<NotableDate> after = service.Resolve(2026, "US"); // reflects the reloaded data
///]]>
/// </code>
/// </example>
/// <seealso cref="MutableNotableDateResourceProvider" /> <seealso cref="NotableDateService" />
/// <seealso href="../guides/calendar/dependency-injection.html">Calendar dependency injection (guide)</seealso>
public sealed class ReloadableNotableDateService
    : INotableDateService
{
    /// <summary>The provider supplying the resource currently in effect.</summary>
    private readonly INotableDateResourceProvider _provider;

    /// <summary>The optional collaborators passed to each rebuilt inner service, or <see langword="null" /> for built-ins only.</summary>
    private readonly NotableDateServiceOptions? _options;

    /// <summary>Serializes rebuilds so a reload constructs the inner service once, not once per racing query.</summary>
    private readonly object _gate = new();

    /// <summary>The logger that records resolution-state rebuilds after a reload.</summary>
    private readonly ILogger _logger;

    /// <summary>The immutable (source resource, inner service) pair currently in effect, swapped atomically on rebuild so the steady-state read path needs no lock.</summary>
    private volatile Tuple<NotableDateResource, NotableDateService> _snapshot;

    /// <summary>
    /// Initializes a new instance of the <see cref="ReloadableNotableDateService" /> class.
    /// </summary>
    /// <param name="provider">The provider supplying the resource currently in effect.</param>
    /// <param name="logger">
    /// The logger that records resolution-state rebuilds. <see langword="null" /> selects
    /// <see cref="NullLogger.Instance" />.
    /// </param>
    /// <exception cref="ArgumentNullException"><paramref name="provider" /> is <see langword="null" />.</exception>
    public ReloadableNotableDateService(INotableDateResourceProvider provider, ILogger? logger = null)
        : this(provider, null, logger)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ReloadableNotableDateService" /> class with the optional
    /// collaborators propagated to each rebuilt inner service.
    /// </summary>
    /// <param name="provider">The provider supplying the resource currently in effect.</param>
    /// <param name="options">
    /// The optional collaborators propagated to each rebuilt inner <see cref="NotableDateService" />, or
    /// <see langword="null" /> for built-ins only.
    /// </param>
    /// <param name="logger">
    /// The logger that records resolution-state rebuilds. <see langword="null" /> selects
    /// <see cref="NullLogger.Instance" />.
    /// </param>
    /// <exception cref="ArgumentNullException"><paramref name="provider" /> is <see langword="null" />.</exception>
    public ReloadableNotableDateService(INotableDateResourceProvider provider, NotableDateServiceOptions? options, ILogger? logger = null)
    {
        ThrowHelper.ThrowIfNull(provider);

        _provider = provider;
        _options = options;
        _logger = logger ?? NullLogger.Instance;

        NotableDateResource initial = provider.Current;
        _snapshot = Tuple.Create(initial, new NotableDateService(initial, options));
    }

    /// <inheritdoc />
    public IReadOnlyList<NotableDate> Resolve(DateOnly date, string territory) =>
        Current().Resolve(date, territory);

    /// <inheritdoc />
    public IReadOnlyList<NotableDate> Resolve(DateRange range, string territory) =>
        Current().Resolve(range, territory);

    /// <inheritdoc />
    public IReadOnlyList<NotableDate> Resolve(DateOnly date, string territory, NotableDateFilter filter) =>
        Current().Resolve(date, territory, filter);

    /// <inheritdoc />
    public IReadOnlyList<NotableDate> Resolve(DateRange range, string territory, NotableDateFilter filter) =>
        Current().Resolve(range, territory, filter);

    /// <inheritdoc />
    public IReadOnlyList<string> GetSupportedTerritories() =>
        Current().GetSupportedTerritories();

    /// <inheritdoc />
    public IReadOnlyList<CalendarSystem> GetSupportedCalendars() =>
        Current().GetSupportedCalendars();

    /// <summary>
    /// Returns the inner service for the resource currently in effect, rebuilding it when the provider has reloaded.
    /// </summary>
    /// <returns>The current inner <see cref="NotableDateService" />.</returns>
    private NotableDateService Current()
    {
        NotableDateResource current = _provider.Current;
        Tuple<NotableDateResource, NotableDateService> snapshot = _snapshot;

        // Steady state: the provider still supplies the resource the snapshot was built from, so the query proceeds
        // without taking the gate — reloads are rare and queries are hot.
        if (ReferenceEquals(current, snapshot.Item1))
            return snapshot.Item2;

        lock (_gate)
        {
            // A racing query may have rebuilt while this one waited; only build when still stale for the resource this
            // query observed.
            snapshot = _snapshot;

            if (!ReferenceEquals(current, snapshot.Item1))
            {
                snapshot = Tuple.Create(current, new NotableDateService(current, _options));
                _snapshot = snapshot;
                Log.ResolutionStateRebuilt(_logger, current.ResourceId);
            }

            return snapshot.Item2;
        }
    }
}
