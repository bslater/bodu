// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CompoundReleaseNoteCorpusTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers.Binary;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

using Bodu.IO.Compound.Builders;
using Bodu.Test.Corpus;

namespace Bodu.IO.Compound;

/// <summary>
/// Holds Bodu.IO.Compound to the defect fixes other compound-file libraries list in their release notes, as cataloged
/// in <c>corpus/compound/fixes/</c> and embedded under <c>Fixtures/ReleaseNotes/</c>.
/// </summary>
/// <remarks>
/// <para>
/// A read is rendered as a listing: one line per entry, depth first in the order the API enumerates, separated by line
/// feeds. A storage renders as <c>&lt;path&gt;\tstorage</c> and a stream as
/// <c>&lt;path&gt;\tstream\t&lt;length&gt;\t&lt;hash&gt;</c>, where the root's path is <c>/</c>, the length is
/// <see cref="CompoundEntryInfo.Length" /> and the hash is the first 16 lowercase hexadecimal digits of the SHA-256 of
/// the bytes <see cref="CompoundStream.ReadAllBytes" /> returns. Every stream is read, so a failure that surfaces only
/// when a stream is read counts as a failure to read.
/// </para>
/// <para>
/// Every input is generated: a <c>gen:</c> pipeline builds a container with <see cref="CompoundStorageBuilder" /> and
/// then patches its bytes, and an edit script after <c> =&gt; </c> runs on a writable <see cref="CompoundFile" />. The
/// notation is described in <c>corpus/compound/README.md</c>; the patches resolve every name against the container as
/// built, through <see cref="Layout" />, so a row never depends on what an earlier patch changed.
/// </para>
/// </remarks>
[TestClass]
public sealed partial class CompoundReleaseNoteCorpusTests
{
    /// <summary>The corpus area whose catalogs this class runs.</summary>
    private const string Area = "compound";

    /// <summary>The separator between a container and its edit script.</summary>
    private const string EditSeparator = " => ";

    /// <summary>The prefix of a <c>gen:</c> pipeline input.</summary>
    private const string GeneratorPrefix = "gen:";

    /// <summary>The prefix of a literal container input, whose bytes follow in the catalog escapes.</summary>
    private const string BytesPrefix = "bytes:";

    /// <summary>The end-of-chain sentinel of an allocation table.</summary>
    private const uint EndOfChain = 0xFFFFFFFE;

    /// <summary>The free-sector sentinel of an allocation table.</summary>
    private const uint FreeSector = 0xFFFFFFFF;

    /// <summary>The marker of a sector that holds the FAT.</summary>
    private const uint FatSector = 0xFFFFFFFD;

    /// <summary>The marker of a sector that holds the DIFAT.</summary>
    private const uint DifatSector = 0xFFFFFFFC;

    /// <summary>The number of FAT sector numbers the header holds.</summary>
    private const int HeaderDifatSlots = 109;

    /// <summary>The option names a catalog row may use, each mapped by <see cref="RowOptions.Parse" />.</summary>
    private static readonly string[] s_optionNames = ["ValidationLevel", "ReadStrategy"];

    /// <summary>The bases an edit script may start from instead of a container.</summary>
    private static readonly string[] s_newContainers = ["new:V3", "new:V4"];

    /// <summary>The header fields a <c>Header</c> step may name, with their offsets and widths.</summary>
    private static readonly Dictionary<string, (int Offset, int Width)> s_headerFields = CreateHeaderFields();

    /// <summary>The directory-entry fields a <c>Dir</c> step may name, with their offsets and widths.</summary>
    private static readonly Dictionary<string, (int Offset, int Width)> s_entryFields = new(StringComparer.Ordinal)
    {
        ["NameLength"] = (64, 2),
        ["Type"] = (66, 1),
        ["Color"] = (67, 1),
        ["Left"] = (68, 4),
        ["Right"] = (72, 4),
        ["Child"] = (76, 4),
        ["StateBits"] = (96, 4),
        ["Created"] = (100, 8),
        ["Modified"] = (108, 8),
        ["Start"] = (116, 4),
        ["Size"] = (120, 8),
        ["SizeLow"] = (120, 4),
        ["SizeHigh"] = (124, 4),
    };

    /// <summary>The symbolic values an expression may name.</summary>
    private static readonly Dictionary<string, long> s_symbols = new(StringComparer.Ordinal)
    {
        ["ENDOFCHAIN"] = EndOfChain,
        ["FREESECT"] = FreeSector,
        ["FATSECT"] = FatSector,
        ["DIFSECT"] = DifatSector,
        ["MAXREGSECT"] = 0xFFFFFFFA,
        ["NOSTREAM"] = 0xFFFFFFFF,
    };

    /// <summary>The builder steps of a <c>gen:</c> pipeline, with their argument counts.</summary>
    private static readonly Dictionary<string, int> s_builderSteps = new(StringComparer.Ordinal)
    {
        ["Storage"] = 1,
        ["Stream"] = 2,
    };

