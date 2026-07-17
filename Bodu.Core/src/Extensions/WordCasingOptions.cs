// ---------------------------------------------------------------------------------------------------------------
// <copyright file="WordCasingOptions.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;

namespace Bodu.Extensions;

/// <summary>
/// Supplies acronym, minor-word, and culture configuration for the acronym-aware casing converters on
/// <see cref="StringExtensions" />.
/// </summary>
/// <remarks>
/// <para>
/// Instances are immutable once constructed: every property is <c>init</c>-only. Construct a customised instance with
/// an object initialiser, or use <see cref="Default" /> for the standard configuration.
/// </para>
/// <para>
/// The same options object is consumed by every rich casing overload —
/// <see cref="StringExtensions.ToCamelCase(string, WordCasingOptions)" />,
/// <see cref="StringExtensions.ToPascalCase(string, WordCasingOptions)" />,
/// <see cref="StringExtensions.ToSnakeCase(string, WordCasingOptions)" />,
/// <see cref="StringExtensions.ToKebabCase(string, WordCasingOptions)" />,
/// <see cref="StringExtensions.ToTrainCase(string, WordCasingOptions)" />,
/// <see cref="StringExtensions.ToConstantCase(string, WordCasingOptions)" />,
/// <see cref="StringExtensions.ToDotCase(string, WordCasingOptions)" />,
/// <see cref="StringExtensions.ToTitleCase(string, WordCasingOptions)" />, and
/// <see cref="StringExtensions.ToSentenceCase(string, WordCasingOptions)" /> — so a single configuration can drive an
/// entire formatting pipeline consistently.
/// </para>
/// </remarks>
public sealed class WordCasingOptions
{
    /// <summary>The default acronym catalogue applied when an instance does not supply its own list.</summary>
    private static readonly string[] s_defaultAcronyms =
    [
        "API", "ASCII", "ATO", "BCL", "CPU", "CSS", "CSV", "DNS", "DTO", "EOF",
        "GUID", "HTML", "HTTP", "HTTPS", "ID", "IO", "IP", "IPv4", "IPv6", "JSON",
        "JWT", "OAuth", "PDF", "REST", "RFC", "RPC", "RSA", "SHA", "SHA256", "SQL",
        "TCP", "TLS", "UI", "URI", "URL", "UTF8", "XML", "XOF",
    ];

    /// <summary>The default minor-word list applied when an instance does not supply its own list.</summary>
    private static readonly string[] s_defaultMinorWords =
    [
        "a", "an", "and", "as", "at", "but", "by", "for", "from", "in", "into",
        "nor", "of", "on", "or", "over", "per", "the", "to", "via", "with",
    ];

    /// <summary>The shared default instance returned by <see cref="Default" />.</summary>
    private static readonly WordCasingOptions s_default = new();

    /// <summary>
    /// Gets a shared <see cref="WordCasingOptions" /> instance whose every property holds its default value.
    /// </summary>
    /// <value>An immutable, reusable options object suitable as the implicit default for every overload.</value>
    public static WordCasingOptions Default => s_default;

    /// <summary>
    /// Gets the known acronyms recognised during tokenisation and casing.
    /// </summary>
    /// <value>
    /// Defaults to a built-in list of 38 common technology acronyms. Supply a custom collection to extend or replace
    /// the catalogue.
    /// </value>
    public IReadOnlyCollection<string> Acronyms { get; init; } = s_defaultAcronyms;

    /// <summary>
    /// Gets the minor (connective) words eligible for down-casing in title case.
    /// </summary>
    /// <value>
    /// Defaults to a built-in list of 21 English connective words. Only consulted when
    /// <see cref="LowerCaseMinorWords" /> is <see langword="true" />.
    /// </value>
    public IReadOnlyCollection<string> MinorWords { get; init; } = s_defaultMinorWords;

    /// <summary>
    /// Gets the culture used for all case conversions.
    /// </summary>
    /// <value>
    /// Defaults to <see cref="CultureInfo.InvariantCulture" />. Supply a specific culture (for example <c>tr-TR</c>)
    /// when locale-sensitive casing is required.
    /// </value>
    public CultureInfo Culture { get; init; } = CultureInfo.InvariantCulture;

    /// <summary>
    /// Gets a value indicating whether all-uppercase tokens and known acronyms keep their acronym spelling in the
    /// converters that consult this flag.
    /// </summary>
    /// <value>Defaults to <see langword="true" />.</value>
    /// <remarks>
    /// Only <see cref="StringExtensions.ToTitleCase(string, WordCasingOptions)" /> and
    /// <see cref="StringExtensions.ToSentenceCase(string, WordCasingOptions)" /> read this flag. The tokeniser used by
    /// the other case converters (camel, Pascal, snake, kebab, train, constant, dot) canonicalises tokens that match a
    /// <see cref="Acronyms" /> entry unconditionally, so those converters render known acronyms in their catalogue
    /// spelling regardless of this setting.
    /// </remarks>
    public bool PreserveAcronyms { get; init; } = true;

    /// <summary>
    /// Gets a value indicating whether mixed-case tokens such as <c>OAuth</c>, <c>iPhone</c>, and <c>eBay</c> survive
    /// casing changes verbatim.
    /// </summary>
    /// <value>Defaults to <see langword="true" />.</value>
    public bool PreserveMixedCaseWords { get; init; } = true;

    /// <summary>
    /// Gets a value indicating whether interior minor words are down-cased in title case.
    /// </summary>
    /// <value>
    /// Defaults to <see langword="false" />. Affects only
    /// <see cref="StringExtensions.ToTitleCase(string, WordCasingOptions)" />.
    /// </value>
    public bool LowerCaseMinorWords { get; init; }
}
