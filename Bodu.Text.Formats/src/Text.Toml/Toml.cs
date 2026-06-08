// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Toml.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;

namespace Bodu.Text.Toml;

/// <summary>
/// Provides methods for parsing TOML v1.1.0 configuration text into a <see cref="TomlTable" /> document model.
/// </summary>
/// <remarks>
/// <para>
/// The parser implements the TOML v1.1.0 grammar: bare, quoted, and dotted keys; the four string forms (basic,
/// multi-line basic, literal, multi-line literal) with the v1.1.0 <c>\e</c> and <c>\xHH</c> escapes; decimal,
/// hexadecimal, octal, and binary integers; floats including <c>inf</c> and <c>nan</c>; booleans; the four RFC 3339
/// date-time types with v1.1.0 optional seconds; arrays; inline tables (including the v1.1.0 multi-line and
/// trailing-comma forms); standard tables; and arrays of tables. Redefinition and ordering rules are enforced per the
/// specification.
/// </para>
/// </remarks>
/// <example>
///<![CDATA[
/// TomlTable doc = Toml.Parse("""
///     title = "example"
///
///     [owner]
///     name = "Tom"
///     dob = 1979-05-27T07:32:00Z
///     """);
///
/// string title = ((TomlString)doc["title"]).Value;
/// var owner = (TomlTable)doc["owner"];
///]]>
/// </example>
public static partial class Toml
{
    /// <summary>
    /// Parses a TOML document from the specified character span.
    /// </summary>
    /// <param name="source">The TOML source text.</param>
    /// <returns>The root <see cref="TomlTable" /> of the parsed document.</returns>
    /// <exception cref="TomlFormatException">
    /// Thrown when <paramref name="source" /> is not a valid TOML document.
    /// </exception>
    public static TomlTable Parse(ReadOnlySpan<char> source)
    {
        Parser parser = new(source);
        return parser.Parse();
    }

    /// <summary>
    /// Attempts to parse a TOML document from the specified character span.
    /// </summary>
    /// <param name="source">The TOML source text.</param>
    /// <param name="document">
    /// When this method returns <see langword="true" />, the parsed document; otherwise <see langword="null" />.
    /// </param>
    /// <returns><see langword="true" /> when parsing succeeded; otherwise <see langword="false" />.</returns>
    public static bool TryParse(ReadOnlySpan<char> source, [NotNullWhen(true)] out TomlTable? document)
    {
        try
        {
            document = Parse(source);
            return true;
        }
        catch (TomlFormatException)
        {
            document = null;
            return false;
        }
    }
}
