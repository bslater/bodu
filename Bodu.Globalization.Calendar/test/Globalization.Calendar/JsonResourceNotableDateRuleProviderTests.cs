// ---------------------------------------------------------------------------------------------------------------
// <copyright file="JsonResourceNotableDateRuleProviderTests.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Reflection;

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Verifies that <see cref="JsonResourceNotableDateRuleProvider" /> flattens cherry-pick directives in JSON resources
/// using the same shared base pipeline as the XML provider.
/// </summary>
[TestClass]
public sealed class JsonResourceNotableDateRuleProviderTests
{
    private const string SourceJson = "Bodu.Globalization.Calendar.Fixtures.Source.json";
    private const string MultiRuleSourceJson = "Bodu.Globalization.Calendar.Fixtures.MultiRuleSource.json";
    private const string MissingNameReferrerJson = "Bodu.Globalization.Calendar.Fixtures.MissingNameReferrer.json";
    private const string CircularJsonA = "Bodu.Globalization.Calendar.Fixtures.CircularJsonA.json";

    /// <summary>
    /// Verifies that loading a standalone JSON source resource exposes its rules unchanged.
    /// </summary>
    [TestMethod]
    public void LoadRules_WhenLoadingSourceJson_ShouldExposeAllLocalRules()
    {
        var provider = new JsonResourceNotableDateRuleProvider(
            SourceJson,
            new ResourcePathResolver(),
            Assembly.GetExecutingAssembly());

        var rules = provider.LoadRules().ToList();

        Assert.AreEqual(2, rules.Count);
        Assert.IsTrue(rules.Any(r => r.Name == "Shared Alpha"));
        Assert.IsTrue(rules.Any(r => r.Name == "Shared Beta"));
    }

    /// <summary>
    /// Verifies that loading a multi-rule JSON source resource exposes every rule variant declared on a single
    /// notable date entry.
    /// </summary>
    [TestMethod]
    public void LoadRules_WhenLoadingMultiRuleSourceJson_ShouldExposeAllRuleVariants()
    {
        var provider = new JsonResourceNotableDateRuleProvider(
            MultiRuleSourceJson,
            new ResourcePathResolver(),
            Assembly.GetExecutingAssembly());

        var rules = provider.LoadRules().ToList();

        Assert.AreEqual(2, rules.Count);
        Assert.IsTrue(rules.Any(r => r.RuleName == "June Variant"));
        Assert.IsTrue(rules.Any(r => r.RuleName == "October Variant"));
    }

    /// <summary>
    /// Verifies that the constructor rejects a <see langword="null" /> resource path resolver with
    /// <see cref="ArgumentNullException" /> and exposes the parameter name on the exception.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenResourcePathResolverIsNull_ShouldThrowExactly()
    {
        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = new JsonResourceNotableDateRuleProvider(SourceJson, null!);
        });

        Assert.AreEqual("resourcePathResolver", ex.ParamName);
    }

    /// <summary>
    /// Verifies that requesting a missing JSON resource surfaces as a <see cref="FileNotFoundException" /> whose
    /// message references the resource name to aid diagnosis.
    /// </summary>
    [TestMethod]
    public void LoadRules_WhenResourceMissing_ShouldThrowExactly()
    {
        var provider = new JsonResourceNotableDateRuleProvider(
            "Bodu.Globalization.Calendar.Fixtures.NotThere.json",
            new ResourcePathResolver(),
            Assembly.GetExecutingAssembly());

        FileNotFoundException ex = Assert.ThrowsExactly<FileNotFoundException>(() =>
        {
            _ = provider.LoadRules().ToList();
        });

        Assert.IsTrue(ex.Message.Contains("NotThere", StringComparison.Ordinal));
    }

    /// <summary>
    /// Verifies that a <c>useFrom</c> directive referencing a rule that does not exist in the source surfaces as
    /// <see cref="InvalidOperationException" />.
    /// </summary>
    [TestMethod]
    public void LoadRules_WhenSourceRuleNameMissing_ShouldThrowExactly()
    {
        var provider = new JsonResourceNotableDateRuleProvider(
            MissingNameReferrerJson,
            new ResourcePathResolver(),
            Assembly.GetExecutingAssembly());

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = provider.LoadRules().ToList();
        });
    }

    /// <summary>
    /// Verifies that a circular reference graph among JSON resources surfaces as
    /// <see cref="InvalidOperationException" /> through the shared circular-reference detection in the base provider.
    /// </summary>
    [TestMethod]
    public void LoadRules_WhenJsonGraphIsCircular_ShouldThrowExactly()
    {
        var provider = new JsonResourceNotableDateRuleProvider(
            CircularJsonA,
            new ResourcePathResolver(),
            Assembly.GetExecutingAssembly());

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = provider.LoadRules().ToList();
        });
    }
}
