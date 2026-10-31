// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CompoundMutationSweepTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers.Binary;
using System.Diagnostics;
using Bodu.Test;

namespace Bodu.IO.Compound;

/// <summary>
/// Sweeps seeded random mutations of the valid reference fixtures through the reader at every validation level and
/// read strategy. A mutant either reads clean or is rejected with the <see cref="CompoundFileException" /> family,
/// within a bounded time and allocation; every departure is collected and reported at once, so one run shows
/// everything the sweep finds.
/// </summary>
/// <remarks>
/// <para>
/// The embedded <c>invalid/*.dat</c> corpus and <see cref="CompoundSynthesizedCorruptionTests" /> each pin a named
/// structural defect. This class is the complement: it subjects the whole read path - header, allocation tables,
/// directory, sector chains, and the two standard property sets - to damage nobody chose, four kinds of it per
/// fixture, and holds the reader to its exception contract on all of it. The mutants are generated inside each test
/// from a seed named in every finding, so a failure is reproduced by its seed alone and nothing binary enters the
/// test discovery data.
/// </para>
/// <para>
/// A rejection from <see cref="CompoundFile.Open(Stream, CompoundFileOptions, bool)" /> or from an enumeration ends
/// a configuration, but a rejection of one stream or one property set does not: the walk counts it and goes on to the
/// next entry, so one mutant exercises every entry the reader yields under every level. Every stream is opened
/// through <see cref="CompoundStorage.TryOpenStream(string, out CompoundStream)" /> by the name
/// <see cref="CompoundStorage.EnumerateStreams" /> yielded, and a lookup that disagrees with the enumeration is a
/// finding; a name that repeats within a storage under the compound-file comparison is noted against the mutant, so
/// the finding shows why.
/// </para>
/// <para>
/// Each test also counts the configurations that read clean and the ones that were rejected, and requires both to be
/// non-zero, so a reader that rejected every mutant at open, or accepted all of them, would fail the sweep rather
/// than pass it silently. Over the committed seeds every sweep has dozens to thousands of configurations of each
/// kind, so both counts are deterministic.
/// </para>
/// <para>
/// The seeds are <c>fixtureIndex * 100_000 + i</c> for <c>i</c> below <see cref="SeedsPerKind" />, offset by
/// <see cref="SeedBase" />, which is zero when committed and is raised during development to prove the sweep is not
/// passing trivially.
/// </para>
/// </remarks>
[TestClass]
public sealed class CompoundMutationSweepTests
{
    /// <summary>The offset added to every seed; zero when committed.</summary>
    private const int SeedBase = 0;

    /// <summary>The number of mutants of each kind generated per fixture.</summary>
    private const int SeedsPerKind = 4000;

    /// <summary>The distance between the seed ranges of consecutive fixtures.</summary>
    private const int SeedStride = 100_000;

    /// <summary>The length, in bytes, of the compound-file header.</summary>
    private const int HeaderLength = 512;

    /// <summary>The byte offset, within the header, of the 16-bit sector shift.</summary>
    private const int SectorShiftOffset = 30;

    /// <summary>The recursion budget for a hierarchy walk over a mutant.</summary>
    /// <remarks>
    /// No fixture nests anywhere near this deep, so exhausting it means the hierarchy the reader yielded is not a
    /// tree. Bounding the walk turns that into a finding instead of a stack overflow that takes the test host with it.
    /// </remarks>
    private const int MaxDepth = 64;

    /// <summary>The length, in bytes, of the buffer every stream is read through.</summary>
    private const int ReadBufferLength = 64 * 1024;

    /// <summary>The most bytes one mutant may allocate under one configuration before it is a finding.</summary>
    private const long MaxAllocatedBytes = 256L << 20;

    /// <summary>The longest one mutant may take under one configuration before it is a finding.</summary>
    private static readonly TimeSpan MaxDuration = TimeSpan.FromSeconds(5);

