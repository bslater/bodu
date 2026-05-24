// ---------------------------------------------------------------------------------------------------------------
// <copyright file="StrongNamePluginTrustPolicy.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.Plugins;

/// <summary>
/// A trust policy that admits plugin assemblies whose strong-name public-key token appears in a consumer-supplied
/// allowlist.
/// </summary>
/// <remarks>
/// <para>
/// The policy reads the public-key token from <see cref="System.Reflection.AssemblyName.GetPublicKeyToken" /> on the
/// candidate's <see cref="PluginTrustContext.AssemblyName" />. Tokens are compared case-insensitively after converting
/// both sides to lower- case hexadecimal. An assembly that is not signed (null or empty token) is always rejected.
/// </para>
/// <para>
/// This policy verifies the token value declared in the assembly manifest; it does <em>not</em> cryptographically
/// verify the strong-name signature. Callers that need tamper-resistance at the byte level should compose this policy
/// with <see cref="FileHashPluginTrustPolicy" /> via <see cref="CompositePluginTrustPolicy" />.
/// </para>
/// </remarks>
public sealed class StrongNamePluginTrustPolicy
    : IPluginTrustPolicy
{
    /// <summary>
    /// The set of normalized, lowercase-hex public-key tokens that are permitted to load.
    /// </summary>
    private readonly HashSet<string> _allowedTokens;

    /// <summary>
    /// Initializes a new instance of the <see cref="StrongNamePluginTrustPolicy" /> class.
    /// </summary>
    /// <param name="allowedPublicKeyTokens">
    /// The lowercase-hex public-key tokens to admit. Must not be <see langword="null" />. An empty collection rejects
    /// every assembly.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="allowedPublicKeyTokens" /> is <see langword="null" />.
    /// </exception>
    public StrongNamePluginTrustPolicy(IEnumerable<string> allowedPublicKeyTokens)
    {
        if (allowedPublicKeyTokens is null) throw new ArgumentNullException(nameof(allowedPublicKeyTokens));
        _allowedTokens = new HashSet<string>(allowedPublicKeyTokens.Select(NormalizeToken), StringComparer.OrdinalIgnoreCase);
    }

    /// <inheritdoc />
    public PluginTrustResult Evaluate(PluginTrustContext context)
    {
        var tokenBytes = context.AssemblyName.GetPublicKeyToken();
        if (tokenBytes is null || tokenBytes.Length == 0)
            return new PluginTrustResult(Trusted: false, Reason: "Plugin assembly is not strong-named.");

        var token = ToHexString(tokenBytes);
        return _allowedTokens.Contains(token)
            ? new PluginTrustResult(Trusted: true, Reason: null)
            : new PluginTrustResult(Trusted: false, Reason: $"Public-key token '{token}' is not in the allowlist.");
    }

    /// <summary>
    /// Normalizes a public-key token string to trimmed lowercase, returning an empty string for <see langword="null" />
    /// input.
    /// </summary>
    /// <param name="token">The raw token string.</param>
    /// <returns>The normalized lowercase token.</returns>
    private static string NormalizeToken(string token) =>
        (token ?? string.Empty).Trim().ToLowerInvariant();

    /// <summary>
    /// Converts a byte array to its lowercase hexadecimal string representation.
    /// </summary>
    /// <param name="bytes">The bytes to convert.</param>
    /// <returns>A lowercase hex string of length <c>bytes.Length * 2</c>.</returns>
    private static string ToHexString(byte[] bytes)
    {
        var chars = new char[bytes.Length * 2];
        for (var i = 0; i < bytes.Length; i++)
        {
            var b = bytes[i];
            chars[i * 2] = GetHexChar(b >> 4);
            chars[(i * 2) + 1] = GetHexChar(b & 0x0F);
        }

        return new string(chars);
    }

    /// <summary>
    /// Returns the lowercase hex character for a nibble value in the range 0–15.
    /// </summary>
    /// <param name="nibble">The nibble value (0–15).</param>
    /// <returns>The corresponding lowercase hexadecimal character.</returns>
    private static char GetHexChar(int nibble) => (char)(nibble < 10 ? '0' + nibble : 'a' + (nibble - 10));
}
