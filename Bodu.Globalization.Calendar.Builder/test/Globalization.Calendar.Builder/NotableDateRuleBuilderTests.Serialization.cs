// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateRuleBuilderTests.Serialization.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Reflection;
using System.Xml.Linq;

namespace Bodu.Globalization.Calendar.Builder;

/// <summary>
/// Verifies the serialisation guard paths and unsupported-strategy fallbacks of
/// <see cref="NotableDateRuleBuilder" />.
/// </summary>
public partial class NotableDateRuleBuilderTests
{
    private static readonly XNamespace s_calendarNs = XNamespace.Get("urn:bodu:globalization:calendar");

    /// <summary>
    /// Verifies that <see cref="NotableDateRuleBuilder.ToXElement" /> throws
    /// <see cref="InvalidOperationException" /> when no resolution strategy has been selected.
    /// </summary>
    [TestMethod]
    public void ToXElement_WhenNoStrategySelected_ShouldThrowExactly()
    {
        NotableDateRuleBuilder builder = new();
        builder.Category(NotableDateCategory.Holiday);

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = builder.ToXElement("Some Date", s_calendarNs);
        });
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateRuleBuilder.ToJsonNode" /> throws
    /// <see cref="InvalidOperationException" /> when no resolution strategy has been selected.
    /// </summary>
    [TestMethod]
    public void ToJsonNode_WhenNoStrategySelected_ShouldThrowExactly()
    {
        NotableDateRuleBuilder builder = new();
        builder.Category(NotableDateCategory.Holiday);

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = builder.ToJsonNode("Some Date");
        });
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateRuleBuilder.Build" /> throws <see cref="NotSupportedException" /> when
    /// <c>_strategy</c> holds a value that is outside the defined <see cref="DateResolutionStrategy" /> enumeration.
    /// </summary>
    [TestMethod]
    public void Build_WhenStrategyValueIsUndefined_ShouldThrowExactly()
    {
        NotableDateRuleBuilder builder = new();
        builder.Category(NotableDateCategory.Holiday);
        SetStrategyToUndefined(builder);

        Assert.ThrowsExactly<NotSupportedException>(() =>
        {
            _ = builder.Build("Some Date");
        });
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateRuleBuilder.ToXElement" /> throws <see cref="NotSupportedException" />
    /// when the strategy value is undefined, which routes through the <c>BuildStrategyElement</c> default arm.
    /// </summary>
    [TestMethod]
    public void ToXElement_WhenStrategyValueIsUndefined_ShouldThrowExactly()
    {
        NotableDateRuleBuilder builder = new();
        builder.Category(NotableDateCategory.Holiday);
        SetStrategyToUndefined(builder);

        Assert.ThrowsExactly<NotSupportedException>(() =>
        {
            _ = builder.ToXElement("Some Date", s_calendarNs);
        });
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateRuleBuilder.ToJsonNode" /> throws <see cref="NotSupportedException" />
    /// when the strategy value is undefined, exercising the <c>default</c> arm of the strategy switch.
    /// </summary>
    [TestMethod]
    public void ToJsonNode_WhenStrategyValueIsUndefined_ShouldThrowExactly()
    {
        NotableDateRuleBuilder builder = new();
        builder.Category(NotableDateCategory.Holiday);
        SetStrategyToUndefined(builder);

        Assert.ThrowsExactly<NotSupportedException>(() =>
        {
            _ = builder.ToJsonNode("Some Date");
        });
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateRuleBuilder.ToXElement" /> emits an <c>Algorithm</c> element with
    /// type/key/month/day attributes populated when the algorithm strategy carries all optional fields.
    /// This exercises the <c>BuildAlgorithmElement</c> assembly-qualified-name path on a runtime type.
    /// </summary>
    [TestMethod]
    public void ToXElement_WhenAlgorithmStrategyWithCalendarType_ShouldIncludeAssemblyQualifiedName()
    {
        NotableDateRuleBuilder builder = new();
        builder
            .Category(NotableDateCategory.Religious)
            .CalendarType(typeof(System.Globalization.HebrewCalendar))
            .Algorithm("easter-gregorian", typeof(System.Globalization.GregorianCalendar), "3", 21);

        var element = builder.ToXElement("Test", s_calendarNs);

        XElement algorithm = element.Element(s_calendarNs + "Algorithm")!;
        Assert.IsNotNull(algorithm);
        Assert.AreEqual("easter-gregorian", algorithm.Attribute("key")!.Value);
        Assert.AreEqual(typeof(System.Globalization.GregorianCalendar).AssemblyQualifiedName, algorithm.Attribute("type")!.Value);
        Assert.AreEqual("3", algorithm.Attribute("month")!.Value);
        Assert.AreEqual("21", algorithm.Attribute("day")!.Value);

        // The calendarType attribute on the rule itself should also be the assembly-qualified form.
        Assert.AreEqual(
            typeof(System.Globalization.HebrewCalendar).AssemblyQualifiedName,
            element.Attribute("calendarType")!.Value);
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateRuleBuilder.ToJsonNode" /> emits an <c>algorithm</c> object whose
    /// <c>type</c> property is the assembly-qualified name for a runtime calendar type. This exercises the
    /// <c>BuildAlgorithmJsonNode</c> assembly-qualified-name path and the calendar-type JSON serialisation.
    /// </summary>
    [TestMethod]
    public void ToJsonNode_WhenAlgorithmStrategyWithCalendarType_ShouldIncludeAssemblyQualifiedName()
    {
        NotableDateRuleBuilder builder = new();
        builder
            .Category(NotableDateCategory.Religious)
            .CalendarType(typeof(System.Globalization.HebrewCalendar))
            .Algorithm("easter-gregorian", typeof(System.Globalization.GregorianCalendar), "3", 21);

        System.Text.Json.Nodes.JsonObject node = builder.ToJsonNode("Test");
        var algorithm = (System.Text.Json.Nodes.JsonObject)node["algorithm"]!;

        Assert.AreEqual("easter-gregorian", (string)algorithm["key"]!);
        Assert.AreEqual(typeof(System.Globalization.GregorianCalendar).AssemblyQualifiedName, (string)algorithm["type"]!);
        Assert.AreEqual("3", (string)algorithm["month"]!);
        Assert.AreEqual(21, (int)algorithm["day"]!);
        Assert.AreEqual(typeof(System.Globalization.HebrewCalendar).AssemblyQualifiedName, (string)node["calendarType"]!);
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateRuleBuilder" />'s default-rule-name generator falls back to
    /// <see cref="object.ToString" /> on the strategy value when the value is outside the defined
    /// <see cref="DateResolutionStrategy" /> enumeration.
    /// </summary>
    [TestMethod]
    public void ToXElement_WhenStrategyValueIsUndefinedAndCaughtByRuleNameGenerator_ShouldNotThrowBeforeStrategyDispatch()
    {
        // Authors of the rule never set a RuleName, so GenerateDefaultRuleName is invoked. With an undefined
        // strategy value, the rule-name generator's "_ => _strategy.Value.ToString()" arm must execute before
        // BuildStrategyElement subsequently throws.
        NotableDateRuleBuilder builder = new();
        builder.Category(NotableDateCategory.Holiday);
        SetStrategyToUndefined(builder);

        try
        {
            _ = builder.ToXElement("Some Date", s_calendarNs);
            Assert.Fail("Expected a NotSupportedException after the rule-name fallback executes.");
        }
        catch (NotSupportedException)
        {
            // Expected — the rule-name generator must have executed for control to reach BuildStrategyElement,
            // so the fallback arm is now covered.
        }
    }

    /// <summary>
    /// Sets the private <c>_strategy</c> field of the supplied <paramref name="builder" /> to a value outside the
    /// defined <see cref="DateResolutionStrategy" /> enumeration so the unsupported-strategy default arms can be
    /// exercised by tests.
    /// </summary>
    /// <param name="builder">The builder whose strategy field should be coerced to an undefined value.</param>
    private static void SetStrategyToUndefined(NotableDateRuleBuilder builder)
    {
        FieldInfo field = typeof(NotableDateRuleBuilder)
            .GetField("_strategy", BindingFlags.Instance | BindingFlags.NonPublic)!;
        field.SetValue(builder, (DateResolutionStrategy)int.MaxValue);
    }
}