    /// <summary>The valid reference fixtures, in the order that fixes each one's seed range.</summary>
    private static readonly string[] s_fixtures =
    [
        "clean.dat",
        "example.dat",
        "example2.dat",
        "sample1.doc",
        "sample1.xls",
        "sample2.doc",
        "strange_sector_size_v4.dat",
        "test-ole-file.doc",
    ];

    /// <summary>The validation levels every mutant is opened at.</summary>
    private static readonly CompoundValidationLevel[] s_levels =
        [CompoundValidationLevel.Strict, CompoundValidationLevel.Compatible, CompoundValidationLevel.Minimal];

    /// <summary>The read strategies every mutant is opened with.</summary>
    private static readonly CompoundReadStrategy[] s_strategies =
        [CompoundReadStrategy.Buffered, CompoundReadStrategy.Streaming];

    /// <summary>
    /// The kinds of damage the sweep inflicts on a fixture.
    /// </summary>
    private enum MutationKind
    {
        /// <summary>One to eight bytes anywhere in the file are each XORed with a random non-zero mask.</summary>
        FlipBytes,

        /// <summary>One to four 32-bit fields in the header are overwritten with random values.</summary>
        HeaderFields,

        /// <summary>The file is cut at a random length, sometimes inside the header.</summary>
        Truncate,

        /// <summary>One whole sector, of the size the header declares, is overwritten with random bytes.</summary>
        Sector,
    }

    /// <summary>
    /// What the reader did with one mutant under one configuration, for the census.
    /// </summary>
    private enum Outcome
    {
        /// <summary>Every entry was read and both property sets were tried without a rejection or a finding.</summary>
        Clean,

        /// <summary>
        /// At least one call was rejected with the <see cref="CompoundFileException" /> family: the open, an
        /// enumeration, a stream, or a property set.
        /// </summary>
        Rejected,

        /// <summary>Nothing was rejected, but the run recorded a finding against the reader.</summary>
        Faulted,
    }

    /// <summary>
    /// Verifies that a fixture with one to eight random bytes flipped either reads clean or is rejected with the
    /// <see cref="CompoundFileException" /> family, within bounded time and allocation, at every validation level and
    /// read strategy.
    /// </summary>
    /// <param name="fixture">The valid reference fixture to mutate.</param>
    [TestMethod]
    [TestCategory(TestCategories.Regression)]
    [Timeout(120_000)]
    [DataRow("clean.dat")]
    [DataRow("example.dat")]
    [DataRow("example2.dat")]
    [DataRow("sample1.doc")]
    [DataRow("sample1.xls")]
    [DataRow("sample2.doc")]
    [DataRow("strange_sector_size_v4.dat")]
    [DataRow("test-ole-file.doc")]
    public void Open_WhenRandomBytesAreFlipped_ShouldReadCleanOrThrowCompoundFileException(string fixture) =>
        Sweep(fixture, MutationKind.FlipBytes);

    /// <summary>
    /// Verifies that a fixture with one to four header fields overwritten with random 32-bit values either reads
    /// clean or is rejected with the <see cref="CompoundFileException" /> family, within bounded time and allocation,
    /// at every validation level and read strategy.
    /// </summary>
    /// <param name="fixture">The valid reference fixture to mutate.</param>
    [TestMethod]
    [TestCategory(TestCategories.Regression)]
    [Timeout(120_000)]
    [DataRow("clean.dat")]
    [DataRow("example.dat")]
    [DataRow("example2.dat")]
    [DataRow("sample1.doc")]
    [DataRow("sample1.xls")]
    [DataRow("sample2.doc")]
    [DataRow("strange_sector_size_v4.dat")]
    [DataRow("test-ole-file.doc")]
    public void Open_WhenRandomHeaderFieldsAreOverwritten_ShouldReadCleanOrThrowCompoundFileException(string fixture) =>
        Sweep(fixture, MutationKind.HeaderFields);

