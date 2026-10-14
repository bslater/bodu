// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateBinaryResourceTests.Write.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using Bodu.Globalization.Calendar.Algorithms;
using Bodu.Globalization.Calendar.RangeResolution;

namespace Bodu.Globalization.Calendar;

public sealed partial class NotableDateBinaryResourceTests
{
    /// <summary>The applicability of the single rule the write tests encode.</summary>
    private static readonly RuleApplicability GlobalApplicability = new(CalendarSystem.Gregorian, null, null, [], [], []);

    /// <summary>
    /// Verifies that writing a rule whose calculation strategy is outside the sealed set throws
    /// <see cref="NotSupportedException" /> with the package's resource message naming the strategy type.
    /// </summary>
    [TestMethod]
    public void Write_WhenStrategyIsCustom_ShouldThrowNotSupportedExceptionFromResource()
    {
        NotableDateResource resource = SingleRuleResource(
            new NotableDateRule("r", 0, null, null, null, GlobalApplicability, new CustomStrategy(), [], []));

        NotSupportedException ex = Assert.ThrowsExactly<NotSupportedException>(() =>
        {
            _ = WritePack(resource);
        });

        Assert.AreEqual(UnsupportedTypeMessage(nameof(CustomStrategy)), ex.Message);
    }

    /// <summary>
    /// Verifies that writing a rule whose recurrence strategy is outside the sealed set throws
    /// <see cref="NotSupportedException" /> with the package's resource message naming the recurrence type.
    /// </summary>
    [TestMethod]
    public void Write_WhenRecurrenceIsCustom_ShouldThrowNotSupportedExceptionFromResource()
    {
        NotableDateResource resource = SingleRuleResource(
            new NotableDateRule("r", 0, null, null, null, GlobalApplicability, new CustomRecurrence(), [], []));

        NotSupportedException ex = Assert.ThrowsExactly<NotSupportedException>(() =>
        {
            _ = WritePack(resource);
        });

        Assert.AreEqual(UnsupportedTypeMessage(nameof(CustomRecurrence)), ex.Message);
    }

    /// <summary>
    /// Verifies that writing a rule whose duration definition is outside the sealed set throws
    /// <see cref="NotSupportedException" /> with the package's resource message naming the duration type.
    /// </summary>
    [TestMethod]
    public void Write_WhenDurationIsCustom_ShouldThrowNotSupportedExceptionFromResource()
    {
        NotableDateResource resource = SingleRuleResource(
            new NotableDateRule("r", 0, null, null, new CustomDuration(), GlobalApplicability, new FixedDateStrategy(1, 1), [], []));

        NotSupportedException ex = Assert.ThrowsExactly<NotSupportedException>(() =>
        {
            _ = WritePack(resource);
        });

        Assert.AreEqual(UnsupportedTypeMessage(nameof(CustomDuration)), ex.Message);
    }

    /// <summary>
    /// Verifies that the identifier test resource writes when every identifier, reference and territory code matches the
    /// pattern the loader requires, so each rejection the per-field test observes is caused by the one value it
    /// replaces.
    /// </summary>
    [TestMethod]
    public void Write_WhenEveryIdentifierMatchesSchemaPattern_ShouldWrite()
    {
        byte[] pack = WritePack(IdentifierFieldResource(null, null));

        Assert.AreEqual("test.write", ReadPack(pack).ResourceId);
    }

    /// <summary>
    /// Verifies that writing a resource holding an identifier, reference or territory code that breaks the pattern the
    /// loader requires of it throws <see cref="NotableDateValidationException" /> carrying the loader's
    /// <c>BODU-CAL-SCHEMA</c> diagnostic naming the value, rather than encoding a pack the loader would never have
    /// produced.
    /// </summary>
    /// <param name="field">The field holding the value.</param>
    /// <param name="value">The value that breaks the field's pattern.</param>
    /// <param name="reported">
    /// The value the diagnostic names: the value itself, or, for a scope's qualified rule reference, the part that
    /// breaks the pattern, which the loader checks on its own.
    /// </param>
    [TestMethod]
    [DataRow("resourceId", "Test.Upper", "Test.Upper")]
    [DataRow("resourceId", "test..write", "test..write")]
    [DataRow("resourceId", "", "")]
    [DataRow("policy.id", "Observed", "Observed")]
    [DataRow("scope.territories", "au", "au")]
    [DataRow("scope.notableDateRefs", "Anchor", "Anchor")]
    [DataRow("scope.ruleRefs", "anchor/R", "R")]
    [DataRow("scope.ruleRefs", "Anchor/r", "Anchor")]
    [DataRow("scope.ruleRefs", "r_1", "r_1")]
    [DataRow("action.notableDateRef", "Anchor", "Anchor")]
    [DataRow("action.ruleRef", "R", "R")]
    [DataRow("notableDate.id", "New-Year", "New-Year")]
    [DataRow("notableDate.id", "new year", "new year")]
    [DataRow("rule.id", "R", "R")]
    [DataRow("applicability.territories", "au", "au")]
    [DataRow("applicability.territories", "AUS", "AUS")]
    [DataRow("rule.adjustments", "-observed", "-observed")]
    public void Write_WhenIdentifierBreaksSchemaPattern_ShouldThrowSchemaDiagnostic(string field, string value, string reported)
    {
        NotableDateResource resource = IdentifierFieldResource(field, value);

        var ex = Assert.ThrowsExactly<NotableDateValidationException>(() =>
        {
            _ = WritePack(resource);
        });

        Assert.Contains(d => d.Code == "BODU-CAL-SCHEMA" && d.Severity == NotableDateValidationSeverity.Error && d.Message.Contains("'" + reported + "'", StringComparison.Ordinal), ex.Diagnostics, "BODU-CAL-SCHEMA");
    }

