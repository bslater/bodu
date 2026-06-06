// ---------------------------------------------------------------------------------------------------------------
// <copyright file="StrongNamePluginTrustPolicy.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;

namespace Bodu.Globalization.Calendar.Plugins;

/// <summary>
/// A trust policy that admits plugin assemblies whose strong-name public-key token appears in a consumer-supplied
/// allowlist.
/// </summary>
/// <remarks>
/// <para>
/// The policy reads <see cref="PluginTrustContext.PublicKeyToken" /> — the lowercase-hexadecimal token the loader
/// extracts from the assembly manifest — and compares it case-insensitively against the allowlist. An assembly that is
/// not strong-named (no token) is always rejected.
/// </para>
/// <para>
/// This verifies the token value declared in the manifest; it does <em>not</em> cryptographically verify the
/// strong-name signature. For byte-level tamper resistance, compose it with a <see cref="FileHashPluginTrustPolicy" />
/// through a <see cref="CompositePluginTrustPolicy" />.
/// </para>
/// </remarks>
public sealed class StrongNamePluginTrustPolicy : IPluginTrustPolicy
{
    /// <summary>
    /// The set of normalized, lowercase-hex public-key tokens permitted to load.
    /// </summary>
    private readonly HashSet<string> _allowedTokens;

    /// <summary>
    /// Initializes a new instance of the <see cref="StrongNamePluginTrustPolicy" /> class.
    /// </summary>
    /// <param name="allowedPublicKeyTokens">The public-key tokens to admit, as hexadecimal strings.</param>
    /// <exception cref="ArgumentNullException"><paramref name="allowedPublicKeyTokens" /> is <see langword="null" />.</exception>
    /// <remarks>
    /// Tokens are normalized to trimmed lowercase. Blank entries are ignored; an empty allowlist rejects every assembly.
    /// </remarks>
    public StrongNamePluginTrustPolicy(IEnumerable<string> allowedPublicKeyTokens)
    {
        ThrowHelper.ThrowIfNull(allowedPublicKeyTokens);

        this._allowedTokens = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var token in allowedPublicKeyTokens)
        {
            if (!string.IsNullOrWhiteSpace(token))
                this._allowedTokens.Add(token.Trim().ToLowerInvariant());
        }
    }

    /// <inheritdoc />
    /// <exception cref="ArgumentNullException"><paramref name="context" /> is <see langword="null" />.</exception>
    public PluginTrustResult Evaluate(PluginTrustContext context)
    {
        ThrowHelper.ThrowIfNull(context);

        if (string.IsNullOrEmpty(context.PublicKeyToken))
            return PluginTrustResult.Rejected(PluginsResourceStrings.Op_NotTrusted_PluginNotStrongNamed);

        return this._allowedTokens.Contains(context.PublicKeyToken)
            ? PluginTrustResult.Trusted()
            : PluginTrustResult.Rejected(string.Format(CultureInfo.InvariantCulture, PluginsResourceStrings.Op_NotTrusted_PluginTokenNotAllowed, context.PublicKeyToken));
    }
}