    /// <summary>
    /// Verifies that a fixture truncated at a random length, inside the header included, either reads clean or is
    /// rejected with the <see cref="CompoundFileException" /> family, within bounded time and allocation, at every
    /// validation level and read strategy.
    /// </summary>
    /// <param name="fixture">The valid reference fixture to mutate.</param>
    [TestMethod]
    [TestCategory(TestCategories.Regression)]
    [Timeout(120_000)]
    [DataRow("clean.dat")]
    [DataRow("example.dat")]
    [DataRow("example2.dat")]
    [DataRow("sample1.doc")]
    [DataRow("sample1.xls")]
    [DataRow("sample2.doc")]
    [DataRow("strange_sector_size_v4.dat")]
    [DataRow("test-ole-file.doc")]
    public void Open_WhenTruncatedAtRandomLength_ShouldReadCleanOrThrowCompoundFileException(string fixture) =>
        Sweep(fixture, MutationKind.Truncate);

    /// <summary>
    /// Verifies that a fixture with one whole random sector overwritten with random bytes either reads clean or is
    /// rejected with the <see cref="CompoundFileException" /> family, within bounded time and allocation, at every
    /// validation level and read strategy.
    /// </summary>
    /// <param name="fixture">The valid reference fixture to mutate.</param>
    [TestMethod]
    [TestCategory(TestCategories.Regression)]
    [Timeout(120_000)]
    [DataRow("clean.dat")]
    [DataRow("example.dat")]
    [DataRow("example2.dat")]
    [DataRow("sample1.doc")]
    [DataRow("sample1.xls")]
    [DataRow("sample2.doc")]
    [DataRow("strange_sector_size_v4.dat")]
    [DataRow("test-ole-file.doc")]
    public void Open_WhenRandomSectorIsOverwritten_ShouldReadCleanOrThrowCompoundFileException(string fixture) =>
        Sweep(fixture, MutationKind.Sector);

    /// <summary>
    /// Generates every mutant of one kind for a fixture, exercises each under every configuration, fails once with
    /// every finding collected, and then requires the census to show configurations that read clean and configurations
    /// that were rejected.
    /// </summary>
    /// <param name="fixture">The valid reference fixture to mutate.</param>
    /// <param name="kind">The kind of damage to inflict.</param>
    private static void Sweep(string fixture, MutationKind kind)
    {
        var fixtureIndex = Array.IndexOf(s_fixtures, fixture);
        Assert.IsGreaterThanOrEqualTo(0, fixtureIndex, $"'{fixture}' is not one of the swept fixtures.");

        byte[] original = CompoundFixtures.ReadReferenceBytes("valid/" + fixture);
        var buffer = new byte[ReadBufferLength];
        var findings = new List<string>();
        var clean = 0;
        var rejected = 0;

        for (var i = 0; i < SeedsPerKind; i++)
        {
            var seed = SeedBase + (fixtureIndex * SeedStride) + i;
            var rng = new Random(seed);
            byte[] mutant = Mutate(kind, original, rng, out var description);

            foreach (CompoundValidationLevel level in s_levels)
            {
                foreach (CompoundReadStrategy strategy in s_strategies)
                {
                    var scenario = $"{fixture} seed {seed} ({description}) at {level}/{strategy}";
                    Outcome outcome = Exercise(mutant, level, strategy, buffer, scenario, findings);

                    if (outcome == Outcome.Clean)
                        clean++;
                    else if (outcome == Outcome.Rejected)
                        rejected++;
                }
            }
        }

        var configurations = SeedsPerKind * s_levels.Length * s_strategies.Length;
        var census =
            $"{clean} of the {configurations} configurations read clean and {rejected} were rejected with a " +
            nameof(CompoundFileException);

        if (findings.Count > 0)
        {
            Assert.Fail(
                $"{findings.Count} finding(s) over {SeedsPerKind} {kind} mutants of {fixture} ({census}):{Environment.NewLine}" +
                string.Join(Environment.NewLine, findings));
        }

        // Both halves of the contract were exercised. A reader that rejected every mutant at open, or accepted all of
        // them, would produce no finding while the sweep no longer tested anything; over the committed seeds every
        // sweep has dozens to thousands of configurations of each kind, so neither count is zero.
        Assert.IsGreaterThan(0, clean, $"No {kind} mutant of {fixture} read clean under any configuration; {census}.");
        Assert.IsGreaterThan(0, rejected, $"No {kind} mutant of {fixture} was rejected under any configuration; {census}.");
    }

