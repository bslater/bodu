// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ParserValidationTests.JsonSchema.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using System.Reflection;
using System.Text.Json.Nodes;
using System.Xml.Linq;

namespace Bodu.Globalization.Calendar;

public sealed partial class ParserValidationTests
{
    /// <summary>
    /// The JSON pointer into <see cref="BoundsTemplateJson" /> of each integer the bound tests set, keyed by the name
    /// that stands for it.
    /// </summary>
    private static readonly Dictionary<string, string> s_boundFields = new(StringComparer.Ordinal)
    {
        ["rule.priority"] = "/notableDates/0/rules/0/priority",
        ["rule.durationDays"] = "/notableDates/0/rules/0/durationDays",
        ["fixed.day"] = "/notableDates/0/rules/0/strategy/fixed/day",
        ["applicability.everyYears"] = "/notableDates/0/rules/0/applicability/everyYears",
        ["applicability.onlyYears.item"] = "/notableDates/0/rules/0/applicability/onlyYears/0",
        ["applicability.exceptYears.item"] = "/notableDates/0/rules/0/applicability/exceptYears/0",
        ["nthWeekdayFromRule.ordinal"] = "/notableDates/0/rules/1/strategy/nthWeekdayFromRule/ordinal",
        ["workingDayInMonth.ordinal"] = "/notableDates/0/rules/2/strategy/workingDayInMonth/ordinal",
        ["ordinalDayOfMonth.ordinal"] = "/notableDates/0/rules/3/strategy/ordinalDayOfMonth/ordinal",
        ["dayOfYear.ordinal"] = "/notableDates/0/rules/4/strategy/dayOfYear/ordinal",
    };

    /// <summary>
    /// The JSON document under the bound tests, which loads and which the companion JSON schema accepts, holding one of
    /// each integer the bound tests set.
    /// </summary>
    private const string BoundsTemplateJson = """
        { "schemaVersion": "1.0", "resourceId": "test.bounds",
          "notableDates": [ { "id": "anchor", "displayName": "Anchor", "category": "Observance",
            "rules": [
              { "id": "r", "priority": 1, "durationDays": 1, "applicability": { "everyYears": 1, "anchorYear": 2000, "onlyYears": [ 2026 ], "exceptYears": [ 2027 ] },
                "strategy": { "fixed": { "month": 3, "day": 1 } } },
              { "id": "nth", "strategy": { "nthWeekdayFromRule": { "notableDateRef": "anchor", "ruleRef": "r", "dayOfWeek": "Sunday", "ordinal": 2 } } },
              { "id": "working", "strategy": { "workingDayInMonth": { "month": 6, "ordinal": 1 } } },
              { "id": "ordinal-day", "strategy": { "ordinalDayOfMonth": { "month": 7, "ordinal": 2 } } },
              { "id": "day-of-year", "strategy": { "dayOfYear": { "ordinal": 100 } } } ] } ] }
        """;

    /// <summary>
    /// Verifies that the bound test document loads and that the companion JSON schema accepts it, so each verdict the
    /// per-value test compares is caused by the one integer it sets.
    /// </summary>
    [TestMethod]
    public void LoadJson_WhenEveryIntegerIsWithinItsBounds_ShouldLoadAndPassTheJsonSchema()
    {
        string json = BoundFieldJson(null, null);

        NotableDateResource resource = NotableDateResourceLoader.LoadJson(json);
        IReadOnlyList<string> errors = JsonSchemaValidator.Validate(CompanionJsonSchema(), JsonNode.Parse(json));

        Assert.AreEqual("test.bounds", resource.ResourceId);
        Assert.IsEmpty(errors, string.Join(" | ", errors));
    }

