// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BoduConfigurationResolver.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;
using Bodu.Text.Formats;

namespace Bodu.Text.Configuration;

/// <summary>
/// Projects an <see cref="IniDocument" /> into a flattened <see cref="BoduConfigurationView" /> for a specific
/// target path, applying glob matching, preamble layering, and the EditorConfig <c>unset</c> sentinel.
/// </summary>
internal sealed class BoduConfigurationResolver
{
    private readonly BoduConfigurationResolveOptions _options;

    internal BoduConfigurationResolver(BoduConfigurationResolveOptions options)
    {
        this._options = options;
    }

    internal BoduConfigurationView Resolve(IniDocument document, string? targetPath)
    {
        ThrowHelper.ThrowIfNull(document);

        string? pathRoot = this._options.PathRoot;
        if (pathRoot is null && this._options.MissingPathRootMode == BoduConfigurationMissingPathRootMode.Throw && targetPath is null)
            throw new InvalidOperationException(ConfigurationResourceStrings.Op_Invalid_ResolveWithoutPathRoot);

        StringComparer comparer = this._options.KeyOptions.KeyComparer;
        Dictionary<string, string?> values = new(comparer);

        string normalizedTarget = targetPath is null ? string.Empty : NormalizePath(targetPath, pathRoot);

        // Apply the global section (preamble) first when enabled.
        if (this._options.ApplyPreambleProperties)
            this.ApplySection(document.GlobalSection, values);

        // Apply sections whose name (interpreted as a glob pattern) matches the target path, in source order.
        // Last-wins precedence is naturally handled by dictionary overwrite.
        foreach (IniSection section in document.Sections)
        {
            if (string.IsNullOrEmpty(normalizedTarget))
                continue;

            if (BoduConfigurationPattern.Compile(section.Name).IsMatch(normalizedTarget))
                this.ApplySection(section, values);
        }

        return new BoduConfigurationView(values);
    }

    private void ApplySection(IniSection section, Dictionary<string, string?> values)
    {
        foreach (IniEntry entry in section.Entries)
        {
            string key = BoduConfigurationKey.Parse(entry.Key, this._options.KeyOptions).ConfigurationKey;

            // EditorConfig "unset" sentinel handling.
            if (this._options.UnsetValueMode == BoduConfigurationUnsetValueMode.RemoveEffectiveValue
                && string.Equals(entry.Value, "unset", StringComparison.OrdinalIgnoreCase))
            {
                values.Remove(key);
                continue;
            }

            values[key] = entry.Value;
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
