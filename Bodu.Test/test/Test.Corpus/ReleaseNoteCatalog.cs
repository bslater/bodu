// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ReleaseNoteCatalog.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;
using System.Text;

namespace Bodu.Test.Corpus;

/// <summary>
/// Reads and checks one release-note fix catalogue, <c>corpus/&lt;area&gt;/fixes/&lt;library&gt;-fixes.csv</c>: the
/// fixes another library's release notes list, each classified against Bodu and, where it runs, carrying a scenario.
/// </summary>
/// <remarks>
/// <para>
/// A catalogue is UTF-8 holding only printable ASCII and line feeds, and ends with a line feed. It opens with the
/// header lines <c># library: &lt;library&gt; -- &lt;repository&gt;</c>, <c># releases: ...</c> and
/// <c># licence: ...</c>, in that order, then any number of <c># note: ...</c> lines, then <see cref="ColumnLine" />,
/// then one row per line in RFC 4180 quoting.
/// </para>
/// <para>
/// <see cref="Load" /> never throws for a malformed catalogue: it reads what it can and lists every problem it finds in
/// <see cref="Problems" />, so a governance test can report them all at once. The checks cover the file's bytes, the
/// header, the columns, and each row's class, kind, case, options, escapes, expectation and reason.
/// </para>
/// </remarks>
public sealed class ReleaseNoteCatalog
{
    /// <summary>The column line every catalogue carries after its header.</summary>
    public const string ColumnLine = "library,version,reference,summary,class,case,kind,options,input,expected,expectation,reason";

    /// <summary>The suffix every catalogue's file name carries after its library name.</summary>
    public const string FileNameSuffix = "-fixes.csv";

    /// <summary>The number of columns in a row.</summary>
    private const int ColumnCount = 12;

    /// <summary>The prefix of the optional header lines that follow the required ones.</summary>
    private const string NoteHeader = "# note: ";

    /// <summary>The part of an embedded resource name that marks a catalogue copied into a test project.</summary>
    private const string ResourceMarker = ".Fixtures.ReleaseNotes.";

    /// <summary>The prefixes of the header lines every catalogue opens with, in order.</summary>
    private static readonly string[] s_requiredHeaders = ["# library: ", "# releases: ", "# licence: "];

    /// <summary>
    /// Initializes a new instance of the <see cref="ReleaseNoteCatalog" /> class.
    /// </summary>
    /// <param name="fileName">The catalogue's file name.</param>
    /// <param name="library">The library the file name names.</param>
    /// <param name="headerLines">The header lines, in order.</param>
    /// <param name="rows">The rows that could be read, in order.</param>
    /// <param name="problems">Every problem found.</param>
    private ReleaseNoteCatalog(
        string fileName,
        string library,
        IReadOnlyList<string> headerLines,
        IReadOnlyList<ReleaseNoteFix> rows,
        IReadOnlyList<string> problems)
    {
        FileName = fileName;
        Library = library;
        HeaderLines = headerLines;
        Rows = rows;
        Problems = problems;
    }

    /// <summary>
    /// Gets the classes a row may have.
    /// </summary>
    /// <value><c>applies</c>, <c>dialect</c>, <c>n/a</c> and <c>unknown</c>.</value>
    public static IReadOnlyList<string> Classes { get; } = ["applies", "dialect", "n/a", "unknown"];

    /// <summary>
    /// Gets the kinds an <c>applies</c> or <c>dialect</c> row may have.
    /// </summary>
    /// <value><c>parse</c>, <c>reject</c>, <c>write</c>, <c>write-reject</c>, <c>roundtrip</c> and <c>unit</c>.</value>
    public static IReadOnlyList<string> Kinds { get; } = ["parse", "reject", "write", "write-reject", "roundtrip", "unit"];

    /// <summary>
    /// Gets the catalogue's file name.
    /// </summary>
    /// <value>A name such as <c>tomli-fixes.csv</c>.</value>
    public string FileName { get; }

    /// <summary>
    /// Gets the library the catalogue covers, as its file name gives it.
    /// </summary>
    /// <value>The file name without <see cref="FileNameSuffix" />, or the empty string when the name lacks it.</value>
    public string Library { get; }

    /// <summary>
    /// Gets the header lines, in order.
    /// </summary>
    /// <value>The lines before <see cref="ColumnLine" />, each with its <c>#</c> prefix.</value>
    public IReadOnlyList<string> HeaderLines { get; }

    /// <summary>
    /// Gets the rows that could be read, in order.
    /// </summary>
    /// <value>Every row with the right number of columns, including rows that have other problems.</value>
    public IReadOnlyList<ReleaseNoteFix> Rows { get; }

