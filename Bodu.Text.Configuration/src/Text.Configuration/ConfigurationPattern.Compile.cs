// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConfigurationPattern.Compile.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Bodu.Text.Configuration;

public sealed partial class ConfigurationPattern
{
    /// <summary>
    /// Translates a glob expression into the equivalent <see cref="Regex" /> pattern, anchored to the start and end of
    /// the input.
    /// </summary>
    /// <param name="pattern">The glob expression to translate.</param>
    /// <returns>The translated regex source.</returns>
    /// <exception cref="ConfigurationParseException">The pattern contains an unbalanced brace or bracket.</exception>
    private static string TranslateToRegex(string pattern)
    {
        var sb = new StringBuilder(pattern.Length * 2);

        var hasSlash = pattern.Contains('/');

        // Anchor unconditionally to the end. The start is anchored only when the pattern contains a slash;
        // EditorConfig treats slashless patterns as matching at any directory depth.
        sb.Append('^');
        if (!hasSlash)
            sb.Append("(?:.*/)?");

        TranslateExpression(pattern, sb);

        sb.Append('$');
        return sb.ToString();
    }

    /// <summary>
    /// Translates the glob expression <paramref name="pattern" /> into regex syntax, appending the result to
    /// <paramref name="sb" />.
    /// </summary>
    /// <param name="pattern">The glob expression to translate.</param>
    /// <param name="sb">The buffer that receives the translated regex.</param>
    /// <exception cref="ConfigurationParseException">The pattern contains an unbalanced brace or bracket.</exception>
    private static void TranslateExpression(string pattern, StringBuilder sb)
    {
        for (var i = 0; i < pattern.Length;)
        {
            var c = pattern[i];

            switch (c)
            {
                case '\\':
                    // Escape next character literally.
                    if (i + 1 < pattern.Length)
                    {
                        sb.Append(Regex.Escape(pattern[i + 1].ToString()));
                        i += 2;
                    }
                    else
                    {
                        sb.Append(Regex.Escape("\\"));
                        i++;
                    }
                    break;

                case '/':
                    // The pattern `/**/` matches zero or more path segments, mirroring gitignore semantics.
                    if (i + 3 < pattern.Length && pattern[i + 1] == '*' && pattern[i + 2] == '*' && pattern[i + 3] == '/')
                    {
                        sb.Append("(?:/|/.*/)");
                        i += 4;
                    }
                    else
                    {
                        sb.Append('/');
                        i++;
                    }
                    break;

                case '*':
                    if (i + 1 < pattern.Length && pattern[i + 1] == '*')
                    {
                        sb.Append(".*");
                        i += 2;
                    }
                    else
                    {
                        sb.Append("[^/]*");
                        i++;
                    }
                    break;

                case '?':
                    sb.Append("[^/]");
                    i++;
                    break;

                case '[':
                    i = TranslateCharClass(pattern, i, sb);
                    break;

                case '{':
                    i = TranslateBraceGroup(pattern, i, sb);
                    break;

                default:
                    sb.Append(Regex.Escape(c.ToString()));
                    i++;
                    break;
            }
        }
    }

    /// <summary>
    /// Translates the bracket character class beginning at <paramref name="start" /> into a regex set.
    /// </summary>
    /// <param name="pattern">The glob expression being translated.</param>
    /// <param name="start">The index of the opening <c>[</c>.</param>
    /// <param name="sb">The buffer that receives the translated regex.</param>
    /// <returns>The index immediately following the closing <c>]</c>.</returns>
    /// <exception cref="ConfigurationParseException">The bracket is unbalanced.</exception>
    private static int TranslateCharClass(string pattern, int start, StringBuilder sb)
    {
        var close = FindClosingBracket(pattern, start);
        if (close < 0)
            throw new ConfigurationParseException(new ConfigurationDiagnostic(
                ConfigurationDiagnosticSeverity.Error,
                ConfigurationDiagnosticCode.UnbalancedBracket,
                ConfigurationResourceStrings.Format_Invalid_UnbalancedBracket,
                ConfigurationSourceLocation.None));

        var body = pattern.Substring(start + 1, close - start - 1);
        sb.Append('[');
        var j = 0;
        if (body.Length > 0 && body[0] == '!')
        {
            sb.Append('^');
            j = 1;
        }

        for (; j < body.Length; j++)
        {
            var ch = body[j];
            if (ch == '\\' && j + 1 < body.Length)
            {
                sb.Append(Regex.Escape(body[j + 1].ToString()));
                j++;
            }
            else if (ch is ']' or '\\' or '^')
            {
                sb.Append('\\').Append(ch);
            }
            else
            {
                sb.Append(ch);
            }
        }

        sb.Append(']');
        return close + 1;
    }