    /// <summary>The patch steps of a <c>gen:</c> pipeline, with their argument counts.</summary>
    private static readonly Dictionary<string, int> s_patchSteps = new(StringComparer.Ordinal)
    {
        ["Header"] = 2,
        ["Set"] = 3,
        ["Hex"] = 2,
        ["Dir"] = 3,
        ["Name"] = 2,
        ["Fat"] = 2,
        ["MiniFat"] = 2,
        ["Truncate"] = 1,
        ["Append"] = 2,
        ["GrowFat"] = 1,
        ["MoveLast"] = 1,
    };

    /// <summary>The steps of an edit script, with their argument counts.</summary>
    private static readonly Dictionary<string, int> s_editSteps = new(StringComparer.Ordinal)
    {
        ["CreateStorage"] = 1,
        ["CreateStream"] = 2,
        ["Write"] = 2,
        ["Append"] = 2,
        ["Overwrite"] = 3,
        ["SetLength"] = 2,
        ["Delete"] = 1,
        ["Rename"] = 2,
        ["Commit"] = 0,
        ["Revert"] = 0,
        ["Reopen"] = 0,
    };

    /// <summary>The catalogs embedded in this test assembly, loaded once.</summary>
    private static readonly Lazy<IReadOnlyList<ReleaseNoteCatalog>> s_catalogs =
        new(() => ReleaseNoteCatalog.LoadEmbedded(typeof(CompoundReleaseNoteCorpusTests).Assembly));

    /// <summary>
    /// Gets the runnable rows of kind <c>parse</c>.
    /// </summary>
    /// <value>One single-element argument array per row.</value>
    public static IEnumerable<object[]> ParseRows =>
        RunnableRows("parse");

    /// <summary>
    /// Gets the runnable rows of kind <c>reject</c>.
    /// </summary>
    /// <value>One single-element argument array per row.</value>
    public static IEnumerable<object[]> RejectRows =>
        RunnableRows("reject");

    /// <summary>
    /// Gets the runnable rows of kind <c>write-reject</c>.
    /// </summary>
    /// <value>One single-element argument array per row.</value>
    public static IEnumerable<object[]> WriteRejectRows =>
        RunnableRows("write-reject");

    /// <summary>
    /// Gets the runnable rows of kind <c>roundtrip</c>.
    /// </summary>
    /// <value>One single-element argument array per row.</value>
    public static IEnumerable<object[]> RoundTripRows =>
        RunnableRows("roundtrip");

    /// <summary>
    /// Gets every row of every embedded catalog.
    /// </summary>
    /// <value>The rows, catalog by catalog in file order.</value>
    private static IEnumerable<ReleaseNoteFix> AllRows =>
        s_catalogs.Value.SelectMany(catalog => catalog.Rows);

    /// <summary>
    /// Selects the runnable rows of one kind as test data.
    /// </summary>
    /// <param name="kind">The row kind.</param>
    /// <returns>
    /// One single-element argument array per <c>applies</c> or <c>dialect</c> row of <paramref name="kind" />.
    /// </returns>
    private static IEnumerable<object[]> RunnableRows(string kind) =>
        AllRows.Where(fix => fix.IsRunnable && fix.Kind == kind).Select(fix => new object[] { fix });

    /// <summary>
    /// Opens a container read-only and renders its listing, reading every stream.
    /// </summary>
    /// <param name="container">The container bytes.</param>
    /// <param name="options">The row options.</param>
    /// <returns>The listing.</returns>
    /// <exception cref="CompoundFileFormatException">The container, or one of its streams, cannot be read.</exception>
    private static string OpenAndRender(byte[] container, RowOptions options)
    {
        using CompoundFile file = CompoundFile.Open(new MemoryStream(container, writable: false), options.ToFileOptions());
        var lines = new List<string>();
        RenderStorage(file.RootStorage, "/", lines);
        return string.Join('\n', lines);
    }

    /// <summary>
    /// Renders one storage and, depth first, its children.
    /// </summary>
    /// <param name="storage">The storage to render.</param>
    /// <param name="path">The storage's path.</param>
    /// <param name="lines">Receives the rendered lines.</param>
    /// <remarks>
    /// The children are listed in the order <see cref="CompoundStorage.EnumerateEntries" /> yields them, and a child
    /// storage is rendered through the matching element of <see cref="CompoundStorage.EnumerateStorages" />, so the
    /// listing also checks that the two enumerations agree.
    /// </remarks>
    private static void RenderStorage(CompoundStorage storage, string path, List<string> lines)
    {
        lines.Add(path + "\tstorage");
        using IEnumerator<CompoundStorage> storages = storage.EnumerateStorages().GetEnumerator();
        foreach (CompoundEntryInfo entry in storage.EnumerateEntries())
        {
            string childPath = (path == "/" ? "/" : path + "/") + entry.Name;
            if (entry.EntryType is CompoundEntryType.Storage or CompoundEntryType.RootStorage)
            {
                Assert.IsTrue(storages.MoveNext(), $"EnumerateStorages yielded fewer storages than EnumerateEntries listed, at {childPath}.");
                RenderStorage(storages.Current, childPath, lines);
                continue;
            }

            using CompoundStream stream = storage.OpenStream(entry.Name);
            byte[] content = stream.ReadAllBytes();
            string hash = Convert.ToHexString(SHA256.HashData(content), 0, 8).ToLowerInvariant();
            lines.Add($"{childPath}\tstream\t{entry.Length.ToString(CultureInfo.InvariantCulture)}\t{hash}");
        }

        Assert.IsFalse(storages.MoveNext(), $"EnumerateStorages yielded more storages than EnumerateEntries listed, under {path}.");
    }

