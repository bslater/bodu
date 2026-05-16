// ---------------------------------------------------------------------------------------------------------------
// <copyright file="XmlDocConfigurationLoader.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Immutable;
using System.IO;
using System.Threading;
using Bodu.CodeStyle.XmlDocumentation;
using Bodu.CodeStyle.XmlDocumentation.Configuration;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace Bodu.CodeStyle.XmlDocumentation.Analyzers.Configuration;

/// <summary>
/// Loads the XML documentation formatting policy from defaults, the optional
/// <c>bodu.xmldocstyle.json</c> additional file, and per-tree <c>.editorconfig</c> scalar overrides.
/// </summary>
internal static class XmlDocConfigurationLoader
{
    /// <summary>
    /// Computes the base options for a compilation by overlaying the JSON additional file (when present) on top
    /// of the Bodu defaults.
    /// </summary>
    /// <param name="additionalFiles">The compilation's additional files.</param>
    /// <param name="cancellationToken">The cancellation token to observe.</param>
    /// <returns>The compilation-level options.</returns>
    public static XmlDocFormatOptions LoadCompilationOptions(
        ImmutableArray<AdditionalText> additionalFiles,
        CancellationToken cancellationToken)
    {
        XmlDocFormatOptions options = XmlDocFormatPolicyDefaults.CreateBoduDefaults();

        foreach (AdditionalText file in additionalFiles)
        {
            if (!IsConfigFile(file.Path)) continue;
            cancellationToken.ThrowIfCancellationRequested();

            SourceText? text = file.GetText(cancellationToken);
            if (text is null) continue;

            try
            {
                options = XmlDocConfigJsonReader.Read(text.ToString());
            }
            catch (XmlDocConfigException)
            {
                // Malformed config file — fall back to defaults silently so the analyzer keeps reporting on
                // valid trivia. The user-facing error path is the JSON schema validator, not the analyzer.
            }
#pragma warning disable RCS1075 // Avoid empty catch clause that catches System.Exception
            catch (System.Exception)
            {
                // Defensive: System.Text.Json may not be available in every analyzer host (e.g. legacy MSBuild).
                // Falling back to defaults keeps the analyzer functional for the common no-config case.
            }
#pragma warning restore RCS1075
        }

        return options;
    }

    /// <summary>
    /// Applies per-tree <c>.editorconfig</c> scalar overrides on top of the compilation-level options.
    /// </summary>
    /// <param name="compilationOptions">The compilation-level options.</param>
    /// <param name="treeOptions">The Roslyn-supplied options for the syntax tree under analysis.</param>
    /// <returns>The effective per-file options.</returns>
    public static XmlDocFormatOptions ApplyEditorConfigOverrides(
        XmlDocFormatOptions compilationOptions,
        AnalyzerConfigOptions treeOptions)
    {
        if (compilationOptions is null) throw new ArgumentNullException(nameof(compilationOptions));
        if (treeOptions is null) throw new ArgumentNullException(nameof(treeOptions));

        XmlDocFormatOptions current = compilationOptions;

        if (treeOptions.TryGetValue(EditorConfigKeys.MaxLineLength, out string? maxLineLength) &&
            int.TryParse(maxLineLength, out int parsedMaxLineLength) &&
            parsedMaxLineLength > 0)
        {
            current = current.WithMaxLineLength(parsedMaxLineLength);
        }

        return current;
    }

    /// <summary>
    /// Resolves the line ending to use for emitted trivia, honouring <c>.editorconfig</c> <c>end_of_line</c>.
    /// </summary>
    /// <param name="treeOptions">The Roslyn-supplied options for the syntax tree under analysis.</param>
    /// <returns>The resolved line ending; defaults to <c>"\r\n"</c>.</returns>
    public static string ResolveLineEnding(AnalyzerConfigOptions treeOptions)
    {
        if (treeOptions is null) throw new ArgumentNullException(nameof(treeOptions));

        if (treeOptions.TryGetValue(EditorConfigKeys.EndOfLine, out string? raw))
        {
            switch (raw)
            {
                case "lf":
                    return "\n";
                case "cr":
                    return "\r";
                case "crlf":
                    return "\r\n";
            }
        }

        return "\r\n";
    }

    private static bool IsConfigFile(string path)
    {
        string fileName = Path.GetFileName(path);
        return string.Equals(fileName, "bodu.xmldocstyle.json", StringComparison.OrdinalIgnoreCase);
    }
}