    /// <summary>
    /// Verifies that the companion JSON schema accepts a JSON integer at or beyond a bound of the range the loader holds
    /// it to exactly when <c>LoadJson</c> loads the document, so the schema states the loader's bounds: an ordinal
    /// excludes zero, <c>everyYears</c> has no upper bound short of a 32-bit integer, a year in a list is held to 0 to
    /// 65535, and every other integer to the 32-bit range the loader reads.
    /// </summary>
    /// <param name="field">The name of the integer.</param>
    /// <param name="literal">The integer, as a JSON literal.</param>
    [TestMethod]
    [DataRow("rule.priority", "-2147483649")]
    [DataRow("rule.priority", "-2147483648")]
    [DataRow("rule.priority", "2147483647")]
    [DataRow("rule.priority", "2147483648")]
    [DataRow("rule.durationDays", "2147483647")]
    [DataRow("rule.durationDays", "2147483648")]
    [DataRow("applicability.everyYears", "0")]
    [DataRow("applicability.everyYears", "1")]
    [DataRow("applicability.everyYears", "65535")]
    [DataRow("applicability.everyYears", "65536")]
    [DataRow("applicability.everyYears", "2147483647")]
    [DataRow("applicability.everyYears", "2147483648")]
    [DataRow("applicability.onlyYears.item", "-1")]
    [DataRow("applicability.onlyYears.item", "0")]
    [DataRow("applicability.onlyYears.item", "65535")]
    [DataRow("applicability.onlyYears.item", "65536")]
    [DataRow("applicability.exceptYears.item", "-1")]
    [DataRow("applicability.exceptYears.item", "0")]
    [DataRow("applicability.exceptYears.item", "65535")]
    [DataRow("applicability.exceptYears.item", "70000")]
    [DataRow("nthWeekdayFromRule.ordinal", "-367")]
    [DataRow("nthWeekdayFromRule.ordinal", "-366")]
    [DataRow("nthWeekdayFromRule.ordinal", "-1")]
    [DataRow("nthWeekdayFromRule.ordinal", "0")]
    [DataRow("nthWeekdayFromRule.ordinal", "1")]
    [DataRow("nthWeekdayFromRule.ordinal", "366")]
    [DataRow("nthWeekdayFromRule.ordinal", "367")]
    [DataRow("workingDayInMonth.ordinal", "0")]
    [DataRow("ordinalDayOfMonth.ordinal", "0")]
    [DataRow("dayOfYear.ordinal", "0")]
    [DataRow("dayOfYear.ordinal", "-1")]
    public void LoadJson_WhenIntegerIsAtOrBeyondItsBound_ShouldLoadExactlyWhenTheJsonSchemaAcceptsIt(string field, string literal)
    {
        string json = BoundFieldJson(field, literal);

        bool loaded = NotableDateResourceLoader.TryLoadJson(json, static _ => null, out _, out IReadOnlyList<NotableDateValidationDiagnostic> diagnostics);
        IReadOnlyList<string> errors = JsonSchemaValidator.Validate(CompanionJsonSchema(), JsonNode.Parse(json));

        Assert.AreEqual(
            loaded,
            errors.Count == 0,
            "LoadJson " + (loaded ? "loaded" : "rejected") + " " + field + " " + literal + string.Concat(diagnostics.Select(d => "; " + d.Code + ": " + d.Message)) + "; the schema reported: " + string.Join(" | ", errors));
    }

    /// <summary>The namespace of the XML Schema language.</summary>
    private static readonly XNamespace s_xs = "http://www.w3.org/2001/XMLSchema";

    /// <summary>
    /// The definition of the companion JSON schema each XSD complex type stands for, as a path of property names from
    /// the schema's root; an empty path is the document's root object.
    /// </summary>
    private static readonly Dictionary<string, string[]> s_xsdTypeDefinitions = new(StringComparer.Ordinal)
    {
        ["NotableDateResource"] = [],
        ["Metadata"] = ["properties", "metadata"],
        ["ResolutionPolicy"] = ["$defs", "resolutionPolicy"],
        ["AdjustmentPolicy"] = ["$defs", "adjustmentPolicy"],
        ["AdjustmentScope"] = ["$defs", "adjustmentScope"],
        ["AdjustmentTrigger"] = ["$defs", "adjustmentPolicy", "properties", "trigger"],
        ["AdjustmentAction"] = ["$defs", "adjustmentPolicy", "properties", "action"],
        ["AdjustmentEmission"] = ["$defs", "adjustmentPolicy", "properties", "emission"],
        ["Import"] = ["$defs", "import"],
        ["ImportUse"] = ["$defs", "importUse"],
        ["NotableDate"] = ["$defs", "notableDate"],
        ["Rule"] = ["$defs", "rule"],
        ["Applicability"] = ["$defs", "applicability"],
        ["Strategy"] = ["$defs", "strategy"],
        ["Recurrence"] = ["$defs", "recurrence"],
        ["Duration"] = ["$defs", "duration"],
        ["UntilDate"] = ["$defs", "duration", "properties", "untilDate"],
        ["FixedStrategy"] = ["$defs", "fixedStrategy"],
        ["DayOfWeekInMonthStrategy"] = ["$defs", "dayOfWeekInMonthStrategy"],
        ["WeekdayNearDateStrategy"] = ["$defs", "weekdayNearDateStrategy"],
        ["RelativeWeekdayInMonthStrategy"] = ["$defs", "relativeWeekdayInMonthStrategy"],
        ["OffsetFromRuleStrategy"] = ["$defs", "offsetFromRuleStrategy"],
        ["AlgorithmStrategy"] = ["$defs", "algorithmStrategy"],
        ["WeekdayNearRuleStrategy"] = ["$defs", "weekdayNearRuleStrategy"],
        ["NthWeekdayFromRuleStrategy"] = ["$defs", "nthWeekdayFromRuleStrategy"],
        ["WorkingDayOffsetFromRuleStrategy"] = ["$defs", "workingDayOffsetFromRuleStrategy"],
        ["WorkingDayInMonthStrategy"] = ["$defs", "workingDayInMonthStrategy"],
        ["OrdinalDayOfMonthStrategy"] = ["$defs", "ordinalDayOfMonthStrategy"],
        ["IsoWeekDateStrategy"] = ["$defs", "isoWeekDateStrategy"],
        ["DayOfYearStrategy"] = ["$defs", "dayOfYearStrategy"],
        ["DailyIntervalRecurrence"] = ["$defs", "dailyIntervalRecurrence"],
        ["WeeklyRecurrence"] = ["$defs", "weeklyRecurrence"],
        ["MonthlyDayRecurrence"] = ["$defs", "monthlyDayRecurrence"],
        ["MonthlyWeekdayRecurrence"] = ["$defs", "monthlyWeekdayRecurrence"],
        ["RuleRef"] = ["$defs", "scopeRuleRef"],
    };