    /// <summary>
    /// Produces the container an input field names.
    /// </summary>
    /// <param name="field">The escaped input field: a <c>gen:</c> pipeline or a <c>bytes:</c> literal.</param>
    /// <returns>The container bytes.</returns>
    /// <exception cref="FormatException">The field is not well formed.</exception>
    private static byte[] ReadContainer(string field) =>
        field.StartsWith(BytesPrefix, StringComparison.Ordinal)
            ? CorpusEscapes.Decode(field[BytesPrefix.Length..])
            : Build(CorpusEscapes.DecodeText(field));

    /// <summary>
    /// Builds the container a decoded <c>gen:</c> pipeline describes.
    /// </summary>
    /// <param name="input">The decoded pipeline, starting with <c>gen:</c>.</param>
    /// <returns>The container bytes.</returns>
    /// <exception cref="FormatException">
    /// The pipeline is not well formed, or names something the container lacks.
    /// </exception>
    private static byte[] Build(string input)
    {
        if (!input.StartsWith(GeneratorPrefix, StringComparison.Ordinal))
            throw new FormatException($"The input '{input}' is neither a gen: pipeline nor a bytes: literal.");

        List<Step> steps = ParseSteps(input[GeneratorPrefix.Length..]);
        if (steps.Count == 0 || steps[0].Name is not ("V3" or "V4") || steps[0].Args.Count != 0)
            throw new FormatException("A gen: pipeline starts with V3 or V4.");

        CompoundFileVersion version = steps[0].Name == "V4" ? CompoundFileVersion.V4 : CompoundFileVersion.V3;
        var root = CompoundStorageBuilder.CreateRoot();
        int index = 1;
        for (; index < steps.Count && s_builderSteps.ContainsKey(steps[index].Name); index++)
        {
            Step step = steps[index];
            string[] segments = SplitPath(step.Args[0]);
            if (segments.Length == 0)
                throw new FormatException($"The step '{step}' names the root, which already exists.");

            CompoundStorageBuilder parent = root;
            for (int i = 0; i < segments.Length - 1; i++)
            {
                if (!parent.TryGetStorage(segments[i], out CompoundStorageBuilder? child))
                    throw new FormatException($"The step '{step}' names no storage '{segments[i]}'.");

                parent = child;
            }

            if (step.Name == "Storage")
                _ = parent.AddStorage(segments[^1]);
            else
                _ = parent.AddStream(segments[^1], Content(step.Args[1]));
        }

        byte[] built = root.ToArray(new CompoundBuildOptions { Version = version });
        Layout layout = Layout.Parse(built);
        var output = new List<byte>(built);
        for (; index < steps.Count; index++)
            ApplyPatch(output, layout, steps[index]);

        return [.. output];
    }

    /// <summary>
    /// Checks that a decoded input is well formed, without building it.
    /// </summary>
    /// <param name="input">The decoded input.</param>
    /// <param name="allowEdits">Whether the row's kind runs an edit script.</param>
    /// <exception cref="FormatException">The input is not well formed for its kind.</exception>
    /// <remarks>
    /// The check covers the notation only: step names, argument counts, the order of builder and patch steps, and the
    /// edit names. Whether a pipeline's paths and sectors exist is known only once the container is built.
    /// </remarks>
    private static void ValidateInput(string input, bool allowEdits)
    {
        string container = input;
        if (HasEdits(input))
        {
            if (!allowEdits)
                throw new FormatException("Only roundtrip and write-reject rows carry an edit script.");

            (container, IReadOnlyList<Step> edits) = SplitEdits(input);
            foreach (Step edit in edits)
            {
                if (!s_editSteps.TryGetValue(edit.Name, out int arity) || edit.Args.Count != arity)
                    throw new FormatException($"'{edit}' is not an edit with the arguments it takes.");
            }

            if (s_newContainers.Contains(container, StringComparer.Ordinal))
                return;
        }

        if (!container.StartsWith(GeneratorPrefix, StringComparison.Ordinal))
            throw new FormatException("The input is neither a gen: pipeline nor, before an edit script, new:V3 or new:V4.");

        List<Step> steps = ParseSteps(container[GeneratorPrefix.Length..]);
        if (steps.Count == 0 || steps[0].Name is not ("V3" or "V4") || steps[0].Args.Count != 0)
            throw new FormatException("A gen: pipeline starts with V3 or V4.");

        bool patching = false;
        foreach (Step step in steps.Skip(1))
        {
            if (!patching && s_builderSteps.TryGetValue(step.Name, out int builderArity))
            {
                if (step.Args.Count != builderArity)
                    throw new FormatException($"'{step}' takes {builderArity} arguments.");

                continue;
            }

            if (!s_patchSteps.TryGetValue(step.Name, out int patchArity))
                throw new FormatException($"'{step}' is not a builder or patch step, or a builder step follows a patch.");

            if (step.Args.Count != patchArity)
                throw new FormatException($"'{step}' takes {patchArity} arguments.");

            patching = true;
        }
    }