    /// <summary>
    /// Verifies that writing a resource that fails one of the loader's duplicate or cross-reference checks throws
    /// <see cref="NotableDateValidationException" /> carrying the diagnostic the loader reports for it.
    /// </summary>
    /// <param name="code">The diagnostic the resource's one violation raises.</param>
    [TestMethod]
    [DataRow("BODU-CAL-DUP-ND")]
    [DataRow("BODU-CAL-DUP-RULE")]
    [DataRow("BODU-CAL-DUP-POLICY")]
    [DataRow("BODU-CAL-ALGORITHM")]
    [DataRow("BODU-CAL-OFFSET-MISSING")]
    [DataRow("BODU-CAL-ADJREF")]
    [DataRow("BODU-CAL-YEARS")]
    [DataRow("BODU-CAL-REPLACE-MISSING")]
    [DataRow("BODU-CAL-HANDLER-MISSING")]
    public void Write_WhenResourceFailsLoaderValidation_ShouldThrowLoadersDiagnostic(string code)
    {
        NotableDateResource resource = InvalidResource(code);

        var ex = Assert.ThrowsExactly<NotableDateValidationException>(() =>
        {
            _ = WritePack(resource);
        });

        Assert.AreEqual(code, string.Join(", ", ex.Diagnostics.Where(d => d.Severity == NotableDateValidationSeverity.Error).Select(d => d.Code)));
    }

    /// <summary>
    /// Verifies that a resource the loader would reject is refused before anything is encoded, so the stream is left
    /// empty rather than holding a partial pack.
    /// </summary>
    [TestMethod]
    public void Write_WhenResourceFailsLoaderValidation_ShouldWriteNothing()
    {
        NotableDateResource resource = IdentifierFieldResource("notableDate.id", "New-Year");
        using MemoryStream stream = new();

        _ = Assert.ThrowsExactly<NotableDateValidationException>(() =>
        {
            NotableDateBinaryResource.Write(resource, stream);
        });

        Assert.AreEqual(0L, stream.Length);
    }

    /// <summary>
    /// Verifies that each notable-date pack the library and its regional data bundles embed passes the checks writing
    /// runs, and that writing the resource read back from its pack reproduces the pack byte for byte.
    /// </summary>
    /// <param name="pack">The pack, as its assembly's name and its resource name.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DynamicData(nameof(ParserValidationTests.BundledPacks), typeof(ParserValidationTests))]
    public void Write_WhenBundledPack_ShouldRewriteItsReadBackByteForByte(string pack)
    {
        NotableDateResource resource = NotableDateResourceLoader.Load(ParserValidationTests.BundledPackXml(pack), ParserValidationTests.ResolveBundledPack);

        byte[] written = WritePack(resource);

        CollectionAssert.AreEqual(written, WritePack(ReadPack(written)), pack);
    }

    /// <summary>
    /// Builds a resource holding every identifier, reference and territory code a resource carries, each set to its
    /// well-formed value except the one named, which holds the supplied value.
    /// </summary>
    /// <param name="field">The field to set, or <see langword="null" /> to set none.</param>
    /// <param name="value">The value the named field holds.</param>
    /// <returns>The resource.</returns>
    private static NotableDateResource IdentifierFieldResource(string? field, string? value)
    {
        string Value(string name, string valid) =>
            name == field ? value! : valid;

        AdjustmentPolicy policy = new(
            Value("policy.id", "observed"),
            0,
            new AdjustmentScope(
                [Value("scope.territories", "AU")],
                [],
                [],
                [Value("scope.notableDateRefs", "anchor")],
                [Value("scope.ruleRefs", "anchor/r")]),
            AdjustmentTrigger.IfWeekend,
            [],
            AdjustmentAction.ReplaceWithRule,
            null,
            0,
            null,
            false,
            false,
            EmissionMode.ObservedOnly,
            null,
            null,
            actionNotableDateRef: Value("action.notableDateRef", "anchor"),
            actionRuleRef: Value("action.ruleRef", "r"));

        NotableDateRule rule = new(
            Value("rule.id", "r"),
            0,
            null,
            null,
            null,
            new RuleApplicability(CalendarSystem.Gregorian, null, null, [Value("applicability.territories", "AU")], [], []),
            new FixedDateStrategy(3, 1),
            [Value("rule.adjustments", "observed")],
            []);

        return new NotableDateResource(
            Value("resourceId", "test.write"),
            "1.0",
            ResolutionPolicy.Default,
            [policy],
            [new NotableDateDefinition(Value("notableDate.id", "anchor"), "Anchor", NotableDateCategory.PublicHoliday, true, 1, [], [rule])]);
    }

