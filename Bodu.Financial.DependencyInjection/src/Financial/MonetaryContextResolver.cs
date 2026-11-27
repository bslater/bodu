// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MonetaryContextResolver.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Bodu.Financial.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Bodu.Financial;

/// <summary>
/// Implements <see cref="IMonetaryContextResolver" /> over the <see cref="IOptionsMonitor{TOptions}" /> of
/// <see cref="FinancialOptions" />, rebuilding its contexts on every options change and falling back to the keyed
/// contexts that <c>AddMonetaryContext</c> registers.
/// </summary>
/// <remarks>
/// The contexts are held as one immutable set that a rebuild replaces whole, so a reader sees either the contexts from
/// before a change or those from after it, never a mixture. A rebuild that fails validation is logged and leaves the
/// current set in effect: a broken configuration edit never faults the reload thread or a caller.
/// </remarks>
internal sealed partial class MonetaryContextResolver
    : IMonetaryContextResolver, IDisposable
{
    /// <summary>The provider whose keyed <see cref="MonetaryContext" /> services back unconfigured names.</summary>
    private readonly IServiceProvider _services;

    /// <summary>The logger that records applied and failed rebuilds.</summary>
    private readonly ILogger _logger;

    /// <summary>The options-change subscription, disposed to stop following changes.</summary>
    private readonly IDisposable? _subscription;

    /// <summary>The contexts currently served, replaced whole by each successful rebuild.</summary>
    private ContextSet _contexts;

    /// <summary>
    /// Initializes a new instance of the <see cref="MonetaryContextResolver" /> class, builds the contexts from the
    /// current options, and starts following their changes.
    /// </summary>
    /// <param name="monitor">The options monitor whose current value and changes configure the contexts.</param>
    /// <param name="services">
    /// The provider whose keyed <see cref="MonetaryContext" /> services supply the names the options do not configure.
    /// </param>
    /// <param name="logger">
    /// The logger that records applied and failed rebuilds. <see langword="null" /> selects
    /// <see cref="NullLogger.Instance" />.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="monitor" /> or <paramref name="services" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="OptionsValidationException">
    /// The current options do not describe valid contexts; <see cref="OptionsValidationException.Failures" /> names
    /// each failure.
    /// </exception>
    public MonetaryContextResolver(IOptionsMonitor<FinancialOptions> monitor, IServiceProvider services, ILogger? logger)
    {
        ThrowHelper.ThrowIfNull(monitor);
        ThrowHelper.ThrowIfNull(services);

        _services = services;
        _logger = logger ?? NullLogger.Instance;
        _contexts = Build(monitor.CurrentValue);
        _subscription = monitor.OnChange(OnOptionsChanged);
    }

    /// <inheritdoc />
    public MonetaryContext Default =>
        Volatile.Read(ref _contexts).Default;

    /// <inheritdoc />
    public MonetaryContext Get(string name)
    {
        ThrowHelper.ThrowIfNullOrWhiteSpace(name);

        return TryResolve(name, out MonetaryContext? context)
            ? context
            : throw new KeyNotFoundException(
                string.Format(CultureInfo.CurrentCulture, DependencyInjectionResourceStrings.IO_KeyNotFound_MonetaryContext, name));
    }

    /// <inheritdoc />
    public bool TryGet(string name, [NotNullWhen(true)] out MonetaryContext? context)
    {
        ThrowHelper.ThrowIfNull(name);

        if (string.IsNullOrWhiteSpace(name))
        {
            context = null;
            return false;
        }

        return TryResolve(name, out context);
    }

    /// <summary>
    /// Ends the options-change subscription. The contexts built last stay available.
    /// </summary>
    public void Dispose() =>
        _subscription?.Dispose();

    /// <summary>
    /// Builds the contexts that the specified options describe, collecting every validation failure.
    /// </summary>
    /// <param name="options">The options to build from.</param>
    /// <returns>The default context and the named contexts.</returns>
    /// <exception cref="OptionsValidationException">
    /// A context's settings are missing, have a blank name, or fail
    /// <see cref="MonetaryContextOptions.ToMonetaryContext" />; <see cref="OptionsValidationException.Failures" />
    /// names each one.
    /// </exception>
    private static ContextSet Build(FinancialOptions options)
    {
        List<string>? failures = null;
        MonetaryContext defaultContext = MonetaryContext.Default;

        if (options.DefaultMonetaryContext is MonetaryContextOptions defaultOptions)
        {
            try
            {
                defaultContext = defaultOptions.ToMonetaryContext();
            }
            catch (ArgumentException ex)
            {
                (failures ??= []).Add(
                    string.Format(CultureInfo.CurrentCulture, DependencyInjectionResourceStrings.Op_Invalid_DefaultMonetaryContextOptions, ex.Message));
            }
        }

        var named = new Dictionary<string, MonetaryContext>(StringComparer.OrdinalIgnoreCase);
        foreach (KeyValuePair<string, MonetaryContextOptions> entry in options.MonetaryContexts)
        {
            if (string.IsNullOrWhiteSpace(entry.Key))
            {
                (failures ??= []).Add(DependencyInjectionResourceStrings.Op_Invalid_MonetaryContextOptionsNameBlank);
                continue;
            }

            if (entry.Value is null)
            {
                (failures ??= []).Add(
                    string.Format(CultureInfo.CurrentCulture, DependencyInjectionResourceStrings.Op_Invalid_MonetaryContextOptionsMissing, entry.Key));
                continue;
            }

            try
            {
                named[entry.Key] = entry.Value.ToMonetaryContext();
            }
            catch (ArgumentException ex)
            {
                (failures ??= []).Add(
                    string.Format(CultureInfo.CurrentCulture, DependencyInjectionResourceStrings.Op_Invalid_MonetaryContextOptions, entry.Key, ex.Message));
            }
        }

        return failures is null
            ? new ContextSet(defaultContext, named.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase))
            : throw new OptionsValidationException(Options.DefaultName, typeof(FinancialOptions), failures);
    }

    /// <summary>
    /// Rebuilds the contexts after an options change, keeping the current ones when the new options are not valid.
    /// </summary>
    /// <param name="options">The changed options.</param>
    /// <param name="name">The name of the options instance that changed.</param>
    private void OnOptionsChanged(FinancialOptions options, string? name)
    {
        // AddFinancialService binds the unnamed instance; a change to a named instance does not configure the contexts.
        if (!string.Equals(name ?? Options.DefaultName, Options.DefaultName, StringComparison.Ordinal))
            return;

        try
        {
            ContextSet contexts = Build(options);
            Volatile.Write(ref _contexts, contexts);
            MonetaryContextLog.ReloadApplied(_logger, contexts.Named.Count);
        }
        catch (OptionsValidationException ex)
        {
            // The notification runs on the configuration-reload path; invalid settings must keep the previous contexts
            // serving rather than fault that thread.
            MonetaryContextLog.ReloadFailed(_logger, ex);
        }
    }

    /// <summary>
    /// Looks up a name among the configured contexts and then among the keyed contexts the container registers.
    /// </summary>
    /// <param name="name">The context name, neither empty nor white space.</param>
    /// <param name="context">
    /// When this method returns <see langword="true" />, the context; otherwise <see langword="null" />.
    /// </param>
    /// <returns><see langword="true" /> when a context was found; otherwise <see langword="false" />.</returns>
    private bool TryResolve(string name, [NotNullWhen(true)] out MonetaryContext? context)
    {
        if (Volatile.Read(ref _contexts).Named.TryGetValue(name, out context))
            return true;

        context = (_services as IKeyedServiceProvider)?.GetKeyedService(typeof(MonetaryContext), name) as MonetaryContext;
        return context is not null;
    }
}
