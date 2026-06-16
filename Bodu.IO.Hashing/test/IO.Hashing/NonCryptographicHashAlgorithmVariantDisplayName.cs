// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NonCryptographicHashAlgorithmVariantDisplayName.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Reflection;

namespace Bodu.IO.Hashing;

/// <summary>
/// Produces variant-named display strings for MSTest <c>[DynamicData]</c> rows whose first element is a
/// non-cryptographic hash algorithm variant value, so that failures in CI and Test Explorer name the variant rather
/// than showing an opaque row index.
/// </summary>
/// <remarks>
/// <para>
/// Wire this helper to a test method via the <c>DynamicDataDisplayName</c> and
/// <c>DynamicDataDisplayNameDeclaringType</c> arguments of the <c>DynamicData</c> attribute. The signature matches the
/// MSTest contract <c>static string (MethodInfo methodInfo, object?[] data)</c>.
/// </para>
/// <para>
/// Resolution must be wired through <c>DynamicDataDisplayNameDeclaringType</c> because MSTest 4.x resolves the display
/// name method via <see cref="TypeInfo.GetDeclaredMethod(string)" />, which does not walk inheritance. Hosting the
/// helper on a non-generic static class lets every test class — base, intermediate, or concrete — share a single
/// implementation.
/// </para>
/// </remarks>
public static class NonCryptographicHashAlgorithmVariantDisplayName
{
    /// <summary>
    /// Returns a display name for a single MSTest <c>[DynamicData]</c> row driven by
    /// <see cref="NonCryptographicHashAlgorithmTests{TTest, TAlgorithm, TVariant}.NonCryptographicHashAlgorithmVariants" />
    /// .
    /// </summary>
    /// <param name="methodInfo">The test method whose row is being named.</param>
    /// <param name="data">The row payload supplied by the dynamic data source.</param>
    /// <returns>
    /// A readable variant description prefixed with <c>"Variant: "</c>. Falls back to <c>"Variant: Default"</c> when
    /// the row is empty.
    /// </returns>
    public static string GetDisplayName(MethodInfo methodInfo, object[] data) =>
        FormatVariantForDisplay(data.Length > 0 ? data[0] : null);

    /// <summary>
    /// Formats a non-cryptographic hash algorithm variant for use in data-driven test display names.
    /// </summary>
    /// <param name="variant">The variant value.</param>
    /// <returns>A readable variant description.</returns>
    private static string FormatVariantForDisplay(object? variant)
    {
        if (variant is null)
            return "Variant: Default";

        string? variantText = variant.ToString();
        if (!string.IsNullOrWhiteSpace(variantText) &&
            !string.Equals(variantText, variant.GetType().FullName, StringComparison.Ordinal))
        {
            return $"Variant: {variantText}";
        }

        string[] properties = variant.GetType()
            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Where(p => p.CanRead && p.GetIndexParameters().Length == 0)
            .Select(p => $"{p.Name}={p.GetValue(variant)}")
            .ToArray();

        return properties.Length == 0
            ? $"Variant: {variant.GetType().Name}"
            : $"Variant: {variant.GetType().Name}({string.Join(", ", properties)})";
    }
}
