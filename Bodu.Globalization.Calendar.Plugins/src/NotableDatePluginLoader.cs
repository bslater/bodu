// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDatePluginLoader.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using System.Reflection;
using System.Runtime.Loader;
using System.Security.Cryptography;
using Bodu.Globalization.Calendar;

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
/// <seealso cref="IPluginTrustPolicy" />
/// <seealso cref="INotableDateAlgorithmPlugin" />
/// <seealso cref="NotableDateAlgorithmRegistry" />
public static class NotableDatePluginLoader
{
    /// <summary>
    /// Loads the plugin declared by an already-loaded assembly after a trust check.
    /// </summary>
    /// <param name="assembly">The assembly declaring the plugin via <see cref="NotableDatePluginAttribute" />.</param>
    /// <param name="trustPolicy">The policy that must trust the assembly before its plugin is activated.</param>
    /// <returns>The activated plugin.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="assembly" /> or <paramref name="trustPolicy" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="PluginNotTrustedException">The trust policy rejected the assembly.</exception>
    /// <exception cref="PluginMissingAttributeException">The assembly does not declare a plugin attribute.</exception>
    /// <exception cref="PluginActivationException">The plugin type could not be activated or is not a plugin.</exception>
    public static INotableDatePlugin LoadFrom(Assembly assembly, IPluginTrustPolicy trustPolicy)
    {
        ThrowHelper.ThrowIfNull(assembly);
        ThrowHelper.ThrowIfNull(trustPolicy);

        AssemblyName assemblyName = assembly.GetName();
        string name = assemblyName.Name ?? assembly.FullName ?? "<unknown>";
        string? path = string.IsNullOrEmpty(assembly.Location) ? null : assembly.Location;
        byte[]? hash = path is not null && File.Exists(path) ? ComputeHash(path) : null;
        string? token = FormatPublicKeyToken(assemblyName.GetPublicKeyToken());

        PluginTrustResult trust = trustPolicy.Evaluate(new PluginTrustContext(name, path, hash, token));
        if (!trust.IsTrusted)
        {
            throw new PluginNotTrustedException(
                string.Format(CultureInfo.InvariantCulture, PluginsResourceStrings.Op_NotTrusted_Plugin, name, trust.Reason ?? string.Empty),
                name,
                trust.Reason);
        }

        NotableDatePluginAttribute? attribute = assembly.GetCustomAttribute<NotableDatePluginAttribute>();
        if (attribute is null)
        {
            throw new PluginMissingAttributeException(
                string.Format(CultureInfo.InvariantCulture, PluginsResourceStrings.Op_Missing_PluginAttribute, name),
                name);
        }

        return Activate(attribute.PluginType);
    }

    /// <summary>
    /// Loads the plugin declared by an assembly at a file path, into a dedicated load context, after a trust check.
    /// </summary>
    /// <param name="assemblyPath">The file path of the plugin assembly.</param>
    /// <param name="trustPolicy">The policy that must trust the assembly before its plugin is activated.</param>
    /// <returns>The activated plugin.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="assemblyPath" /> or <paramref name="trustPolicy" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="PluginNotTrustedException">The trust policy rejected the assembly.</exception>
    /// <exception cref="PluginMissingAttributeException">The assembly does not declare a plugin attribute.</exception>
    /// <exception cref="PluginActivationException">The plugin type could not be activated or is not a plugin.</exception>
    public static INotableDatePlugin LoadFrom(string assemblyPath, IPluginTrustPolicy trustPolicy)
    {
        ThrowHelper.ThrowIfNull(assemblyPath);
        ThrowHelper.ThrowIfNull(trustPolicy);

        string fullPath = Path.GetFullPath(assemblyPath);
        AssemblyLoadContext context = new($"NotableDatePlugin:{Path.GetFileNameWithoutExtension(fullPath)}", isCollectible: false);
        Assembly assembly = context.LoadFromAssemblyPath(fullPath);

        return LoadFrom(assembly, trustPolicy);
    }

    /// <summary>
    /// Registers the algorithms contributed by a plugin with a registry.
    /// </summary>
    /// <param name="plugin">The plugin whose algorithms are registered.</param>
    /// <param name="registry">The registry to populate.</param>
    /// <returns>The number of algorithms registered.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="plugin" /> or <paramref name="registry" /> is <see langword="null" />.
    /// </exception>
    public static int RegisterAlgorithms(INotableDatePlugin plugin, NotableDateAlgorithmRegistry registry)
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

        return count;
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
                string.Format(CultureInfo.InvariantCulture, PluginsResourceStrings.Op_Invalid_PluginActivation, pluginType.FullName ?? pluginType.Name),
                pluginType,
                ex);
        }

        if (instance is not INotableDatePlugin plugin)
        {
            throw new PluginActivationException(
                string.Format(CultureInfo.InvariantCulture, PluginsResourceStrings.Op_Invalid_PluginType, pluginType.FullName ?? pluginType.Name),
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
