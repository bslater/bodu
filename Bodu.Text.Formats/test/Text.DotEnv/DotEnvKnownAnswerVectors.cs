// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DotEnvKnownAnswerVectors.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.DotEnv;

/// <summary>
/// Provides Known Answer Test vectors for the DotEnv format sourced from the dotenv community specification and
/// common-practice inputs. Each provider yields rows in the shape expected by MSTest's <c>[DynamicData]</c> attribute.
/// </summary>
public static class DotEnvKnownAnswerVectors
{
    /// <summary>
    /// Dotenv community specification citation source for vectors derived from the canonical specification.
    /// </summary>
    private const string Spec = "dotenv community specification";

    /// <summary>
    /// Returns the concatenation of all positive vector providers, used by round-trip and comprehensive property tests.
    /// </summary>
    /// <returns>A sequence suitable for <c>[DynamicData]</c>.</returns>
    public static IEnumerable<object[]> AllVectors()
    {
        foreach (var row in SpecVectors())
            yield return row;
    }

    /// <summary>
    /// Returns positive vectors derived from the dotenv community specification, covering unquoted and quoted values,
    /// escape sequences, blank lines, comments, the <c>export</c> prefix, inline comments, multiline values, line
    /// continuations, CRLF line endings, and common real-world patterns.
    /// </summary>
    /// <returns>A sequence suitable for <c>[DynamicData]</c>.</returns>
    public static IEnumerable<object[]> SpecVectors()
    {
        // 1. Simple unquoted value
        yield return V("Simple unquoted value",
            "KEY=value",
            [("KEY", "value")]);

        // 2. Double-quoted value strips outer quotes
        yield return V("Double-quoted value strips outer quotes",
            "KEY=\"value\"",
            [("KEY", "value")]);

        // 3. Single-quoted value strips outer quotes
        yield return V("Single-quoted value strips outer quotes",
            "KEY='value'",
            [("KEY", "value")]);

        // 4. Empty unquoted value
        yield return V("Empty unquoted value",
            "KEY=",
            [("KEY", "")]);

        // 5. Empty double-quoted value
        yield return V("Empty double-quoted value",
            "KEY=\"\"",
            [("KEY", "")]);

        // 6. Empty single-quoted value
        yield return V("Empty single-quoted value",
            "KEY=''",
            [("KEY", "")]);

        // 7. Blank lines ignored
        yield return V("Blank lines ignored",
            "\n\nKEY=value\n\n",
            [("KEY", "value")]);

        // 8. Hash comment line ignored
        yield return V("Hash comment line ignored",
            "# comment\nKEY=value",
            [("KEY", "value")]);

        // 9. Unquoted value has leading and trailing whitespace trimmed
        yield return V("Unquoted value has leading and trailing whitespace trimmed",
            "KEY=  hello  ",
            [("KEY", "hello")]);

        // 10. Export prefix stripped
        yield return V("Export prefix stripped",
            "export KEY=value",
            [("KEY", "value")]);

        // 11. Double-quoted escape \n produces newline
        // File contains: MSG="line1\nline2"  (where \n is the two-char escape sequence)
        yield return V("Double-quoted escape \\n produces newline",
            "MSG=\"line1\\nline2\"",
            [("MSG", "line1\nline2")]);

        // 12. Double-quoted escape \t produces tab
        // File contains: TAB="a\tb"
        yield return V("Double-quoted escape \\t produces tab",
            "TAB=\"a\\tb\"",
            [("TAB", "a\tb")]);

        // 13. Double-quoted escape \\ produces single backslash
        // File contains: PATH="C:\\Users"  (dotenv \\ → one backslash)
        yield return V("Double-quoted escape \\\\ produces single backslash",
            "PATH=\"C:\\\\Users\"",
            [("PATH", "C:\\Users")]);

        // 14. Double-quoted escape \" produces double-quote
        // File contains: QUOTE="say \"hi\""
        yield return V("Double-quoted escape \\\" produces double-quote",
            "QUOTE=\"say \\\"hi\\\"\"",
            [("QUOTE", "say \"hi\"")]);

        // 15. Double-quoted escape \$ produces dollar sign
        // File contains: DOLLAR="\$VAR"
        yield return V("Double-quoted escape \\$ produces dollar sign",
            "DOLLAR=\"\\$VAR\"",
            [("DOLLAR", "$VAR")]);

        // 16. Inline comment truncates unquoted value
        yield return V("Inline comment truncates unquoted value",
            "KEY=value # comment",
            [("KEY", "value")]);

        // 17. Hash without preceding whitespace is not inline comment
        yield return V("Hash without preceding whitespace is not inline comment",
            "KEY=value#notcomment",
            [("KEY", "value#notcomment")]);

        // 18. Multiline double-quoted value with embedded newline
        // File contains a real newline character inside the quoted value
        yield return V("Multiline double-quoted value with embedded newline",
            "MSG=\"line1\nline2\"",
            [("MSG", "line1\nline2")]);

        // 19. Line continuation in double-quoted value
        // File contains: MSG="hello \<LF>world"  — backslash-newline discards both chars
        yield return V("Line continuation in double-quoted value",
            "MSG=\"hello \\\nworld\"",
            [("MSG", "hello world")]);

        // 20. Single-quoted value treats backslash as literal
        // File contains: PATH='C:\Users'  — single-quoted values do not process escapes
        yield return V("Single-quoted value treats backslash as literal",
            "PATH='C:\\Users'",
            [("PATH", "C:\\Users")]);

        // 21. Multiple entries preserved in order
        yield return V("Multiple entries preserved in order",
            "HOST=localhost\nPORT=8080\nDEBUG=true",
            [("HOST", "localhost"), ("PORT", "8080"), ("DEBUG", "true")]);

        // 22. CRLF line endings
        yield return V("CRLF line endings",
            "HOST=localhost\r\nPORT=8080",
            [("HOST", "localhost"), ("PORT", "8080")]);

        // 23. Underscore-prefixed key
        yield return V("Underscore-prefixed key",
            "_KEY=value",
            [("_KEY", "value")]);

        // 24. Database URL value with special characters (unquoted)
        yield return V("Database URL value with special characters (unquoted)",
            "DATABASE_URL=postgres://user:pass@localhost/db",
            [("DATABASE_URL", "postgres://user:pass@localhost/db")]);

        // 25. Unknown escape sequence in double-quoted value passes through verbatim
        // File contains: MSG="\z test"  — \z is not a recognised escape, both chars appended as-is
        yield return V("Unknown escape sequence in double-quoted value passes through verbatim",
            "MSG=\"\\z test\"",
            [("MSG", "\\z test")]);
    }

    /// <summary>
    /// Constructs a single <c>[DynamicData]</c> row wrapping a <see cref="DotEnvKnownAnswerVector" />.
    /// </summary>
    /// <param name="description">A short human-readable label identifying the vector.</param>
    /// <param name="input">The raw dotenv source text to parse.</param>
    /// <param name="expectedEntries">The expected key/value pairs in source order.</param>
    /// <param name="options">
    /// The parse options to apply, or <see langword="null" /> to use <see cref="DotEnvParseOptions.Default" />.
    /// </param>
    /// <returns>An <c>object[]</c> row suitable for <c>[DynamicData]</c>.</returns>
    private static object[] V(
        string description,
        string input,
        (string Key, string Value)[] expectedEntries,
        DotEnvParseOptions? options = null) =>
        [new DotEnvKnownAnswerVector(description, input, expectedEntries, options, Spec)];
}