    /// <summary>
    /// Produces one mutant of the requested kind from the fixture bytes.
    /// </summary>
    /// <param name="kind">The kind of damage to inflict.</param>
    /// <param name="original">The unmodified fixture bytes.</param>
    /// <param name="rng">The seeded generator that decides where and what the damage is.</param>
    /// <param name="description">When this method returns, a description of the damage for the findings.</param>
    /// <returns>A new array holding the damaged copy.</returns>
    private static byte[] Mutate(MutationKind kind, byte[] original, Random rng, out string description) =>
        kind switch
        {
            MutationKind.FlipBytes => FlipBytes(original, rng, out description),
            MutationKind.HeaderFields => OverwriteHeaderFields(original, rng, out description),
            MutationKind.Truncate => Truncate(original, rng, out description),
            _ => OverwriteSector(original, rng, out description),
        };

    /// <summary>
    /// XORs one to eight bytes, anywhere in the file, each with a random non-zero mask.
    /// </summary>
    /// <param name="original">The unmodified fixture bytes.</param>
    /// <param name="rng">The seeded generator.</param>
    /// <param name="description">When this method returns, the offsets and masks applied.</param>
    /// <returns>The damaged copy.</returns>
    private static byte[] FlipBytes(byte[] original, Random rng, out string description)
    {
        var mutant = (byte[])original.Clone();
        var count = rng.Next(1, 9);
        var edits = new string[count];

        for (var i = 0; i < count; i++)
        {
            var offset = rng.Next(mutant.Length);
            var mask = (byte)rng.Next(1, 256);
            mutant[offset] ^= mask;
            edits[i] = $"[{offset}] ^= 0x{mask:X2}";
        }

        description = "flipped " + string.Join(", ", edits);
        return mutant;
    }

    /// <summary>
    /// Overwrites one to four 32-bit fields in the first 512 bytes with random values.
    /// </summary>
    /// <param name="original">The unmodified fixture bytes.</param>
    /// <param name="rng">The seeded generator.</param>
    /// <param name="description">When this method returns, the fields and values written.</param>
    /// <returns>The damaged copy.</returns>
    private static byte[] OverwriteHeaderFields(byte[] original, Random rng, out string description)
    {
        var mutant = (byte[])original.Clone();
        var count = rng.Next(1, 5);
        var edits = new string[count];
        var fieldCount = Math.Min(HeaderLength, mutant.Length) / sizeof(uint);

        for (var i = 0; i < count; i++)
        {
            var offset = rng.Next(fieldCount) * sizeof(uint);
            Span<byte> field = mutant.AsSpan(offset, sizeof(uint));
            rng.NextBytes(field);
            edits[i] = $"[{offset}] = 0x{BinaryPrimitives.ReadUInt32LittleEndian(field):X8}";
        }

        description = "header " + string.Join(", ", edits);
        return mutant;
    }

    /// <summary>
    /// Cuts the file at a random length; one draw in four lands inside the header.
    /// </summary>
    /// <param name="original">The unmodified fixture bytes.</param>
    /// <param name="rng">The seeded generator.</param>
    /// <param name="description">When this method returns, the length kept.</param>
    /// <returns>The damaged copy.</returns>
    private static byte[] Truncate(byte[] original, Random rng, out string description)
    {
        var length = rng.Next(4) == 0
            ? rng.Next(Math.Min(HeaderLength, original.Length))
            : rng.Next(original.Length);

        description = $"truncated to {length} bytes";
        return original.AsSpan(0, length).ToArray();
    }