    /// <summary>
    /// Finds the index of the <c>]</c> that closes the character class opened at <paramref name="start" />.
    /// </summary>
    /// <param name="pattern">The glob expression to scan.</param>
    /// <param name="start">The index of the opening <c>[</c>.</param>
    /// <returns>The index of the closing <c>]</c>, or <c>-1</c> when none is found.</returns>
    private static int FindClosingBracket(string pattern, int start)
    {
        for (var i = start + 1; i < pattern.Length; i++)
        {
            if (pattern[i] == '\\' && i + 1 < pattern.Length)
            {
                i++;
                continue;
            }

            if (pattern[i] == ']')
                return i;
        }

        return -1;
    }

    /// <summary>
    /// Translates the brace group beginning at <paramref name="start" /> — an alternation or a numeric range — into
    /// regex syntax.
    /// </summary>
    /// <param name="pattern">The glob expression being translated.</param>
    /// <param name="start">The index of the opening <c>{</c>.</param>
    /// <param name="sb">The buffer that receives the translated regex.</param>
    /// <returns>The index immediately following the closing <c>}</c>.</returns>
    /// <exception cref="ConfigurationParseException">The brace is unbalanced.</exception>
    private static int TranslateBraceGroup(string pattern, int start, StringBuilder sb)
    {
        var close = FindMatchingBrace(pattern, start);
        if (close < 0)
            throw new ConfigurationParseException(new ConfigurationDiagnostic(
                ConfigurationDiagnosticSeverity.Error,
                ConfigurationDiagnosticCode.UnbalancedBrace,
                ConfigurationResourceStrings.Format_Invalid_UnbalancedBrace,
                ConfigurationSourceLocation.None));

        var body = pattern.Substring(start + 1, close - start - 1);

        // Numeric range {n1..n2}?
        if (TryTranslateNumericRange(body, sb))
            return close + 1;

        // Brace alternation {a,b,c} with possible nesting.
        List<string> alternatives = SplitTopLevelCommas(body);
        sb.Append("(?:");
        for (var i = 0; i < alternatives.Count; i++)
        {
            if (i > 0)
                sb.Append('|');
            TranslateExpression(alternatives[i], sb);
        }
        sb.Append(')');
        return close + 1;
    }

    /// <summary>
    /// Finds the index of the <c>}</c> that closes the brace group opened at <paramref name="start" />, accounting for
    /// nesting.
    /// </summary>
    /// <param name="pattern">The glob expression to scan.</param>
    /// <param name="start">The index of the opening <c>{</c>.</param>
    /// <returns>The index of the matching <c>}</c>, or <c>-1</c> when none is found.</returns>
    private static int FindMatchingBrace(string pattern, int start)
    {
        var depth = 0;
        for (var i = start; i < pattern.Length; i++)
        {
            if (pattern[i] == '\\' && i + 1 < pattern.Length)
            {
                i++;
                continue;
            }

            if (pattern[i] == '{')
                depth++;
            else if (pattern[i] == '}')
            {
                depth--;
                if (depth == 0)
                    return i;
            }
        }

        return -1;
    }

    /// <summary>
    /// Splits a brace-group body on commas that are not nested inside an inner brace group.
    /// </summary>
    /// <param name="body">The brace-group body, excluding the surrounding braces.</param>
    /// <returns>The top-level comma-separated alternatives.</returns>
    private static List<string> SplitTopLevelCommas(string body)
    {
        List<string> result = new();
        var depth = 0;
        var start = 0;
        for (var i = 0; i < body.Length; i++)
        {
            var c = body[i];
            if (c == '\\' && i + 1 < body.Length)
            {
                i++;
                continue;
            }

            if (c == '{')
                depth++;
            else if (c == '}')
                depth--;
            else if (c == ',' && depth == 0)
            {
                result.Add(body[start..i]);
                start = i + 1;
            }
        }

        result.Add(body[start..]);
        return result;
    }

    /// <summary>
    /// Attempts to translate a brace-group body of the form <c>n1..n2</c> into a regex alternation over the inclusive
    /// integer range.
    /// </summary>
    /// <param name="body">The brace-group body, excluding the surrounding braces.</param>
    /// <param name="sb">The buffer that receives the translated regex when the body is a numeric range.</param>
    /// <returns>
    /// <see langword="true" /> when <paramref name="body" /> was a numeric range and was translated; otherwise,
    /// <see langword="false" />.
    /// </returns>
    private static bool TryTranslateNumericRange(string body, StringBuilder sb)
    {
        var dot = body.IndexOf("..", StringComparison.Ordinal);
        if (dot <= 0 || dot + 2 >= body.Length)
            return false;

        var leftText = body[..dot];
        var rightText = body[(dot + 2)..];

        if (!long.TryParse(leftText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var left)
            || !long.TryParse(rightText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var right))
        {
            return false;
        }

        if (left > right)
            (left, right) = (right, left);

        // Build an alternation over every integer in the range. Acceptable for the modest ranges typical of
        // .editorconfig files (e.g. {1..10}); callers requesting huge ranges will pay the regex compile cost.
        sb.Append("(?:");
        for (var value = left; value <= right; value++)
        {
            if (value > left)
                sb.Append('|');
            sb.Append(value.ToString(CultureInfo.InvariantCulture));
        }
        sb.Append(')');
        return true;
    }
}
