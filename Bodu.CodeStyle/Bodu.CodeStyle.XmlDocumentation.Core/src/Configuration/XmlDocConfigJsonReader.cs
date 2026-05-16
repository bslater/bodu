// ---------------------------------------------------------------------------------------------------------------
// <copyright file="XmlDocConfigJsonReader.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text.Json;

namespace Bodu.CodeStyle.XmlDocumentation.Configuration;

/// <summary>
/// Reads the optional <c>bodu.xmldocstyle.json</c> configuration file and produces a fully populated
/// <see cref="XmlDocFormatOptions" /> by overlaying the JSON values on top of
/// <see cref="XmlDocFormatPolicyDefaults.CreateBoduDefaults" />.
/// </summary>
public static class XmlDocConfigJsonReader
{
    /// <summary>
    /// Parses the supplied JSON document and returns the resulting options.
    /// </summary>
    /// <param name="json">The raw JSON document text.</param>
    /// <returns>The options with the JSON overlay applied to the Bodu defaults.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="json" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="XmlDocConfigException">
    /// Thrown when the JSON is malformed or specifies a value of the wrong type.
    /// </exception>
    public static XmlDocFormatOptions Read(string json)
    {
        if (json is null) throw new ArgumentNullException(nameof(json));

        XmlDocFormatOptions defaults = XmlDocFormatPolicyDefaults.CreateBoduDefaults();

        var maxLineLength = defaults.MaxLineLength;
        var documentationPrefix = defaults.DocumentationPrefix;
        var indentText = defaults.IndentText;
        var collapseProseWhitespace = defaults.CollapseProseWhitespace;
        var preserveBlankLines = defaults.PreserveBlankLines;
        var preserveXmlTagAttributes = defaults.PreserveXmlTagAttributes;
        var preserveCrefText = defaults.PreserveCrefText;
        ImmutableHashSet<string> blockTags = defaults.BlockTags;
        ImmutableHashSet<string> inlineTags = defaults.InlineTags;
        ImmutableHashSet<string> forceMultilineTags = defaults.ForceMultilineTags;
        ImmutableHashSet<string> singleLineWhenShortTags = defaults.SingleLineWhenShortTags;
        ImmutableHashSet<string> neverSplitTagContent = defaults.NeverSplitTagContent;
        ImmutableDictionary<string, XmlDocTagPolicy> tagPolicies = defaults.TagPolicies;

        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(json);
        }
        catch (JsonException ex)
        {
            throw new XmlDocConfigException("Invalid JSON document for bodu.xmldocstyle.json.", ex);
        }

        using (document)
        {
            JsonElement root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
            {
                throw new XmlDocConfigException("Top-level JSON value must be an object.");
            }

            foreach (JsonProperty property in root.EnumerateObject())
            {
                switch (property.Name)
                {
                    case "$schema":
                    case "profile":
                        // Informational only.
                        break;

                    case "maxLineLength":
                        maxLineLength = ReadInt32(property);
                        break;

                    case "documentationPrefix":
                        documentationPrefix = ReadString(property);
                        break;

                    case "indentText":
                        indentText = ReadString(property);
                        break;

                    case "collapseProseWhitespace":
                        collapseProseWhitespace = ReadBoolean(property);
                        break;

                    case "preserveBlankLines":
                        preserveBlankLines = ReadBoolean(property);
                        break;

                    case "preserveXmlTagAttributes":
                        preserveXmlTagAttributes = ReadBoolean(property);
                        break;

                    case "preserveCrefText":
                        preserveCrefText = ReadBoolean(property);
                        break;

                    case "blockTags":
                        blockTags = ReadStringSet(property);
                        break;

                    case "inlineTags":
                        inlineTags = ReadStringSet(property);
                        break;

                    case "forceMultilineTags":
                        forceMultilineTags = ReadStringSet(property);
                        break;

                    case "singleLineWhenShort":
                        singleLineWhenShortTags = ReadStringSet(property);
                        break;

                    case "neverSplitTagContent":
                        neverSplitTagContent = ReadStringSet(property);
                        break;

                    case "tagPolicies":
                        tagPolicies = ReadTagPolicies(property, tagPolicies);
                        break;
                }
            }
        }