    /// <summary>
    /// Overwrites one whole random sector, of the size the header's sector shift declares, with random bytes.
    /// </summary>
    /// <param name="original">The unmodified fixture bytes.</param>
    /// <param name="rng">The seeded generator.</param>
    /// <param name="description">When this method returns, the sector and the range written.</param>
    /// <returns>The damaged copy.</returns>
    private static byte[] OverwriteSector(byte[] original, Random rng, out string description)
    {
        var mutant = (byte[])original.Clone();
        int shift = BinaryPrimitives.ReadUInt16LittleEndian(original.AsSpan(SectorShiftOffset));
        var sectorSize = shift is >= 9 and <= 12 ? 1 << shift : HeaderLength;

        // Sector n occupies [(n + 1) * sectorSize, (n + 2) * sectorSize); the last sector of a fixture may be partial.
        var sectorCount = (original.Length - 1) / sectorSize;
        Assert.IsGreaterThan(0, sectorCount, "The fixture holds no sector after its header.");

        var sector = rng.Next(sectorCount);
        var offset = (sector + 1) * sectorSize;
        var length = Math.Min(sectorSize, mutant.Length - offset);
        rng.NextBytes(mutant.AsSpan(offset, length));

        description = $"sector {sector} ({length} bytes at [{offset}]) overwritten";
        return mutant;
    }

    /// <summary>
    /// Runs a mutant under one configuration, records every finding of the run against the scenario, and classifies
    /// the run for the census.
    /// </summary>
    /// <param name="mutant">The damaged bytes.</param>
    /// <param name="level">The validation level to open at.</param>
    /// <param name="strategy">The read strategy to open with.</param>
    /// <param name="buffer">The buffer every stream is read through.</param>
    /// <param name="scenario">The fixture, seed, damage, and configuration named in a finding.</param>
    /// <param name="findings">The list a finding is added to.</param>
    /// <returns>What the reader did with the mutant.</returns>
    /// <remarks>
    /// A finding is recorded for any exception outside the <see cref="CompoundFileException" /> family, for a hierarchy
    /// deeper than <see cref="MaxDepth" />, for a stream the enumeration yielded that the lookup does not open, and for
    /// a run over the allocation bound. The time bound is a wall-clock assertion that a scheduler stall on a loaded
    /// runner could trip, so a run over it is run once more and recorded only when the second run is over it too: a
    /// deterministic hotspot reproduces, and a stall does not. A note the run made about the mutant, such as a name
    /// that repeats within a storage, is appended to every finding of the run.
    /// </remarks>
    private static Outcome Exercise(
        byte[] mutant,
        CompoundValidationLevel level,
        CompoundReadStrategy strategy,
        byte[] buffer,
        string scenario,
        List<string> findings)
    {
        RunReport report = Run(mutant, level, strategy, buffer);
        var label = report.Notes.Count == 0 ? scenario : $"{scenario} [{string.Join("; ", report.Notes)}]";

        foreach (var finding in report.Findings)
            findings.Add($"{label}: {finding}");

        if (report.Elapsed > MaxDuration)
        {
            // Settle a slow run before reporting it: a stall does not reproduce, a hotspot in the reader does.
            TimeSpan again = Run(mutant, level, strategy, buffer).Elapsed;
            if (again > MaxDuration)
            {
                findings.Add(
                    $"{label}: took {report.Elapsed.TotalSeconds:F1} s, and {again.TotalSeconds:F1} s when run again.");
            }
        }

        if (report.Allocated > MaxAllocatedBytes)
            findings.Add($"{label}: allocated {report.Allocated >> 20} MiB for a {mutant.Length}-byte input.");

        if (report.Rejections > 0)
            return Outcome.Rejected;

        return report.Findings.Count > 0 ? Outcome.Faulted : Outcome.Clean;
    }

    /// <summary>
    /// Opens a mutant under one configuration, reads everything it offers, and measures the run.
    /// </summary>
    /// <param name="mutant">The damaged bytes.</param>
    /// <param name="level">The validation level to open at.</param>
    /// <param name="strategy">The read strategy to open with.</param>
    /// <param name="buffer">The buffer every stream is read through.</param>
    /// <returns>The report of the run: its findings, notes, rejections, duration, and allocation.</returns>
    private static RunReport Run(byte[] mutant, CompoundValidationLevel level, CompoundReadStrategy strategy, byte[] buffer)
    {
        var report = new RunReport();
        var allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
        var started = Stopwatch.GetTimestamp();

        try
        {
            ReadEverything(mutant, level, strategy, buffer, report);
        }
        catch (CompoundFileException)
        {
            // The contract: damage surfaces as the reader's exception family. Thrown by the open or by an
            // enumeration it ends the run; the walk catches the per-entry ones itself and goes on.
            report.Rejections++;
        }
        catch (Exception ex)
        {
            report.Findings.Add($"{ex.GetType().FullName}: {ex.Message} {FirstReaderFrame(ex)}");
        }

        report.Elapsed = Stopwatch.GetElapsedTime(started);
        report.Allocated = GC.GetAllocatedBytesForCurrentThread() - allocatedBefore;
        return report;
    }

