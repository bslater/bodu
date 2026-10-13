// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateBinaryResourceTests.FormatVersion.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers.Binary;
using System.Globalization;
using Bodu.Globalization.Calendar.RangeResolution;

namespace Bodu.Globalization.Calendar;

public sealed partial class NotableDateBinaryResourceTests
{
    /// <summary>
    /// Verifies that a pack whose adjustment scope names a concept's rule is written at format version 2, so a reader
    /// that predates qualified rule references rejects it rather than misreading the scope.
    /// </summary>
    [TestMethod]
    public void Write_WhenScopeNamesConceptRule_ShouldWriteVersionTwo()
    {
        byte[] pack = WritePack(RuleScopedResource("""{ "notableDateRef": "new-year", "ruleRef": "x" }"""));

        Assert.AreEqual((ushort)2, BinaryPrimitives.ReadUInt16LittleEndian(pack.AsSpan(4)));
    }

    /// <summary>
    /// Verifies that a pack with no qualified rule reference is still written at format version 1, byte-compatible with
    /// packs written before qualified references existed.
    /// </summary>
    [TestMethod]
    public void Write_WhenNoScopeNamesConceptRule_ShouldWriteVersionOne()
    {
        byte[] pack = WritePack(RuleScopedResource("\"x\""));

        Assert.AreEqual((ushort)1, BinaryPrimitives.ReadUInt16LittleEndian(pack.AsSpan(4)));
    }

    /// <summary>
    /// Verifies that a version 2 pack keeps a scope's concept qualification through the round trip: the policy still
    /// adjusts New Year's Day (Saturday 1 January 2022) but not the other concept's same-named rule (Saturday 8
    /// January).
    /// </summary>
    [TestMethod]
    public void WriteRead_WhenScopeNamesConceptRule_ShouldKeepConceptScoping()
    {
        NotableDateResource reloaded = ReadPack(WritePack(RuleScopedResource("""{ "notableDateRef": "new-year", "ruleRef": "x" }""")));

        Assert.AreEqual(
            (new DateOnly(2022, 1, 3), new DateOnly(2022, 1, 8)),
            ResolveScopedPair(reloaded));
    }

    /// <summary>
    /// Verifies that a version 1 pack, whose scope stores a rule identifier alone, is still read and keeps matching that
    /// rule identifier in every concept, so both Saturdays are adjusted.
    /// </summary>
    [TestMethod]
    public void Read_WhenVersionOnePackScopesBareRuleIdentifier_ShouldMatchRuleInEveryConcept()
    {
        byte[] pack = WritePack(RuleScopedResource("\"x\""));

        NotableDateResource reloaded = ReadPack(pack);

        Assert.AreEqual(
            ((ushort)1, new DateOnly(2022, 1, 3), new DateOnly(2022, 1, 10)),
            (BinaryPrimitives.ReadUInt16LittleEndian(pack.AsSpan(4)), ResolveScopedPair(reloaded).NewYear, ResolveScopedPair(reloaded).OtherDay));
    }

    /// <summary>
    /// Resolves the two Saturday holidays of <see cref="RuleScopedResource" /> in January 2022.
    /// </summary>
    /// <param name="resource">The resource to resolve.</param>
    /// <returns>The emitted dates of New Year's Day and of the other concept.</returns>
    private static (DateOnly NewYear, DateOnly OtherDay) ResolveScopedPair(NotableDateResource resource)
    {
        IReadOnlyList<NotableDate> results = new NotableDateService(resource)
            .Resolve(new DateRange(new DateOnly(2022, 1, 1), new DateOnly(2022, 1, 14)), "XX");

        return (results.Single(r => r.NotableDateId == "new-year").Date, results.Single(r => r.NotableDateId == "other-day").Date);
    }

