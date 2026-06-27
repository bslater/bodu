// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConfigJsonParser.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Bodu.CodeStyle.XmlDocumentation.Configuration;

/// <summary>
/// Parses the small JSON dialect used by <c>bodu.xmldocstyle.json</c> into a <see cref="ConfigJsonValue" /> tree.
/// </summary>
/// <remarks>
/// This is a minimal, allocation-light recursive-descent parser that supports the JSON grammar (objects, arrays,
/// strings with escapes, numbers, <c>true</c>/<c>false</c>/<c>null</c>). It exists so the configuration reader — and
/// therefore the analyzer package — does not depend on <c>System.Text.Json</c>, which is not guaranteed to be present
/// in every analyzer host. Malformed input throws <see cref="FormatException" />.
/// </remarks>
internal static class ConfigJsonParser
{
    /// <summary>
    /// Parses the supplied JSON text.
    /// </summary>
    /// <param name="json">The JSON document text.</param>
    /// <returns>The parsed value tree.</returns>
    /// <exception cref="FormatException">Thrown when the text is not well-formed JSON.</exception>
    public static ConfigJsonValue Parse(string json)
    {
        var position = 0;
        SkipWhitespace(json, ref position);
        ConfigJsonValue value = ParseValue(json, ref position);
        SkipWhitespace(json, ref position);
        if (position != json.Length)
        {
            throw new FormatException("Unexpected trailing content after the JSON value.");
        }

        return value;
    }

    /// <summary>
    /// Parses a single JSON value starting at the current position.
    /// </summary>
    /// <param name="json">The JSON document text.</param>
    /// <param name="position">The current parse position; advanced past the parsed value.</param>
    /// <returns>The parsed value.</returns>
    /// <exception cref="FormatException">Thrown when no valid value begins at the current position.</exception>
    private static ConfigJsonValue ParseValue(string json, ref int position)
    {
        if (position >= json.Length)
        {
            throw new FormatException("Unexpected end of JSON input.");
        }

        var c = json[position];
        switch (c)
        {
            case '{':
                return ParseObject(json, ref position);
            case '[':
                return ParseArray(json, ref position);
            case '"':
                return ConfigJsonValue.ForString(ParseString(json, ref position));
            case 't':
            case 'f':
                return ConfigJsonValue.ForBoolean(ParseLiteralBoolean(json, ref position));
            case 'n':
                ParseLiteral(json, ref position, "null");
                return ConfigJsonValue.ForNull();
            default:
                if (c == '-' || (c >= '0' && c <= '9'))
                {
                    return ConfigJsonValue.ForNumber(ParseNumber(json, ref position));
                }

                throw new FormatException(FormattableString.Invariant($"Unexpected character '{c}' at position {position}."));
        }
    }

    /// <summary>
    /// Parses a JSON object starting at the opening brace.
    /// </summary>
    /// <param name="json">The JSON document text.</param>
    /// <param name="position">The current parse position; advanced past the closing brace.</param>
    /// <returns>The parsed object value.</returns>
    /// <exception cref="FormatException">Thrown when the object is malformed.</exception>
    private static ConfigJsonValue ParseObject(string json, ref int position)
    {
        position++; // consume '{'
        var members = new Dictionary<string, ConfigJsonValue>(StringComparer.Ordinal);

        SkipWhitespace(json, ref position);
        if (Peek(json, position) == '}')
        {
            position++;
            return ConfigJsonValue.ForObject(members);
        }

        while (true)
        {
            SkipWhitespace(json, ref position);
            if (Peek(json, position) != '"')
            {
                throw new FormatException("Expected a property name string in JSON object.");
            }

            var name = ParseString(json, ref position);
            SkipWhitespace(json, ref position);
            Expect(json, ref position, ':');
            SkipWhitespace(json, ref position);
            members[name] = ParseValue(json, ref position);

            SkipWhitespace(json, ref position);
            var next = NextOrThrow(json, ref position);
            if (next == '}')
            {
                break;
            }

            if (next != ',')
            {
                throw new FormatException("Expected ',' or '}' in JSON object.");
            }
        }

        return ConfigJsonValue.ForObject(members);
    }

