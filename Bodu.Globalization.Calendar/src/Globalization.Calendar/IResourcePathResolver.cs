// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IResourcePathResolver.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Resolves resource paths declared inside notable-date XML resources.
/// </summary>
/// <remarks>
/// <para>
/// Implementations translate the logical path syntax used in <c>&lt;UseFrom&gt;</c> directives — which may be absolute
/// (for example <c>/Bodu/Globalization/Calendar/Resources/uk-all.xml</c>) or relative (for example
/// <c>../regions/uk-england.xml</c>) — into the fully qualified resource name that the provider uses when loading the
/// file.
/// </para>
/// <para>
/// The built-in implementation is <see cref="ResourcePathResolver" />, which resolves paths using a logical <c>/</c>
/// -delimited hierarchy and supports absolute and relative references including <c>.</c> and <c>..</c> segments.
/// </para>
/// </remarks>
public interface IResourcePathResolver
{
    /// <summary>
    /// Resolves a child resource path relative to the specified document resource path.
    /// </summary>
    /// <param name="documentPath">The fully qualified resource path of the document declaring the reference.</param>
    /// <param name="childPath">The referenced child resource path.</param>
    /// <returns>The fully qualified resolved resource path.</returns>
    string Resolve(string documentPath, string childPath);
}