    /// <summary>
    /// Opens the mutant, walks every storage, reads every stream to its end, and tries both standard property sets,
    /// counting in the report each call the reader rejected and recording every finding against it.
    /// </summary>
    /// <param name="mutant">The damaged bytes.</param>
    /// <param name="level">The validation level to open at.</param>
    /// <param name="strategy">The read strategy to open with.</param>
    /// <param name="buffer">The buffer every stream is read through.</param>
    /// <param name="report">The report of the run.</param>
    /// <exception cref="CompoundFileException">
    /// Thrown when the open or an enumeration rejects the mutant, which ends the run; a rejection of one stream or one
    /// property set is counted in the report instead, and the run goes on to the next.
    /// </exception>
    private static void ReadEverything(
        byte[] mutant,
        CompoundValidationLevel level,
        CompoundReadStrategy strategy,
        byte[] buffer,
        RunReport report)
    {
        using var source = new MemoryStream(mutant, writable: false);
        using var file = CompoundFile.Open(source, new CompoundFileOptions
        {
            ValidationLevel = level,
            ReadStrategy = strategy,
        });

        _ = Walk(file.RootStorage, "/", MaxDepth, buffer, report);
        TryPropertySet(() => file.TryGetSummaryInformation(out _), report);
        TryPropertySet(() => file.TryGetDocumentSummaryInformation(out _), report);
    }

    /// <summary>
    /// Enumerates a storage's entries, noting a name that repeats, opens each of its streams by the name the
    /// enumeration yielded and reads it to the end through the buffer, and descends into each of its storages while
    /// the depth budget lasts.
    /// </summary>
    /// <param name="storage">The storage to walk.</param>
    /// <param name="path">The path of the storage from the root, named in a finding.</param>
    /// <param name="depth">The remaining recursion budget.</param>
    /// <param name="buffer">The buffer every stream is read through.</param>
    /// <param name="report">The report of the run.</param>
    /// <returns>
    /// <see langword="true" /> when the walk stayed within the budget; otherwise <see langword="false" />, with the
    /// finding recorded.
    /// </returns>
    /// <exception cref="CompoundFileException">
    /// Thrown when an enumeration rejects the mutant, which ends the run.
    /// </exception>
    private static bool Walk(CompoundStorage storage, string path, int depth, byte[] buffer, RunReport report)
    {
        if (depth == 0)
        {
            report.Findings.Add($"the hierarchy is deeper than {MaxDepth} storages at {Quote(path)}.");
            return false;
        }

        // Names are unique within a storage under the compound-file comparison, which is what the lookup indexes by;
        // a mutant whose directory repeats one is noted, so a finding against the lookup shows why.
        var names = new HashSet<string>(CompoundNameComparer.Instance);
        foreach (CompoundEntryInfo entry in storage.EnumerateEntries())
        {
            if (!names.Add(entry.Name))
                report.Notes.Add($"the name {Quote(entry.Name)} repeats in storage {Quote(path)}");
        }

        foreach (CompoundEntryInfo entry in storage.EnumerateStreams())
            ReadStream(storage, path, entry.Name, buffer, report);

        foreach (CompoundStorage child in storage.EnumerateStorages())
        {
            var childPath = path == "/" ? path + child.Name : path + "/" + child.Name;
            if (!Walk(child, childPath, depth - 1, buffer, report))
                return false;
        }

        return true;
    }

