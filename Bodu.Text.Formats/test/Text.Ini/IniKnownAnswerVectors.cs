// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IniKnownAnswerVectors.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Ini;

/// <summary>
/// Provides Known Answer Test vectors for the INI file format sourced from the Wikipedia INI file format article and
/// curated common-practice inputs. Each provider yields rows in the shape expected by MSTest's <c>[DynamicData]</c>
/// attribute.
/// </summary>
public static class IniKnownAnswerVectors
{
    /// <summary>
    /// Citation source for vectors derived from the Wikipedia INI file format article.
    /// </summary>
    private const string Spec = "INI file format (Wikipedia)";

    /// <summary>
    /// Returns the concatenation of all vector providers, used by exhaustive tests that exercise the full known-answer
    /// catalogue.
    /// </summary>
    /// <returns>A sequence suitable for <c>[DynamicData]</c>.</returns>
    public static IEnumerable<object[]> AllVectors()
    {
        foreach (var row in SpecVectors())
            yield return row;
    }

    /// <summary>
    /// Returns vectors derived from the Wikipedia INI file format article, covering the core parsing rules: key/value
    /// separators, sections, comments, blank lines, whitespace trimming, duplicate handling, and Unicode content.
    /// </summary>
    /// <returns>A sequence suitable for <c>[DynamicData]</c>.</returns>
    public static IEnumerable<object[]> SpecVectors()
    {
        // 1. Simple key=value in global section
        yield return Row(new IniKnownAnswerVector(
            "Simple key=value in global section",
            "key=value",
            GlobalEntries: [("key", "value")],
            Sections: [],
            Source: Spec));

        // 2. Key:value with colon separator
        yield return Row(new IniKnownAnswerVector(
            "Key:value with colon separator",
            "key:value",
            GlobalEntries: [("key", "value")],
            Sections: [],
            Source: Spec));

        // 3. Named section with entries
        yield return Row(new IniKnownAnswerVector(
            "Named section with entries",
            "[server]\nhost=localhost\nport=8080",
            GlobalEntries: [],
            Sections: [("server", [("host", "localhost"), ("port", "8080")])],
            Source: Spec));

        // 4. Global entries and named section
        yield return Row(new IniKnownAnswerVector(
            "Global entries and named section",
            "global=g\n[server]\nhost=localhost",
            GlobalEntries: [("global", "g")],
            Sections: [("server", [("host", "localhost")])],
            Source: Spec));

        // 5. Multiple named sections
        yield return Row(new IniKnownAnswerVector(
            "Multiple named sections",
            "[s1]\na=1\n[s2]\nb=2",
            GlobalEntries: [],
            Sections: [("s1", [("a", "1")]), ("s2", [("b", "2")])],
            Source: Spec));

        // 6. Semicolon comment line skipped
        yield return Row(new IniKnownAnswerVector(
            "Semicolon comment line skipped",
            "; comment\nkey=value",
            GlobalEntries: [("key", "value")],
            Sections: [],
            Source: Spec));

        // 7. Hash comment line skipped
        yield return Row(new IniKnownAnswerVector(
            "Hash comment line skipped",
            "# comment\nkey=value",
            GlobalEntries: [("key", "value")],
            Sections: [],
            Source: Spec));

        // 8. Blank lines skipped
        yield return Row(new IniKnownAnswerVector(
            "Blank lines skipped",
            "\nkey=value\n",
            GlobalEntries: [("key", "value")],
            Sections: [],
            Source: Spec));

        // 9. Whitespace trimmed from key and value
        yield return Row(new IniKnownAnswerVector(
            "Whitespace trimmed from key and value",
            "  key  =  value  ",
            GlobalEntries: [("key", "value")],
            Sections: [],
            Source: Spec));

        // 10. Empty value
        yield return Row(new IniKnownAnswerVector(
            "Empty value",
            "key=",
            GlobalEntries: [("key", "")],
            Sections: [],
            Source: Spec));

        // 11. Key-only line treated as key with empty value
        yield return Row(new IniKnownAnswerVector(
            "Key-only line treated as key with empty value",
            "flag",
            GlobalEntries: [("flag", "")],
            Sections: [],
            Source: Spec));

        // 12. Value containing equals sign
        yield return Row(new IniKnownAnswerVector(
            "Value containing equals sign",
            "url=http://x?a=b&c=d",
            GlobalEntries: [("url", "http://x?a=b&c=d")],
            Sections: [],
            Source: Spec));

        // 13. Value containing colon
        yield return Row(new IniKnownAnswerVector(
            "Value containing colon",
            "time=12:30:00",
            GlobalEntries: [("time", "12:30:00")],
            Sections: [],
            Source: Spec));

        // 14. Multiple keys in one section
        yield return Row(new IniKnownAnswerVector(
            "Multiple keys in one section",
            "[db]\nhost=localhost\nport=5432\nname=mydb",
            GlobalEntries: [],
            Sections: [("db", [("host", "localhost"), ("port", "5432"), ("name", "mydb")])],
            Source: Spec));

        // 15. CRLF line endings
        yield return Row(new IniKnownAnswerVector(
            "CRLF line endings",
            "a=1\r\nb=2",
            GlobalEntries: [("a", "1"), ("b", "2")],
            Sections: [],
            Source: Spec));

        // 16. Duplicate section merged (default)
        yield return Row(new IniKnownAnswerVector(
            "Duplicate section merged (default)",
            "[s]\na=1\n[s]\nb=2",
            GlobalEntries: [],
            Sections: [("s", [("a", "1"), ("b", "2")])],
            Source: Spec));

        // 17. Mixed comment styles
        yield return Row(new IniKnownAnswerVector(
            "Mixed comment styles",
            "; semi\n# hash\nkey=value",
            GlobalEntries: [("key", "value")],
            Sections: [],
            Source: Spec));

        // 18. Section name with spaces
        yield return Row(new IniKnownAnswerVector(
            "Section name with spaces",
            "[my section]\nkey=value",
            GlobalEntries: [],
            Sections: [("my section", [("key", "value")])],
            Source: Spec));

        // 19. Empty document (only comments and blanks)
        yield return Row(new IniKnownAnswerVector(
            "Empty document (only comments and blanks)",
            "\n# only comments\n; more comments\n\n",
            GlobalEntries: [],
            Sections: [],
            Source: Spec));

        // 20. Multiple global entries
        yield return Row(new IniKnownAnswerVector(
            "Multiple global entries",
            "a=1\nb=2\nc=3",
            GlobalEntries: [("a", "1"), ("b", "2"), ("c", "3")],
            Sections: [],
            Source: Spec));

        // 21. Global entries alongside multiple sections
        yield return Row(new IniKnownAnswerVector(
            "Global entries alongside multiple sections",
            "g=0\n[s1]\na=1\n[s2]\nb=2",
            GlobalEntries: [("g", "0")],
            Sections: [("s1", [("a", "1")]), ("s2", [("b", "2")])],
            Source: Spec));

        // 22. Unicode content in value
        yield return Row(new IniKnownAnswerVector(
            "Unicode content in value",
            "greeting=héllo",
            GlobalEntries: [("greeting", "héllo")],
            Sections: [],
            Source: Spec));

        // 23. Key with only colon separator, no whitespace
        yield return Row(new IniKnownAnswerVector(
            "Key with only colon separator, no whitespace",
            "key:value",
            GlobalEntries: [("key", "value")],
            Sections: [],
            Source: Spec));
    }

    /// <summary>
    /// Wraps a single <see cref="IniKnownAnswerVector" /> in the <c>object[]</c> envelope required by MSTest's
    /// <c>[DynamicData]</c> infrastructure.
    /// </summary>
    /// <param name="vector">The vector to wrap.</param>
    /// <returns>A single-element array containing <paramref name="vector" />.</returns>
    private static object[] Row(IniKnownAnswerVector vector) =>
        [vector];
}
