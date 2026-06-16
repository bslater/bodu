// ---------------------------------------------------------------------------------------------------------------
// <copyright file="XmlNamespaceResolverTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Xml.Linq;

namespace Bodu.Xml.Linq;

[TestClass]
public sealed class XmlNamespaceResolverTests
{

    private static readonly XNamespace s_sampleNamespace = "http://example.com/sample";

    /// <summary>
    /// Verifies that the constructor extracts the namespace of the supplied root element.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenRootHasNamespace_ShouldCaptureNamespace()
    {
        var root = new XElement(s_sampleNamespace + "root");
        var resolver = new XmlNamespaceResolver(root);

        Assert.AreEqual(s_sampleNamespace + "child", resolver.Name("child"));
    }

    /// <summary>
    /// Verifies that the constructor captures <see cref="XNamespace.None" /> when the root element has no explicit namespace, and that
    /// the resolver subsequently produces unqualified <see cref="XName" /> values that match the source document.
    /// </summary>
    /// <remarks>
    /// <see cref="XName.Namespace" /> is contractually never <see langword="null" /> in <c>System.Xml.Linq</c> — unqualified names
    /// carry <see cref="XNamespace.None" /> rather than a missing namespace. This test exercises that boundary so that any future
    /// change to the resolver's null-handling contract is detected.
    /// </remarks>
    [TestMethod]
    public void Ctor_WhenRootHasNoNamespace_ShouldCaptureXNamespaceNoneAndResolveLocalNames()
    {
        var root = new XElement("root",
            new XElement("child", "value"));

        var resolver = new XmlNamespaceResolver(root);

        XName qualified = resolver.Name("child");
        Assert.AreEqual(XNamespace.None, qualified.Namespace);
        Assert.AreEqual("child", qualified.LocalName);

        XElement? child = resolver.Element(root, "child");
        Assert.IsNotNull(child);
        Assert.AreEqual("value", child!.Value);
    }

    /// <summary>
    /// Verifies that the constructor throws <see cref="ArgumentNullException" /> when the root element is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenRootIsNull_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = new XmlNamespaceResolver(null!);
        });
    }

    /// <summary>
    /// Verifies that <see cref="XmlNamespaceResolver.Element" /> returns the matching child element when one exists.
    /// </summary>
    [TestMethod]
    public void Element_WhenChildExists_ShouldReturnMatchingElement()
    {
        var root = new XElement(s_sampleNamespace + "root",
            new XElement(s_sampleNamespace + "child", "value"));
        var resolver = new XmlNamespaceResolver(root);

        XElement? child = resolver.Element(root, "child");

        Assert.IsNotNull(child);
        Assert.AreEqual("value", child!.Value);
    }

    /// <summary>
    /// Verifies that <see cref="XmlNamespaceResolver.Element" /> returns <see langword="null" /> when no matching child exists.
    /// </summary>
    [TestMethod]
    public void Element_WhenChildIsMissing_ShouldReturnNull()
    {
        var root = new XElement(s_sampleNamespace + "root");
        var resolver = new XmlNamespaceResolver(root);

        Assert.IsNull(resolver.Element(root, "missing"));
    }

    /// <summary>
    /// Verifies that <see cref="XmlNamespaceResolver.Element" /> throws <see cref="ArgumentNullException" /> when the parent element is
    /// <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void Element_WhenParentIsNull_ShouldThrowExactly()
    {
        var root = new XElement(s_sampleNamespace + "root");
        var resolver = new XmlNamespaceResolver(root);

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = resolver.Element(null!, "child");
        });
    }

    /// <summary>
    /// Verifies that <see cref="XmlNamespaceResolver.Elements" /> returns an empty sequence when no matching child exists.
    /// </summary>
    [TestMethod]
    public void Elements_WhenChildrenAreMissing_ShouldReturnEmptySequence()
    {
        var root = new XElement(s_sampleNamespace + "root");
        var resolver = new XmlNamespaceResolver(root);

        XElement[] items = [.. resolver.Elements(root, "missing")];

        Assert.IsEmpty(items);
    }

    /// <summary>
    /// Verifies that <see cref="XmlNamespaceResolver.Elements" /> returns every matching child element in document order.
    /// </summary>
    [TestMethod]
    public void Elements_WhenChildrenExist_ShouldReturnAllMatchingElements()
    {
        var root = new XElement(s_sampleNamespace + "root",
            new XElement(s_sampleNamespace + "item", "one"),
            new XElement(s_sampleNamespace + "item", "two"),
            new XElement(s_sampleNamespace + "other", "three"),
            new XElement(s_sampleNamespace + "item", "four"));

        var resolver = new XmlNamespaceResolver(root);
        string[] items = resolver.Elements(root, "item").Select(e => e.Value).ToArray();

        CollectionAssert.AreEqual(new[] { "one", "two", "four" }, items);
    }

    /// <summary>
    /// Verifies that <see cref="XmlNamespaceResolver.Elements" /> throws <see cref="ArgumentNullException" /> when the parent element is
    /// <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void Elements_WhenParentIsNull_ShouldThrowExactly()
    {
        var root = new XElement(s_sampleNamespace + "root");
        var resolver = new XmlNamespaceResolver(root);

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = resolver.Elements(null!, "child").ToList();
        });
    }

    /// <summary>
    /// Verifies that <see cref="XmlNamespaceResolver.Elements" /> returns every matching unqualified child when the resolver was
    /// constructed from a namespace-less root, confirming that the resolver does not silently filter to a non-default namespace.
    /// </summary>
    [TestMethod]
    public void Elements_WhenRootHasNoNamespace_ShouldReturnAllMatchingUnqualifiedChildren()
    {
        var root = new XElement("root",
            new XElement("item", "one"),
            new XElement("item", "two"),
            new XElement("item", "three"));
        var resolver = new XmlNamespaceResolver(root);

        string[] items = resolver.Elements(root, "item").Select(e => e.Value).ToArray();

        CollectionAssert.AreEqual(new[] { "one", "two", "three" }, items);
    }

    /// <summary>
    /// Verifies that <see cref="XmlNamespaceResolver.Name" /> qualifies a local name with the captured namespace.
    /// </summary>
    [TestMethod]
    public void Name_WhenCalled_ShouldReturnNamespacedXName()
    {
        var root = new XElement(s_sampleNamespace + "root");
        var resolver = new XmlNamespaceResolver(root);

        XName qualified = resolver.Name("widget");

        Assert.AreEqual(s_sampleNamespace, qualified.Namespace);
        Assert.AreEqual("widget", qualified.LocalName);
    }

}
