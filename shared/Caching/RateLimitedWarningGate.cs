// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RateLimitedWarningGate.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Caching;

/// <summary>
/// Rate-limits a repeated best-effort failure warning to at most one emission per cooldown window, tracking the number
/// of failures suppressed since the previous warning.
/// </summary>
/// <remarks>
/// <para>
/// The first failure after construction, and the first after each cooldown elapses, claims the single warning slot and
/// reports the count of failures suppressed since the previous warning; failures inside the window only increment that
/// count. The slot is claimed with <see cref="Interlocked.CompareExchange(ref long, long, long)" /> so that under
/// concurrent swallows exactly one caller emits per window. The cooldown is measured against the injected
/// <see cref="TimeProvider" /> so the rate-limiting is deterministic under test. Callers own the actual log emission,
/// so each cache can route the warning through its own source-generated logger.
/// </para>
/// <para>
/// This type is a shared cache-infrastructure primitive: it is linked as source into each caching package that needs
/// it (namespace <c>Bodu.Caching</c>) and compiled <see langword="internal" /> per assembly, so no public surface is
/// exposed and no two assemblies collide on the type identity.
/// </para>
/// </remarks>
internal sealed class RateLimitedWarningGate
{
    /// <summary>
    /// The default cooldown every Bodu cache backend rate-limits its best-effort degradation warning with: at most one
    /// emitted warning per minute.
    /// </summary>
    public static readonly TimeSpan DefaultCooldown = TimeSpan.FromMinutes(1);

    /// <summary>The clock the cooldown is measured against.</summary>
    private readonly TimeProvider _timeProvider;

    /// <summary>The minimum interval, in ticks, between two emitted warnings.</summary>
    private readonly long _cooldownTicks;

    /// <summary>The UTC tick count of the last emitted warning, or <see cref="long.MinValue" /> when none has been emitted.</summary>
    private long _lastWarnUtcTicks = long.MinValue;

    /// <summary>The number of failures suppressed since the last emitted warning.</summary>
    private int _suppressedSinceLastWarn;

    /// <summary>
    /// Initializes a new instance of the <see cref="RateLimitedWarningGate" /> class.
    /// </summary>
    /// <param name="timeProvider">The clock the cooldown is measured against; <see langword="null" /> selects <see cref="TimeProvider.System" />.</param>
    /// <param name="cooldown">The minimum interval between two emitted warnings.</param>
    public RateLimitedWarningGate(TimeProvider? timeProvider, TimeSpan cooldown)
    {
        _timeProvider = timeProvider ?? TimeProvider.System;
        _cooldownTicks = cooldown.Ticks;
    }

    /// <summary>
    /// Attempts to claim the single warning slot for the current cooldown window.
    /// </summary>
    /// <param name="suppressedSinceLastWarn">
    /// When this method returns <see langword="true" />, the number of failures suppressed since the previous warning
    /// (the counter is reset); otherwise zero.
    /// </param>
    /// <returns>
    /// <see langword="true" /> when the caller should emit a warning; <see langword="false" /> when this failure was
    /// folded into the suppressed count instead.
    /// </returns>
    public bool TryClaimWarning(out int suppressedSinceLastWarn)
    {
        long now = _timeProvider.GetUtcNow().UtcTicks;
        long last = Interlocked.Read(ref _lastWarnUtcTicks);

        // Emit when no warning has been logged yet, or the cooldown has elapsed since the last one. The MinValue
        // sentinel is checked before the subtraction so a never-warned instance does not underflow the comparison.
        bool due = last == long.MinValue || (now - last) >= _cooldownTicks;
        if (due && Interlocked.CompareExchange(ref _lastWarnUtcTicks, now, last) == last)
        {
            suppressedSinceLastWarn = Interlocked.Exchange(ref _suppressedSinceLastWarn, 0);
            return true;
        }

        Interlocked.Increment(ref _suppressedSinceLastWarn);
        suppressedSinceLastWarn = 0;
        return false;
    }
}