    /// <summary>
    /// Opens one stream by the name the enumeration yielded and reads it to the end, recording a finding when the
    /// lookup does not open it and counting a rejection rather than ending the run on it.
    /// </summary>
    /// <param name="storage">The storage that holds the stream.</param>
    /// <param name="path">The path of the storage from the root, named in a finding.</param>
    /// <param name="name">The stream's name, as <see cref="CompoundStorage.EnumerateStreams" /> yielded it.</param>
    /// <param name="buffer">The buffer the stream is read through.</param>
    /// <param name="report">The report of the run.</param>
    private static void ReadStream(CompoundStorage storage, string path, string name, byte[] buffer, RunReport report)
    {
        try
        {
            if (!storage.TryOpenStream(name, out CompoundStream? stream))
            {
                report.Findings.Add(
                    $"{nameof(CompoundStorage.TryOpenStream)} returned false for the stream {Quote(name)} that " +
                    $"{nameof(CompoundStorage.EnumerateStreams)} yielded in storage {Quote(path)}.");
                return;
            }

            using (stream)
            {
                _ = stream.Length;

                while (stream.Read(buffer, 0, buffer.Length) > 0)
                {
                }
            }
        }
        catch (CompoundFileException)
        {
            // The contract: a malformed chain or a short stream surfaces as the reader's exception family, and the
            // next entry is still exercised.
            report.Rejections++;
        }
    }

    /// <summary>
    /// Tries one standard property set, counting a rejection in the report rather than ending the run on it.
    /// </summary>
    /// <param name="read">The call that reads the property set.</param>
    /// <param name="report">The report of the run.</param>
    private static void TryPropertySet(Func<bool> read, RunReport report)
    {
        try
        {
            _ = read();
        }
        catch (CompoundFileException)
        {
            // The contract: a malformed property-set stream surfaces as the reader's exception family, and the other
            // property set is still tried.
            report.Rejections++;
        }
    }

    /// <summary>
    /// Quotes an entry name or a storage path for a finding, escaping every character outside printable ASCII so a
    /// corrupt name keeps the finding on one line.
    /// </summary>
    /// <param name="text">The name or path.</param>
    /// <returns>
    /// The text in single quotes, with each character outside printable ASCII written as a <c>\u</c> escape.
    /// </returns>
    private static string Quote(string text) =>
        "'" + string.Concat(text.Select(c => c is >= ' ' and <= '~' ? char.ToString(c) : $"\\u{(int)c:X4}")) + "'";

    /// <summary>
    /// Finds the first stack frame of an exception inside the reader, so a finding names where it escaped.
    /// </summary>
    /// <param name="ex">The exception that escaped.</param>
    /// <returns>The frame, or a note that there is none.</returns>
    private static string FirstReaderFrame(Exception ex)
    {
        foreach (var line in (ex.StackTrace ?? string.Empty).Split('\n'))
        {
            var frame = line.Trim();
            if (frame.StartsWith("at Bodu.IO.Compound", StringComparison.Ordinal)
                && !frame.Contains(nameof(CompoundMutationSweepTests), StringComparison.Ordinal))
            {
                return frame;
            }
        }

        return "(no frame inside Bodu.IO.Compound)";
    }

    /// <summary>
    /// What one run of a mutant under one configuration produced: the findings against the reader, the notes about
    /// the mutant that explain them, how many calls the reader rejected, and what the run cost.
    /// </summary>
    private sealed class RunReport
    {
        /// <summary>Gets the findings against the reader, each naming the storage path and the entry.</summary>
        public List<string> Findings { get; } = [];

        /// <summary>Gets the notes about the mutant, such as a name that repeats within a storage.</summary>
        public List<string> Notes { get; } = [];

        /// <summary>Gets or sets the number of calls the reader rejected with its exception family.</summary>
        public int Rejections { get; set; }

        /// <summary>Gets or sets how long the run took.</summary>
        public TimeSpan Elapsed { get; set; }

        /// <summary>Gets or sets the number of bytes the run allocated on the test thread.</summary>
        public long Allocated { get; set; }
    }
}
