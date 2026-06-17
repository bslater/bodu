// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BoduJsonParser.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Bodu.CodeStyle.XmlDocumentation.Configuration;

/// <summary>
/// Parses the small JSON dialect used by <c>bodu.xmldocstyle.json</c> into a <see cref="BoduJsonValue" /> tree.
/// </summary>
/// <remarks>
/// This is a minimal, allocation-light recursive-descent parser that supports the JSON grammar (objects, arrays,
/// strings with escapes, numbers, <c>true</c>/<c>false</c>/<c>null</c>). It exists so the configuration reader —
/// and therefore the analyzer package — does not depend on <c>System.Text.Json</c>, which is not guaranteed to be
/// present in every analyzer host. Malformed input throws <see cref="FormatException" />.
/// </remarks>
internal static class BoduJsonParser
{
    /// <summary>
    /// Parses the supplied JSON text.
    /// </summary>
    /// <param name="json">The JSON document text.</param>
    /// <returns>The parsed value tree.</returns>
    /// <exception cref="FormatException">Thrown when the text is not well-formed JSON.</exception>
    public static BoduJsonValue Parse(string json)
    {
        var position = 0;
        SkipWhitespace(json, ref position);
        BoduJsonValue value = ParseValue(json, ref position);
        SkipWhitespace(json, ref position);
        if (position != json.Length)
        {
            throw new FormatException("Unexpected trailing content after the JSON value.");
        }

        return value;
    }

    private static BoduJsonValue ParseValue(string json, ref int position)
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
                return BoduJsonValue.ForString(ParseString(json, ref position));
            case 't':
            case 'f':
                return BoduJsonValue.ForBoolean(ParseLiteralBoolean(json, ref position));
            case 'n':
                ParseLiteral(json, ref position, "null");
                return BoduJsonValue.ForNull();
            default:
                if (c == '-' || (c >= '0' && c <= '9'))
                {
                    return BoduJsonValue.ForNumber(ParseNumber(json, ref position));
                }

                throw new FormatException(FormattableString.Invariant($"Unexpected character '{c}' at position {position}."));
        }
    }

    private static BoduJsonValue ParseObject(string json, ref int position)
    {
        position++; // consume '{'
        var members = new Dictionary<string, BoduJsonValue>(StringComparer.Ordinal);

        SkipWhitespace(json, ref position);
        if (Peek(json, position) == '}')
        {
            position++;
            return BoduJsonValue.ForObject(members);
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

        return BoduJsonValue.ForObject(members);
    }

    private static BoduJsonValue ParseArray(string json, ref int position)
    {
        position++; // consume '['
        var items = new List<BoduJsonValue>();

        SkipWhitespace(json, ref position);
        if (Peek(json, position) == ']')
        {
            position++;
            return BoduJsonValue.ForArray(items);
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

        return BoduJsonValue.ForArray(items);
    }

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

    private static void ParseLiteral(string json, ref int position, string literal)
    {
        if (position + literal.Length > json.Length ||
            string.CompareOrdinal(json, position, literal, 0, literal.Length) != 0)
        {
            throw new FormatException(FormattableString.Invariant($"Expected '{literal}' literal."));
        }

        position += literal.Length;
    }

    private static bool IsNumberChar(char c) =>
        (c >= '0' && c <= '9') || c == '.' || c == 'e' || c == 'E' || c == '+' || c == '-';

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

    private static char Peek(string json, int position) =>
        position < json.Length ? json[position] : '\0';

    private static char NextOrThrow(string json, ref int position)
    {
        if (position >= json.Length)
        {
            throw new FormatException("Unexpected end of JSON input.");
        }

        return json[position++];
    }

    private static void Expect(string json, ref int position, char expected)
    {
        if (NextOrThrow(json, ref position) != expected)
        {
            throw new FormatException(FormattableString.Invariant($"Expected '{expected}' in JSON input."));
        }
    }
}
