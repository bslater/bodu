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
    /// <summary>
    /// The provider supplying the resource currently in effect.
    /// </summary>
    private readonly INotableDateResourceProvider _provider;

    /// <summary>
    /// The optional collaborators passed to each rebuilt inner service, or <see langword="null" /> for built-ins only.
    /// </summary>
    private readonly NotableDateServiceOptions? _options;

    /// <summary>
    /// Guards the paired update of <see cref="_inner" /> and <see cref="_builtFrom" />.
    /// </summary>
    private readonly object _gate = new();

    /// <summary>
    /// The logger that records resolution-state rebuilds after a reload.
    /// </summary>
    private readonly ILogger _logger;

    /// <summary>
    /// The inner service resolving against <see cref="_builtFrom" />.
    /// </summary>
    private NotableDateService _inner;

    /// <summary>
    /// The resource <see cref="_inner" /> was built from, used to detect a reload.
    /// </summary>
    private NotableDateResource _builtFrom;

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

        _builtFrom = provider.Current;
        _inner = new NotableDateService(_builtFrom, options);
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

        lock (_gate)
        {
            if (!ReferenceEquals(current, _builtFrom))
            {
                _inner = new NotableDateService(current, _options);
                _builtFrom = current;
                Log.ResolutionStateRebuilt(_logger, current.ResourceId);
            }

            return _inner;
        }
    }
}