    /// <summary>
    /// Determines whether a decoded input carries an edit script.
    /// </summary>
    /// <param name="input">The decoded input.</param>
    /// <returns><see langword="true" /> when the input contains <c> =&gt; </c>.</returns>
    private static bool HasEdits(string input) =>
        input.Contains(EditSeparator, StringComparison.Ordinal);

    /// <summary>
    /// Splits a decoded input into its base and its edit script.
    /// </summary>
    /// <param name="input">The decoded input.</param>
    /// <returns>The base, and the edits in order; none when the input has no edit script.</returns>
    /// <exception cref="FormatException">An edit is not well formed.</exception>
    private static (string Container, IReadOnlyList<Step> Edits) SplitEdits(string input)
    {
        int at = input.IndexOf(EditSeparator, StringComparison.Ordinal);
        return at < 0 ? (input, []) : (input[..at], ParseSteps(input[(at + EditSeparator.Length)..]));
    }

    /// <summary>
    /// Runs an edit script on a writable compound file and commits it.
    /// </summary>
    /// <param name="input">
    /// The decoded input: <c>new:V3</c>, <c>new:V4</c> or a <c>gen:</c> pipeline, then the edits.
    /// </param>
    /// <returns>The committed bytes.</returns>
    /// <remarks>
    /// <c>new:V3</c> and <c>new:V4</c> start from
    /// <see cref="CompoundFile.Create(Stream, bool, CompoundBuildOptions)" /> at that version; a pipeline's container
    /// is opened with <see cref="FileMode.Open" /> and <see cref="FileAccess.ReadWrite" />. The script ends with an
    /// implicit <see cref="CompoundFile.Commit" />, and <c>Reopen</c> commits, disposes, and opens the committed bytes
    /// for update again.
    /// </remarks>
    private static byte[] RunEdits(string input)
    {
        (string baseInput, IReadOnlyList<Step> edits) = SplitEdits(input);
        var destination = new MemoryStream();
        CompoundFile file;
        if (s_newContainers.Contains(baseInput, StringComparer.Ordinal))
        {
            CompoundFileVersion version = baseInput == "new:V4" ? CompoundFileVersion.V4 : CompoundFileVersion.V3;
            file = CompoundFile.Create(destination, leaveOpen: true, new CompoundBuildOptions { Version = version });
        }
        else
        {
            destination.Write(Build(baseInput));
            destination.Position = 0;
            file = CompoundFile.Open(destination, FileMode.Open, FileAccess.ReadWrite, leaveOpen: true);
        }

        try
        {
            foreach (Step edit in edits)
            {
                if (edit.Name == "Reopen")
                {
                    file.Commit();
                    file.Dispose();
                    destination.Position = 0;
                    file = CompoundFile.Open(destination, FileMode.Open, FileAccess.ReadWrite, leaveOpen: true);
                    continue;
                }

                ApplyEdit(file, edit);
            }

            file.Commit();
        }
        finally
        {
            file.Dispose();
        }

        return destination.ToArray();
    }

    /// <summary>
    /// Applies one edit to a writable compound file.
    /// </summary>
    /// <param name="file">The writable file.</param>
    /// <param name="edit">The edit.</param>
    /// <exception cref="FormatException">The edit is not one this notation defines.</exception>
    private static void ApplyEdit(CompoundFile file, Step edit)
    {
        switch (edit.Name)
        {
            case "Commit":
                file.Commit();
                return;

            case "Revert":
                file.Revert();
                return;
        }

        (CompoundStorage parent, string name) = ResolveParent(file, edit.Args[0]);
        switch (edit.Name)
        {
            case "CreateStorage":
                _ = parent.CreateStorage(name);
                break;

            case "CreateStream":
                parent.CreateStream(name, Content(edit.Args[1]));
                break;

            case "Write":
                using (CompoundStream stream = parent.OpenStream(name, FileMode.Create, FileAccess.Write))
                    stream.Write(Content(edit.Args[1]));

                break;

            case "Append":
                using (CompoundStream stream = parent.OpenStream(name, FileMode.Append, FileAccess.Write))
                    stream.Write(Content(edit.Args[1]));

                break;

            case "Overwrite":
                using (CompoundStream stream = parent.OpenStream(name, FileMode.Open, FileAccess.ReadWrite))
                {
                    stream.Position = Number(edit.Args[1]);
                    stream.Write(Content(edit.Args[2]));
                }

                break;

            case "SetLength":
                using (CompoundStream stream = parent.OpenStream(name, FileMode.Open, FileAccess.ReadWrite))
                    stream.SetLength(Number(edit.Args[1]));

                break;

            case "Delete":
                _ = parent.Delete(name);
                break;

            case "Rename":
                parent.Rename(name, edit.Args[1]);
                break;

            default:
                throw new FormatException($"'{edit}' is not an edit.");
        }
    }

    /// <summary>
    /// Resolves the storage that holds a path's last segment, opening each parent storage by name from the root.
    /// </summary>
    /// <param name="file">The file.</param>
    /// <param name="path">The path, such as <c>/S/A</c>.</param>
    /// <returns>The parent storage and the last segment.</returns>
    /// <exception cref="FormatException">The path names the root, or does not start with <c>/</c>.</exception>
    private static (CompoundStorage Parent, string Name) ResolveParent(CompoundFile file, string path)
    {
        string[] segments = SplitPath(path);
        if (segments.Length == 0)
            throw new FormatException("An edit cannot name the root.");

        CompoundStorage storage = file.RootStorage;
        for (int i = 0; i < segments.Length - 1; i++)
            storage = storage.OpenStorage(segments[i]);

        return (storage, segments[^1]);
    }