    /// <summary>
    /// The XSD complex types that JSON writes as a list or a map in the property of the type that holds them, or as
    /// one of the operations of an override, rather than as a definition of their own.
    /// </summary>
    private static readonly HashSet<string> s_xsdInlineTypes = new(StringComparer.Ordinal)
    {
        "AdjustmentPolicies", "CategoryPrecedence", "HandlerParameters", "HandlerParam", "NotableDates", "Imports",
        "Tags", "Tag", "Rules", "RuleAdjustments", "AdjustmentRef", "Overrides", "AddRule", "PatchRule",
        "TerritoryRef", "CalendarRef", "CategoryRef", "NotableDateScopeRef", "WeekdayRef", "YearRef",
    };

    /// <summary>
    /// The JSON property an XSD child element is read from where it is not the element's name with its first letter
    /// in lowercase, keyed by the element's name, or by its type's and its name where the type decides.
    /// </summary>
    private static readonly Dictionary<string, string> s_xsdElementProperties = new(StringComparer.Ordinal)
    {
        ["Source"] = "sources",
        ["Territory"] = "territories",
        ["Calendar"] = "calendars",
        ["Category"] = "categories",
        ["OnlyYear"] = "onlyYears",
        ["ExceptYear"] = "exceptYears",
        ["Weekday"] = "weekdays",
        ["Use"] = "uses",
        ["Day"] = "days",
        ["AdjustmentScope/NotableDate"] = "notableDateRefs",
        ["AdjustmentScope/Rule"] = "ruleRefs",
    };

    /// <summary>
    /// The JSON documents under the member tests, each using a member the loader reads that a smaller document lacks,
    /// keyed by the name that stands for the member.
    /// </summary>
    private static readonly Dictionary<string, string> s_jsonSchemaScenarios = new(StringComparer.Ordinal)
    {
        ["imports"] = ScenarioJson(root: """, "imports": [ { "resource": "lib", "uses": [ { "notableDateRef": "lib-day", "as": "alias", "territory": "AU-NSW", "category": "Observance", "nonWorking": true, "adjustments": [ "policy" ] } ] } ]"""),
        ["calendar.Gregorian"] = ScenarioJson(rule: """ "applicability": { "calendar": "Gregorian" }, """),
        ["calendar.Hijri"] = ScenarioJson(rule: """ "applicability": { "calendar": "Hijri" }, """),
        ["calendar.UmmAlQura"] = ScenarioJson(rule: """ "applicability": { "calendar": "UmmAlQura" }, """),
        ["calendar.Hebrew"] = ScenarioJson(rule: """ "applicability": { "calendar": "Hebrew" }, """),
        ["calendar.Persian"] = ScenarioJson(rule: """ "applicability": { "calendar": "Persian" }, """),
        ["calendar.ChineseLunisolar"] = ScenarioJson(rule: """ "applicability": { "calendar": "ChineseLunisolar" }, """),
        ["scope.calendars"] = ScenarioJson(policy: """, "scope": { "calendars": [ "Gregorian", "Hijri", "UmmAlQura", "Hebrew", "Persian", "ChineseLunisolar" ] }"""),
        ["scope.years"] = ScenarioJson(policy: """, "scope": { "fromYear": 2000, "toYear": 2100, "onlyYears": [ 2026 ], "exceptYears": [ 2027 ] }"""),
        ["trigger.IfLeapYear"] = ScenarioJson(trigger: """ "type": "IfLeapYear" """),
        ["trigger.IfBeforeFixedDate"] = ScenarioJson(trigger: """ "type": "IfBeforeFixedDate", "month": "June", "day": 15 """),
        ["trigger.IfAfterFixedDate"] = ScenarioJson(trigger: """ "type": "IfAfterFixedDate", "month": 6, "day": 15 """),
        ["trigger.IfNthOccurrenceInMonth"] = ScenarioJson(trigger: """ "type": "IfNthOccurrenceInMonth", "weekOrdinal": "Second" """),
        ["trigger.Custom"] = ScenarioJson(trigger: """ "type": "Custom", "handlerKey": "custom-trigger" """, policy: """, "parameters": { "text": "a", "number": 5, "flag": true }"""),
        ["action.ReplaceWithRule"] = ScenarioJson(action: """ "type": "ReplaceWithRule", "notableDateRef": "anchor", "ruleRef": "s" """),
        ["action.Custom"] = ScenarioJson(action: """ "type": "Custom", "handlerKey": "custom-action" """, policy: """, "parameters": { "text": "a" }"""),
        ["patch"] = ScenarioJson(overrides: """, "overrides": [ { "operation": "PatchRule", "notableDateRef": "anchor", "ruleRef": "s", "comment": "Patched.", "applicability": { "calendar": "Gregorian", "territories": [ "NZ" ] }, "tags": [ "patched" ], "adjustments": [ "policy" ] } ]"""),
    };