    /// <summary>
    /// Gets every problem found in the catalogue.
    /// </summary>
    /// <value>One message per problem, each starting with the file name and, where it has one, the line number.</value>
    public IReadOnlyList<string> Problems { get; }

    /// <summary>
    /// Reads and checks a catalogue.
    /// </summary>
    /// <param name="fileName">The catalogue's file name, such as <c>tomli-fixes.csv</c>.</param>
    /// <param name="content">The catalogue's bytes.</param>
    /// <returns>The catalogue, with every problem found listed in <see cref="Problems" />.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="fileName" /> is <see langword="null" />.</exception>
    public static ReleaseNoteCatalog Load(string fileName, ReadOnlySpan<byte> content)
    {
        ArgumentNullException.ThrowIfNull(fileName);

        var problems = new List<string>();
        var headerLines = new List<string>();
        var rows = new List<ReleaseNoteFix>();

        string library = fileName.EndsWith(FileNameSuffix, StringComparison.Ordinal) ? fileName[..^FileNameSuffix.Length] : string.Empty;
        if (library.Length == 0)
            problems.Add($"{fileName}: the file name is not <library>{FileNameSuffix}");

        CheckBytes(fileName, content, problems);

        string[] lines = Encoding.ASCII.GetString(content).Split('\n');
        int count = lines.Length > 0 && lines[^1].Length == 0 ? lines.Length - 1 : lines.Length;
        int index = 0;

        foreach (string prefix in s_requiredHeaders)
        {
            if (index < count && lines[index].StartsWith(prefix, StringComparison.Ordinal) && lines[index].Length > prefix.Length)
            {
                headerLines.Add(lines[index]);
                index++;
            }
            else
            {
                problems.Add($"{fileName}:{index + 1}: expected a header line starting '{prefix.TrimEnd()}'");
            }
        }

        if (headerLines.Count > 0 && headerLines[0].StartsWith(s_requiredHeaders[0], StringComparison.Ordinal)
            && !headerLines[0].StartsWith($"{s_requiredHeaders[0]}{library} -- ", StringComparison.Ordinal))
        {
            problems.Add($"{fileName}:1: the library header does not read '{s_requiredHeaders[0]}{library} -- <repository>'");
        }

        while (index < count && lines[index].StartsWith(NoteHeader, StringComparison.Ordinal))
        {
            headerLines.Add(lines[index]);
            index++;
        }

        if (index >= count || !string.Equals(lines[index], ColumnLine, StringComparison.Ordinal))
        {
            problems.Add($"{fileName}:{index + 1}: expected the column line '{ColumnLine}'");
            return new ReleaseNoteCatalog(fileName, library, headerLines, rows, problems);
        }

        var names = new HashSet<string>(StringComparer.Ordinal);
        for (index++; index < count; index++)
        {
            int lineNumber = index + 1;
            if (!TrySplit(lines[index], out List<string> fields, out string? error))
            {
                problems.Add($"{fileName}:{lineNumber}: {error}");
                continue;
            }

            if (fields.Count != ColumnCount)
            {
                problems.Add($"{fileName}:{lineNumber}: the row has {fields.Count} columns, not {ColumnCount}");
                continue;
            }

            var row = new ReleaseNoteFix(
                fileName,
                lineNumber,
                fields[0],
                fields[1],
                fields[2],
                fields[3],
                fields[4],
                fields[5],
                fields[6],
                fields[7],
                fields[8],
                fields[9],
                fields[10],
                fields[11]);

            CheckRow(row, library, problems);
            if (!names.Add(row.Name))
                problems.Add($"{fileName}:{lineNumber}: the row's name repeats an earlier row's: '{row.Name}'");

            rows.Add(row);
        }

        return new ReleaseNoteCatalog(fileName, library, headerLines, rows, problems);
    }

    /// <summary>
    /// Reads and checks every catalogue embedded in an assembly under a <c>Fixtures/ReleaseNotes</c> folder.
    /// </summary>
    /// <param name="assembly">The test assembly that embeds the catalogues.</param>
    /// <returns>The catalogues, ordered by file name.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="assembly" /> is <see langword="null" />.</exception>
    /// <remarks>
    /// A catalogue is any manifest resource whose name contains <c>.Fixtures.ReleaseNotes.</c> and ends with
    /// <see cref="FileNameSuffix" />; its file name is the part after that marker.
    /// </remarks>
    public static IReadOnlyList<ReleaseNoteCatalog> LoadEmbedded(Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly);