    /// <summary>
    /// Parses a JSON array starting at the opening bracket.
    /// </summary>
    /// <param name="json">The JSON document text.</param>
    /// <param name="position">The current parse position; advanced past the closing bracket.</param>
    /// <returns>The parsed array value.</returns>
    /// <exception cref="FormatException">Thrown when the array is malformed.</exception>
    private static ConfigJsonValue ParseArray(string json, ref int position)
    {
        position++; // consume '['
        var items = new List<ConfigJsonValue>();

        SkipWhitespace(json, ref position);
        if (Peek(json, position) == ']')
        {
            position++;
            return ConfigJsonValue.ForArray(items);
        }

        while (true)
        {
            SkipWhitespace(json, ref position);
            items.Add(ParseValue(json, ref position));

            SkipWhitespace(json, ref position);
            var next = NextOrThrow(json, ref position);
            if (next == ']')
            {
                break;
            }

            if (next != ',')
            {
                throw new FormatException("Expected ',' or ']' in JSON array.");
            }
        }

        return ConfigJsonValue.ForArray(items);
    }

    /// <summary>
    /// Parses a JSON string literal starting at the opening quote, resolving escape sequences.
    /// </summary>
    /// <param name="json">The JSON document text.</param>
    /// <param name="position">The current parse position; advanced past the closing quote.</param>
    /// <returns>The decoded string content.</returns>
    /// <exception cref="FormatException">
    /// Thrown when the string is unterminated or contains an invalid character.
    /// </exception>
    private static string ParseString(string json, ref int position)
    {
        position++; // consume opening quote
        var builder = new StringBuilder();
        while (true)
        {
            if (position >= json.Length)
            {
                throw new FormatException("Unterminated JSON string.");
            }

            var c = json[position++];
            if (c == '"')
            {
                return builder.ToString();
            }

            if (c == '\\')
            {
                builder.Append(ParseEscape(json, ref position));
                continue;
            }

            if (c < ' ')
            {
                throw new FormatException("Unescaped control character in JSON string.");
            }

            builder.Append(c);
        }
    }

    /// <summary>
    /// Parses a single escape sequence following a backslash within a JSON string.
    /// </summary>
    /// <param name="json">The JSON document text.</param>
    /// <param name="position">The current parse position, just past the backslash; advanced past the escape.</param>
    /// <returns>The character represented by the escape sequence.</returns>
    /// <exception cref="FormatException">Thrown when the escape sequence is invalid or unterminated.</exception>
    private static char ParseEscape(string json, ref int position)
    {
        if (position >= json.Length)
        {
            throw new FormatException("Unterminated escape sequence in JSON string.");
        }

        var c = json[position++];
        switch (c)
        {
            case '"': return '"';
            case '\\': return '\\';
            case '/': return '/';
            case 'b': return '\b';
            case 'f': return '\f';
            case 'n': return '\n';
            case 'r': return '\r';
            case 't': return '\t';
            case 'u':
                if (position + 4 > json.Length ||
                    !int.TryParse(json.Substring(position, 4), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var code))
                {
                    throw new FormatException("Invalid \\u escape in JSON string.");
                }

                position += 4;
                return (char)code;
            default:
                throw new FormatException(FormattableString.Invariant($"Invalid escape '\\{c}' in JSON string."));
        }
    }

