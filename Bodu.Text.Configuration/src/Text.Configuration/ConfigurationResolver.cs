// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConfigurationResolver.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Ini;

namespace Bodu.Text.Configuration;

/// <summary>
/// Projects an <see cref="IniDocument" /> into a flattened <see cref="ConfigurationView" /> for a specific target
/// path, applying glob matching, preamble layering, and the EditorConfig <c>unset</c> sentinel.
/// </summary>
internal sealed class ConfigurationResolver
{
    private readonly ConfigurationResolveOptions _options;

    internal ConfigurationResolver(ConfigurationResolveOptions options)
    {
        _options = options;
    }

    internal ConfigurationView Resolve(IniDocument document, string? targetPath)
    {
        ThrowHelper.ThrowIfNull(document);

        var pathRoot = _options.PathRoot;
        if (pathRoot is null && _options.MissingPathRootMode == ConfigurationMissingPathRootMode.Throw && targetPath is null)
            ConfigurationHelpers.ThrowResolveWithoutPathRoot();

        StringComparer comparer = _options.KeyOptions.KeyComparer;
        Dictionary<string, string?> values = new(comparer);

        var normalizedTarget = targetPath is null ? string.Empty : NormalizePath(targetPath, pathRoot);

        // Apply the global section (preamble) first when enabled.
        if (_options.ApplyPreambleProperties)
            ApplySection(document.GlobalSection, values);

        // Apply sections whose name (interpreted as a glob pattern) matches the target path, in source order.
        // Last-wins precedence is naturally handled by dictionary overwrite.
        foreach (IniSection section in document.Sections)
        {
            if (string.IsNullOrEmpty(normalizedTarget))
                continue;

            if (ConfigurationPattern.Compile(section.Name).IsMatch(normalizedTarget))
                ApplySection(section, values);
        }

        return new ConfigurationView(values);
    }

    private void ApplySection(IniSection section, Dictionary<string, string?> values)
    {
        foreach (IniEntry entry in section.Entries)
        {
            var key = ConfigurationKey.Parse(entry.Key, _options.KeyOptions).Path;

            // EditorConfig "unset" sentinel handling.
            if (_options.UnsetValueMode == ConfigurationUnsetValueMode.RemoveEffectiveValue
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
        var normalizedTarget = targetPath.Replace('\\', '/');
        if (string.IsNullOrEmpty(pathRoot))
            return normalizedTarget;

        var normalizedRoot = pathRoot.Replace('\\', '/').TrimEnd('/');
        if (normalizedTarget.StartsWith(normalizedRoot + "/", StringComparison.OrdinalIgnoreCase))
            return normalizedTarget[(normalizedRoot.Length + 1)..];

        return string.Equals(normalizedTarget, normalizedRoot, StringComparison.OrdinalIgnoreCase)
            ? Path.GetFileName(normalizedTarget)
            : normalizedTarget;
    }
}
