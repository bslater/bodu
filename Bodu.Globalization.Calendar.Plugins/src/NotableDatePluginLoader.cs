// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDatePluginLoader.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using System.Reflection;
using System.Runtime.Loader;
using System.Security.Cryptography;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Bodu.Globalization.Calendar.Plugins;

/// <summary>
/// Loads notable-date plugins from assemblies, gating activation behind an <see cref="IPluginTrustPolicy" /> and
/// registering the contributed algorithms with a <see cref="NotableDateAlgorithmRegistry" />.
/// </summary>
/// <remarks>
/// <para>
/// Trust is evaluated before the plugin's entry-point type is activated, so an untrusted assembly's constructor never
/// runs. The file-path overload loads the assembly into a dedicated <see cref="AssemblyLoadContext" />.
/// </para>
/// <para>
/// <strong>When to use.</strong> Load a plugin with one of the <c>LoadFrom</c> overloads, register its contributed
/// algorithms into a <see cref="NotableDateAlgorithmRegistry" /> with <see cref="RegisterAlgorithms" />, then pass that
/// registry to both <see cref="NotableDateResourceLoader" /> (so documents may reference the plugin's algorithm keys
/// during validation) and the <see cref="NotableDateService" /> (so they resolve at query time). Always supply a
/// production-grade <see cref="IPluginTrustPolicy" /> — <see cref="AllowAllPluginTrustPolicy" /> is for development
/// only.
/// </para>
/// <para>
/// <strong>Logging.</strong> Each <c>LoadFrom</c> / <see cref="RegisterAlgorithms" /> overload accepts an optional
/// <see cref="ILogger" /> (defaulting to <see cref="NullLogger.Instance" />, so logging is opt-in). When supplied it
/// records a trust-policy rejection (<see cref="LogLevel.Warning" />), a passed trust check (<see cref="LogLevel.Debug" />),
/// an activated plugin (<see cref="LogLevel.Information" />), and the number of algorithms a plugin contributed (<see cref="LogLevel.Information" />).
/// These levels are fixed.
/// </para>
/// </remarks>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// // Gate activation behind a strong-name trust policy, then register the plugin's algorithms.
/// IPluginTrustPolicy trust = new StrongNamePluginTrustPolicy(new[] { "c0ffee1234567890" });
/// INotableDatePlugin plugin = NotableDatePluginLoader.LoadFrom("Contoso.Calendar.Plugin.dll", trust);
///
/// NotableDateAlgorithmRegistry registry = new();
/// int registered = NotableDatePluginLoader.RegisterAlgorithms(plugin, registry);
///
/// // Wire the registry into the load and resolve pipeline so rules can reference the plugin keys.
/// NotableDateResource resource = NotableDateResourceLoader.Load(documentXml, _ => null, registry);
/// NotableDateService service = new(resource, registry);
///]]>
/// </code>
/// </example>
/// <seealso cref="IPluginTrustPolicy" /> <seealso cref="INotableDateAlgorithmPlugin" />
/// <seealso cref="NotableDateAlgorithmRegistry" /> <seealso href="../guides/calendar/algorithms.html">Date calculation
/// algorithms (guide)</seealso>
public static class NotableDatePluginLoader
{
    /// <summary>
    /// Loads the plugin declared by an already-loaded assembly after a trust check.
    /// </summary>
    /// <param name="assembly">The assembly declaring the plugin via <see cref="NotableDatePluginAttribute" />.</param>
    /// <param name="trustPolicy">The policy that must trust the assembly before its plugin is activated.</param>
    /// <param name="logger">
    /// The logger that receives diagnostics for the load. <see langword="null" /> selects
    /// <see cref="NullLogger.Instance" />.
    /// </param>
    /// <returns>The activated plugin.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="assembly" /> or <paramref name="trustPolicy" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="PluginNotTrustedException">The trust policy rejected the assembly.</exception>
    /// <exception cref="PluginMissingAttributeException">The assembly does not declare a plugin attribute.</exception>
    /// <exception cref="PluginActivationException">
    /// The plugin type could not be activated or is not a plugin.
    /// </exception>
    public static INotableDatePlugin LoadFrom(Assembly assembly, IPluginTrustPolicy trustPolicy, ILogger? logger = null)
    {
        ThrowHelper.ThrowIfNull(assembly);
        ThrowHelper.ThrowIfNull(trustPolicy);

        ILogger log = logger ?? NullLogger.Instance;

        AssemblyName assemblyName = assembly.GetName();
        string name = assemblyName.Name ?? assembly.FullName ?? "<unknown>";
        string? path = string.IsNullOrEmpty(assembly.Location) ? null : assembly.Location;
        byte[]? hash = path is not null && File.Exists(path) ? ComputeHash(path) : null;
        string? token = FormatPublicKeyToken(assemblyName.GetPublicKeyToken());

        return EvaluateTrustAndActivate(assembly, new PluginTrustContext(name, path, hash, token), trustPolicy, log);
    }

