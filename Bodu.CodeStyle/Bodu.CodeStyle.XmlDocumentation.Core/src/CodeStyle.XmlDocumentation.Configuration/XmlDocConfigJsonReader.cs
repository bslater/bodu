// ---------------------------------------------------------------------------------------------------------------
// <copyright file="XmlDocConfigJsonReader.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;

namespace Bodu.CodeStyle.XmlDocumentation.Configuration;

/// <summary>
/// Reads the optional <c>bodu.xmldocstyle.json</c> configuration file and produces a fully populated
/// <see cref="XmlDocFormatOptions" /> by overlaying the JSON values on top of
/// <see cref="XmlDocFormatPolicyDefaults.CreateDefaults" />.
/// </summary>
/// <remarks>
/// Parsing uses the dependency-free <see cref="ConfigJsonParser" /> rather than <c>System.Text.Json</c> so the
/// analyzer package needs no external runtime assembly in the analyzer host.
/// </remarks>
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

        XmlDocFormatOptions defaults = XmlDocFormatPolicyDefaults.CreateDefaults();

        var maxLineLength = defaults.MaxLineLength;
        var documentationPrefix = defaults.DocumentationPrefix;
        var indentText = defaults.IndentText;
        var collapseProseWhitespace = defaults.CollapseProseWhitespace;
        var preserveBlankLines = defaults.PreserveBlankLines;
        var preserveXmlTagAttributes = defaults.PreserveXmlTagAttributes;
        var preserveCrefText = defaults.PreserveCrefText;
        var keepFieldSummaryOnSingleLine = defaults.KeepFieldSummaryOnSingleLine;
        ImmutableHashSet<string> blockTags = defaults.BlockTags;
        ImmutableHashSet<string> inlineTags = defaults.InlineTags;
        ImmutableHashSet<string> forceMultilineTags = defaults.ForceMultilineTags;
        ImmutableHashSet<string> singleLineWhenShortTags = defaults.SingleLineWhenShortTags;
        ImmutableHashSet<string> neverSplitTagContent = defaults.NeverSplitTagContent;
        ImmutableDictionary<string, XmlDocTagPolicy> tagPolicies = defaults.TagPolicies;

        ConfigJsonValue root;
        try
        {
            root = ConfigJsonParser.Parse(json);
        }
        catch (FormatException ex)
        {
            throw new XmlDocConfigException(XmlDocResourceStrings.Json_Invalid_Document, ex);
        }

        if (root.Kind != ConfigJsonValueKind.Object || root.Members is null)
        {
            throw new XmlDocConfigException(XmlDocResourceStrings.Json_Invalid_TopLevelNotObject);
        }

        foreach (KeyValuePair<string, ConfigJsonValue> property in root.Members)
        {
            switch (property.Key)
            {
                case "$schema":
                case "profile":
                    // Informational only.
                    break;

                case "maxLineLength":
                    maxLineLength = ReadInt32(property.Key, property.Value);
                    break;

                case "documentationPrefix":
                    documentationPrefix = ReadString(property.Key, property.Value);
                    break;

                case "indentText":
                    indentText = ReadString(property.Key, property.Value);
                    break;

                case "collapseProseWhitespace":
                    collapseProseWhitespace = ReadBoolean(property.Key, property.Value);
                    break;

                case "preserveBlankLines":
                    preserveBlankLines = ReadBoolean(property.Key, property.Value);
                    break;

                case "preserveXmlTagAttributes":
                    preserveXmlTagAttributes = ReadBoolean(property.Key, property.Value);
                    break;

                case "preserveCrefText":
                    preserveCrefText = ReadBoolean(property.Key, property.Value);
                    break;

                case "keepFieldSummaryOnSingleLine":
                    keepFieldSummaryOnSingleLine = ReadBoolean(property.Key, property.Value);
                    break;

                case "blockTags":
                    blockTags = ReadStringSet(property.Key, property.Value);
                    break;

                case "inlineTags":
                    inlineTags = ReadStringSet(property.Key, property.Value);
                    break;

                case "forceMultilineTags":
                    forceMultilineTags = ReadStringSet(property.Key, property.Value);
                    break;

                case "singleLineWhenShort":
                    singleLineWhenShortTags = ReadStringSet(property.Key, property.Value);
                    break;

                case "neverSplitTagContent":
                    neverSplitTagContent = ReadStringSet(property.Key, property.Value);
                    break;

                case "tagPolicies":
                    tagPolicies = ReadTagPolicies(property.Key, property.Value, tagPolicies);
                    break;
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
            keepFieldSummaryOnSingleLine,
            blockTags,
            inlineTags,
            forceMultilineTags,
            singleLineWhenShortTags,
            neverSplitTagContent,
            tagPolicies);
    }

    private static int ReadInt32(string name, ConfigJsonValue value)
    {
        if (!value.TryGetInt32(out var result))
        {
            throw new XmlDocConfigException(string.Format(CultureInfo.CurrentCulture, XmlDocResourceStrings.Json_Invalid_PropertyInteger, name));
        }

        return result;
    }

    private static string ReadString(string name, ConfigJsonValue value)
    {
        if (value.Kind != ConfigJsonValueKind.String)
        {
            throw new XmlDocConfigException(string.Format(CultureInfo.CurrentCulture, XmlDocResourceStrings.Json_Invalid_PropertyString, name));
        }

        return value.StringValue ?? string.Empty;
    }

    private static bool ReadBoolean(string name, ConfigJsonValue value)
    {
        if (value.Kind != ConfigJsonValueKind.Boolean)
        {
            throw new XmlDocConfigException(string.Format(CultureInfo.CurrentCulture, XmlDocResourceStrings.Json_Invalid_PropertyBoolean, name));
        }

        return value.BooleanValue;
    }

    private static ImmutableHashSet<string> ReadStringSet(string name, ConfigJsonValue value)
    {
        if (value.Kind != ConfigJsonValueKind.Array || value.Items is null)
        {
            throw new XmlDocConfigException(string.Format(CultureInfo.CurrentCulture, XmlDocResourceStrings.Json_Invalid_PropertyArrayOfStrings, name));
        }

        ImmutableHashSet<string>.Builder builder = ImmutableHashSet.CreateBuilder(StringComparer.Ordinal);
        foreach (ConfigJsonValue element in value.Items)
        {
            if (element.Kind != ConfigJsonValueKind.String)
            {
                throw new XmlDocConfigException(string.Format(CultureInfo.CurrentCulture, XmlDocResourceStrings.Json_Invalid_PropertyArrayOfStrings, name));
            }

            var entry = element.StringValue;
            if (!string.IsNullOrEmpty(entry))
            {
                builder.Add(entry!);
            }
        }

        return builder.ToImmutable();
    }

    private static ImmutableDictionary<string, XmlDocTagPolicy> ReadTagPolicies(string name, ConfigJsonValue value, ImmutableDictionary<string, XmlDocTagPolicy> defaults)
    {
        if (value.Kind != ConfigJsonValueKind.Object || value.Members is null)
        {
            throw new XmlDocConfigException(string.Format(CultureInfo.CurrentCulture, XmlDocResourceStrings.Json_Invalid_PropertyObject, name));
        }

        ImmutableDictionary<string, XmlDocTagPolicy>.Builder builder = defaults.ToBuilder();
        foreach (KeyValuePair<string, ConfigJsonValue> tag in value.Members)
        {
            builder[tag.Key] = ReadTagPolicy(tag.Key, tag.Value);
        }

        return builder.ToImmutable();
    }

    private static XmlDocTagPolicy ReadTagPolicy(string name, ConfigJsonValue value)
    {
        if (value.Kind != ConfigJsonValueKind.Object || value.Members is null)
        {
            throw new XmlDocConfigException(string.Format(CultureInfo.CurrentCulture, XmlDocResourceStrings.Json_Invalid_TagPolicyObject, name));
        }

        XmlDocTagLayout layout = XmlDocTagLayout.Auto;
        int? maxSingleLineLength = null;
        bool? allowLineBreakInside = null;
        bool? selfClosingTrailingSpace = null;

        foreach (KeyValuePair<string, ConfigJsonValue> entry in value.Members)
        {
            switch (entry.Key)
            {
                case "layout":
                    layout = ParseLayout(entry.Key, entry.Value);
                    break;

                case "maxSingleLineLength":
                    maxSingleLineLength = ReadInt32(entry.Key, entry.Value);
                    break;

                case "allowLineBreakInside":
                    allowLineBreakInside = ReadBoolean(entry.Key, entry.Value);
                    break;

                case "selfClosingTrailingSpace":
                    selfClosingTrailingSpace = ReadBoolean(entry.Key, entry.Value);
                    break;
            }
        }

        return new XmlDocTagPolicy(layout, maxSingleLineLength, allowLineBreakInside, selfClosingTrailingSpace);
    }

    private static XmlDocTagLayout ParseLayout(string name, ConfigJsonValue value)
    {
        var raw = ReadString(name, value);
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
                throw new XmlDocConfigException(string.Format(CultureInfo.CurrentCulture, XmlDocResourceStrings.Json_Invalid_UnknownTagLayout, raw));
        }
    }
}
