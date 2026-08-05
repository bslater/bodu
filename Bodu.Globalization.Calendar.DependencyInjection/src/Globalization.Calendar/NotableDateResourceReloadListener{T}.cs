// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateResourceReloadListener{T}.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Bridges <see cref="IOptionsMonitor{TOptions}" /> change notifications to
/// <see cref="MutableNotableDateResourceProvider.Reload" />, rebuilding the resource through the registration's factory
/// whenever the monitored options change.
/// </summary>
/// <typeparam name="TOptions">The monitored options type.</typeparam>
/// <remarks>
/// <para>
/// A factory failure during a change notification is logged and swallowed: the previously loaded resource remains in
/// effect, and the configuration-reload thread is never faulted. Disposing the listener ends the subscription.
/// </para>
/// </remarks>
internal sealed class NotableDateResourceReloadListener<TOptions>
    : IDisposable
    where TOptions : class
{
    /// <summary>The change-token subscription, disposed to stop listening.</summary>
    private readonly IDisposable? _subscription;

    /// <summary>
    /// Initializes a new instance of the <see cref="NotableDateResourceReloadListener{TOptions}" /> class and starts
    /// listening for options changes.
    /// </summary>
    /// <param name="serviceProvider">The provider passed through to the resource factory.</param>
    /// <param name="monitor">The options monitor whose changes trigger a reload.</param>
    /// <param name="provider">The resource provider that receives the rebuilt resource.</param>
    /// <param name="resourceFactory">The factory producing a resource from the current options.</param>
    /// <param name="logger">
    /// The logger that records applied and failed reloads. <see langword="null" /> selects
    /// <see cref="NullLogger.Instance" />.
    /// </param>
    public NotableDateResourceReloadListener(
        IServiceProvider serviceProvider,
        IOptionsMonitor<TOptions> monitor,
        MutableNotableDateResourceProvider provider,
        Func<IServiceProvider, TOptions, NotableDateResource> resourceFactory,
        ILogger? logger)
    {
        ILogger log = logger ?? NullLogger.Instance;

        _subscription = monitor.OnChange(options =>
        {
            try
            {
                NotableDateResource resource = resourceFactory(serviceProvider, options);
                provider.Reload(resource);
                Log.OptionsReloadApplied(log, resource.ResourceId);
            }
            catch (Exception ex)
            {
                // The notification runs on the configuration-reload path; a broken rebuild must keep the previous
                // resource serving rather than fault that thread.
                Log.OptionsReloadFailed(log, ex);
            }
        });
    }

    /// <summary>
    /// Ends the options-change subscription.
    /// </summary>
    public void Dispose() =>
        _subscription?.Dispose();
}
