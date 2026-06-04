// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AdjustmentTriggerHandlerRegistry.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.V2;

/// <summary>
/// A mutable registry of custom <see cref="IAdjustmentTriggerHandler" /> implementations keyed by handler key.
/// </summary>
/// <remarks>
/// <para>
/// Keys are matched ordinally and are case-sensitive. Registering a key that is already present replaces the previous
/// handler.
/// </para>
/// </remarks>
public sealed class AdjustmentTriggerHandlerRegistry : IAdjustmentTriggerHandlerRegistry
{
    /// <summary>
    /// The registered handlers keyed by handler key.
    /// </summary>
    private readonly Dictionary<string, IAdjustmentTriggerHandler> _handlers = new(StringComparer.Ordinal);

    /// <summary>
    /// Registers a handler under a key, replacing any existing registration.
    /// </summary>
    /// <param name="key">The handler key the policy references.</param>
    /// <param name="handler">The handler implementation.</param>
    /// <returns>The same registry, to allow chaining.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="key" /> or <paramref name="handler" /> is <see langword="null" />.
    /// </exception>
    public AdjustmentTriggerHandlerRegistry Register(string key, IAdjustmentTriggerHandler handler)
    {
        ThrowHelper.ThrowIfNull(key);
        ThrowHelper.ThrowIfNull(handler);

        this._handlers[key] = handler;
        return this;
    }

    /// <inheritdoc />
    /// <exception cref="ArgumentNullException"><paramref name="key" /> is <see langword="null" />.</exception>
    public bool Contains(string key)
    {
        ThrowHelper.ThrowIfNull(key);

        return this._handlers.ContainsKey(key);
    }

    /// <inheritdoc />
    /// <exception cref="ArgumentNullException"><paramref name="key" /> is <see langword="null" />.</exception>
    public bool TryGet(string key, out IAdjustmentTriggerHandler? handler)
    {
        ThrowHelper.ThrowIfNull(key);

        bool found = this._handlers.TryGetValue(key, out IAdjustmentTriggerHandler? value);
        handler = value;
        return found;
    }
}
