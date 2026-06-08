// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ExchangeRateProviderSelectionPolicy.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.ComponentModel;

namespace Bodu.Financial;

/// <summary>
/// Describes how a multi-provider lookup picks between candidate providers when more than one can satisfy the requested
/// pair.
/// </summary>
/// <remarks>
/// <para>
/// Only <see cref="ProviderPriorityFirst" /> is implemented in v1.0; the remaining members exist so the public API can
/// express the intent today while later releases add the corresponding lookup strategies.
/// </para>
/// </remarks>
public enum ExchangeRateProviderSelectionPolicy
{
    /// <summary>
    /// Returns the first successful result from the priority-ordered set of providers. A preferred provider's
    /// fallback-date hit therefore beats a lower-priority provider's exact-date hit. This matches the existing v1.0
    /// behaviour and is the default.
    /// </summary>
    ProviderPriorityFirst = 0,

    /// <summary>
    /// Reserved: prefer any provider's exact-date hit over any provider's fallback-date hit, and only then apply the
    /// priority list to break ties. Not yet implemented; supplying it throws <see cref="NotSupportedException" />.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    ExactDateBeforeProviderFallback = 1,

    /// <summary>
    /// Reserved: pick the provider whose resolved date is closest to the requested date (using absolute offset),
    /// breaking ties by the priority list. Not yet implemented; supplying it throws
    /// <see cref="NotSupportedException" />.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    SmallestDateOffsetThenProviderPriority = 2,
}
