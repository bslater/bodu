// ---------------------------------------------------------------------------------------------------------------
// <copyright file="XmlResourceNotableDateRuleProviderTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Reflection;

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Verifies that <see cref="XmlResourceNotableDateRuleProvider" /> correctly flattens the embedded global resources, applying
/// cherry-pick directives, per-directive overrides, and <c>UseAll</c> wildcards. Region-specific assertions (US, GB, FR, AU)
/// live alongside the corresponding companion data pack test projects, since region XMLs are no longer embedded in the main
/// library.
/// </summary>
[TestClass]
public sealed partial class XmlResourceNotableDateRuleProviderTests
{
    private const string CommonResource = "Bodu/Globalization/Calendar/Resources/global-all.xml";
    private const string ChristianResource = "Bodu/Globalization/Calendar/Resources/christian-gregorian.xml";
    private const string DefaultMinimalResource = "Bodu/Globalization/Calendar/Resources/default-minimal.xml";

    /// <summary>
    /// Verifies that loading the standalone Common resource exposes its rules without errors.
    /// </summary>
    [TestMethod]
    public void LoadRules_WhenLoadingCommonResource_ShouldExposeUniversalRules()
    {
        var provider = new XmlResourceNotableDateRuleProvider(CommonResource, new ResourcePathResolver());

        var rules = provider.LoadRules().ToList();

        Assert.IsTrue(rules.Any(r => r.Name == "New Year's Day"));
        Assert.IsTrue(rules.Any(r => r.Name == "Halloween"));
        Assert.IsTrue(rules.Any(r => r.Name == "International Workers' Day"));
    }

    /// <summary>
    /// Verifies that the default composite resource pulls in every rule from the Common and Christian sets via the wildcard
    /// <c>UseAll</c>, but no country-specific rules.
    /// </summary>
    [TestMethod]
    public void LoadRules_WhenLoadingDefaultComposite_ShouldExposeUniversalRulesViaUseAll()
    {
        var provider = new XmlResourceNotableDateRuleProvider(CommonResource, new ResourcePathResolver());

        var rules = provider.LoadRules().ToList();

        Assert.IsTrue(rules.Any(r => r.Name == "New Year's Day"));
        Assert.IsTrue(rules.Any(r => r.Name == "Easter Sunday"));
        Assert.IsTrue(rules.Any(r => r.Name == "Christmas Day"));
        Assert.IsTrue(rules.Any(r => r.Name == "Halloween"));
        Assert.IsFalse(rules.Any(r => r.Name == "Independence Day"), "Country-specific rules should not appear in the default composite.");
        Assert.IsFalse(rules.Any(r => r.Name == "Bastille Day"));
    }

    /// <summary>
    /// Verifies that loading the embedded minimal default resource exposes only New Year's Day, so consumers calling
    /// <c>new NotableDateService()</c> without a companion data pack receive a usable but intentionally narrow rule set.
    /// </summary>
    [TestMethod]
    public void LoadRules_WhenLoadingDefaultMinimalResource_ShouldExposeNewYearsDayOnly()
    {
        var provider = new XmlResourceNotableDateRuleProvider(DefaultMinimalResource, new ResourcePathResolver());

        var rules = provider.LoadRules().ToList();

        Assert.AreEqual(1, rules.Count);
        Assert.AreEqual("New Year's Day", rules[0].Name);
    }

    /// <summary>
    /// Verifies that loading a non-existent resource throws a clear <see cref="FileNotFoundException" />.
    /// </summary>
    [TestMethod]
    public void LoadRules_WhenResourceMissing_ShouldThrowExactly()
    {
        var provider = new XmlResourceNotableDateRuleProvider("Bodu/Globalization/Calendar/Resources/Imaginary.xml", new ResourcePathResolver());

        Assert.ThrowsExactly<FileNotFoundException>(() => _ = provider.LoadRules().ToList());
    }

