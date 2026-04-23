// ---------------------------------------------------------------------------------------------------------------
// <copyright file="XmlNamespaaceResolver.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Xml.Linq;

namespace Bodu.Xml.Linq;

/// <summary>
/// Helper class for resolving and accessing XML elements within a specific namespace.
/// </summary>
public sealed class XmlNamespaceResolver
{
    private readonly XNamespace _xNamespace;

    /// <summary>
    /// Initializes a new instance of the <see cref="XmlNamespaceResolver"/> class with the specified root element.
    /// </summary>
    /// <param name="root">The root element from which to extract the default namespace.</param>
    /// <exception cref="InvalidOperationException">Thrown if the root element is <see langword="null"/> or has no namespace.</exception>
    public XmlNamespaceResolver(XElement root)
    {
        ThrowHelper.ThrowIfNull(root);

        _xNamespace = root.Name.Namespace ?? throw new InvalidOperationException("Missing XML _xNamespace on root element.");
    }

    /// <summary>
    /// Safely gets a child element with the specified local name in the current namespace.
    /// </summary>
    /// <param name="parent">The parent element.</param>
    /// <param name="localName">The local name of the child element.</param>
    /// <returns>The matching child XElement, or null if not found.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="parent" /> is <see langword="null" />.</exception>
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
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="parent" /> is <see langword="null" />.</exception>
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
