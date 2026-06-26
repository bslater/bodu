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

    /// <summary>
    /// Reads a property value as a 32-bit integer.
    /// </summary>
    /// <param name="name">The property name, used in error messages.</param>
    /// <param name="value">The JSON value to interpret.</param>
    /// <returns>The integer value.</returns>
    /// <exception cref="XmlDocConfigException">Thrown when the value is not an integer.</exception>
    private static int ReadInt32(string name, ConfigJsonValue value)
    {
        if (!value.TryGetInt32(out var result))
        {
            throw new XmlDocConfigException(string.Format(CultureInfo.CurrentCulture, XmlDocResourceStrings.Json_Invalid_PropertyInteger, name));
        }

        return result;
    }

    /// <summary>
    /// Reads a property value as a string.
    /// </summary>
    /// <param name="name">The property name, used in error messages.</param>
    /// <param name="value">The JSON value to interpret.</param>
    /// <returns>The string content, or an empty string when the content is absent.</returns>
    /// <exception cref="XmlDocConfigException">Thrown when the value is not a string.</exception>
    private static string ReadString(string name, ConfigJsonValue value)
    {
        if (value.Kind != ConfigJsonValueKind.String)
        {
            throw new XmlDocConfigException(string.Format(CultureInfo.CurrentCulture, XmlDocResourceStrings.Json_Invalid_PropertyString, name));
        }

        return value.StringValue ?? string.Empty;
    }

    /// <summary>
    /// Reads a property value as a boolean.
    /// </summary>
    /// <param name="name">The property name, used in error messages.</param>
    /// <param name="value">The JSON value to interpret.</param>
    /// <returns>The boolean content.</returns>
    /// <exception cref="XmlDocConfigException">Thrown when the value is not a boolean.</exception>
    private static bool ReadBoolean(string name, ConfigJsonValue value)
    {
        if (value.Kind != ConfigJsonValueKind.Boolean)
        {
            throw new XmlDocConfigException(string.Format(CultureInfo.CurrentCulture, XmlDocResourceStrings.Json_Invalid_PropertyBoolean, name));
        }

        return value.BooleanValue;
    }

    /// <summary>
    /// Reads a property value as an immutable set of non-empty strings.
    /// </summary>
    /// <param name="name">The property name, used in error messages.</param>
    /// <param name="value">The JSON value to interpret.</param>
    /// <returns>The set of string entries, with empty entries discarded.</returns>
    /// <exception cref="XmlDocConfigException">Thrown when the value is not an array of strings.</exception>
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

    /// <summary>
    /// Reads the <c>tagPolicies</c> object, overlaying each per-tag policy on top of the supplied defaults.
    /// </summary>
    /// <param name="name">The property name, used in error messages.</param>
    /// <param name="value">The JSON value to interpret.</param>
    /// <param name="defaults">The default tag policies to overlay the JSON entries onto.</param>
    /// <returns>The merged tag policy map.</returns>
    /// <exception cref="XmlDocConfigException">Thrown when the value is not an object.</exception>
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

    /// <summary>
    /// Reads a single per-tag policy object.
    /// </summary>
    /// <param name="name">The tag name the policy applies to, used in error messages.</param>
    /// <param name="value">The JSON value to interpret.</param>
    /// <returns>The parsed tag policy.</returns>
    /// <exception cref="XmlDocConfigException">Thrown when the value is not an object.</exception>
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

    /// <summary>
    /// Parses a tag layout name into the corresponding <see cref="XmlDocTagLayout" /> value.
    /// </summary>
    /// <param name="name">The property name, used in error messages.</param>
    /// <param name="value">The JSON value carrying the layout name.</param>
    /// <returns>The parsed layout.</returns>
    /// <exception cref="XmlDocConfigException">Thrown when the value is not a string or names an unknown layout.</exception>
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
