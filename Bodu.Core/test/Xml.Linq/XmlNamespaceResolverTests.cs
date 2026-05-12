// ---------------------------------------------------------------------------------------------------------------
// <copyright file="XmlNamespaceResolverTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Xml.Linq;

namespace Bodu.Xml.Linq;

[TestClass]
public sealed class XmlNamespaceResolverTests
{
    private static readonly XNamespace SampleNamespace = "http://example.com/sample";

    /// <summary>
    /// Verifies that the constructor extracts the namespace of the supplied root element.
    /// </summary>
    [TestMethod]
    public void Constructor_WhenRootHasNamespace_ShouldCaptureNamespace()
    {
        var root = new XElement(SampleNamespace + "root");
        var resolver = new XmlNamespaceResolver(root);

        Assert.AreEqual(SampleNamespace + "child", resolver.Name("child"));
    }

    /// <summary>
    /// Verifies that the constructor throws <see cref="ArgumentNullException" /> when the root element is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void Constructor_WhenRootIsNull_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = new XmlNamespaceResolver(null!);
        });
    }

    /// <summary>
    /// Verifies that <see cref="XmlNamespaceResolver.Name" /> qualifies a local name with the captured namespace.
    /// </summary>
    [TestMethod]
    public void Name_WhenCalled_ShouldReturnNamespacedXName()
    {
        var root = new XElement(SampleNamespace + "root");
        var resolver = new XmlNamespaceResolver(root);

        var qualified = resolver.Name("widget");

        Assert.AreEqual(SampleNamespace, qualified.Namespace);
        Assert.AreEqual("widget", qualified.LocalName);
    }

    /// <summary>
    /// Verifies that <see cref="XmlNamespaceResolver.Element" /> returns the matching child element when one exists.
    /// </summary>
    [TestMethod]
    public void Element_WhenChildExists_ShouldReturnMatchingElement()
    {
        var root = new XElement(SampleNamespace + "root",
            new XElement(SampleNamespace + "child", "value"));
        var resolver = new XmlNamespaceResolver(root);

        var child = resolver.Element(root, "child");

        Assert.IsNotNull(child);
        Assert.AreEqual("value", child!.Value);
    }

    /// <summary>
    /// Verifies that <see cref="XmlNamespaceResolver.Element" /> returns <see langword="null" /> when no matching child exists.
    /// </summary>
    [TestMethod]
    public void Element_WhenChildIsMissing_ShouldReturnNull()
    {
        var root = new XElement(SampleNamespace + "root");
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
        var root = new XElement(SampleNamespace + "root");
        var resolver = new XmlNamespaceResolver(root);

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = resolver.Element(null!, "child");
        });
    }

    /// <summary>
    /// Verifies that <see cref="XmlNamespaceResolver.Elements" /> returns every matching child element in document order.
    /// </summary>
    [TestMethod]
    public void Elements_WhenChildrenExist_ShouldReturnAllMatchingElements()
    {
        var root = new XElement(SampleNamespace + "root",
            new XElement(SampleNamespace + "item", "one"),
            new XElement(SampleNamespace + "item", "two"),
            new XElement(SampleNamespace + "other", "three"),
            new XElement(SampleNamespace + "item", "four"));

        var resolver = new XmlNamespaceResolver(root);
        var items = resolver.Elements(root, "item").Select(e => e.Value).ToArray();

        CollectionAssert.AreEqual(new[] { "one", "two", "four" }, items);
    }

    /// <summary>
    /// Verifies that <see cref="XmlNamespaceResolver.Elements" /> returns an empty sequence when no matching child exists.
    /// </summary>
    [TestMethod]
    public void Elements_WhenChildrenAreMissing_ShouldReturnEmptySequence()
    {
        var root = new XElement(SampleNamespace + "root");
        var resolver = new XmlNamespaceResolver(root);

        var items = resolver.Elements(root, "missing").ToArray();

        Assert.AreEqual(0, items.Length);
    }

    /// <summary>
    /// Verifies that <see cref="XmlNamespaceResolver.Elements" /> throws <see cref="ArgumentNullException" /> when the parent element is
    /// <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void Elements_WhenParentIsNull_ShouldThrowExactly()
    {
        var root = new XElement(SampleNamespace + "root");
        var resolver = new XmlNamespaceResolver(root);

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = resolver.Elements(null!, "child").ToList();
        });
    }
}
