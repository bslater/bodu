// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateContext.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Provides the ambient <see cref="INotableDateService" /> consulted by the <c>NotableDateTimeExtensions</c> and
/// <c>NotableDateOnlyExtensions</c> overloads that do not accept an explicit service argument.
/// </summary>
/// <remarks>
/// <para>
/// The context exposes a single <see cref="Default" /> service that callers may replace at composition time — typically
/// once during host start-up — so that downstream code can use the parameterless extension-method overloads (for
/// example, <c>date.NextWorkingDay()</c>) without threading a service instance through every call site.
/// </para>
/// <para>
/// When <see cref="Default" /> has not been assigned, the property lazily materializes a new
/// <see cref="NotableDateService" /> configured with the embedded minimal rule set. The lazy fallback is constructed
/// under <see cref="System.Threading.LazyThreadSafetyMode.ExecutionAndPublication" /> and is therefore safe to consume
/// from multiple threads.
/// </para>
/// <para>
/// Hosts that prefer explicit dependency injection should pass an <see cref="INotableDateService" /> directly to the
/// service-aware extension overloads instead of relying on this ambient context. The ambient default is intended for
/// application-wide convenience, not as a replacement for proper composition.
/// </para>
/// </remarks>
/// <example>
/// <para>
/// Replace the ambient default at application start-up with a richly configured service:
/// </para>
/// <code>
///<![CDATA[
/// In Program.cs / Startup.cs:
/// NotableDateContext.Default = new NotableDateService(
///     ruleProviders: new[] { AustraliaCalendarData.CreateProvider() },
///     workingDaysOfWeek: WorkingDaysOfWeek.MondayToFriday);
///
/// Anywhere downstream:
/// DateTime nextWorkingDay = DateTime.Today.NextWorkingDay(territoryCode: "AU-NSW");
///]]>
/// </code>
/// </example>
public static class NotableDateContext
{
    /// <summary>
    /// The lazily-constructed fallback service used when no explicit default has been assigned.
    /// </summary>
    private static readonly Lazy<INotableDateService> s_lazyDefault =
        new(() => new NotableDateService(), System.Threading.LazyThreadSafetyMode.ExecutionAndPublication);

    /// <summary>
    /// The explicitly assigned ambient service, or <see langword="null" /> when reverted to the lazy fallback.
    /// </summary>
    private static INotableDateService? s_default;

    /// <summary>
    /// Gets or sets the ambient <see cref="INotableDateService" /> consulted by parameterless extension-method
    /// overloads.
    /// </summary>
    /// <returns>
    /// The service previously assigned via the setter, or — when no service has been assigned — a lazily-constructed
    /// <see cref="NotableDateService" /> backed by the embedded minimal rule set.
    /// </returns>
    /// <value>
    /// A non-<see langword="null" /> <see cref="INotableDateService" /> instance. Assigning <see langword="null" />
    /// reverts the property to its lazy fallback.
    /// </value>
    /// <remarks>
    /// <para>
    /// Reading this property never throws and always yields a usable service, so extension-method consumers do not need
    /// to guard against an unconfigured context. Hosts that wish to enforce explicit configuration should validate
    /// <see cref="Default" /> against an expected sentinel during start-up.
    /// </para>
    /// <para>
    /// The setter is thread-safe with respect to the underlying field write; concurrent readers may briefly observe
    /// either the old or new instance. Callers should perform assignment during start-up before any other thread begins
    /// to consume the ambient service.
    /// </para>
    /// </remarks>
    [AllowNull]
    public static INotableDateService Default
    {
        get => s_default ?? s_lazyDefault.Value;
        set => s_default = value;
    }

    /// <summary>
    /// Reverts <see cref="Default" /> to its lazy fallback service, discarding any previously assigned instance.
    /// </summary>
    /// <remarks>
    /// This method is intended primarily for unit tests that need to restore the ambient context to a known baseline
    /// between cases. Production code should rely on a single assignment of <see cref="Default" /> during host
    /// composition.
    /// </remarks>
    public static void Reset() => Default = null;
}
