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
    /// Verifies that an algorithm key a custom registry declares, which the loader accepts with that registry, is
    /// accepted by the overload that takes the registry and written as given.
    /// </summary>
    [TestMethod]
    public void Write_WhenRegistryDeclaresAlgorithmKey_ShouldWrite()
    {
        NotableDateResource resource = SingleRuleResource(
            new NotableDateRule("r", 0, null, null, null, GlobalApplicability, new AlgorithmDateStrategy("no-such-algorithm"), [], []));
        NotableDateAlgorithmRegistry registry = new NotableDateAlgorithmRegistry()
            .Register("no-such-algorithm", new StubAlgorithm());
        using MemoryStream stream = new();

        NotableDateBinaryResource.Write(resource, stream, registry);
        stream.Position = 0;

        Assert.AreEqual("no-such-algorithm", ((AlgorithmDateStrategy)NotableDateBinaryResource.Read(stream).NotableDates[0].Rules[0].Strategy!).Key);
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
    /// Verifies that the numeric test resource writes when every number the loader bounds lies at the edge of its range,
    /// so each rejection the per-field test observes is caused by the one value it moves outside the range.
    /// </summary>
    [TestMethod]
    public void Write_WhenEveryNumericFieldIsAtTheLoadersBounds_ShouldWrite()
    {
        byte[] pack = WritePack(NumericFieldResource(null, 0));

        Assert.AreEqual("test.write", ReadPack(pack).ResourceId);
    }

    /// <summary>
    /// Verifies that writing a resource holding a number outside the range the loader holds its field to throws
    /// <see cref="NotableDateValidationException" /> carrying the loader's <c>BODU-CAL-SCHEMA</c> diagnostic naming the
    /// field and the value, rather than encoding a pack the loader would never have produced.
    /// </summary>
    /// <param name="field">The field holding the value.</param>
    /// <param name="value">The value outside the field's range.</param>
    /// <param name="property">The JSON property the loader names in its diagnostic.</param>
    [TestMethod]
    [DataRow("definition.defaultDurationDays", 0, "defaultDurationDays")]
    [DataRow("definition.defaultDurationDays", -1, "defaultDurationDays")]
    [DataRow("rule.durationDays", 0, "durationDays")]
    [DataRow("applicability.fromYear", -1, "fromYear")]
    [DataRow("applicability.fromYear", 70000, "fromYear")]
    [DataRow("applicability.toYear", 65536, "toYear")]
    [DataRow("applicability.onlyYears", 65536, "onlyYears")]
    [DataRow("applicability.exceptYears", -1, "exceptYears")]
    [DataRow("applicability.everyYears", 0, "everyYears")]
    [DataRow("applicability.anchorYear", 65536, "anchorYear")]
    [DataRow("scope.fromYear", 65536, "fromYear")]
    [DataRow("scope.toYear", -1, "toYear")]
    [DataRow("scope.onlyYears", 70000, "onlyYears")]
    [DataRow("scope.exceptYears", 65536, "exceptYears")]
    [DataRow("policy.maxSearchDays", 0, "maxSearchDays")]
    [DataRow("policy.triggerDay", 0, "day")]
    [DataRow("policy.triggerDay", 40, "day")]
    public void Write_WhenNumericFieldIsOutsideTheLoadersRange_ShouldThrowSchemaDiagnostic(string field, int value, string property)
    {
        NotableDateResource resource = NumericFieldResource(field, value);

        var ex = Assert.ThrowsExactly<NotableDateValidationException>(() =>
        {
            _ = WritePack(resource);
        });

        string named = string.Format(CultureInfo.InvariantCulture, "'{0}' value '{1}'", property, value);
        Assert.Contains(d => d.Code == "BODU-CAL-SCHEMA" && d.Severity == NotableDateValidationSeverity.Error && d.Message.Contains(named, StringComparison.Ordinal), ex.Diagnostics, named);
    }

    /// <summary>
    /// Verifies that writing a resource whose adjustment trigger names a month outside 1 to 12 throws
    /// <see cref="NotableDateValidationException" /> carrying the <c>BODU-CAL-VALUE</c> diagnostic the loader reports
    /// for a trigger month that is not a month.
    /// </summary>
    /// <param name="month">The trigger month.</param>
    [TestMethod]
    [DataRow(0)]
    [DataRow(13)]
    public void Write_WhenTriggerMonthIsNotAMonth_ShouldThrowValueDiagnostic(int month)
    {
        NotableDateResource resource = NumericFieldResource("policy.triggerMonth", month);

        var ex = Assert.ThrowsExactly<NotableDateValidationException>(() =>
        {
            _ = WritePack(resource);
        });

        string named = string.Format(CultureInfo.InvariantCulture, "trigger month value '{0}'", month);
        Assert.Contains(d => d.Code == "BODU-CAL-VALUE" && d.Severity == NotableDateValidationSeverity.Error && d.Message.Contains(named, StringComparison.Ordinal), ex.Diagnostics, named);
    }

    /// <summary>
    /// Verifies that a resource holding a number outside the loader's range is refused before anything is encoded, so
    /// the stream is left empty rather than holding a pack.
    /// </summary>
    [TestMethod]
    public void Write_WhenNumericFieldIsOutsideTheLoadersRange_ShouldWriteNothing()
    {
        NotableDateResource resource = NumericFieldResource("definition.defaultDurationDays", 0);
        using MemoryStream stream = new();

        _ = Assert.ThrowsExactly<NotableDateValidationException>(() =>
        {
            NotableDateBinaryResource.Write(resource, stream);
        });

        Assert.AreEqual(0L, stream.Length);
    }

    /// <summary>
    /// Verifies that the enumeration test resource writes when every enumeration value is one its type defines, so each
    /// rejection the per-field test observes is caused by the one value it replaces.
    /// </summary>
    [TestMethod]
    public void Write_WhenEveryEnumValueIsDefined_ShouldWrite()
    {
        byte[] pack = WritePack(EnumFieldResource(null, 0));

        Assert.AreEqual("test.write", ReadPack(pack).ResourceId);
    }

    /// <summary>
    /// Verifies that writing a resource holding an enumeration value its type does not define throws
    /// <see cref="NotableDateValidationException" /> carrying the loader's <c>BODU-CAL-ENUM</c> diagnostic naming the
    /// field and the value, rather than encoding a pack that cannot be read back or, for a value beyond a byte, one that
    /// reads back as another member.
    /// </summary>
    /// <param name="field">The field holding the value.</param>
    /// <param name="value">The undefined value.</param>
    /// <param name="label">The name the loader gives the field in its diagnostic.</param>
    [TestMethod]
    [DataRow("duplicatePolicy", 99, "duplicatePolicy")]
    [DataRow("sameDayCollisionPolicy", 99, "sameDayCollisionPolicy")]
    [DataRow("spanCollisionPolicy", 99, "spanCollisionPolicy")]
    [DataRow("priorityDirection", 99, "priorityDirection")]
    [DataRow("observedDateRangePolicy", 99, "observedDateRangePolicy")]
    [DataRow("categoryPrecedence", 99, "categoryPrecedence")]
    [DataRow("scope.calendars", 99, "scope calendars")]
    [DataRow("scope.categories", 99, "scope categories")]
    [DataRow("trigger", 99, "trigger type")]
    [DataRow("trigger", 258, "trigger type")]
    [DataRow("triggerWeekdays", 7, "trigger weekdays")]
    [DataRow("action", 99, "action type")]
    [DataRow("actionWeekday", 7, "action dayOfWeek")]
    [DataRow("emission", 99, "emission mode")]
    [DataRow("triggerWeekOrdinal", 99, "trigger weekOrdinal")]
    [DataRow("definition.category", 99, "category")]
    [DataRow("definition.category", 256, "category")]
    [DataRow("rule.category", -1, "category")]
    [DataRow("applicability.calendar", 99, "calendar")]
    [DataRow("untilDate.startBoundary", 99, "untilDate startBoundary")]
    [DataRow("untilDate.endBoundary", 99, "untilDate endBoundary")]
    [DataRow("untilDate.selection", 99, "untilDate selection")]
    public void Write_WhenEnumValueIsUndefined_ShouldThrowEnumDiagnostic(string field, int value, string label)
    {
        NotableDateResource resource = EnumFieldResource(field, value);

        var ex = Assert.ThrowsExactly<NotableDateValidationException>(() =>
        {
            _ = WritePack(resource);
        });

        string named = string.Format(CultureInfo.InvariantCulture, "The {0} value '{1}'", label, value);
        Assert.Contains(d => d.Code == "BODU-CAL-ENUM" && d.Severity == NotableDateValidationSeverity.Error && d.Message.StartsWith(named, StringComparison.Ordinal), ex.Diagnostics, named);
    }

    /// <summary>
    /// Verifies that a resource holding an undefined enumeration value is refused before anything is encoded, so the
    /// stream is left empty rather than holding a pack.
    /// </summary>
    [TestMethod]
    public void Write_WhenEnumValueIsUndefined_ShouldWriteNothing()
    {
        NotableDateResource resource = EnumFieldResource("trigger", 258);
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
    /// Builds a resource holding every number the loader bounds, each at the edge of its range except the one named,
    /// which holds the supplied value.
    /// </summary>
    /// <param name="field">The field to set, or <see langword="null" /> to set none.</param>
    /// <param name="value">The value the named field holds.</param>
    /// <returns>The resource.</returns>
    private static NotableDateResource NumericFieldResource(string? field, int value)
    {
        int Value(string name, int valid) =>
            name == field ? value : valid;

        AdjustmentPolicy policy = new(
            "observed",
            0,
            new AdjustmentScope(
                [],
                [],
                [],
                [],
                [],
                fromYear: Value("scope.fromYear", 0),
                toYear: Value("scope.toYear", 65535),
                onlyYears: [Value("scope.onlyYears", 65535)],
                exceptYears: [Value("scope.exceptYears", 0)]),
            AdjustmentTrigger.IfWeekend,
            [],
            AdjustmentAction.MoveToNextWorkingDay,
            null,
            0,
            Value("policy.maxSearchDays", 1),
            false,
            false,
            EmissionMode.ObservedOnly,
            null,
            null,
            triggerMonth: Value("policy.triggerMonth", 12),
            triggerDay: Value("policy.triggerDay", 31));

        NotableDateRule rule = new(
            "r",
            0,
            null,
            null,
            new FixedDurationDefinition(Value("rule.durationDays", 1)),
            new RuleApplicability(
                CalendarSystem.Gregorian,
                Value("applicability.fromYear", 0),
                Value("applicability.toYear", 65535),
                [],
                [Value("applicability.onlyYears", 65535)],
                [Value("applicability.exceptYears", 0)],
                everyYears: Value("applicability.everyYears", 1),
                anchorYear: Value("applicability.anchorYear", 65535)),
            new FixedDateStrategy(3, 1),
            ["observed"],
            []);

        return new NotableDateResource(
            "test.write",
            "1.0",
            ResolutionPolicy.Default,
            [policy],
            [new NotableDateDefinition("anchor", "Anchor", NotableDateCategory.PublicHoliday, true, Value("definition.defaultDurationDays", 1), [], [rule])]);
    }

    /// <summary>
    /// Builds a resource holding every enumeration value the binary format writes outside a strategy or recurrence, each
    /// one its type defines except the one named, which holds the supplied value.
    /// </summary>
    /// <param name="field">The field to set, or <see langword="null" /> to set none.</param>
    /// <param name="value">The value the named field holds.</param>
    /// <returns>The resource.</returns>
    private static NotableDateResource EnumFieldResource(string? field, int value)
    {
        TEnum Value<TEnum>(string name, TEnum valid)
            where TEnum : struct, Enum =>
            name == field ? (TEnum)Enum.ToObject(typeof(TEnum), value) : valid;

        ResolutionPolicy resolutionPolicy = new(
            Value("duplicatePolicy", DuplicatePolicy.KeepFirst),
            Value("sameDayCollisionPolicy", CollisionPolicy.HighestPriorityOnly),
            Value("spanCollisionPolicy", CollisionPolicy.KeepAll),
            Value("priorityDirection", PriorityDirection.LowerWins),
            Value("observedDateRangePolicy", ObservedDateRangePolicy.ObservedOccurrenceControlsInclusion),
            null,
            [Value("categoryPrecedence", NotableDateCategory.PublicHoliday)]);

        AdjustmentPolicy policy = new(
            "observed",
            0,
            new AdjustmentScope(
                [],
                [Value("scope.calendars", CalendarSystem.Gregorian)],
                [Value("scope.categories", NotableDateCategory.PublicHoliday)],
                [],
                []),
            Value("trigger", AdjustmentTrigger.IfWeekend),
            [Value("triggerWeekdays", DayOfWeek.Saturday)],
            Value("action", AdjustmentAction.MoveToNextWorkingDay),
            Value("actionWeekday", DayOfWeek.Monday),
            0,
            null,
            false,
            false,
            Value("emission", EmissionMode.ObservedOnly),
            null,
            null,
            triggerWeekOrdinal: Value("triggerWeekOrdinal", WeekOrdinal.First));

        NotableDateRule rule = new(
            "r",
            0,
            Value("rule.category", NotableDateCategory.Observance),
            null,
            new CalculatedEndDateDurationDefinition(
                new FixedDateStrategy(3, 5),
                Value("untilDate.startBoundary", DateBoundary.Inclusive),
                Value("untilDate.endBoundary", DateBoundary.Exclusive),
                Value("untilDate.selection", EndDateSelection.FirstOnOrAfterStart)),
            new RuleApplicability(Value("applicability.calendar", CalendarSystem.Gregorian), null, null, [], [], []),
            new FixedDateStrategy(3, 1),
            ["observed"],
            []);

        return new NotableDateResource(
            "test.write",
            "1.0",
            resolutionPolicy,
            [policy],
            [new NotableDateDefinition("anchor", "Anchor", Value("definition.category", NotableDateCategory.PublicHoliday), true, 1, [], [rule])]);
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

    /// <summary>An algorithm a custom registry declares under a key that is not built in.</summary>
    private sealed class StubAlgorithm
        : INotableDateAlgorithm
    {
        /// <inheritdoc />
        public DateOnly? Calculate(int year) =>
            new DateOnly(year, 5, 5);
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
