// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BoduConfigurationPattern.Compile.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Bodu.Text.Configuration;

public sealed partial class BoduConfigurationPattern
{
    /// <summary>
    /// Translates a glob expression into the equivalent <see cref="Regex" /> pattern, anchored to the start
    /// and end of the input.
    /// </summary>
    /// <param name="pattern">The glob expression to translate.</param>
    /// <returns>The translated regex source.</returns>
    /// <exception cref="BoduConfigurationParseException">The pattern contains an unbalanced brace or bracket.</exception>
    private static string TranslateToRegex(string pattern)
    {
        StringBuilder sb = new StringBuilder(pattern.Length * 2);

        bool hasSlash = pattern.Contains('/');

        // Anchor unconditionally to the end. The start is anchored only when the pattern contains a slash;
        // EditorConfig treats slashless patterns as matching at any directory depth.
        sb.Append('^');
        if (!hasSlash)
            sb.Append("(?:.*/)?");

        TranslateExpression(pattern, sb);

        sb.Append('$');
        return sb.ToString();
    }

    private static void TranslateExpression(string pattern, StringBuilder sb)
    {
        for (int i = 0; i < pattern.Length;)
        {
            char c = pattern[i];

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

    private static int TranslateCharClass(string pattern, int start, StringBuilder sb)
    {
        int close = FindClosingBracket(pattern, start);
        if (close < 0)
            throw new BoduConfigurationParseException(new BoduConfigurationDiagnostic(
                BoduConfigurationDiagnosticSeverity.Error,
                BoduConfigurationDiagnosticCode.UnbalancedBracket,
                ConfigurationResourceStrings.Format_Invalid_UnbalancedBracket,
                BoduConfigurationSourceLocation.None));

        string body = pattern.Substring(start + 1, close - start - 1);
        sb.Append('[');
        int j = 0;
        if (body.Length > 0 && body[0] == '!')
        {
            sb.Append('^');
            j = 1;
        }

        for (; j < body.Length; j++)
        {
            char ch = body[j];
            if (ch == '\\' && j + 1 < body.Length)
            {
                sb.Append(Regex.Escape(body[j + 1].ToString()));
                j++;
            }
            else if (ch == ']' || ch == '\\' || ch == '^')
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

    private static int FindClosingBracket(string pattern, int start)
    {
        for (int i = start + 1; i < pattern.Length; i++)
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

    private static int TranslateBraceGroup(string pattern, int start, StringBuilder sb)
    {
        int close = FindMatchingBrace(pattern, start);
        if (close < 0)
            throw new BoduConfigurationParseException(new BoduConfigurationDiagnostic(
                BoduConfigurationDiagnosticSeverity.Error,
                BoduConfigurationDiagnosticCode.UnbalancedBrace,
                ConfigurationResourceStrings.Format_Invalid_UnbalancedBrace,
                BoduConfigurationSourceLocation.None));

        string body = pattern.Substring(start + 1, close - start - 1);

        // Numeric range {n1..n2}?
        if (TryTranslateNumericRange(body, sb))
            return close + 1;

        // Brace alternation {a,b,c} with possible nesting.
        List<string> alternatives = SplitTopLevelCommas(body);
        sb.Append("(?:");
        for (int i = 0; i < alternatives.Count; i++)
        {
            if (i > 0)
                sb.Append('|');
            TranslateExpression(alternatives[i], sb);
        }
        sb.Append(')');
        return close + 1;
    }

    private static int FindMatchingBrace(string pattern, int start)
    {
        int depth = 0;
        for (int i = start; i < pattern.Length; i++)
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

    private static List<string> SplitTopLevelCommas(string body)
    {
        List<string> result = new();
        int depth = 0;
        int start = 0;
        for (int i = 0; i < body.Length; i++)
        {
            char c = body[i];
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
                result.Add(body.Substring(start, i - start));
                start = i + 1;
            }
        }

        result.Add(body.Substring(start));
        return result;
    }

    private static bool TryTranslateNumericRange(string body, StringBuilder sb)
    {
        int dot = body.IndexOf("..", StringComparison.Ordinal);
        if (dot <= 0 || dot + 2 >= body.Length)
            return false;

        string leftText = body.Substring(0, dot);
        string rightText = body.Substring(dot + 2);

        if (!long.TryParse(leftText, NumberStyles.Integer, CultureInfo.InvariantCulture, out long left)
            || !long.TryParse(rightText, NumberStyles.Integer, CultureInfo.InvariantCulture, out long right))
        {
            return false;
        }

        if (left > right)
            (left, right) = (right, left);

        // Build an alternation over every integer in the range. Acceptable for the modest ranges typical of
        // .editorconfig files (e.g. {1..10}); callers requesting huge ranges will pay the regex compile cost.
        sb.Append("(?:");
        for (long value = left; value <= right; value++)
        {
            if (value > left)
                sb.Append('|');
            sb.Append(value.ToString(CultureInfo.InvariantCulture));
        }
        sb.Append(')');
        return true;
    }
}