    /// <summary>
    /// Verifies that the constructor rejects a <see langword="null" /> resource name with
    /// <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenXmlResourceNameIsNull_ShouldThrowExactly()
    {
        var ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = new XmlResourceNotableDateRuleProvider(null!, new ResourcePathResolver());
        });

        Assert.AreEqual("rootResourceName", ex.ParamName);
    }

    /// <summary>
    /// Verifies that the <paramref name="assembly" /> override routes resource lookups through the supplied assembly rather
    /// than the currently-executing one.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenAssemblyOverrideSupplied_ShouldUseSuppliedAssembly()
    {
        var provider = new XmlResourceNotableDateRuleProvider(
            CommonResource,
            new ResourcePathResolver(),
            typeof(NotableDateService).Assembly);

        IReadOnlyList<NotableDateRule> rules = provider.LoadRules().ToList();

        Assert.IsTrue(rules.Count > 0);
    }

    /// <summary>
    /// Verifies that the multi-assembly constructor rejects a <see langword="null" /> chain.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenAssemblyChainIsNull_ShouldThrowExactly()
    {
        var ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = new XmlResourceNotableDateRuleProvider(CommonResource, new ResourcePathResolver(), (IEnumerable<Assembly>)null!);
        });

        Assert.AreEqual("assemblies", ex.ParamName);
    }

    /// <summary>
    /// Verifies that the multi-assembly constructor rejects an empty chain with <see cref="ArgumentException" />.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenAssemblyChainIsEmpty_ShouldThrowExactly()
    {
        var ex = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = new XmlResourceNotableDateRuleProvider(CommonResource, new ResourcePathResolver(), Array.Empty<Assembly>());
        });

        Assert.AreEqual("assemblies", ex.ParamName);
    }

    /// <summary>
    /// Verifies that the multi-assembly constructor rejects a chain containing a <see langword="null" /> entry with
    /// <see cref="ArgumentException" />.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenAssemblyChainContainsNull_ShouldThrowExactly()
    {
        var ex = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = new XmlResourceNotableDateRuleProvider(
                CommonResource,
                new ResourcePathResolver(),
                new Assembly?[] { typeof(NotableDateService).Assembly, null }!);
        });

        Assert.AreEqual("assemblies", ex.ParamName);
    }

    /// <summary>
    /// Verifies that when the root resource is missing from the first assembly in the chain but present in a later one, the
    /// provider falls through and resolves it successfully.
    /// </summary>
    [TestMethod]
    public void LoadRules_WhenResourceLivesInFallbackAssembly_ShouldResolveViaAssemblyChain()
    {
        // First assembly is the test assembly, which doesn't carry global-all.xml; the main library does.
        var provider = new XmlResourceNotableDateRuleProvider(
            CommonResource,
            new ResourcePathResolver(),
            new[] { typeof(XmlResourceNotableDateRuleProviderTests).Assembly, typeof(NotableDateService).Assembly });

        var rules = provider.LoadRules().ToList();

        Assert.IsTrue(rules.Count > 0);
        Assert.IsTrue(rules.Any(r => r.Name == "New Year's Day"));
    }

    /// <summary>
    /// Verifies that loading the same provider twice returns the same cached flatten — both
    /// <see cref="XmlResourceNotableDateRuleProvider.LoadRules" /> calls hit the
    /// <c>Lazy&lt;List&lt;NotableDateRule&gt;&gt;</c> fast-path after the first one has materialised the list.
    /// </summary>
    [TestMethod]
    public void LoadRules_WhenCalledTwice_ShouldReturnCachedResult()
    {
        var provider = new XmlResourceNotableDateRuleProvider(CommonResource, new ResourcePathResolver());

        IEnumerable<NotableDateRule> first = provider.LoadRules();
        IEnumerable<NotableDateRule> second = provider.LoadRules();

        Assert.AreSame(first, second);
    }
}
