// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ReloadableNotableDateService.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

/// <summary>
/// An <see cref="INotableDateService" /> that resolves against the resource currently supplied by an
/// <see cref="INotableDateResourceProvider" />, rebuilding its resolution state whenever that resource is reloaded.
/// </summary>
/// <remarks>
/// <para>
/// This service is a drop-in replacement for <see cref="NotableDateService" /> in scenarios where the underlying data is
/// reloaded at runtime — for example a dependency-injection singleton whose source document changes. Each query reads the
/// provider's current resource; when it differs from the one the inner service was built from, a fresh inner service is
/// constructed. Construction is cheap, so reloads are inexpensive.
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
/// <seealso cref="MutableNotableDateResourceProvider" />
/// <seealso cref="NotableDateService" />
/// <seealso href="../guides/calendar/dependency-injection.html">Calendar dependency injection (guide)</seealso>
public sealed class ReloadableNotableDateService : INotableDateService
{
    /// <summary>
    /// The provider supplying the resource currently in effect.
    /// </summary>
    private readonly INotableDateResourceProvider _provider;

    /// <summary>
    /// The custom algorithm registry passed to each rebuilt inner service.
    /// </summary>
    private readonly INotableDateAlgorithmRegistry? _algorithms;

    /// <summary>
    /// The custom collision resolver passed to each rebuilt inner service.
    /// </summary>
    private readonly INotableDateCollisionResolver? _collisionResolver;

    /// <summary>
    /// The custom adjustment-handler registry passed to each rebuilt inner service.
    /// </summary>
    private readonly IAdjustmentHandlerRegistry? _handlers;

    /// <summary>
    /// The custom trigger-handler registry passed to each rebuilt inner service.
    /// </summary>
    private readonly IAdjustmentTriggerHandlerRegistry? _triggerHandlers;

    /// <summary>
    /// Guards the paired update of <see cref="_inner" /> and <see cref="_builtFrom" />.
    /// </summary>
    private readonly object _gate = new();

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
    /// <exception cref="ArgumentNullException"><paramref name="provider" /> is <see langword="null" />.</exception>
    public ReloadableNotableDateService(INotableDateResourceProvider provider)
        : this(provider, null, null, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ReloadableNotableDateService" /> class with custom extensibility
    /// registries propagated to each rebuilt inner service.
    /// </summary>
    /// <param name="provider">The provider supplying the resource currently in effect.</param>
    /// <param name="algorithms">The custom algorithm registry, or <see langword="null" /> for built-ins only.</param>
    /// <param name="collisionResolver">The collision resolver consulted for a custom collision policy, or <see langword="null" />.</param>
    /// <param name="handlers">The adjustment-handler registry consulted for a custom action, or <see langword="null" />.</param>
    /// <exception cref="ArgumentNullException"><paramref name="provider" /> is <see langword="null" />.</exception>
    public ReloadableNotableDateService(
        INotableDateResourceProvider provider,
        INotableDateAlgorithmRegistry? algorithms,
        INotableDateCollisionResolver? collisionResolver,
        IAdjustmentHandlerRegistry? handlers)
        : this(provider, algorithms, collisionResolver, handlers, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ReloadableNotableDateService" /> class with custom extensibility
    /// registries, including a trigger-handler registry, propagated to each rebuilt inner service.
    /// </summary>
    /// <param name="provider">The provider supplying the resource currently in effect.</param>
    /// <param name="algorithms">The custom algorithm registry, or <see langword="null" /> for built-ins only.</param>
    /// <param name="collisionResolver">The collision resolver consulted for a custom collision policy, or <see langword="null" />.</param>
    /// <param name="handlers">The adjustment-handler registry consulted for a custom action, or <see langword="null" />.</param>
    /// <param name="triggerHandlers">The trigger-handler registry consulted for a custom trigger, or <see langword="null" />.</param>
    /// <exception cref="ArgumentNullException"><paramref name="provider" /> is <see langword="null" />.</exception>
    public ReloadableNotableDateService(
        INotableDateResourceProvider provider,
        INotableDateAlgorithmRegistry? algorithms,
        INotableDateCollisionResolver? collisionResolver,
        IAdjustmentHandlerRegistry? handlers,
        IAdjustmentTriggerHandlerRegistry? triggerHandlers)
    {
        ThrowHelper.ThrowIfNull(provider);

        this._provider = provider;
        this._algorithms = algorithms;
        this._collisionResolver = collisionResolver;
        this._handlers = handlers;
        this._triggerHandlers = triggerHandlers;

        this._builtFrom = provider.Current;
        this._inner = new NotableDateService(this._builtFrom, algorithms, collisionResolver, handlers, triggerHandlers);
    }

    /// <inheritdoc />
    public IReadOnlyList<NotableDate> Resolve(DateOnly date, string territory) =>
        this.Current().Resolve(date, territory);

    /// <inheritdoc />
    public IReadOnlyList<NotableDate> Resolve(DateRange range, string territory) =>
        this.Current().Resolve(range, territory);

    /// <inheritdoc />
    public IReadOnlyList<NotableDate> Resolve(DateOnly date, string territory, NotableDateFilter filter) =>
        this.Current().Resolve(date, territory, filter);

    /// <inheritdoc />
    public IReadOnlyList<NotableDate> Resolve(DateRange range, string territory, NotableDateFilter filter) =>
        this.Current().Resolve(range, territory, filter);

    /// <inheritdoc />
    public IReadOnlyList<string> GetSupportedTerritories() =>
        this.Current().GetSupportedTerritories();

    /// <inheritdoc />
    public IReadOnlyList<CalendarSystem> GetSupportedCalendars() =>
        this.Current().GetSupportedCalendars();

    /// <summary>
    /// Returns the inner service for the resource currently in effect, rebuilding it when the provider has reloaded.
    /// </summary>
    /// <returns>The current inner <see cref="NotableDateService" />.</returns>
    private NotableDateService Current()
    {
        NotableDateResource current = this._provider.Current;

        lock (this._gate)
        {
            if (!ReferenceEquals(current, this._builtFrom))
            {
                this._inner = new NotableDateService(current, this._algorithms, this._collisionResolver, this._handlers, this._triggerHandlers);
                this._builtFrom = current;
            }

            return this._inner;
        }
    }
}
