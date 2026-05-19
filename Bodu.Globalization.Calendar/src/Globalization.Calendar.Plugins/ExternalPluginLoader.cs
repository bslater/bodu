// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ExternalPluginLoader.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using System.Reflection;
using System.Runtime.Loader;
using System.Security.Cryptography;

namespace Bodu.Globalization.Calendar.Plugins;

/// <summary>
/// Loads an external notable-date plugin assembly from the filesystem, gating the load on a consumer-supplied
/// <see cref="IPluginTrustPolicy" /> and isolating the assembly in its own <see cref="AssemblyLoadContext" />.
/// </summary>
/// <remarks>
/// <para>
/// The loader follows a conservative discovery model: it reads a <em>single</em> attribute —
/// <see cref="NotableDatePluginAttribute" /> — from the plugin assembly and activates the declared type. It does not
/// enumerate the assembly's type list, does not invoke module initializers of unrelated types, and does not consider
/// any assembly that fails the trust policy. The plugin's static constructors and module initializer still run at load
/// time (a limitation of .NET assembly loading that cannot be bypassed without a sandbox); the trust policy is the line
/// of defense against that.
/// </para>
/// <para>
/// Each plugin is isolated in a dedicated non-collectible <see cref="AssemblyLoadContext" /> so its transitive private
/// dependencies do not leak into the host or into other plugins. Framework and host references (including
/// <c>Bodu.Globalization.Calendar</c> itself) resolve through <see cref="AssemblyLoadContext.Default" />.
/// </para>
/// </remarks>
/// <example>
///<![CDATA[
/// // Production: pin every trusted plugin by SHA-256.
/// IPluginTrustPolicy policy = new FileHashPluginTrustPolicy(
///     new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
///     {
///         ["AcmeHolidayPack.dll"] = "3a7bd3...e92f",
///     });
///
/// var loader = new ExternalPluginLoader(policy);
///
/// try
/// {
///     INotableDatePlugin plugin = loader.Load("plugins/AcmeHolidayPack.dll");
///
///     // Pass the rules into the service. Plugins typically also implement
///     // INotableDateRulePlugin or INotableDateAlgorithmPlugin.
///     if (plugin is INotableDateRulePlugin rules)
///         service.Register(rules.GetRules());
/// }
/// catch (PluginNotTrustedException ex)
/// {
///     Console.Error.WriteLine($"Untrusted plugin rejected: {ex.Message}");
/// }
///]]>
/// </example>
public sealed class ExternalPluginLoader
{
    /// <summary>
    /// The trust policy consulted before any assembly code is loaded into the process.
    /// </summary>
    private readonly IPluginTrustPolicy _trustPolicy;

    /// <summary>
    /// Initializes a new instance of the <see cref="ExternalPluginLoader" /> class.
    /// </summary>
    /// <param name="trustPolicy">
    /// The trust policy to consult before loading each plugin assembly. Must not be <see langword="null" />. Use
    /// <see cref="AllowAllPluginTrustPolicy" /> only in development and unit tests; production hosts must supply an
    /// allowlist-style policy such as <see cref="FileHashPluginTrustPolicy" /> or
    /// <see cref="StrongNamePluginTrustPolicy" />, optionally composed via <see cref="CompositePluginTrustPolicy" />.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="trustPolicy" /> is <see langword="null" />.
    /// </exception>
    public ExternalPluginLoader(IPluginTrustPolicy trustPolicy)
    {
        _trustPolicy = trustPolicy ?? throw new ArgumentNullException(nameof(trustPolicy));
    }