    /// <summary>
    /// Builds a resource whose one violation of the loader's duplicate or cross-reference checks raises the named
    /// diagnostic.
    /// </summary>
    /// <param name="code">The diagnostic the violation raises.</param>
    /// <returns>The resource.</returns>
    private static NotableDateResource InvalidResource(string code)
    {
        NotableDateRule Rule(string id, IDateCalculationStrategy strategy, RuleApplicability? applicability = null, string? policyRef = null) =>
            new(id, 0, null, null, null, applicability ?? GlobalApplicability, strategy, policyRef is null ? [] : [policyRef], []);

        NotableDateDefinition Concept(string id, params NotableDateRule[] rules) =>
            new(id, "Concept", NotableDateCategory.Observance, false, 1, [], rules);

        AdjustmentPolicy Policy(string id, AdjustmentAction action) =>
            new(id, 0, AdjustmentScope.Global, AdjustmentTrigger.IfWeekend, [], action, null, 0, null, false, false, EmissionMode.ObservedOnly, null, null);

        NotableDateResource Resource(IReadOnlyList<AdjustmentPolicy> policies, params NotableDateDefinition[] concepts) =>
            new("test.write", "1.0", ResolutionPolicy.Default, policies, concepts);

        NotableDateDefinition valid = Concept("anchor", Rule("r", new FixedDateStrategy(3, 1)));

        return code switch
        {
            "BODU-CAL-DUP-ND" => Resource([], valid, valid),
            "BODU-CAL-DUP-RULE" => Resource([], Concept("anchor", Rule("r", new FixedDateStrategy(3, 1)), Rule("r", new FixedDateStrategy(4, 1)))),
            "BODU-CAL-DUP-POLICY" => Resource([Policy("observed", AdjustmentAction.MoveToNextWorkingDay), Policy("observed", AdjustmentAction.MoveToNextWorkingDay)], valid),
            "BODU-CAL-ALGORITHM" => Resource([], Concept("anchor", Rule("r", new AlgorithmDateStrategy("no-such-algorithm")))),
            "BODU-CAL-OFFSET-MISSING" => Resource([], valid, Concept("offset", Rule("r", new OffsetFromRuleStrategy("missing", null, 1)))),
            "BODU-CAL-ADJREF" => Resource([], Concept("anchor", Rule("r", new FixedDateStrategy(3, 1), policyRef: "missing"))),
            "BODU-CAL-YEARS" => Resource([], Concept("anchor", Rule("r", new FixedDateStrategy(3, 1), new RuleApplicability(CalendarSystem.Gregorian, 2030, 2020, [], [], [])))),
            "BODU-CAL-REPLACE-MISSING" => Resource([Policy("observed", AdjustmentAction.ReplaceWithRule)], valid),
            "BODU-CAL-HANDLER-MISSING" => Resource([Policy("observed", AdjustmentAction.Custom)], valid),
            _ => throw new ArgumentOutOfRangeException(nameof(code)),
        };
    }

    /// <summary>
    /// Builds a resource holding one concept with the supplied rule.
    /// </summary>
    /// <param name="rule">The concept's only rule.</param>
    /// <returns>The resource.</returns>
    private static NotableDateResource SingleRuleResource(NotableDateRule rule) =>
        new(
            "test.write",
            "1.0",
            ResolutionPolicy.Default,
            [],
            [new NotableDateDefinition("custom", "Custom", NotableDateCategory.Observance, false, 1, [], [rule])]);

    /// <summary>
    /// Formats the package's message for a type the binary format cannot encode, read from its resources.
    /// </summary>
    /// <param name="typeName">The name of the unsupported type.</param>
    /// <returns>The expected message, or <see langword="null" /> when the resource is absent.</returns>
    private static string? UnsupportedTypeMessage(string typeName)
    {
        string? format = CalendarResourceStrings.ResourceManager.GetString("Op_NotSupported_BinaryPackType", CultureInfo.CurrentCulture);

        return format is null ? null : string.Format(CultureInfo.CurrentCulture, format, typeName);
    }

    /// <summary>A calculation strategy outside the sealed set the binary format enumerates.</summary>
    private sealed class CustomStrategy
        : IDateCalculationStrategy
    {
        /// <inheritdoc />
        public DateOnly? Calculate(int year, StrategyResolutionContext context) =>
            new DateOnly(year, 1, 1);
    }

    /// <summary>A recurrence strategy outside the sealed set the binary format enumerates.</summary>
    private sealed class CustomRecurrence
        : IDateRecurrenceStrategy
    {
        /// <inheritdoc />
        public IEnumerable<DateOnly> GetOccurrences(DateRange range, StrategyResolutionContext context) =>
            [range.StartDate];
    }

    /// <summary>A duration definition outside the sealed set the binary format enumerates.</summary>
    private sealed record CustomDuration
        : NotableDateDurationDefinition;
}