    /// <summary>
    /// Verifies that the companion JSON schema lists, for each enumeration the XSD declares, the names the XSD lists,
    /// so a document naming any member the loader reads passes the schema; the calendar names, the adjustment triggers
    /// and the adjustment actions among them.
    /// </summary>
    [TestMethod]
    public void LoadJson_ForEveryXsdEnumeration_ShouldAcceptTheNamesTheJsonSchemaAccepts()
    {
        JsonObject definitions = CompanionJsonSchema()["$defs"]!.AsObject();
        List<string> mismatches = new();

        foreach (XElement simpleType in Xsd().Root!.Elements(s_xs + "simpleType"))
        {
            string[] expected = [.. simpleType.Descendants(s_xs + "enumeration").Select(facet => (string)facet.Attribute("value")!)];
            if (expected.Length == 0)
                continue;

            string name = (string)simpleType.Attribute("name")!;
            string[] actual = definitions[name]?["enum"] is JsonArray names ? [.. names.Select(item => item!.GetValue<string>())] : [];
            if (!expected.SequenceEqual(actual))
                mismatches.Add(name + ": the XSD lists " + string.Join(", ", expected) + "; the JSON schema lists " + string.Join(", ", actual));
        }

        Assert.IsEmpty(mismatches, string.Join(" | ", mismatches));
    }

    /// <summary>
    /// Verifies that each definition of the companion JSON schema holds the properties that the XSD complex type it
    /// stands for declares as attributes and child elements, no more and no fewer, so a document using any member the
    /// loader reads passes the schema; an override holds those of the three operations, and its <c>operation</c>.
    /// </summary>
    [TestMethod]
    public void LoadJson_ForEveryXsdComplexType_ShouldReadThePropertiesTheJsonSchemaDeclares()
    {
        JsonObject schema = CompanionJsonSchema();
        XDocument xsd = Xsd();
        List<string> mismatches = new();

        foreach (string type in xsd.Root!.Elements(s_xs + "complexType").Select(element => (string)element.Attribute("name")!))
        {
            if (!s_xsdTypeDefinitions.ContainsKey(type) && !s_xsdInlineTypes.Contains(type))
                mismatches.Add(type + ": the XSD type has no JSON schema definition");
        }

        foreach ((string type, string[] path) in s_xsdTypeDefinitions)
            CompareProperties(type, XsdProperties(xsd, type), JsonSchemaProperties(schema, path), mismatches);

        CompareProperties(
            "override",
            [.. XsdProperties(xsd, "AddRule").Union(XsdProperties(xsd, "PatchRule")).Union(XsdProperties(xsd, "RuleRef")).Append("operation")],
            JsonSchemaProperties(schema, ["$defs", "override"]),
            mismatches);

        Assert.IsEmpty(mismatches, string.Join(" | ", mismatches));
    }

    /// <summary>
    /// Verifies that a JSON document using a member the loader reads, such as an import, a calendar other than the
    /// Gregorian, a trigger's or an action's refinements, handler parameters, a scope's year bounds, or a patch's
    /// applicability, tags and adjustments, loads and passes the companion JSON schema.
    /// </summary>
    /// <param name="scenario">The name of the member the document uses.</param>
    [TestMethod]
    [DataRow("imports")]
    [DataRow("calendar.Gregorian")]
    [DataRow("calendar.Hijri")]
    [DataRow("calendar.UmmAlQura")]
    [DataRow("calendar.Hebrew")]
    [DataRow("calendar.Persian")]
    [DataRow("calendar.ChineseLunisolar")]
    [DataRow("scope.calendars")]
    [DataRow("scope.years")]
    [DataRow("trigger.IfLeapYear")]
    [DataRow("trigger.IfBeforeFixedDate")]
    [DataRow("trigger.IfAfterFixedDate")]
    [DataRow("trigger.IfNthOccurrenceInMonth")]
    [DataRow("trigger.Custom")]
    [DataRow("action.ReplaceWithRule")]
    [DataRow("action.Custom")]
    [DataRow("patch")]
    public void LoadJson_WhenDocumentUsesMember_ShouldLoadAndPassTheJsonSchema(string scenario)
    {
        string json = s_jsonSchemaScenarios[scenario];

        bool loaded = NotableDateResourceLoader.TryLoadJson(json, EnumImportResolver, out _, out IReadOnlyList<NotableDateValidationDiagnostic> diagnostics);
        IReadOnlyList<string> errors = JsonSchemaValidator.Validate(CompanionJsonSchema(), JsonNode.Parse(json));

        Assert.IsTrue(loaded, string.Join(" | ", diagnostics.Select(d => d.Code + ": " + d.Message)));
        Assert.IsEmpty(errors, string.Join(" | ", errors));
    }

