// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FileHashPluginTrustPolicy.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using System.Security.Cryptography;

namespace Bodu.Globalization.Calendar.Plugins;

/// <summary>
/// A trust policy that admits plugin assemblies whose SHA-256 file hash matches a digest pinned by the consumer under
/// the candidate's assembly name.
/// </summary>
/// <remarks>
/// <para>
/// The consumer pins, per assembly name (matched case-insensitively), the exact SHA-256 digest of the bytes on disk.
/// The loader computes the candidate's hash before this policy runs; the policy asserts equality in constant time.
/// </para>
/// <para>
/// This gives byte-level tamper resistance without requiring a strong name. It does not roll with plugin versions —
/// pinning a fresh digest when the plugin is updated is the consumer's responsibility. An assembly loaded in memory
/// (no file, so no hash) is always rejected.
/// </para>
/// </remarks>
public sealed class FileHashPluginTrustPolicy : IPluginTrustPolicy
{
    /// <summary>
    /// The case-insensitive map from assembly name to pinned SHA-256 digest.
    /// </summary>
    private readonly Dictionary<string, byte[]> _allowedHashesByAssemblyName;

    /// <summary>
    /// Initializes a new instance of the <see cref="FileHashPluginTrustPolicy" /> class.
    /// </summary>
    /// <param name="allowedHashesByAssemblyName">The trusted assembly names mapped to their SHA-256 digests.</param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="allowedHashesByAssemblyName" /> is <see langword="null" />.
    /// </exception>
    /// <remarks>
    /// Assembly names are compared case-insensitively. Entries with a blank name or a <see langword="null" /> digest are
    /// ignored; an empty map rejects every assembly.
    /// </remarks>
    public FileHashPluginTrustPolicy(IReadOnlyDictionary<string, byte[]> allowedHashesByAssemblyName)
    {
        ThrowHelper.ThrowIfNull(allowedHashesByAssemblyName);

        this._allowedHashesByAssemblyName = new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase);
        foreach (KeyValuePair<string, byte[]> entry in allowedHashesByAssemblyName)
        {
            if (!string.IsNullOrWhiteSpace(entry.Key) && entry.Value is not null)
                this._allowedHashesByAssemblyName[entry.Key] = entry.Value;
        }
    }

    /// <inheritdoc />
    /// <exception cref="ArgumentNullException"><paramref name="context" /> is <see langword="null" />.</exception>
    public PluginTrustResult Evaluate(PluginTrustContext context)
    {
        ThrowHelper.ThrowIfNull(context);

        if (string.IsNullOrEmpty(context.AssemblyName))
            return PluginTrustResult.Rejected(PluginsResourceStrings.Op_NotTrusted_PluginAssemblyNameMissing);

        if (context.FileHash is null)
            return PluginTrustResult.Rejected(string.Format(CultureInfo.InvariantCulture, PluginsResourceStrings.Op_NotTrusted_PluginHashUnavailable, context.AssemblyName));

        if (!this._allowedHashesByAssemblyName.TryGetValue(context.AssemblyName, out byte[]? expected))
            return PluginTrustResult.Rejected(string.Format(CultureInfo.InvariantCulture, PluginsResourceStrings.Op_NotTrusted_PluginAssemblyNotAllowed, context.AssemblyName));

        return CryptographicOperations.FixedTimeEquals(expected, context.FileHash)
            ? PluginTrustResult.Trusted()
            : PluginTrustResult.Rejected(string.Format(CultureInfo.InvariantCulture, PluginsResourceStrings.Op_NotTrusted_PluginHashMismatch, context.AssemblyName));
    }
}
