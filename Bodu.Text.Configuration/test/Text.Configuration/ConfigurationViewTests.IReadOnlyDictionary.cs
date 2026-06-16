// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConfigurationViewTests.IReadOnlyDictionary.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Configuration;

public partial class ConfigurationViewTests
{
    /// <summary>
    /// Verifies that <see cref="ConfigurationView" /> can be consumed through the
    /// <see cref="System.Collections.Generic.IReadOnlyDictionary{TKey, TValue}" /> surface and that its
    /// <c>Count</c>/<c>Keys</c>/<c>Values</c> agree with the resolved values.
    /// </summary>
    [TestMethod]
    public void IReadOnlyDictionary_WhenResolved_ShouldExposeCountKeysAndValues()
    {
        var doc = ConfigurationDocument.Parse("[*]\nindent = 4\nstyle = space\n");
        IReadOnlyDictionary<string, string?> view = doc.Resolve("any.cs");

        Assert.AreEqual(2, view.Count);
        CollectionAssert.AreEquivalent(new[] { "indent", "style" }, view.Keys.ToArray());
        CollectionAssert.AreEquivalent(new string?[] { "4", "space" }, view.Values.ToArray());
    }

    /// <summary>
    /// Verifies that <see cref="ConfigurationView.ContainsKey(string)" /> accepts both the colon-delimited and the
    /// equivalent dotted form, and reports <see langword="false" /> for absent keys.
    /// </summary>
    [TestMethod]
    public void ContainsKey_WhenColonOrDottedForm_ShouldMatchEquivalently()
    {
        var doc = ConfigurationDocument.Parse("[*]\nlogging.level.default = info\n");
        ConfigurationView view = doc.Resolve("any.cs");

        Assert.IsTrue(view.ContainsKey("logging:level:default"));
        Assert.IsTrue(view.ContainsKey("logging.level.default"));
        Assert.IsFalse(view.ContainsKey("missing"));
    }

    /// <summary>
    /// Verifies that the explicit <see cref="System.Collections.Generic.IReadOnlyDictionary{TKey, TValue}.TryGetValue" />
    /// implementation resolves dotted and colon forms and reports failure for absent keys.
    /// </summary>
    [TestMethod]
    public void IReadOnlyDictionaryTryGetValue_WhenColonOrDottedForm_ShouldResolveEquivalently()
    {
        var doc = ConfigurationDocument.Parse("[*]\nlogging.level.default = info\n");
        IReadOnlyDictionary<string, string?> view = doc.Resolve("any.cs");

        Assert.IsTrue(view.TryGetValue("logging:level:default", out string? colon));
        Assert.AreEqual("info", colon);
        Assert.IsTrue(view.TryGetValue("logging.level.default", out string? dotted));
        Assert.AreEqual("info", dotted);
        Assert.IsFalse(view.TryGetValue("missing", out _));
    }

    /// <summary>
    /// Verifies that the <see cref="System.Collections.Generic.IReadOnlyDictionary{TKey, TValue}" /> indexer keeps the
    /// view's null-on-absent convention rather than throwing <see cref="KeyNotFoundException" />.
    /// </summary>
    [TestMethod]
    public void IReadOnlyDictionaryIndexer_WhenKeyAbsent_ShouldReturnNull()
    {
        var doc = ConfigurationDocument.Parse("[*]\nindent = 4\n");
        IReadOnlyDictionary<string, string?> view = doc.Resolve("any.cs");

        Assert.AreEqual("4", view["indent"]);
        Assert.IsNull(view["missing"]);
    }
}
