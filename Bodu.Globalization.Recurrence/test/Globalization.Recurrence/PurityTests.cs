// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PurityTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

namespace Bodu.Globalization.Recurrence;

/// <summary>
/// Guards the purity contract of the <c>Bodu.Globalization.Recurrence</c> assembly: every occurrence answer is a pure
/// function of the arguments, so no API may read the wall clock or consult the machine time zone. The failure mode
/// this guards against is silent — code that consults the machine time zone passes every test on a UTC build agent
/// and fails only on a user's machine — so the ban is enforced against the compiled assembly's member references
/// rather than left to review.
/// </summary>
[TestClass]
public sealed class PurityTests
{
    /// <summary>
    /// The types no member of the assembly may reference at all: time-zone resolution and wall-clock timing.
    /// </summary>
    private static readonly string[] s_bannedTypes =
    [
        "System.TimeZoneInfo",
        "System.Diagnostics.Stopwatch",
    ];

    /// <summary>
    /// The individual members no code in the assembly may reference: wall-clock reads and machine-time-zone
    /// conversions. Pure offset arithmetic such as <see cref="DateTimeOffset.UtcDateTime" /> remains allowed.
    /// </summary>
    private static readonly (string TypeName, string MemberName)[] s_bannedMembers =
    [
        ("System.DateTime", "get_Now"),
        ("System.DateTime", "get_UtcNow"),
        ("System.DateTime", "get_Today"),
        ("System.DateTime", "ToLocalTime"),
        ("System.DateTime", "ToUniversalTime"),
        ("System.DateTimeOffset", "get_Now"),
        ("System.DateTimeOffset", "get_UtcNow"),
        ("System.DateTimeOffset", "get_LocalDateTime"),
        ("System.DateTimeOffset", "ToLocalTime"),
        ("System.Environment", "get_TickCount"),
        ("System.Environment", "get_TickCount64"),
    ];

    /// <summary>
    /// The namespace prefix of the tracker type a coverage collector injects into the assembly it instruments.
    /// </summary>
    private const string InstrumentationNamespacePrefix = "Coverlet.Core.Instrumentation";

    /// <summary>
    /// Determines whether the scanned module is an instrumented copy rather than the compiled product assembly.
    /// </summary>
    /// <param name="metadata">The metadata reader positioned over the assembly on disk.</param>
    /// <returns>
    /// <see langword="true" /> when a coverage collector has rewritten the assembly; otherwise, <see langword="false" />.
    /// </returns>
    private static bool IsInstrumented(MetadataReader metadata)
    {
        foreach (TypeDefinitionHandle handle in metadata.TypeDefinitions)
        {
            TypeDefinition type = metadata.GetTypeDefinition(handle);

            if (metadata.GetString(type.Namespace).StartsWith(InstrumentationNamespacePrefix, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Verifies that the assembly references no wall-clock, timing, or machine-time-zone API, keeping every
    /// occurrence answer a pure function of its arguments.
    /// </summary>
    [TestMethod]
    public void Assembly_ShouldNotReferenceWallClockOrTimeZoneApis()
    {
        var violations = new List<string>();

        using FileStream stream = File.OpenRead(typeof(RecurrenceRule).Assembly.Location);
        using var reader = new PEReader(stream);
        MetadataReader metadata = reader.GetMetadataReader();

        // The scan reads the assembly from disk, and under a coverage run that file is the instrumented copy rather
        // than the compiled product. The injected tracker references System.DateTime.get_UtcNow to timestamp hit
        // records, so scanning it reports a violation the product code does not contain. Report the scan as
        // inconclusive instead: the ban is still enforced on every ordinary build and test run, which is the
        // configuration the guard was written for.
        if (IsInstrumented(metadata))
        {
            Assert.Inconclusive(
                "The assembly on disk has been rewritten by a coverage collector, so its member references are not "
                + "those of the product assembly. Run without coverage instrumentation to enforce the purity ban.");
        }

        // An empty member-reference table would mean the scan is not looking at real metadata; fail loudly instead
        // of passing vacuously.
        Assert.AreNotEqual(0, metadata.MemberReferences.Count);

        foreach (MemberReferenceHandle handle in metadata.MemberReferences)
        {
            MemberReference member = metadata.GetMemberReference(handle);
            if (member.Parent.Kind != HandleKind.TypeReference)
            {
                continue;
            }

            TypeReference type = metadata.GetTypeReference((TypeReferenceHandle)member.Parent);
            string typeName = $"{metadata.GetString(type.Namespace)}.{metadata.GetString(type.Name)}";
            string memberName = metadata.GetString(member.Name);

            if (Array.IndexOf(s_bannedTypes, typeName) >= 0
                || Array.IndexOf(s_bannedMembers, (typeName, memberName)) >= 0)
            {
                violations.Add($"{typeName}.{memberName}");
            }
        }

        Assert.AreEqual(
            0,
            violations.Count,
            $"The assembly references banned wall-clock or time-zone APIs: {string.Join(", ", violations)}.");
    }
}