    /// <summary>
    /// Verifies that a <c>PatchRule</c> override carrying both a strategy and a recurrence, which the loader rejects
    /// with <c>BODU-CAL-RULE-SOURCE</c>, fails the companion JSON schema too.
    /// </summary>
    [TestMethod]
    public void LoadJson_WhenPatchRuleCarriesStrategyAndRecurrence_ShouldFailTheJsonSchema()
    {
        string json = ScenarioJson(overrides: """, "overrides": [ { "operation": "PatchRule", "notableDateRef": "anchor", "ruleRef": "s", "strategy": { "fixed": { "month": 5, "day": 1 } }, "recurrence": { "monthlyDay": { "dayOfMonth": 1 } } } ]""");

        bool loaded = NotableDateResourceLoader.TryLoadJson(json, EnumImportResolver, out _, out _);
        IReadOnlyList<string> errors = JsonSchemaValidator.Validate(CompanionJsonSchema(), JsonNode.Parse(json));

        Assert.IsFalse(loaded, "LoadJson loaded the patch.");
        Assert.IsNotEmpty(errors, "The JSON schema accepted the patch.");
    }

    /// <summary>
    /// Verifies that each notable-date pack the library and its regional data bundles embed, written in the JSON form
    /// the loader reads, passes the companion JSON schema, imports and calendars other than the Gregorian included.
    /// </summary>
    /// <param name="pack">The pack, as its assembly's name and its resource name.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DynamicData(nameof(BundledPacks))]
    public void LoadJson_WhenBundledPackIsWrittenAsJson_ShouldPassTheJsonSchema(string pack)
    {
        string json = NotableDateJsonTranscoder.Transcode(BundledPackXml(pack));

        IReadOnlyList<string> errors = JsonSchemaValidator.Validate(CompanionJsonSchema(), JsonNode.Parse(json));

        Assert.IsEmpty(errors, string.Join(" | ", errors));
    }

    /// <summary>
    /// Verifies that each notable-date pack the library and its regional data bundles embed, written in the JSON form
    /// the loader reads, loads into the resource its XML form loads into, compared byte for byte as binary rule packs,
    /// so the documents the schema test holds to the schema are the packs themselves.
    /// </summary>
    /// <param name="pack">The pack, as its assembly's name and its resource name.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DynamicData(nameof(BundledPacks))]
    public void LoadJson_WhenBundledPackIsWrittenAsJson_ShouldLoadWhatLoadLoads(string pack)
    {
        string xml = BundledPackXml(pack);

        NotableDateResource loaded = NotableDateResourceLoader.Load(xml, ResolveBundledPack);
        NotableDateResource loadedJson = NotableDateResourceLoader.LoadJson(NotableDateJsonTranscoder.Transcode(xml), ResolveBundledPack);

        CollectionAssert.AreEqual(SchemaPackBytes(loaded), SchemaPackBytes(loadedJson), pack);
    }

    /// <summary>
    /// Gets every notable-date pack the library and its regional data bundles embed, as its assembly's name and its
    /// resource name.
    /// </summary>
    /// <value>One row per pack.</value>
    public static IEnumerable<object[]> BundledPacks =>
        BundledPackAssemblies()
            .SelectMany(assembly => assembly.GetManifestResourceNames()
                .Where(name => name.StartsWith(BundledPackPrefix, StringComparison.Ordinal) && name.EndsWith(".xml", StringComparison.Ordinal))
                .Select(name => new object[] { assembly.GetName().Name + "/" + name[BundledPackPrefix.Length..^".xml".Length] }))
            .OrderBy(row => (string)row[0], StringComparer.Ordinal);

    /// <summary>
    /// Gets the prefix of the manifest resource name of every embedded notable-date pack.
    /// </summary>
    private static string BundledPackPrefix =>
        "Bodu.Globalization.Calendar.Resources.";

    /// <summary>
    /// Returns the assemblies that embed notable-date packs: the library and its regional data bundles.
    /// </summary>
    /// <returns>The assemblies.</returns>
    private static Assembly[] BundledPackAssemblies() =>
    [
        typeof(NotableDateResourceLoader).Assembly,
        typeof(AfricaCalendarData).Assembly,
        typeof(AmericasCalendarData).Assembly,
        typeof(AsiaPacificCalendarData).Assembly,
        typeof(EuropeCalendarData).Assembly,
        typeof(MiddleEastCalendarData).Assembly,
    ];

