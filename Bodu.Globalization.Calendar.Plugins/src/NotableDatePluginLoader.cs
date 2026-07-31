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
/// NotableDateService service = new(resource, new NotableDateServiceOptions { Algorithms = registry });
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
    /// <remarks>
    /// <para>
    /// This overload offers a weaker guarantee than the path-based
    /// <see cref="LoadFrom(string, IPluginTrustPolicy, ILogger?)" /> and must not be used for untrusted input. The
    /// assembly is <b>already loaded</b> when the trust policy runs, so any module initializer or type-load side effect
    /// it carries has had the opportunity to execute <i>before</i> the trust check — a rejection here cannot prevent
    /// code that ran at load time.
    /// </para>
    /// <para>
    /// The <see cref="PluginTrustContext.FileHash" /> supplied to the policy is computed by re-reading
    /// <see cref="Assembly.Location" /> from disk, not from the bytes that were actually loaded, so it does not close
    /// the time-of-check/time-of-use gap that the path-based overload avoids by hashing the in-memory image it maps.
    /// For untrusted assemblies, load through the path-based overload instead.
    /// </para>
    /// </remarks>
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
    /// <exception cref="ArgumentException"><paramref name="assemblyPath" /> is empty.</exception>
    /// <exception cref="FileNotFoundException">No file exists at <paramref name="assemblyPath" />.</exception>
    /// <exception cref="DirectoryNotFoundException">
    /// A directory component of <paramref name="assemblyPath" /> does not exist.
    /// </exception>
    /// <exception cref="UnauthorizedAccessException">The file cannot be read.</exception>
    /// <exception cref="NotableDatePluginException">
    /// The file is not a valid managed assembly image.
    /// </exception>
    /// <exception cref="PluginNotTrustedException">The trust policy rejected the assembly.</exception>
    /// <exception cref="PluginMissingAttributeException">The assembly does not declare a plugin attribute.</exception>
    /// <exception cref="PluginActivationException">
    /// The plugin type could not be loaded or activated, or is not a plugin.
    /// </exception>
    public static INotableDatePlugin LoadFrom(string assemblyPath, IPluginTrustPolicy trustPolicy, ILogger? logger = null)
    {
        ThrowHelper.ThrowIfNullOrEmpty(assemblyPath);
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

        // Resolve the plugin's own dependencies from its directory (via its .deps.json) into the plugin context.
        // Shared contracts still unify on the default context because the fallback probe runs before this handler:
        // the handler only fires for assemblies the default context cannot supply.
        AssemblyDependencyResolver resolver = new(fullPath);
        context.Resolving += (loadContext, assemblyName) =>
        {
            string? resolvedPath = resolver.ResolveAssemblyToPath(assemblyName);
            return resolvedPath is null ? null : loadContext.LoadFromAssemblyPath(resolvedPath);
        };

        try
        {
            Assembly assembly;
            try
            {
                using MemoryStream stream = new(image, writable: false);
                assembly = context.LoadFromStream(stream);
            }
            catch (BadImageFormatException ex)
            {
                throw new NotableDatePluginException(
                    string.Format(CultureInfo.CurrentCulture, PluginsResourceStrings.Op_Invalid_PluginImage, fullPath),
                    ex);
            }

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
    /// Registers the algorithms contributed by a plugin with a registry, rejecting keys that collide with built-in
    /// algorithms or existing registrations.
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
    /// <exception cref="NotableDatePluginException">
    /// The plugin's contribution is malformed or faults, or a contributed key collides with a built-in algorithm or an
    /// existing registration. The registry is left unchanged.
    /// </exception>
    public static int RegisterAlgorithms(INotableDatePlugin plugin, NotableDateAlgorithmRegistry registry, ILogger? logger = null) =>
        RegisterAlgorithms(plugin, registry, PluginAlgorithmRegistrationOptions.Default, logger);

    /// <summary>
    /// Registers the algorithms contributed by a plugin with a registry under the supplied collision policy.
    /// </summary>
    /// <param name="plugin">The plugin whose algorithms are registered.</param>
    /// <param name="registry">The registry to populate.</param>
    /// <param name="options">The options controlling how colliding keys are treated.</param>
    /// <param name="logger">
    /// The logger that receives diagnostics for the registration. <see langword="null" /> selects
    /// <see cref="NullLogger.Instance" />.
    /// </param>
    /// <returns>The number of algorithms registered.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="plugin" />, <paramref name="registry" />, or <paramref name="options" /> is
    /// <see langword="null" />.
    /// </exception>
    /// <exception cref="NotableDatePluginException">
    /// The plugin's contribution is malformed or faults, or a contributed key collides with a built-in algorithm or an
    /// existing registration while <see cref="PluginAlgorithmRegistrationOptions.AllowOverride" /> is
    /// <see langword="false" />. The registry is left unchanged.
    /// </exception>
    /// <remarks>
    /// <para>
    /// Registration is atomic: the plugin's contribution is fully staged and validated before the registry is touched,
    /// so a faulting or malformed plugin never leaves the registry partially mutated. With
    /// <see cref="PluginAlgorithmRegistrationOptions.AllowOverride" /> enabled, each replacement of a built-in or
    /// existing key is logged at <see cref="LogLevel.Warning" />.
    /// </para>
    /// </remarks>
    public static int RegisterAlgorithms(
        INotableDatePlugin plugin,
        NotableDateAlgorithmRegistry registry,
        PluginAlgorithmRegistrationOptions options,
        ILogger? logger = null)
    {
        ThrowHelper.ThrowIfNull(plugin);
        ThrowHelper.ThrowIfNull(registry);
        ThrowHelper.ThrowIfNull(options);

        ILogger log = logger ?? NullLogger.Instance;
        string pluginTypeName = plugin.GetType().FullName ?? plugin.GetType().Name;

        if (plugin is not INotableDateAlgorithmPlugin algorithmPlugin)
        {
            Log.PluginContributesNoAlgorithms(log, pluginTypeName);
            return 0;
        }

        // Stage the whole contribution before touching the shared registry: the plugin's enumeration is untrusted code
        // and may fault or produce malformed entries, and a partial commit would leave the registry inconsistent.
        Dictionary<string, INotableDateAlgorithm> staged = new(StringComparer.Ordinal);
        try
        {
            IEnumerable<KeyValuePair<string, INotableDateAlgorithm>>? pairs = algorithmPlugin.GetAlgorithms()
                ?? throw new NotableDatePluginException(
                    string.Format(CultureInfo.CurrentCulture, PluginsResourceStrings.Op_Invalid_PluginAlgorithmsNull, pluginTypeName));

            foreach (KeyValuePair<string, INotableDateAlgorithm> pair in pairs)
            {
                if (pair.Key is null || pair.Value is null)
                {
                    throw new NotableDatePluginException(
                        string.Format(CultureInfo.CurrentCulture, PluginsResourceStrings.Op_Invalid_PluginAlgorithmEntry, pluginTypeName));
                }

                if (staged.ContainsKey(pair.Key))
                {
                    throw new NotableDatePluginException(
                        string.Format(CultureInfo.CurrentCulture, PluginsResourceStrings.Op_Invalid_PluginAlgorithmKeyCollision, pluginTypeName, pair.Key));
                }

                staged.Add(pair.Key, pair.Value);
            }
        }
        catch (Exception ex) when (ex is not NotableDatePluginException)
        {
            throw new NotableDatePluginException(
                string.Format(CultureInfo.CurrentCulture, PluginsResourceStrings.Op_Invalid_PluginAlgorithmsFaulted, pluginTypeName),
                ex);
        }

        // Validate every key against the built-ins and the target registry before committing anything, keeping the
        // reject path side-effect free.
        List<string> overridden = new();
        foreach (string key in staged.Keys)
        {
            if (!registry.Contains(key) && !AlgorithmDateStrategy.IsKnownKey(key))
                continue;

            if (!options.AllowOverride)
            {
                throw new NotableDatePluginException(
                    string.Format(CultureInfo.CurrentCulture, PluginsResourceStrings.Op_Invalid_PluginAlgorithmKeyCollision, pluginTypeName, key));
            }

            overridden.Add(key);
        }

        foreach (KeyValuePair<string, INotableDateAlgorithm> pair in staged)
            registry.Register(pair.Key, pair.Value);

        foreach (string key in overridden)
            Log.PluginAlgorithmOverridesKey(log, key, pluginTypeName);

        Log.PluginAlgorithmsRegistered(log, staged.Count, pluginTypeName);

        return staged.Count;
    }

    /// <summary>
    /// Evaluates the trust policy against the supplied context and, when trusted, activates the assembly's declared
    /// plugin.
    /// </summary>
    /// <param name="assembly">
    /// The candidate assembly whose plugin attribute is read after the trust check passes.
    /// </param>
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
        catch (Exception ex) when (ex
            is MissingMethodException
            or TargetInvocationException
            or MemberAccessException
            or TypeInitializationException
            or TypeLoadException
            or FileNotFoundException
            or FileLoadException
            or BadImageFormatException)
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