    /// <summary>
    /// Produces the bytes a content specification names.
    /// </summary>
    /// <param name="specification">
    /// <c>&lt;n&gt;</c> for the <c>n</c> bytes of the catalog pattern, whose byte <c>i</c> is
    /// <c>(i * 31 + 7) mod 256</c>, or <c>&lt;n&gt;*&lt;b&gt;</c> for <c>n</c> copies of the byte <c>b</c>.
    /// </param>
    /// <returns>The content.</returns>
    /// <exception cref="FormatException">The specification is not well formed.</exception>
    private static byte[] Content(string specification)
    {
        int star = specification.IndexOf('*', StringComparison.Ordinal);
        long length = Number(star < 0 ? specification : specification[..star]);
        if (length < 0)
            throw new FormatException($"The content '{specification}' has a negative length.");

        byte[] content = new byte[length];
        if (star >= 0)
        {
            long value = Number(specification[(star + 1)..]);
            if (value is < 0 or > 255)
                throw new FormatException($"The content '{specification}' repeats a value that is not a byte.");

            content.AsSpan().Fill((byte)value);
            return content;
        }

        for (long i = 0; i < length; i++)
            content[i] = unchecked((byte)((i * 31) + 7));

        return content;
    }

    /// <summary>
    /// Parses a decimal or <c>0x</c> hexadecimal integer, optionally negative.
    /// </summary>
    /// <param name="text">The text.</param>
    /// <returns>The value.</returns>
    /// <exception cref="FormatException">The text is not such an integer.</exception>
    private static long Number(string text)
    {
        bool negative = text.StartsWith('-');
        string digits = negative ? text[1..] : text;
        bool parsed = digits.StartsWith("0x", StringComparison.Ordinal)
            ? long.TryParse(digits.AsSpan(2), NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out long value)
            : long.TryParse(digits, NumberStyles.None, CultureInfo.InvariantCulture, out value);
        if (!parsed)
            throw new FormatException($"'{text}' is not a decimal or 0x hexadecimal integer.");

        return negative ? -value : value;
    }

    /// <summary>
    /// Splits a path such as <c>/S/A</c> into its segments.
    /// </summary>
    /// <param name="path">The path; the root is <c>/</c>.</param>
    /// <returns>The segments; none for the root.</returns>
    /// <exception cref="FormatException">The path does not start with <c>/</c>.</exception>
    private static string[] SplitPath(string path)
    {
        if (!path.StartsWith('/'))
            throw new FormatException($"The path '{path}' does not start with /.");

        return path == "/" ? [] : path[1..].Split('/');
    }

    /// <summary>
    /// Parses a pipeline or an edit script into steps.
    /// </summary>
    /// <param name="text">The steps, separated by <c>|</c>.</param>
    /// <returns>The steps, in order.</returns>
    /// <exception cref="FormatException">A step's arguments are not closed by <c>)</c>.</exception>
    private static List<Step> ParseSteps(string text)
    {
        var steps = new List<Step>();
        foreach (string part in SplitTopLevel(text, '|'))
        {
            string trimmed = part.Trim();
            int open = trimmed.IndexOf('(', StringComparison.Ordinal);
            if (open < 0)
            {
                steps.Add(new Step(trimmed, []));
                continue;
            }

            if (!trimmed.EndsWith(')'))
                throw new FormatException($"The step '{trimmed}' does not end with ).");

            string inner = trimmed[(open + 1)..^1];
            steps.Add(new Step(trimmed[..open], inner.Length == 0 ? [] : SplitTopLevel(inner, ',')));
        }

        return steps;
    }

    /// <summary>
    /// Splits text on a separator that lies outside parentheses.
    /// </summary>
    /// <param name="text">The text.</param>
    /// <param name="separator">The separator.</param>
    /// <returns>The parts, in order.</returns>
    private static List<string> SplitTopLevel(string text, char separator)
    {
        var parts = new List<string>();
        int depth = 0;
        int start = 0;
        for (int i = 0; i < text.Length; i++)
        {
            if (text[i] == '(')
            {
                depth++;
            }
            else if (text[i] == ')')
            {
                depth--;
            }
            else if (text[i] == separator && depth == 0)
            {
                parts.Add(text[start..i]);
                start = i + 1;
            }
        }

        parts.Add(text[start..]);
        return parts;
    }

