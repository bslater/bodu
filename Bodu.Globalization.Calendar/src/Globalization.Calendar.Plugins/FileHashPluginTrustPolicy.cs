// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FileHashPluginTrustPolicy.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.Plugins;

/// <summary>
/// A trust policy that admits plugin assemblies whose SHA-256 file hash exactly matches a value pinned by the consumer under
/// the candidate's assembly name.
/// </summary>
/// <remarks>
/// <para>
/// The consumer builds a dictionary mapping each trusted assembly name (matched case-insensitively against
/// <see cref="System.Reflection.AssemblyName.Name" />) to the exact SHA-256 digest of the bytes on disk. The loader has already
/// computed the candidate's hash by the time this policy runs; the policy simply asserts equality.
/// </para>
/// <para>
/// This policy gives byte-level tamper resistance without requiring the plugin to be strong-named. It does not automatically
/// roll with plugin versions — pinning a new hash when the plugin is updated is the consumer's responsibility.
/// </para>
/// </remarks>
public sealed class FileHashPluginTrustPolicy : IPluginTrustPolicy
{
    /// <summary>A normalised, case-insensitive map from assembly name to the pinned SHA-256 digest.</summary>
    private readonly IReadOnlyDictionary<string, byte[]> _allowedHashesByAssemblyName;

    /// <summary>
    /// Initializes a new instance of the <see cref="FileHashPluginTrustPolicy" /> class.
    /// </summary>
    /// <param name="allowedHashesByAssemblyName">Dictionary of trusted assembly names mapped to their SHA-256 digests. Assembly names are compared case-insensitively. Must not be <see langword="null" />.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="allowedHashesByAssemblyName" /> is <see langword="null" />.</exception>
    public FileHashPluginTrustPolicy(IReadOnlyDictionary<string, byte[]> allowedHashesByAssemblyName)
    {
        if (allowedHashesByAssemblyName is null) throw new ArgumentNullException(nameof(allowedHashesByAssemblyName));

        var normalised = new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase);
        foreach (KeyValuePair<string, byte[]> entry in allowedHashesByAssemblyName)
        {
            if (string.IsNullOrWhiteSpace(entry.Key))
                continue;
            if (entry.Value is null)
                continue;

            normalised[entry.Key] = entry.Value;
        }

        _allowedHashesByAssemblyName = normalised;
    }

    /// <inheritdoc />
    public PluginTrustResult Evaluate(PluginTrustContext context)
    {
        var name = context.AssemblyName.Name;
        if (string.IsNullOrEmpty(name))
            return new PluginTrustResult(Trusted: false, Reason: "Plugin assembly name is missing.");

        if (!_allowedHashesByAssemblyName.TryGetValue(name, out var expected))
            return new PluginTrustResult(Trusted: false, Reason: $"Assembly '{name}' is not in the hash allowlist.");

        if (expected.Length != context.FileHash.Length)
            return new PluginTrustResult(Trusted: false, Reason: $"Assembly '{name}' hash length mismatch.");

        // Constant-time comparison is overkill for a plain integrity check, but cheap — use it anyway.
        var diff = 0;
        for (var i = 0; i < expected.Length; i++)
            diff |= expected[i] ^ context.FileHash[i];

        return diff == 0
            ? new PluginTrustResult(Trusted: true, Reason: null)
            : new PluginTrustResult(Trusted: false, Reason: $"Assembly '{name}' hash does not match the pinned value.");
    }
}