    /// <summary>
    /// Loads the plugin declared by an assembly at a file path, into a dedicated load context, after a trust check.
    /// </summary>
    /// <param name="assemblyPath">The file path of the plugin assembly.</param>
    /// <param name="trustPolicy">The policy that must trust the assembly before its plugin is activated.</param>
    /// <param name="logger">
    /// The logger that receives diagnostics for the load. <see langword="null" /> selects
    /// <see cref="NullLogger.Instance" />.
    /// </param>
    /// <returns>The activated plugin.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="assemblyPath" /> or <paramref name="trustPolicy" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="PluginNotTrustedException">The trust policy rejected the assembly.</exception>
    /// <exception cref="PluginMissingAttributeException">The assembly does not declare a plugin attribute.</exception>
    /// <exception cref="PluginActivationException">
    /// The plugin type could not be activated or is not a plugin.
    /// </exception>
    public static INotableDatePlugin LoadFrom(string assemblyPath, IPluginTrustPolicy trustPolicy, ILogger? logger = null)
    {
        ThrowHelper.ThrowIfNull(assemblyPath);
        ThrowHelper.ThrowIfNull(trustPolicy);

        ILogger log = logger ?? NullLogger.Instance;

        string fullPath = Path.GetFullPath(assemblyPath);

        // Read the image exactly once and hash those same bytes, so the digest the trust policy verifies is the digest
        // of the bytes that are loaded. Hashing a re-read of the file (as an already-loaded assembly must) would open a
        // time-of-check/time-of-use gap an attacker could use to swap the file between the load and the hash.
        byte[] image = File.ReadAllBytes(fullPath);
        byte[] hash = SHA256.HashData(image);

        // Use a collectible context so a rejected — or failed — plugin can be unloaded rather than pinned for the life
        // of the process. Mapping the image into the context does not run plugin code; activation, which does, happens
        // only after the trust check below passes.
        AssemblyLoadContext context = new($"NotableDatePlugin:{Path.GetFileNameWithoutExtension(fullPath)}", isCollectible: true);
        try
        {
            using MemoryStream stream = new(image, writable: false);
            Assembly assembly = context.LoadFromStream(stream);

            AssemblyName assemblyName = assembly.GetName();
            string name = assemblyName.Name ?? assembly.FullName ?? "<unknown>";
            string? token = FormatPublicKeyToken(assemblyName.GetPublicKeyToken());

            return EvaluateTrustAndActivate(assembly, new PluginTrustContext(name, fullPath, hash, token), trustPolicy, log);
        }
        catch
        {
            context.Unload();
            throw;
        }
    }

    /// <summary>
    /// Registers the algorithms contributed by a plugin with a registry.
    /// </summary>
    /// <param name="plugin">The plugin whose algorithms are registered.</param>
    /// <param name="registry">The registry to populate.</param>
    /// <param name="logger">
    /// The logger that receives diagnostics for the registration. <see langword="null" /> selects
    /// <see cref="NullLogger.Instance" />.
    /// </param>
    /// <returns>The number of algorithms registered.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="plugin" /> or <paramref name="registry" /> is <see langword="null" />.
    /// </exception>
    public static int RegisterAlgorithms(INotableDatePlugin plugin, NotableDateAlgorithmRegistry registry, ILogger? logger = null)
    {
        ThrowHelper.ThrowIfNull(plugin);
        ThrowHelper.ThrowIfNull(registry);

        if (plugin is not INotableDateAlgorithmPlugin algorithmPlugin)
            return 0;

        int count = 0;
        foreach (KeyValuePair<string, INotableDateAlgorithm> pair in algorithmPlugin.GetAlgorithms())
        {
            registry.Register(pair.Key, pair.Value);
            count++;
        }

        Log.PluginAlgorithmsRegistered(logger ?? NullLogger.Instance, count, plugin.GetType().FullName ?? plugin.GetType().Name);

        return count;
    }

