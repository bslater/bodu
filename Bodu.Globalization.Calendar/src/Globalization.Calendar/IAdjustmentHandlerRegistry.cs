// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IAdjustmentHandlerRegistry.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Provides lookup of registered <see cref="IAdjustmentHandler" /> instances by stable key.
/// </summary>
/// <remarks>
/// <para>
/// The registry is consulted during observance-adjustment evaluation when an <see cref="ObservanceAdjustment" />
/// declares <see cref="AdjustmentTrigger.Custom" /> or <see cref="AdjustmentAction.Custom" />. The adjuster resolves
/// the handler by looking up <see cref="ObservanceAdjustment.HandlerKey" /> in the supplied registry. Custom handlers
/// receive the full <see cref="AdjustmentHandlerContext" /> and may produce arbitrary date shifts or non-working-day
/// overrides.
/// </para>
/// <para>
/// Supply a populated <see cref="AdjustmentHandlerRegistry" /> to the <see cref="NotableDateService" /> constructor to
/// enable custom trigger and action logic.
/// </para>
/// </remarks>
public interface IAdjustmentHandlerRegistry
{
    /// <summary>
    /// Attempts to retrieve the <see cref="IAdjustmentHandler" /> registered against the specified key.
    /// </summary>
    /// <param name="key">The handler key.</param>
    /// <param name="handler">When this method returns <see langword="true" />, contains the resolved handler.</param>
    /// <returns>
    /// <see langword="true" /> if a handler is registered for <paramref name="key" />; otherwise
    /// <see langword="false" />.
    /// </returns>
    bool TryGet(string key, out IAdjustmentHandler handler);
}