        return new XmlDocFormatOptions(
            maxLineLength,
            documentationPrefix,
            indentText,
            collapseProseWhitespace,
            preserveBlankLines,
            preserveXmlTagAttributes,
            preserveCrefText,
            blockTags,
            inlineTags,
            forceMultilineTags,
            singleLineWhenShortTags,
            neverSplitTagContent,
            tagPolicies);
    }

    private static int ReadInt32(JsonProperty property)
    {
        if (property.Value.ValueKind != JsonValueKind.Number || !property.Value.TryGetInt32(out var value))
        {
            throw new XmlDocConfigException($"Property '{property.Name}' must be an integer.");
        }

        return value;
    }

    private static string ReadString(JsonProperty property)
    {
        if (property.Value.ValueKind != JsonValueKind.String)
        {
            throw new XmlDocConfigException($"Property '{property.Name}' must be a string.");
        }

        return property.Value.GetString() ?? string.Empty;
    }

    private static bool ReadBoolean(JsonProperty property)
    {
        if (property.Value.ValueKind != JsonValueKind.True && property.Value.ValueKind != JsonValueKind.False)
        {
            throw new XmlDocConfigException($"Property '{property.Name}' must be a boolean.");
        }

        return property.Value.GetBoolean();
    }

    private static ImmutableHashSet<string> ReadStringSet(JsonProperty property)
    {
        if (property.Value.ValueKind != JsonValueKind.Array)
        {
            throw new XmlDocConfigException($"Property '{property.Name}' must be an array of strings.");
        }

        ImmutableHashSet<string>.Builder builder = ImmutableHashSet.CreateBuilder(StringComparer.Ordinal);
        foreach (JsonElement element in property.Value.EnumerateArray())
        {
            if (element.ValueKind != JsonValueKind.String)
            {
                throw new XmlDocConfigException($"Property '{property.Name}' must be an array of strings.");
            }

            var value = element.GetString();
            if (!string.IsNullOrEmpty(value))
            {
                builder.Add(value!);
            }
        }

        return builder.ToImmutable();
    }

    private static ImmutableDictionary<string, XmlDocTagPolicy> ReadTagPolicies(JsonProperty property, ImmutableDictionary<string, XmlDocTagPolicy> defaults)
    {
        if (property.Value.ValueKind != JsonValueKind.Object)
        {
            throw new XmlDocConfigException($"Property '{property.Name}' must be an object.");
        }

        var builder = defaults.ToBuilder();
        foreach (JsonProperty tag in property.Value.EnumerateObject())
        {
            XmlDocTagPolicy policy = ReadTagPolicy(tag);
            builder[tag.Name] = policy;
        }

        return builder.ToImmutable();
    }

    private static XmlDocTagPolicy ReadTagPolicy(JsonProperty property)
    {
        if (property.Value.ValueKind != JsonValueKind.Object)
        {
            throw new XmlDocConfigException($"Tag policy '{property.Name}' must be an object.");
        }

        XmlDocTagLayout layout = XmlDocTagLayout.Auto;
        int? maxSingleLineLength = null;
        bool? allowLineBreakInside = null;
        bool? selfClosingTrailingSpace = null;

        foreach (JsonProperty entry in property.Value.EnumerateObject())
        {
            switch (entry.Name)
            {
                case "layout":
                    layout = ParseLayout(entry);
                    break;

                case "maxSingleLineLength":
                    maxSingleLineLength = ReadInt32(entry);
                    break;

                case "allowLineBreakInside":
                    allowLineBreakInside = ReadBoolean(entry);
                    break;

                case "selfClosingTrailingSpace":
                    selfClosingTrailingSpace = ReadBoolean(entry);
                    break;
            }
        }

        return new XmlDocTagPolicy(layout, maxSingleLineLength, allowLineBreakInside, selfClosingTrailingSpace);
    }

    private static XmlDocTagLayout ParseLayout(JsonProperty property)
    {
        var raw = ReadString(property);
        switch (raw)
        {
            case "auto":
                return XmlDocTagLayout.Auto;
            case "multilineBlock":
                return XmlDocTagLayout.MultilineBlock;
            case "singleLineWhenShort":
                return XmlDocTagLayout.SingleLineWhenShort;
            case "inlineAtomic":
                return XmlDocTagLayout.InlineAtomic;
            default:
                throw new XmlDocConfigException($"Unknown tag layout '{raw}'.");
        }
    }
}
