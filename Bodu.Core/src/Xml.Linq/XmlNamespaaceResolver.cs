// ---------------------------------------------------------------------------------------------------------------
// <copyright file="XmlNamespaaceResolver.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Xml.Linq;

namespace Bodu.Xml.Linq;

/// <summary>
/// Resolves child <see cref="XElement" /> and <see cref="XName" /> instances against the default XML namespace of a
/// supplied root element, removing the need to qualify every lookup with an inline namespace prefix.
/// </summary>
/// <remarks>
/// <para>
/// Working with <see cref="System.Xml.Linq" /> documents that carry a default namespace requires every element and
/// attribute access to be qualified with that namespace; otherwise <see cref="XContainer.Element(XName)" /> returns
/// <see langword="null" /> even when the named element is present. <see cref="XmlNamespaceResolver" /> captures the
/// namespace once from the document's root element and exposes <see cref="Element" />, <see cref="Elements" />, and
/// <see cref="Name" /> helpers that pre-qualify lookups, allowing call-sites to reference elements by local name only.
/// </para>
/// <para>
/// The captured namespace is fixed for the lifetime of the instance — it is not refreshed if the underlying document
/// mutates, and the resolver does not retain a reference to the root element itself. Callers that work with multiple
/// namespaces in the same document should construct a separate resolver per namespace.
/// </para>
/// <para>
/// The constructor throws <see cref="InvalidOperationException" /> when the supplied root element exposes no namespace
/// at all (an unusual but legal state for hand-built <see cref="XElement" /> trees). Documents loaded from a stream or
/// string source always carry the empty namespace at minimum, so the typical load-from-source flow does not encounter
/// this condition.
/// </para>
/// </remarks>
/// <example>
///<![CDATA[
/// XDocument doc = XDocument.Parse(
///     "<library xmlns='urn:books'><book><title>Foundation</title></book></library>");
///
/// var ns = new XmlNamespaceResolver(doc.Root!);
///
/// // Look up by local name without re-stating the namespace at every call site.
/// XElement? book  = ns.Element(doc.Root!, "book");
/// XElement? title = ns.Element(book!,     "title");
/// Console.WriteLine(title?.Value); // Foundation
///]]>
/// </example>
public sealed class XmlNamespaceResolver
{
    private readonly XNamespace _xNamespace;

    /// <summary>
    /// Initializes a new instance of the <see cref="XmlNamespaceResolver" /> class with the specified root element.
    /// </summary>
    /// <param name="root">The root element from which to extract the default namespace.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown if the root element is <see langword="null" /> or has no namespace.
    /// </exception>
    public XmlNamespaceResolver(XElement root)
    {
        ThrowHelper.ThrowIfNull(root);

        _xNamespace = root.Name.Namespace ?? throw new InvalidOperationException("Missing XML _xNamespace on root element."); //TODO: define a BCL-style exception message in the resx file and remove inline static text
    }

    /// <summary>
    /// Safely gets a child element with the specified local name in the current namespace.
    /// </summary>
    /// <param name="parent">The parent element.</param>
    /// <param name="localName">The local name of the child element.</param>
    /// <returns>The matching child XElement, or null if not found.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="parent" /> is <see langword="null" />.
    /// </exception>
    public XElement? Element(XElement parent, string localName)
    {
        ThrowHelper.ThrowIfNull(parent);
        return parent.Element(Name(localName));
    }

    /// <summary>
    /// Safely gets all child elements with the specified local name in the current namespace.
    /// </summary>
    /// <param name="parent">The parent element.</param>
    /// <param name="localName">The local name of the child elements.</param>
    /// <returns>An enumerable of matching XElement objects.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="parent" /> is <see langword="null" />.
    /// </exception>
    public IEnumerable<XElement> Elements(XElement parent, string localName)
    {
        ThrowHelper.ThrowIfNull(parent);
        return parent.Elements(Name(localName));
    }

    /// <summary>
    /// Gets the fully qualified XName for the given local name in the current namespace.
    /// </summary>
    /// <param name="localName">The local (unqualified) element or attribute name.</param>
    /// <returns>The namespaced XName.</returns>
    public XName Name(string localName) => _xNamespace + localName;
}
