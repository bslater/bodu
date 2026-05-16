// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BoduConfigurationResolver.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;

namespace Bodu.Text.Configuration;

/// <summary>
/// Projects a <see cref="BoduConfigurationDocument" /> into a flattened
/// <see cref="BoduConfigurationView" /> for a specific target path.
/// </summary>
internal sealed class BoduConfigurationResolver
{
    private readonly BoduConfigurationResolveOptions _options;

    internal BoduConfigurationResolver(BoduConfigurationResolveOptions options)
    {
        this._options = options;
    }

    internal BoduConfigurationView Resolve(BoduConfigurationDocument document, string? targetPath)
    {
        ThrowHelper.ThrowIfNull(document);

        string? pathRoot = this._options.PathRoot;
        if (pathRoot is null && this._options.MissingPathRootMode == BoduConfigurationMissingPathRootMode.Throw && targetPath is null)
            throw new InvalidOperationException(ConfigurationResourceStrings.InvalidOperation_ResolveWithoutPathRoot);

        StringComparer comparer = this._options.KeyOptions.KeyComparer;
        Dictionary<string, string?> values = new(comparer);

        string normalizedTarget = targetPath is null ? string.Empty : NormalizePath(targetPath, pathRoot);

        // Apply preamble first (when enabled).
        if (this._options.ApplyPreambleProperties)
            this.ApplySection(document.Preamble, values);

        // Apply sections that match the target path in source order; last-wins precedence.
        foreach (BoduConfigurationSection section in document.Sections)
        {
            if (string.IsNullOrEmpty(normalizedTarget))
                continue;

            if (section.Pattern is not null && BoduConfigurationPattern.Compile(section.Pattern).IsMatch(normalizedTarget))
                this.ApplySection(section, values);
        }

        return new BoduConfigurationView(values);
    }

    private void ApplySection(BoduConfigurationSection section, Dictionary<string, string?> values)
    {
        foreach (BoduConfigurationProperty property in section.Properties)
        {
            string key = property.ConfigurationKey;

            // EditorConfig "unset" sentinel handling.
            if (this._options.UnsetValueMode == BoduConfigurationUnsetValueMode.RemoveEffectiveValue
                && string.Equals(property.Value, "unset", StringComparison.OrdinalIgnoreCase))
            {
                values.Remove(key);
                continue;
            }

            values[key] = property.Value;
        }
    }

    private static string NormalizePath(string targetPath, string? pathRoot)
    {
        string normalizedTarget = targetPath.Replace('\\', '/');
        if (string.IsNullOrEmpty(pathRoot))
            return normalizedTarget;

        string normalizedRoot = pathRoot.Replace('\\', '/').TrimEnd('/');
        if (normalizedTarget.StartsWith(normalizedRoot + "/", StringComparison.OrdinalIgnoreCase))
            return normalizedTarget.Substring(normalizedRoot.Length + 1);

        if (string.Equals(normalizedTarget, normalizedRoot, StringComparison.OrdinalIgnoreCase))
            return Path.GetFileName(normalizedTarget);

        return normalizedTarget;
    }
}