        var catalogs = new List<ReleaseNoteCatalog>();
        foreach (string resource in assembly.GetManifestResourceNames())
        {
            int marker = resource.IndexOf(ResourceMarker, StringComparison.Ordinal);
            if (marker < 0 || !resource.EndsWith(FileNameSuffix, StringComparison.Ordinal))
                continue;

            using Stream stream = assembly.GetManifestResourceStream(resource)!;
            using var copy = new MemoryStream();
            stream.CopyTo(copy);
            catalogs.Add(Load(resource[(marker + ResourceMarker.Length)..], copy.ToArray()));
        }

        catalogs.Sort((left, right) => string.CompareOrdinal(left.FileName, right.FileName));
        return catalogs;
    }

    /// <summary>
    /// Counts the catalogue's rows in each class.
    /// </summary>
    /// <returns>
    /// The number of rows in each of <see cref="Classes" />, in that order; a class with no rows counts zero.
    /// </returns>
    public IReadOnlyDictionary<string, int> CountByClass()
    {
        var counts = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (string rowClass in Classes)
            counts[rowClass] = Rows.Count(row => string.Equals(row.Class, rowClass, StringComparison.Ordinal));

        return counts;
    }

    /// <summary>
    /// Checks that a catalogue's bytes are printable ASCII and line feeds, and end with a line feed.
    /// </summary>
    /// <param name="fileName">The catalogue's file name, for messages.</param>
    /// <param name="content">The catalogue's bytes.</param>
    /// <param name="problems">Receives one message per problem found.</param>
    private static void CheckBytes(string fileName, ReadOnlySpan<byte> content, List<string> problems)
    {
        if (content.IsEmpty || content[^1] != (byte)'\n')
            problems.Add($"{fileName}: the file does not end with a line feed");

        int line = 1;
        for (int i = 0; i < content.Length; i++)
        {
            byte b = content[i];
            if (b == (byte)'\n')
            {
                line++;
                continue;
            }

            if (b is < (byte)' ' or > (byte)'~')
            {
                problems.Add($"{fileName}:{line}: the byte 0x{b:X2} at offset {i} is not printable ASCII");
                return;
            }
        }
    }

    /// <summary>
    /// Checks one row's library, version, summary, reason, class, case, kind, options, escapes and expectation.
    /// </summary>
    /// <param name="row">The row to check.</param>
    /// <param name="library">The library the catalogue's file name names.</param>
    /// <param name="problems">Receives one message per problem found.</param>
    private static void CheckRow(ReleaseNoteFix row, string library, List<string> problems)
    {
        string at = $"{row.FileName}:{row.LineNumber}";

        if (!string.Equals(row.Library, library, StringComparison.Ordinal))
            problems.Add($"{at}: the library '{row.Library}' is not the one the file name gives");

        if (row.Version.Length == 0)
            problems.Add($"{at}: the version is empty");

        if (row.Summary.Length == 0)
            problems.Add($"{at}: the summary is empty");

        if (row.Reason.Length == 0)
            problems.Add($"{at}: the reason is empty");

        if (row.Case.Length != 0 && (!int.TryParse(row.Case, NumberStyles.None, CultureInfo.InvariantCulture, out int caseNumber) || caseNumber < 1))
            problems.Add($"{at}: the case '{row.Case}' is not a positive whole number");

        if (!Classes.Contains(row.Class, StringComparer.Ordinal))
        {
            problems.Add($"{at}: the class '{row.Class}' is not one of {string.Join(", ", Classes)}");
            return;
        }

        if (row.Class is "n/a" or "unknown")
        {
            if (row.Kind.Length != 0 || row.Options.Length != 0 || row.Input.Length != 0 || row.Expected.Length != 0 || row.Expectation.Length != 0)
                problems.Add($"{at}: a {row.Class} row carries a kind, options, input, expected value or expectation");

            return;
        }

        if (!Kinds.Contains(row.Kind, StringComparer.Ordinal))
        {
            problems.Add($"{at}: the kind '{row.Kind}' is not one of {string.Join(", ", Kinds)}");
            return;
        }

        if (!IsExpectation(row.Expectation))
            problems.Add($"{at}: the expectation '{row.Expectation}' is not upstream, spec, derived or oracle:<tool>");

        if (!ReleaseNoteOptions.TryParse(row.Options, out _, out string? optionsError))
            problems.Add($"{at}: options: {optionsError}");

        if (!CorpusEscapes.TryDecode(row.Input, out _, out string? inputError))
            problems.Add($"{at}: input: {inputError}");

        if (!CorpusEscapes.TryDecode(row.Expected, out _, out string? expectedError))
            problems.Add($"{at}: expected: {expectedError}");

        switch (row.Kind)
        {
            case "unit":
                if (row.Expected.Length != 0 || !IsTestMethodName(row.Input))
                    problems.Add($"{at}: a unit row names <TestClass>.<Member>_When<Condition>_Should<Result> and has no expected value");

                break;

            case "reject":
            case "write-reject":
                if (!IsExceptionName(row.Expected))
                    problems.Add($"{at}: a {row.Kind} row expects an exception type name, optionally followed by ':<code>'");

                break;

            case "write":
                if (row.Expected.Length == 0)
                    problems.Add($"{at}: a write row expects the written text");

                break;
        }
    }

    /// <summary>
    /// Determines whether an expectation names a known source.
    /// </summary>
    /// <param name="value">The expectation field.</param>
    /// <returns>
    /// <see langword="true" /> for <c>upstream</c>, <c>spec</c>, <c>derived</c> and <c>oracle:&lt;tool&gt;</c>.
    /// </returns>
    private static bool IsExpectation(string value) =>
        value is "upstream" or "spec" or "derived"
        || (value.StartsWith("oracle:", StringComparison.Ordinal) && value.Length > "oracle:".Length);

    /// <summary>
    /// Determines whether an expected field names an exception type, optionally followed by a code.
    /// </summary>
    /// <param name="value">The expected field.</param>
    /// <returns>
    /// <see langword="true" /> for <c>&lt;Name&gt;Exception</c> or <c>&lt;Name&gt;Exception:&lt;code&gt;</c>.
    /// </returns>
    private static bool IsExceptionName(string value)
    {
        int colon = value.IndexOf(':', StringComparison.Ordinal);
        string typeName = colon < 0 ? value : value[..colon];
        return typeName.Length > "Exception".Length
            && typeName.EndsWith("Exception", StringComparison.Ordinal)
            && typeName.All(char.IsAsciiLetterOrDigit)
            && (colon < 0 || colon < value.Length - 1);
    }

    /// <summary>
    /// Determines whether a unit row's input names a test method in the repository's convention.
    /// </summary>
    /// <param name="value">The input field.</param>
    /// <returns>
    /// <see langword="true" /> for <c>&lt;TestClass&gt;.&lt;Member&gt;_When&lt;Condition&gt;_Should&lt;Result&gt;</c>.
    /// </returns>
    private static bool IsTestMethodName(string value)
    {
        int dot = value.IndexOf('.', StringComparison.Ordinal);
        if (dot <= 0 || dot == value.Length - 1 || value.IndexOf('.', dot + 1) >= 0)
            return false;

        string method = value[(dot + 1)..];
        int when = method.IndexOf("_When", StringComparison.Ordinal);
        int should = method.IndexOf("_Should", StringComparison.Ordinal);
        return when > 0 && should > when && value.All(c => char.IsAsciiLetterOrDigit(c) || c is '_' or '.');
    }

    /// <summary>
    /// Splits one catalogue line into fields under RFC 4180 quoting.
    /// </summary>
    /// <param name="line">The line, without its line feed.</param>
    /// <param name="fields">Receives the unquoted fields.</param>
    /// <param name="error">When the method returns <see langword="false" />, a description of the problem.</param>
    /// <returns><see langword="true" /> when the line is well formed.</returns>
    private static bool TrySplit(string line, out List<string> fields, [NotNullWhen(false)] out string? error)
    {
        fields = [];
        error = null;

        var field = new StringBuilder();
        int i = 0;
        while (true)
        {
            field.Clear();
            if (i < line.Length && line[i] == '"')
            {
                i++;
                while (true)
                {
                    if (i >= line.Length)
                    {
                        error = "a quoted field is not closed";
                        return false;
                    }

                    if (line[i] == '"')
                    {
                        if (i + 1 < line.Length && line[i + 1] == '"')
                        {
                            field.Append('"');
                            i += 2;
                            continue;
                        }

                        i++;
                        break;
                    }

                    field.Append(line[i]);
                    i++;
                }

                if (i < line.Length && line[i] != ',')
                {
                    error = "text follows a closing quote";
                    return false;
                }
            }
            else
            {
                while (i < line.Length && line[i] != ',')
                {
                    if (line[i] == '"')
                    {
                        error = "a double quote appears inside an unquoted field";
                        return false;
                    }

                    field.Append(line[i]);
                    i++;
                }
            }

            fields.Add(field.ToString());
            if (i >= line.Length)
                return true;

            i++;
        }
    }
}