    /// <summary>
    /// Parses a JSON number starting at the current position.
    /// </summary>
    /// <param name="json">The JSON document text.</param>
    /// <param name="position">The current parse position; advanced past the number token.</param>
    /// <returns>The parsed numeric value.</returns>
    /// <exception cref="FormatException">Thrown when the number token is not a valid JSON number.</exception>
    private static double ParseNumber(string json, ref int position)
    {
        var start = position;
        if (Peek(json, position) == '-') position++;
        while (position < json.Length && IsNumberChar(json[position])) position++;

        var token = json.Substring(start, position - start);
        if (!double.TryParse(token, NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
        {
            throw new FormatException(FormattableString.Invariant($"Invalid JSON number '{token}'."));
        }

        return value;
    }

    /// <summary>
    /// Parses a <c>true</c> or <c>false</c> literal starting at the current position.
    /// </summary>
    /// <param name="json">The JSON document text.</param>
    /// <param name="position">The current parse position; advanced past the literal.</param>
    /// <returns><see langword="true" /> for the <c>true</c> literal; otherwise <see langword="false" />.</returns>
    /// <exception cref="FormatException">Thrown when the expected boolean literal is not present.</exception>
    private static bool ParseLiteralBoolean(string json, ref int position)
    {
        if (Peek(json, position) == 't')
        {
            ParseLiteral(json, ref position, "true");
            return true;
        }

        ParseLiteral(json, ref position, "false");
        return false;
    }

    /// <summary>
    /// Consumes an expected keyword literal at the current position.
    /// </summary>
    /// <param name="json">The JSON document text.</param>
    /// <param name="position">The current parse position; advanced past the literal on success.</param>
    /// <param name="literal">The literal text expected at the current position.</param>
    /// <exception cref="FormatException">Thrown when the expected literal is not present.</exception>
    private static void ParseLiteral(string json, ref int position, string literal)
    {
        if (position + literal.Length > json.Length ||
            string.CompareOrdinal(json, position, literal, 0, literal.Length) != 0)
        {
            throw new FormatException(FormattableString.Invariant($"Expected '{literal}' literal."));
        }

        position += literal.Length;
    }

    /// <summary>
    /// Determines whether a character can appear within a JSON number token.
    /// </summary>
    /// <param name="c">The character to test.</param>
    /// <returns>
    /// <see langword="true" /> when the character is a valid number character; otherwise <see langword="false" />.
    /// </returns>
    private static bool IsNumberChar(char c) =>
        (c >= '0' && c <= '9') || c == '.' || c == 'e' || c == 'E' || c == '+' || c == '-';

    /// <summary>
    /// Advances the parse position past any JSON whitespace.
    /// </summary>
    /// <param name="json">The JSON document text.</param>
    /// <param name="position">The current parse position; advanced past contiguous whitespace.</param>
    private static void SkipWhitespace(string json, ref int position)
    {
        while (position < json.Length)
        {
            var c = json[position];
            if (c == ' ' || c == '\t' || c == '\r' || c == '\n')
            {
                position++;
                continue;
            }

            break;
        }
    }

    /// <summary>
    /// Returns the character at the current position without advancing.
    /// </summary>
    /// <param name="json">The JSON document text.</param>
    /// <param name="position">The position to inspect.</param>
    /// <returns>The character at the position, or <c>'\0'</c> when the position is at or past the end.</returns>
    private static char Peek(string json, int position) =>
        position < json.Length ? json[position] : '\0';

    /// <summary>
    /// Returns the character at the current position and advances past it.
    /// </summary>
    /// <param name="json">The JSON document text.</param>
    /// <param name="position">The current parse position; advanced by one.</param>
    /// <returns>The character consumed at the current position.</returns>
    /// <exception cref="FormatException">Thrown when the position is at or past the end of input.</exception>
    private static char NextOrThrow(string json, ref int position)
    {
        if (position >= json.Length)
        {
            throw new FormatException("Unexpected end of JSON input.");
        }

        return json[position++];
    }

    /// <summary>
    /// Consumes the character at the current position and verifies it matches the expected character.
    /// </summary>
    /// <param name="json">The JSON document text.</param>
    /// <param name="position">The current parse position; advanced by one.</param>
    /// <param name="expected">The character required at the current position.</param>
    /// <exception cref="FormatException">
    /// Thrown when the current character does not match <paramref name="expected" />.
    /// </exception>
    private static void Expect(string json, ref int position, char expected)
    {
        if (NextOrThrow(json, ref position) != expected)
        {
            throw new FormatException(FormattableString.Invariant($"Expected '{expected}' in JSON input."));
        }
    }
}