    /// <summary>
    /// Evaluates the trust policy against the supplied context and, when trusted, activates the assembly's declared
    /// plugin.
    /// </summary>
    /// <param name="assembly">The candidate assembly whose plugin attribute is read after the trust check passes.</param>
    /// <param name="trustContext">The metadata the trust policy evaluates.</param>
    /// <param name="trustPolicy">The policy that must trust the assembly before its plugin is activated.</param>
    /// <param name="log">The logger that receives diagnostics for the load.</param>
    /// <returns>The activated plugin.</returns>
    /// <exception cref="PluginNotTrustedException">The trust policy rejected the assembly.</exception>
    /// <exception cref="PluginMissingAttributeException">The assembly does not declare a plugin attribute.</exception>
    /// <exception cref="PluginActivationException">
    /// The plugin type could not be activated or is not a plugin.
    /// </exception>
    private static INotableDatePlugin EvaluateTrustAndActivate(Assembly assembly, PluginTrustContext trustContext, IPluginTrustPolicy trustPolicy, ILogger log)
    {
        string name = trustContext.AssemblyName;

        PluginTrustResult trust = trustPolicy.Evaluate(trustContext);
        if (!trust.IsTrusted)
        {
            Log.PluginTrustRejected(log, name, trust.Reason ?? string.Empty);
            throw new PluginNotTrustedException(
                string.Format(CultureInfo.CurrentCulture, PluginsResourceStrings.Op_NotTrusted_Plugin, name, trust.Reason ?? string.Empty),
                name,
                trust.Reason);
        }

        Log.PluginTrusted(log, name);

        NotableDatePluginAttribute? attribute = assembly.GetCustomAttribute<NotableDatePluginAttribute>() ?? throw new PluginMissingAttributeException(
                string.Format(CultureInfo.CurrentCulture, PluginsResourceStrings.Op_Missing_PluginAttribute, name),
                name);

        INotableDatePlugin plugin = Activate(attribute.PluginType);
        Log.PluginActivated(log, name, attribute.PluginType.FullName ?? attribute.PluginType.Name);

        return plugin;
    }

    /// <summary>
    /// Activates a plugin type, validating that it implements <see cref="INotableDatePlugin" />.
    /// </summary>
    /// <param name="pluginType">The plugin entry-point type.</param>
    /// <returns>The activated plugin.</returns>
    /// <exception cref="PluginActivationException">The type could not be activated or is not a plugin.</exception>
    private static INotableDatePlugin Activate(Type pluginType)
    {
        object? instance;
        try
        {
            instance = Activator.CreateInstance(pluginType);
        }
        catch (Exception ex) when (ex is MissingMethodException or TargetInvocationException or MemberAccessException)
        {
            throw new PluginActivationException(
                string.Format(CultureInfo.CurrentCulture, PluginsResourceStrings.Op_Invalid_PluginActivation, pluginType.FullName ?? pluginType.Name),
                pluginType,
                ex);
        }

        if (instance is not INotableDatePlugin plugin)
        {
            throw new PluginActivationException(
                string.Format(CultureInfo.CurrentCulture, PluginsResourceStrings.Op_Invalid_PluginType, pluginType.FullName ?? pluginType.Name),
                pluginType);
        }

        return plugin;
    }

    /// <summary>
    /// Computes the SHA-256 hash of a file.
    /// </summary>
    /// <param name="path">The file path.</param>
    /// <returns>The hash bytes.</returns>
    private static byte[] ComputeHash(string path)
    {
        using FileStream stream = File.OpenRead(path);
        return SHA256.HashData(stream);
    }

    /// <summary>
    /// Formats a strong-name public-key token as lowercase hexadecimal.
    /// </summary>
    /// <param name="token">The token bytes, or <see langword="null" /> when the assembly is not strong-named.</param>
    /// <returns>The lowercase-hex token, or <see langword="null" /> when there is no token.</returns>
    private static string? FormatPublicKeyToken(byte[]? token) =>
        token is null || token.Length == 0 ? null : Convert.ToHexString(token).ToLowerInvariant();
}