    /// <summary>
    /// Applies one patch step to the container.
    /// </summary>
    /// <param name="output">The container bytes, patched in place.</param>
    /// <param name="layout">The container as built.</param>
    /// <param name="step">The patch.</param>
    /// <exception cref="FormatException">
    /// The patch is not well formed, or names something the container lacks.
    /// </exception>
    private static void ApplyPatch(List<byte> output, Layout layout, Step step)
    {
        switch (step.Name)
        {
            case "Header":
                if (!s_headerFields.TryGetValue(step.Args[0], out (int Offset, int Width) header))
                    throw new FormatException($"The step '{step}' names no header field.");

                WriteInteger(output, header.Offset, header.Width, Evaluate(step.Args[1], layout));
                break;

            case "Set":
                WriteInteger(output, Evaluate(step.Args[0], layout), (int)Number(step.Args[1]), Evaluate(step.Args[2], layout));
                break;

            case "Hex":
                long hexOffset = Evaluate(step.Args[0], layout);
                byte[] bytes = Convert.FromHexString(step.Args[1]);
                if (hexOffset < 0 || hexOffset + bytes.Length > output.Count)
                    throw new FormatException($"The step '{step}' writes outside the container.");

                for (int i = 0; i < bytes.Length; i++)
                    output[(int)hexOffset + i] = bytes[i];

                break;

            case "Dir":
                if (!s_entryFields.TryGetValue(step.Args[1], out (int Offset, int Width) field))
                    throw new FormatException($"The step '{step}' names no directory-entry field.");

                WriteInteger(output, layout.EntryOffset(layout.Resolve(step.Args[0])) + field.Offset, field.Width, Evaluate(step.Args[2], layout));
                break;

            case "Name":
                long entry = layout.EntryOffset(layout.Resolve(step.Args[0]));
                byte[] name = Encoding.Unicode.GetBytes(step.Args[1]);
                if (name.Length > 62)
                    throw new FormatException($"The name in '{step}' is longer than 31 UTF-16 code units.");

                for (int i = 0; i < 64; i++)
                    output[(int)entry + i] = i < name.Length ? name[i] : (byte)0;

                WriteInteger(output, entry + 64, 2, name.Length + 2);
                break;

            case "Fat":
                WriteInteger(output, layout.FatEntryOffset(Evaluate(step.Args[0], layout)), 4, Evaluate(step.Args[1], layout));
                break;

            case "MiniFat":
                WriteInteger(output, layout.MiniFatEntryOffset(Evaluate(step.Args[0], layout)), 4, Evaluate(step.Args[1], layout));
                break;

            case "Truncate":
                long length = Evaluate(step.Args[0], layout);
                if (length < 0 || length > output.Count)
                    throw new FormatException($"The step '{step}' would lengthen the container.");

                output.RemoveRange((int)length, output.Count - (int)length);
                break;

            case "Append":
                long count = Evaluate(step.Args[0], layout);
                long value = Evaluate(step.Args[1], layout);
                if (count < 0 || value is < 0 or > 255)
                    throw new FormatException($"The step '{step}' does not append a count of bytes.");

                output.AddRange(Enumerable.Repeat((byte)value, (int)count));
                break;

            case "GrowFat":
                GrowFat(output, layout, (int)Evaluate(step.Args[0], layout));
                break;

            case "MoveLast" when step.Args[0] == "dir":
                MoveLastSector(output, layout, layout.DirectorySectors, () => WriteInteger(output, 0x30, 4, layout.SectorCount));
                break;

            case "MoveLast":
                int sid = layout.Resolve(step.Args[0]);
                MoveLastSector(output, layout, layout.Chain(sid), () => WriteInteger(output, layout.EntryOffset(sid) + 116, 4, layout.SectorCount));
                break;

            default:
                throw new FormatException($"'{step}' is not a patch step.");
        }
    }

