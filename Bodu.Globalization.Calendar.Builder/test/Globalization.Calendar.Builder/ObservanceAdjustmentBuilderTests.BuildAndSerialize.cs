// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ObservanceAdjustmentBuilderTests.BuildAndSerialize.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text.Json.Nodes;
using System.Xml.Linq;

namespace Bodu.Globalization.Calendar.Builder;

/// <summary>
/// Verifies the build and serialisation guard paths of <see cref="ObservanceAdjustmentBuilder" />.
/// </summary>
public partial class ObservanceAdjustmentBuilderTests
{
    private static readonly XNamespace s_calendarNs = XNamespace.Get("urn:bodu:globalization:calendar");

    /// <summary>
    /// Verifies that <see cref="ObservanceAdjustmentBuilder.Build" /> throws
    /// <see cref="InvalidOperationException" /> when <see cref="ObservanceAdjustmentBuilder.When" /> has not been
    /// called.
    /// </summary>
    [TestMethod]
    public void Build_WhenTriggerNotSet_ShouldThrowInvalidOperationException()
    {
        ObservanceAdjustmentBuilder builder = new();
        builder.Action(AdjustmentAction.None);

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = builder.Build("test-key");
        });
    }

    /// <summary>
    /// Verifies that <see cref="ObservanceAdjustmentBuilder.Build" /> throws
    /// <see cref="InvalidOperationException" /> when <see cref="ObservanceAdjustmentBuilder.Action" /> has not been
    /// called.
    /// </summary>
    [TestMethod]
    public void Build_WhenActionNotSet_ShouldThrowInvalidOperationException()
    {
        ObservanceAdjustmentBuilder builder = new();
        builder.When(AdjustmentTrigger.Always);

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = builder.Build("test-key");
        });
    }

    /// <summary>
    /// Verifies that <see cref="ObservanceAdjustmentBuilder.Build" /> emits a populated
    /// <see cref="ObservanceAdjustment.ComparisonDate" /> when both
    /// <see cref="ObservanceAdjustmentBuilder.ComparisonDate" /> components have been supplied.
    /// </summary>
    [TestMethod]
    public void Build_WhenComparisonDateSet_ShouldPopulateComparisonDate()
    {
        ObservanceAdjustmentBuilder builder = new();
        builder
            .When(AdjustmentTrigger.IfBeforeFixedDate)
            .Action(AdjustmentAction.MoveToNextWeekday)
            .ComparisonDate(3, 17);

        ObservanceAdjustment adjustment = builder.Build("compare");

        Assert.IsNotNull(adjustment.ComparisonDate);
        Assert.AreEqual(3, adjustment.ComparisonDate!.Value.Month);
        Assert.AreEqual(17, adjustment.ComparisonDate.Value.Day);
    }

    /// <summary>
    /// Verifies that <see cref="ObservanceAdjustmentBuilder.Clone" /> performs a deep copy of authored handler
    /// parameters, so that subsequent mutations of either builder do not bleed across the boundary.
    /// </summary>
    [TestMethod]
    public void Clone_WhenHandlerParametersAuthored_ShouldDeepCopyHandlerParameters()
    {
        ObservanceAdjustmentBuilder original = new();
        original
            .When(AdjustmentTrigger.Custom)
            .Action(AdjustmentAction.Custom)
            .HandlerKey("my-handler")
            .AddHandlerParameter("alpha", "1")
            .AddHandlerParameter("beta", "2");

        ObservanceAdjustmentBuilder copy = original.Clone();
        copy.AddHandlerParameter("gamma", "3");

        ObservanceAdjustment originalAdj = original.Build("k1");
        ObservanceAdjustment copyAdj = copy.Build("k2");

        Assert.IsNotNull(originalAdj.HandlerParameters);
        Assert.HasCount(2, originalAdj.HandlerParameters);
        Assert.IsFalse(originalAdj.HandlerParameters.ContainsKey("gamma"));

        Assert.IsNotNull(copyAdj.HandlerParameters);
        Assert.HasCount(3, copyAdj.HandlerParameters);
        Assert.AreEqual("3", copyAdj.HandlerParameters["gamma"]);
    }

    /// <summary>
    /// Verifies that <see cref="ObservanceAdjustmentBuilder.ToXElement" /> throws
    /// <see cref="InvalidOperationException" /> when no trigger has been set.
    /// </summary>
    [TestMethod]
    public void ToXElement_WhenTriggerNotSet_ShouldThrowInvalidOperationException()
    {
        ObservanceAdjustmentBuilder builder = new();
        builder.Action(AdjustmentAction.None);

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = builder.ToXElement("k", s_calendarNs);
        });
    }

    /// <summary>
    /// Verifies that <see cref="ObservanceAdjustmentBuilder.ToXElement" /> throws
    /// <see cref="InvalidOperationException" /> when no action has been set.
    /// </summary>
    [TestMethod]
    public void ToXElement_WhenActionNotSet_ShouldThrowInvalidOperationException()
    {
        ObservanceAdjustmentBuilder builder = new();
        builder.When(AdjustmentTrigger.Always);

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = builder.ToXElement("k", s_calendarNs);
        });
    }

    /// <summary>
    /// Verifies that <see cref="ObservanceAdjustmentBuilder.ToXElement" /> writes the assembly-qualified name of
    /// a calendar type into the <c>calendarType</c> attribute.
    /// </summary>
    [TestMethod]
    public void ToXElement_WhenCalendarTypeSet_ShouldIncludeAssemblyQualifiedName()
    {
        ObservanceAdjustmentBuilder builder = new();
        builder
            .When(AdjustmentTrigger.Always)
            .Action(AdjustmentAction.None)
            .CalendarType(typeof(System.Globalization.HebrewCalendar));

        XElement element = builder.ToXElement("k", s_calendarNs);

        Assert.AreEqual(
            typeof(System.Globalization.HebrewCalendar).AssemblyQualifiedName,
            element.Attribute("calendarType")!.Value);
    }

    /// <summary>
    /// Verifies that <see cref="ObservanceAdjustmentBuilder.ToJsonNode" /> throws
    /// <see cref="InvalidOperationException" /> when no trigger has been set.
    /// </summary>
    [TestMethod]
    public void ToJsonNode_WhenTriggerNotSet_ShouldThrowInvalidOperationException()
    {
        ObservanceAdjustmentBuilder builder = new();
        builder.Action(AdjustmentAction.None);

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = builder.ToJsonNode("k");
        });
    }

    /// <summary>
    /// Verifies that <see cref="ObservanceAdjustmentBuilder.ToJsonNode" /> throws
    /// <see cref="InvalidOperationException" /> when no action has been set.
    /// </summary>
    [TestMethod]
    public void ToJsonNode_WhenActionNotSet_ShouldThrowInvalidOperationException()
    {
        ObservanceAdjustmentBuilder builder = new();
        builder.When(AdjustmentTrigger.Always);

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = builder.ToJsonNode("k");
        });
    }

    /// <summary>
    /// Verifies that <see cref="ObservanceAdjustmentBuilder.ToJsonNode" /> writes the assembly-qualified name of
    /// a calendar type into the <c>calendarType</c> property.
    /// </summary>
    [TestMethod]
    public void ToJsonNode_WhenCalendarTypeSet_ShouldIncludeAssemblyQualifiedName()
    {
        ObservanceAdjustmentBuilder builder = new();
        builder
            .When(AdjustmentTrigger.Always)
            .Action(AdjustmentAction.None)
            .CalendarType(typeof(System.Globalization.HebrewCalendar));

        JsonObject node = builder.ToJsonNode("k");

        Assert.AreEqual(
            typeof(System.Globalization.HebrewCalendar).AssemblyQualifiedName,
            (string)node["calendarType"]!);
    }
}