    /// <summary>
    /// Returns the XML of an embedded notable-date pack.
    /// </summary>
    /// <param name="pack">The pack, as its assembly's name and its resource name.</param>
    /// <returns>The XML document.</returns>
    private static string BundledPackXml(string pack)
    {
        string assemblyName = pack[..pack.IndexOf('/', StringComparison.Ordinal)];
        Assembly assembly = BundledPackAssemblies().Single(candidate => candidate.GetName().Name == assemblyName);

        using Stream stream = assembly.GetManifestResourceStream(BundledPackPrefix + pack[(assemblyName.Length + 1)..] + ".xml")!;
        using StreamReader reader = new(stream);
        return reader.ReadToEnd();
    }

    /// <summary>
    /// Resolves an imported resource to the XML of the embedded pack that bears its name, in whichever assembly embeds
    /// it.
    /// </summary>
    /// <param name="name">The imported resource's name.</param>
    /// <returns>The XML document, or <see langword="null" /> when no assembly embeds the name.</returns>
    private static string? ResolveBundledPack(string name)
    {
        foreach (Assembly assembly in BundledPackAssemblies())
        {
            using Stream? stream = assembly.GetManifestResourceStream(BundledPackPrefix + name + ".xml");
            if (stream is not null)
            {
                using StreamReader reader = new(stream);
                return reader.ReadToEnd();
            }
        }

        return null;
    }

    /// <summary>
    /// Builds a JSON document with one adjustment policy and one concept of two rules, <c>r</c> and <c>s</c>, with the
    /// supplied members added.
    /// </summary>
    /// <param name="root">Members appended to the root object, each preceded by a comma.</param>
    /// <param name="trigger">The trigger's members.</param>
    /// <param name="action">The action's members.</param>
    /// <param name="policy">Members appended to the policy, each preceded by a comma.</param>
    /// <param name="rule">Members inserted at the start of rule <c>r</c>, each followed by a comma.</param>
    /// <param name="overrides">Members appended to the root object after the concepts, each preceded by a comma.</param>
    /// <returns>The JSON document.</returns>
    private static string ScenarioJson(string root = "", string trigger = """ "type": "IfWeekend" """, string action = """ "type": "MoveToNextWorkingDay" """, string policy = "", string rule = "", string overrides = "") =>
        $$"""
        { "schemaVersion": "1.0", "resourceId": "test.scenario"{{root}},
          "adjustmentPolicies": [ { "id": "policy", "trigger": { {{trigger}} }, "action": { {{action}} }, "emission": { "mode": "ObservedOnly" }{{policy}} } ],
          "notableDates": [ { "id": "anchor", "displayName": "Anchor", "category": "Observance",
            "rules": [
              { "id": "r", {{rule}} "strategy": { "fixed": { "month": 3, "day": 1 } }, "adjustments": [ "policy" ] },
              { "id": "s", "strategy": { "fixed": { "month": 4, "day": 1 } } } ] } ]{{overrides}} }
        """;

    /// <summary>
    /// Returns the notable-date XSD embedded in the loader's assembly.
    /// </summary>
    /// <returns>The XSD document.</returns>
    private static XDocument Xsd()
    {
        using Stream? stream = typeof(NotableDateResourceLoader).Assembly.GetManifestResourceStream("Bodu.Globalization.Calendar.NotableDates.xsd");
        Assert.IsNotNull(stream);

        return XDocument.Load(stream);
    }

    /// <summary>
    /// Returns the names of the JSON properties an XSD complex type's attributes and child elements are read from.
    /// </summary>
    /// <param name="xsd">The XSD document.</param>
    /// <param name="type">The complex type's name.</param>
    /// <returns>The property names.</returns>
    private static string[] XsdProperties(XDocument xsd, string type)
    {
        XElement complexType = xsd.Root!.Elements(s_xs + "complexType").Single(element => (string)element.Attribute("name")! == type);

        // An anonymous type nested in the type, such as a weekly recurrence's Day, declares the attributes of its own
        // element rather than the type's.
        IEnumerable<string> attributes = complexType.Descendants(s_xs + "attribute")
            .Where(attribute => attribute.Ancestors(s_xs + "complexType").First() == complexType)
            .Select(attribute => (string)attribute.Attribute("name")!);
        IEnumerable<string> elements = complexType.Descendants(s_xs + "element")
            .Where(element => element.Ancestors(s_xs + "complexType").First() == complexType)
            .Select(element => (string)element.Attribute("name")!)
            .Select(name => s_xsdElementProperties.GetValueOrDefault(type + "/" + name) ?? s_xsdElementProperties.GetValueOrDefault(name) ?? char.ToLowerInvariant(name[0]) + name[1..]);

        return [.. attributes.Concat(elements).Distinct()];
    }

    /// <summary>
    /// Returns the names of the properties a definition of the JSON schema declares.
    /// </summary>
    /// <param name="schema">The schema's root object.</param>
    /// <param name="path">The definition's path of property names from the root.</param>
    /// <returns>The property names; empty when the definition is absent or declares none.</returns>
    private static string[] JsonSchemaProperties(JsonObject schema, string[] path)
    {
        JsonNode? definition = schema;
        foreach (string name in path)
            definition = definition?[name];

        return definition?["properties"] is JsonObject properties ? [.. properties.Select(property => property.Key)] : [];
    }