    /// <summary>
    /// Appends FAT sectors that hold only free entries, listing them after the existing FAT sectors in the header and,
    /// past its 109 slots, in DIFAT sectors appended after them.
    /// </summary>
    /// <param name="output">The container bytes, still as long as the container as built.</param>
    /// <param name="layout">The container as built, which must have no DIFAT sector.</param>
    /// <param name="count">The number of FAT sectors to add.</param>
    /// <exception cref="FormatException">
    /// The container has been resized or has DIFAT sectors, or the new sectors fall outside the FAT as built.
    /// </exception>
    /// <remarks>
    /// With <c>S</c> the sector size, <c>E = S / 4</c>, <c>F</c> the FAT sectors as built and <c>L</c> the sectors as
    /// built: the new FAT sectors are <c>L</c> to <c>L + count - 1</c>, each <c>S</c> bytes of <c>0xFF</c>; the
    /// <c>T = F + count</c> FAT sector numbers go, in order, into the 109 header slots and then into
    /// <c>D = ceil(max(0, T - 109) / (E - 1))</c> DIFAT sectors <c>L + count</c> to <c>L + count + D - 1</c>, each
    /// holding <c>E - 1</c> numbers (unused slots <c>FREESECT</c>) and then the next DIFAT sector, or <c>ENDOFCHAIN</c>
    /// in the last. The FAT marks the new FAT sectors <c>FATSECT</c> and the DIFAT sectors <c>DIFSECT</c>, and the
    /// header's FAT sector count, first DIFAT sector and DIFAT sector count are set.
    /// </remarks>
    private static void GrowFat(List<byte> output, Layout layout, int count)
    {
        int size = layout.SectorSize;
        int perSector = size / 4;
        long built = layout.SectorCount;
        if (output.Count != layout.Length || layout.HasDifatSectors)
            throw new FormatException("GrowFat needs the container as built, with no DIFAT sector.");

        var fatSectors = new List<long>(layout.FatSectors.Select(sector => (long)sector));
        for (int i = 0; i < count; i++)
        {
            fatSectors.Add(built + i);
            output.AddRange(Enumerable.Repeat((byte)0xFF, size));
        }

        int overflow = Math.Max(0, fatSectors.Count - HeaderDifatSlots);
        int difatSectors = (overflow + perSector - 2) / (perSector - 1);
        long firstDifat = built + count;
        if (firstDifat + difatSectors > (long)layout.FatSectors.Count * perSector)
            throw new FormatException("GrowFat needs the new sectors to fall within the FAT as built.");

        Span<byte> word = stackalloc byte[4];
        for (int d = 0; d < difatSectors; d++)
        {
            for (int slot = 0; slot < perSector - 1; slot++)
            {
                int index = HeaderDifatSlots + (d * (perSector - 1)) + slot;
                BinaryPrimitives.WriteUInt32LittleEndian(word, index < fatSectors.Count ? (uint)fatSectors[index] : FreeSector);
                output.AddRange(word.ToArray());
            }

            BinaryPrimitives.WriteUInt32LittleEndian(word, d == difatSectors - 1 ? EndOfChain : (uint)(firstDifat + d + 1));
            output.AddRange(word.ToArray());
        }

        for (int i = 0; i < count; i++)
            WriteInteger(output, layout.FatEntryOffset(built + i), 4, FatSector);

        for (int d = 0; d < difatSectors; d++)
            WriteInteger(output, layout.FatEntryOffset(firstDifat + d), 4, DifatSector);

        for (int i = 0; i < HeaderDifatSlots; i++)
            WriteInteger(output, 0x4C + (4 * i), 4, i < fatSectors.Count ? fatSectors[i] : FreeSector);

        WriteInteger(output, 0x2C, 4, fatSectors.Count);
        WriteInteger(output, 0x44, 4, difatSectors > 0 ? firstDifat : EndOfChain);
        WriteInteger(output, 0x48, 4, difatSectors);
    }

    /// <summary>
    /// Moves the last sector of a chain to a new sector appended at the end of the container.
    /// </summary>
    /// <param name="output">The container bytes, still as long as the container as built.</param>
    /// <param name="layout">The container as built.</param>
    /// <param name="chain">
    /// The chain whose last sector moves: a stream's, the mini stream's, or the directory's.
    /// </param>
    /// <param name="relinkStart">Points the chain's start at the new sector, for a chain of one sector.</param>
    /// <exception cref="FormatException">
    /// The container has been resized, or the new sector falls outside the FAT as built.
    /// </exception>
    /// <remarks>
    /// With <c>L</c> the sectors as built and <c>c</c> the chain: the bytes of sector <c>c[-1]</c> are copied to a new
    /// sector <c>L</c> at the end; the FAT entry of <c>c[-2]</c>, or the chain's start for a chain of one sector,
    /// becomes <c>L</c>; the FAT entry of <c>L</c> becomes <c>ENDOFCHAIN</c>, and that of <c>c[-1]</c> becomes
    /// <c>FREESECT</c>.
    /// </remarks>
    private static void MoveLastSector(List<byte> output, Layout layout, IReadOnlyList<uint> chain, Action relinkStart)
    {
        int size = layout.SectorSize;
        long built = layout.SectorCount;
        if (output.Count != layout.Length)
            throw new FormatException("MoveLast needs the container as built.");
        if (built >= (long)layout.FatSectors.Count * (size / 4))
            throw new FormatException("MoveLast needs the new sector to fall within the FAT as built.");
        if (chain.Count == 0)
            throw new FormatException("MoveLast needs a chain of at least one sector.");

        long last = chain[^1];
        output.AddRange(output.GetRange((int)((last + 1) * size), size));
        if (chain.Count >= 2)
            WriteInteger(output, layout.FatEntryOffset(chain[^2]), 4, built);
        else
            relinkStart();

        WriteInteger(output, layout.FatEntryOffset(built), 4, EndOfChain);
        WriteInteger(output, layout.FatEntryOffset(last), 4, FreeSector);
    }

    /// <summary>
    /// Writes an unsigned little-endian integer of 1, 2, 4 or 8 bytes, keeping the low-order bytes of the value.
    /// </summary>
    /// <param name="output">The container bytes.</param>
    /// <param name="offset">The file offset.</param>
    /// <param name="width">The width in bytes.</param>
    /// <param name="value">The value.</param>
    /// <exception cref="FormatException">
    /// The width is not 1, 2, 4 or 8, or the field lies outside the container.
    /// </exception>
    private static void WriteInteger(List<byte> output, long offset, int width, long value)
    {
        if (width is not (1 or 2 or 4 or 8))
            throw new FormatException($"The width {width} is not 1, 2, 4 or 8.");
        if (offset < 0 || offset + width > output.Count)
            throw new FormatException($"The offset {offset} lies outside the container.");

        Span<byte> bytes = stackalloc byte[8];
        BinaryPrimitives.WriteInt64LittleEndian(bytes, value);
        for (int i = 0; i < width; i++)
            output[(int)offset + i] = bytes[i];
    }