    /// <summary>
    /// Loads a resource whose two concepts both use rule id <c>x</c> on a Saturday (1 and 8 January 2022) and reference
    /// a mondayisation policy scoped by the supplied JSON <c>ruleRefs</c> entry.
    /// </summary>
    /// <param name="ruleReference">The JSON <c>ruleRefs</c> array entry.</param>
    /// <returns>The loaded resource.</returns>
    private static NotableDateResource RuleScopedResource(string ruleReference) =>
        NotableDateResourceLoader.LoadJson($$"""
            { "schemaVersion": "1.0", "resourceId": "data.rule-scope-pack",
              "adjustmentPolicies": [ { "id": "mondayise", "scope": { "ruleRefs": [ {{ruleReference}} ] },
                "trigger": { "type": "IfWeekend" }, "action": { "type": "MoveToNextWorkingDay", "skipWeekends": true },
                "emission": { "mode": "ObservedOnly", "reason": "Substitute" } } ],
              "notableDates": [
                { "id": "new-year", "displayName": "New Year's Day", "category": "PublicHoliday", "defaultNonWorkingDay": true,
                  "rules": [ { "id": "x", "strategy": { "fixed": { "month": 1, "day": 1 } }, "adjustments": [ "mondayise" ] } ] },
                { "id": "other-day", "displayName": "Other Day", "category": "PublicHoliday", "defaultNonWorkingDay": true,
                  "rules": [ { "id": "x", "strategy": { "fixed": { "month": 1, "day": 8 } }, "adjustments": [ "mondayise" ] } ] } ] }
            """);

    /// <summary>
    /// Verifies that a pack whose span collision policy is not <see cref="CollisionPolicy.KeepAll" /> is written at
    /// format version 3, so a reader that predates the policy's application rejects it rather than resolving it
    /// differently.
    /// </summary>
    /// <param name="policy">The span collision policy token.</param>
    [TestMethod]
    [DataRow("HighestPriorityOnly")]
    [DataRow("CategoryPriority")]
    [DataRow("Custom")]
    public void Write_WhenSpanCollisionPolicyIsNotKeepAll_ShouldWriteVersionThree(string policy)
    {
        byte[] pack = WritePack(PolicyResource($"spanCollisionPolicy=\"{policy}\""));

        Assert.AreEqual((ushort)3, BinaryPrimitives.ReadUInt16LittleEndian(pack.AsSpan(4)));
    }

    /// <summary>
    /// Verifies that a pack whose observed-date range policy is not the default is written at format version 3.
    /// </summary>
    /// <param name="policy">The observed-date range policy token.</param>
    [TestMethod]
    [DataRow("ActualOccurrenceControlsInclusion")]
    [DataRow("BothOccurrencesControlInclusion")]
    public void Write_WhenObservedDateRangePolicyIsNotDefault_ShouldWriteVersionThree(string policy)
    {
        byte[] pack = WritePack(PolicyResource($"observedDateRangePolicy=\"{policy}\""));

        Assert.AreEqual((ushort)3, BinaryPrimitives.ReadUInt16LittleEndian(pack.AsSpan(4)));
    }

    /// <summary>
    /// Verifies that a pack whose scope names a concept's rule and whose span collision policy is not the default is
    /// written at format version 3, the version that carries both meanings.
    /// </summary>
    [TestMethod]
    public void Write_WhenScopeNamesConceptRuleAndSpanPolicyIsSet_ShouldWriteVersionThree()
    {
        NotableDateResource scoped = RuleScopedResource("""{ "notableDateRef": "new-year", "ruleRef": "x" }""");
        NotableDateResource resource = new(
            scoped.ResourceId,
            scoped.SchemaVersion,
            new ResolutionPolicy(spanCollisionPolicy: CollisionPolicy.HighestPriorityOnly),
            scoped.AdjustmentPolicies,
            scoped.NotableDates);

        byte[] pack = WritePack(resource);

        Assert.AreEqual((ushort)3, BinaryPrimitives.ReadUInt16LittleEndian(pack.AsSpan(4)));
    }