    /// <summary>
    /// Loads the plugin assembly at <paramref name="assemblyPath" />, verifies it against the configured trust policy,
    /// and returns an activated <see cref="INotableDatePlugin" /> instance ready for consumption by
    /// <see cref="NotableDateService" />.
    /// </summary>
    /// <param name="assemblyPath">
    /// Absolute or relative path to the plugin assembly. Must not be <see langword="null" /> or whitespace.
    /// </param>
    /// <returns>The activated plugin instance.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="assemblyPath" /> is <see langword="null" />, empty, or whitespace.
    /// </exception>
    /// <exception cref="FileNotFoundException">
    /// Thrown when no file exists at <paramref name="assemblyPath" />.
    /// </exception>
    /// <exception cref="PluginNotTrustedException">
    /// Thrown when the configured trust policy rejects the candidate.
    /// </exception>
    /// <exception cref="PluginMissingAttributeException">
    /// Thrown when the assembly is missing a valid <see cref="NotableDatePluginAttribute" /> or the declared type does
    /// not implement <see cref="INotableDatePlugin" />.
    /// </exception>
    /// <exception cref="PluginActivationException">
    /// Thrown when the declared plugin type cannot be instantiated (missing parameterless constructor, constructor
    /// threw, and so on).
    /// </exception>
    public INotableDatePlugin Load(string assemblyPath)
    {
        if (string.IsNullOrWhiteSpace(assemblyPath))
            throw new ArgumentException(CalendarResourceStrings.Arg_Invalid_AssemblyPathNullOrWhiteSpace, nameof(assemblyPath));

        var resolvedPath = Path.GetFullPath(assemblyPath);
        if (!File.Exists(resolvedPath))
            throw new FileNotFoundException(string.Format(CultureInfo.InvariantCulture, CalendarResourceStrings.IO_FileNotFound_PluginAssembly, resolvedPath), resolvedPath);

        // Read bytes + hash before any code from the plugin runs, so the trust policy has the full fingerprint to decide on.
        var fileBytes = File.ReadAllBytes(resolvedPath);
        var fileHash = SHA256.HashData(fileBytes);

        // Acquire the assembly name via metadata inspection (does not execute the assembly).
        var assemblyName = AssemblyName.GetAssemblyName(resolvedPath);

        var trustContext = new PluginTrustContext(resolvedPath, assemblyName, fileHash);
        PluginTrustResult trustResult = _trustPolicy.Evaluate(trustContext);
        if (!trustResult.Trusted)
            throw new PluginNotTrustedException(resolvedPath, trustResult.Reason);

        var loadContext = new AssemblyLoadContext($"bodu-plugin::{resolvedPath}", isCollectible: false);
        loadContext.Resolving += ResolveFromHostOrAlongside;

        Assembly assembly;
        try
        {
            using var stream = new MemoryStream(fileBytes, writable: false);
            assembly = loadContext.LoadFromStream(stream);
        }
        catch (Exception ex)
        {
            throw new PluginActivationException(resolvedPath, pluginType: null, innerException: ex);
        }

        NotableDatePluginAttribute attribute = assembly.GetCustomAttribute<NotableDatePluginAttribute>()
            ?? throw new PluginMissingAttributeException(resolvedPath, CalendarResourceStrings.Op_Invalid_PluginAttributeMissing);

        Type pluginType = attribute.PluginType;
        if (!typeof(INotableDatePlugin).IsAssignableFrom(pluginType))
        {
            throw new PluginMissingAttributeException(
                resolvedPath,
                string.Format(CultureInfo.InvariantCulture, CalendarResourceStrings.Op_Invalid_PluginTypeMissingInterface, pluginType.FullName));
        }

        try
        {
            var instance = Activator.CreateInstance(pluginType)
                ?? throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture, CalendarResourceStrings.Op_Invalid_ActivatorCreateInstanceNull, pluginType.FullName));
            return (INotableDatePlugin)instance;
        }
        catch (Exception ex) when (ex is not PluginActivationException)
        {
            throw new PluginActivationException(resolvedPath, pluginType, ex);
        }
    }

    /// <summary>
    /// Resolves references a plugin makes through the host's default load context so framework and <c>Bodu.*</c> types
    /// stay canonical (preventing duplicate <c>Bodu.Globalization.Calendar</c> loads in each plugin context). Private
    /// dependencies that the plugin bundles alongside its DLL are loaded automatically by the plugin's own
    /// <see cref="AssemblyLoadContext" />.
    /// </summary>
    /// <param name="context">The plugin's <see cref="AssemblyLoadContext" /> raising the resolve event.</param>
    /// <param name="name">The <see cref="AssemblyName" /> being requested.</param>
    /// <returns>
    /// The matching assembly already loaded into <see cref="AssemblyLoadContext.Default" />, or <see langword="null" />
    /// when no host-level match is found and the plugin's own context should attempt the load instead.
    /// </returns>
    private static Assembly? ResolveFromHostOrAlongside(AssemblyLoadContext context, AssemblyName name)
    {
        if (string.IsNullOrEmpty(name.Name))
            return null;

        foreach (Assembly loaded in AssemblyLoadContext.Default.Assemblies)
        {
            if (string.Equals(loaded.GetName().Name, name.Name, StringComparison.OrdinalIgnoreCase))
                return loaded;
        }

        return null;
    }
}