    /// <summary>
    /// Records a difference between the properties an XSD type declares and those its JSON schema definition declares.
    /// </summary>
    /// <param name="type">The type's name.</param>
    /// <param name="expected">The properties the XSD declares.</param>
    /// <param name="actual">The properties the JSON schema declares.</param>
    /// <param name="mismatches">The list that receives a description of the difference.</param>
    private static void CompareProperties(string type, string[] expected, string[] actual, List<string> mismatches)
    {
        string[] missing = [.. expected.Except(actual, StringComparer.Ordinal).Order(StringComparer.Ordinal)];
        string[] extra = [.. actual.Except(expected, StringComparer.Ordinal).Order(StringComparer.Ordinal)];
        if (missing.Length > 0 || extra.Length > 0)
            mismatches.Add(type + ": the JSON schema lacks " + string.Join(", ", missing) + "; it adds " + string.Join(", ", extra));
    }

    /// <summary>
    /// The JSON pointer into <see cref="DatesTemplateJson" /> of each date the date tests set, keyed by the name that
    /// stands for it.
    /// </summary>
    private static readonly Dictionary<string, string> s_dateFields = new(StringComparer.Ordinal)
    {
        ["dailyInterval.anchorDate"] = "/notableDates/0/rules/0/recurrence/dailyInterval/anchorDate",
        ["weekly.anchorDate"] = "/notableDates/0/rules/1/recurrence/weekly/anchorDate",
        ["monthlyDay.anchorDate"] = "/notableDates/0/rules/2/recurrence/monthlyDay/anchorDate",
        ["monthlyWeekday.anchorDate"] = "/notableDates/0/rules/3/recurrence/monthlyWeekday/anchorDate",
    };

    /// <summary>
    /// The JSON document under the date tests, which loads and which the companion JSON schema accepts, holding the
    /// anchor date of each recurrence kind.
    /// </summary>
    private const string DatesTemplateJson = """
        { "schemaVersion": "1.0", "resourceId": "test.dates",
          "notableDates": [ { "id": "anchor", "displayName": "Anchor", "category": "Observance",
            "rules": [
              { "id": "daily", "recurrence": { "dailyInterval": { "anchorDate": "2024-01-01", "intervalDays": 7 } } },
              { "id": "weekly", "recurrence": { "weekly": { "days": [ "Monday" ], "anchorDate": "2024-01-01" } } },
              { "id": "monthly-day", "recurrence": { "monthlyDay": { "dayOfMonth": 1, "anchorDate": "2024-01-01" } } },
              { "id": "monthly-weekday", "recurrence": { "monthlyWeekday": { "dayOfWeek": "Monday", "weekOrdinal": "First", "anchorDate": "2024-01-01" } } } ] } ] }
        """;

    /// <summary>
    /// Verifies that the date test document loads and that the companion JSON schema accepts it, so each verdict the
    /// per-value test compares is caused by the one date it sets.
    /// </summary>
    [TestMethod]
    public void LoadJson_WhenEveryAnchorDateIsWellFormed_ShouldLoadAndPassTheJsonSchema()
    {
        string json = FieldJson(DatesTemplateJson, s_dateFields, null, null);

        NotableDateResource resource = NotableDateResourceLoader.LoadJson(json);
        IReadOnlyList<string> errors = JsonSchemaValidator.Validate(CompanionJsonSchema(), JsonNode.Parse(json));

        Assert.AreEqual("test.dates", resource.ResourceId);
        Assert.IsEmpty(errors, string.Join(" | ", errors));
    }

    /// <summary>
    /// Verifies that the companion JSON schema accepts a recurrence's anchor date exactly when <c>LoadJson</c> loads the
    /// document, so a date not written as <c>yyyy-MM-dd</c>, which the loader rejects, fails the schema too, whether or
    /// not the validator asserts <c>format</c>.
    /// </summary>
    /// <param name="field">The name of the date.</param>
    /// <param name="literal">The date, as a JSON literal.</param>
    [TestMethod]
    [DataRow("dailyInterval.anchorDate", "\"2024-02-29\"")]
    [DataRow("dailyInterval.anchorDate", "\"2024-1-1\"")]
    [DataRow("dailyInterval.anchorDate", "\"2024-01-1\"")]
    [DataRow("dailyInterval.anchorDate", "\"2024/01/01\"")]
    [DataRow("dailyInterval.anchorDate", "\"20240101\"")]
    [DataRow("dailyInterval.anchorDate", "\" 2024-01-01\"")]
    [DataRow("dailyInterval.anchorDate", "\"2024-01-01T00:00:00\"")]
    [DataRow("weekly.anchorDate", "\"2024-1-1\"")]
    [DataRow("monthlyDay.anchorDate", "\"2024-1-1\"")]
    [DataRow("monthlyWeekday.anchorDate", "\"2024-1-1\"")]
    public void LoadJson_WhenAnchorDateIsWrittenInAnotherForm_ShouldLoadExactlyWhenTheJsonSchemaAcceptsIt(string field, string literal)
    {
        string json = FieldJson(DatesTemplateJson, s_dateFields, field, literal);

        bool loaded = NotableDateResourceLoader.TryLoadJson(json, static _ => null, out _, out IReadOnlyList<NotableDateValidationDiagnostic> diagnostics);
        IReadOnlyList<string> errors = JsonSchemaValidator.Validate(CompanionJsonSchema(), JsonNode.Parse(json));

        Assert.AreEqual(
            loaded,
            errors.Count == 0,
            "LoadJson " + (loaded ? "loaded" : "rejected") + " " + field + " " + literal + string.Concat(diagnostics.Select(d => "; " + d.Code + ": " + d.Message)) + "; the schema reported: " + string.Join(" | ", errors));
    }