    /// <summary>
    /// Verifies that a pack that sets only the duplicate policy, which is applied before a pack is written, or only the
    /// same-day collision policy keeps the base format version.
    /// </summary>
    /// <param name="attribute">The resolution-policy attribute the resource sets.</param>
    [TestMethod]
    [DataRow("duplicatePolicy=\"KeepFirst\"")]
    [DataRow("duplicatePolicy=\"Merge\"")]
    [DataRow("sameDayCollisionPolicy=\"HighestPriorityOnly\"")]
    [DataRow("spanCollisionPolicy=\"KeepAll\" observedDateRangePolicy=\"ObservedOccurrenceControlsInclusion\"")]
    public void Write_WhenOnlyEarlierPoliciesAreSet_ShouldWriteVersionOne(string attribute)
    {
        byte[] pack = WritePack(PolicyResource(attribute));

        Assert.AreEqual((ushort)1, BinaryPrimitives.ReadUInt16LittleEndian(pack.AsSpan(4)));
    }

    /// <summary>
    /// Verifies that a version 3 pack reads back with its span collision and observed-date range policies.
    /// </summary>
    [TestMethod]
    public void WriteRead_WhenVersionThree_ShouldKeepThePolicies()
    {
        byte[] pack = WritePack(PolicyResource("spanCollisionPolicy=\"CategoryPriority\" observedDateRangePolicy=\"BothOccurrencesControlInclusion\""));

        ResolutionPolicy policy = ReadPack(pack).ResolutionPolicy;

        Assert.AreEqual(
            ((ushort)3, CollisionPolicy.CategoryPriority, ObservedDateRangePolicy.BothOccurrencesControlInclusion),
            (BinaryPrimitives.ReadUInt16LittleEndian(pack.AsSpan(4)), policy.SpanCollisionPolicy, policy.ObservedDateRangePolicy));
    }

    /// <summary>
    /// Verifies that the reader accepts a pack whose header declares format version 3, the payload being unchanged.
    /// </summary>
    [TestMethod]
    public void Read_WhenHeaderDeclaresVersionThree_ShouldRead()
    {
        byte[] pack = WritePack(PolicyResource(string.Empty));
        BinaryPrimitives.WriteUInt16LittleEndian(pack.AsSpan(4), 3);

        NotableDateResource reloaded = ReadPack(pack);

        Assert.AreEqual("test.policy-pack", reloaded.ResourceId);
    }

    /// <summary>
    /// Verifies that the reader rejects a pack whose header declares a format version beyond 3 with
    /// <see cref="NotableDateBinaryFormatException" />, naming the supported versions.
    /// </summary>
    [TestMethod]
    public void Read_WhenHeaderDeclaresVersionFour_ShouldThrowNotableDateBinaryFormatException()
    {
        byte[] pack = WritePack(PolicyResource(string.Empty));
        BinaryPrimitives.WriteUInt16LittleEndian(pack.AsSpan(4), 4);

        NotableDateBinaryFormatException ex = Assert.ThrowsExactly<NotableDateBinaryFormatException>(() =>
        {
            _ = ReadPack(pack);
        });

        Assert.AreEqual(string.Format(CultureInfo.CurrentCulture, CalendarResourceStrings.Format_Invalid_BinaryVersion, 4, 1, 3), ex.Message);
    }

    /// <summary>
    /// Loads a one-concept resource whose resolution policy carries the supplied attributes.
    /// </summary>
    /// <param name="attributes">The <c>ResolutionPolicy</c> attributes, or the empty string for none.</param>
    /// <returns>The loaded resource.</returns>
    private static NotableDateResource PolicyResource(string attributes) =>
        NotableDateResourceLoader.Load($"""
            <NotableDateResource xmlns="urn:bodu:globalization:calendar" schemaVersion="1.0" resourceId="test.policy-pack">
              <ResolutionPolicy {attributes} />
              <NotableDates>
                <NotableDate id="x" displayName="X" category="PublicHoliday">
                  <Rules><Rule id="r"><Strategy><Fixed month="January" day="1" /></Strategy></Rule></Rules>
                </NotableDate>
              </NotableDates>
            </NotableDateResource>
            """);
}