    /// <summary>
    /// Evaluates an expression: terms joined by <c>+</c> and <c>-</c>, each a number, a symbol, a measure or a
    /// function.
    /// </summary>
    /// <param name="text">The expression.</param>
    /// <param name="layout">The container as built.</param>
    /// <returns>The value.</returns>
    /// <exception cref="FormatException">
    /// The expression is not well formed, or names something the container lacks.
    /// </exception>
    /// <remarks>
    /// A sign is a separator only after a term and outside parentheses, so a leading minus stays with the term it
    /// negates and a function's own arguments are evaluated by the function.
    /// </remarks>
    private static long Evaluate(string text, Layout layout)
    {
        long total = 0;
        int sign = 1;
        int depth = 0;
        int start = 0;
        text = text.Trim();
        for (int i = 0; i <= text.Length; i++)
        {
            if (i < text.Length && text[i] == '(')
            {
                depth++;
                continue;
            }

            if (i < text.Length && text[i] == ')')
            {
                depth--;
                continue;
            }

            bool boundary = i == text.Length || (depth == 0 && i > start && text[i] is '+' or '-');
            if (!boundary)
                continue;

            string term = text[start..i].Trim();
            if (term.Length > 0)
                total += sign * EvaluateTerm(term, layout);
            else if (i < text.Length)
                throw new FormatException($"The expression '{text}' has an empty term.");

            if (i < text.Length)
                sign = text[i] == '+' ? 1 : -1;

            start = i + 1;
        }

        return total;
    }

    /// <summary>
    /// Evaluates one term of an expression.
    /// </summary>
    /// <param name="term">The term.</param>
    /// <param name="layout">The container as built.</param>
    /// <returns>The value.</returns>
    /// <exception cref="FormatException">
    /// The term is not well formed, or names something the container lacks.
    /// </exception>
    private static long EvaluateTerm(string term, Layout layout)
    {
        // A minus that opens an expression, such as the -1 in sector(/A,-1), negates the term after it.
        if (term[0] == '-')
            return -EvaluateTerm(term[1..].Trim(), layout);

        if (char.IsAsciiDigit(term[0]))
            return Number(term);

        if (s_symbols.TryGetValue(term, out long symbol))
            return symbol;

        if (term == "length")
            return layout.Length;

        if (term == "sectors")
            return layout.SectorCount;

        int open = term.IndexOf('(', StringComparison.Ordinal);
        if (open < 0 || !term.EndsWith(')'))
            throw new FormatException($"'{term}' is not a number, a symbol, a measure or a function.");

        string name = term[..open];
        List<string> args = SplitTopLevel(term[(open + 1)..^1], ',');
        return name switch
        {
            "sector" => layout.ChainSector(layout.Resolve(args[0]), (int)Evaluate(args[1], layout)),
            "minisector" => layout.MiniChainSector(layout.Resolve(args[0]), (int)Evaluate(args[1], layout)),
            "sid" => layout.Resolve(args[0]),
            "fatsector" => Layout.Index(layout.FatSectors, (int)Evaluate(args[0], layout)),
            "dirsector" => Layout.Index(layout.DirectorySectors, (int)Evaluate(args[0], layout)),
            "minifatsector" => Layout.Index(layout.MiniFatSectors, (int)Evaluate(args[0], layout)),
            "offset" => (Evaluate(args[0], layout) + 1) * layout.SectorSize,
            "entry" => layout.EntryOffset(layout.Resolve(args[0])),
            _ => throw new FormatException($"'{name}' is not a function."),
        };
    }

    /// <summary>
    /// Creates the table of header fields a <c>Header</c> step may name.
    /// </summary>
    /// <returns>The fields by name, with their offsets and widths.</returns>
    private static Dictionary<string, (int Offset, int Width)> CreateHeaderFields()
    {
        var fields = new Dictionary<string, (int Offset, int Width)>(StringComparer.Ordinal)
        {
            ["MinorVersion"] = (0x18, 2),
            ["MajorVersion"] = (0x1A, 2),
            ["ByteOrder"] = (0x1C, 2),
            ["SectorShift"] = (0x1E, 2),
            ["MiniSectorShift"] = (0x20, 2),
            ["DirectorySectors"] = (0x28, 4),
            ["FatSectors"] = (0x2C, 4),
            ["FirstDirectorySector"] = (0x30, 4),
            ["TransactionSignature"] = (0x34, 4),
            ["MiniStreamCutoff"] = (0x38, 4),
            ["FirstMiniFatSector"] = (0x3C, 4),
            ["MiniFatSectors"] = (0x40, 4),
            ["FirstDifatSector"] = (0x44, 4),
            ["DifatSectors"] = (0x48, 4),
        };

        for (int i = 0; i < HeaderDifatSlots; i++)
            fields["Difat" + i.ToString(CultureInfo.InvariantCulture)] = (0x4C + (4 * i), 4);

        return fields;
    }

    /// <summary>
    /// Describes an exception for a failure message.
    /// </summary>
    /// <param name="exception">The exception.</param>
    /// <returns>Its type name, the category of a format exception, and its message.</returns>
    private static string Describe(Exception exception)
    {
        string category = exception is CompoundFileFormatException format ? ":" + format.Category.ToString() : string.Empty;
        return $"{exception.GetType().Name}{category}: {exception.Message}";
    }
}