    /// <summary>
    /// Verifies that a value draft 2020-12 cannot tell from a valid one, an integer written with a fraction (<c>1.0</c>)
    /// or, where the validator does not assert <c>format</c>, a <c>yyyy-MM-dd</c> date that names no day, passes the
    /// companion JSON schema and fails to load, and that the schema's description names it among the forms the schema
    /// and the loader read differently.
    /// </summary>
    /// <param name="template">The name of the test document: <c>bounds</c> or <c>dates</c>.</param>
    /// <param name="field">The name of the value.</param>
    /// <param name="literal">The value, as a JSON literal.</param>
    /// <param name="described">The text the description names the value's form with.</param>
    [TestMethod]
    [DataRow("bounds", "fixed.day", "1.0", "(1.0)")]
    [DataRow("bounds", "applicability.everyYears", "2.0", "(1.0)")]
    [DataRow("dates", "dailyInterval.anchorDate", "\"2024-02-30\"", "(2024-02-30)")]
    public void LoadJson_WhenValueIsAFormTheJsonSchemaCannotRefuse_ShouldFailToLoadAsTheSchemaDescribes(string template, string field, string literal, string described)
    {
        string json = template == "bounds" ? FieldJson(BoundsTemplateJson, s_boundFields, field, literal) : FieldJson(DatesTemplateJson, s_dateFields, field, literal);
        JsonObject schema = CompanionJsonSchema();

        bool loaded = NotableDateResourceLoader.TryLoadJson(json, static _ => null, out _, out _);
        IReadOnlyList<string> errors = JsonSchemaValidator.Validate(schema, JsonNode.Parse(json));

        Assert.IsFalse(loaded, "LoadJson loaded " + field + " " + literal);
        Assert.IsEmpty(errors, string.Join(" | ", errors));
        Assert.Contains(described, schema["description"]!.GetValue<string>());
    }

    /// <summary>
    /// Returns the companion JSON schema embedded beside the XSD.
    /// </summary>
    /// <returns>The schema's root object.</returns>
    private static JsonObject CompanionJsonSchema()
    {
        using Stream? stream = typeof(NotableDateResourceLoader).Assembly.GetManifestResourceStream("Bodu.Globalization.Calendar.NotableDates.schema.json");
        Assert.IsNotNull(stream);

        return JsonNode.Parse(stream)!.AsObject();
    }

    /// <summary>
    /// Returns <see cref="BoundsTemplateJson" /> with one integer replaced.
    /// </summary>
    /// <param name="field">The name of the integer, or <see langword="null" /> to change none.</param>
    /// <param name="literal">The JSON literal the integer is replaced with.</param>
    /// <returns>The JSON document.</returns>
    private static string BoundFieldJson(string? field, string? literal) =>
        FieldJson(BoundsTemplateJson, s_boundFields, field, literal);

    /// <summary>
    /// Returns a test document with one value replaced.
    /// </summary>
    /// <param name="template">The test document.</param>
    /// <param name="fields">The JSON pointer of each value the document holds, keyed by the name that stands for it.</param>
    /// <param name="field">The name of the value, or <see langword="null" /> to change none.</param>
    /// <param name="literal">The JSON literal the value is replaced with.</param>
    /// <returns>The JSON document.</returns>
    private static string FieldJson(string template, Dictionary<string, string> fields, string? field, string? literal)
    {
        JsonNode root = JsonNode.Parse(template)!;
        if (field is null)
            return root.ToJsonString();

        string[] segments = fields[field].Split('/', StringSplitOptions.RemoveEmptyEntries);
        JsonNode parent = root;
        foreach (string segment in segments[..^1])
            parent = parent is JsonArray array ? array[int.Parse(segment, CultureInfo.InvariantCulture)]! : parent[segment]!;

        string last = segments[^1];
        if (parent is JsonArray items)
            items[int.Parse(last, CultureInfo.InvariantCulture)] = JsonNode.Parse(literal!);
        else
            parent[last] = JsonNode.Parse(literal!);

        return root.ToJsonString();
    }
}
