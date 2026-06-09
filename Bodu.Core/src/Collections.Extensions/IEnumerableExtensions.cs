// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IEnumerableExtensions.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Extensions;

/// <summary>
/// Provides operations over the non-generic <see cref="System.Collections.IEnumerable" /> surface — counting without
/// enumerating where the source is already a collection, and walking heterogeneous tree structures recursively — for
/// code that has to deal with reflection-shaped or legacy sequences.
/// </summary>
/// <remarks>
/// <para>
/// Most modern code uses <see cref="IEnumerable{T}" />, but reflection, COM interop, ADO.NET, and older library
/// boundaries still hand back the non-generic <see cref="System.Collections.IEnumerable" />. The companion class in
/// <c>Bodu.Collections.Generic.Extensions</c> handles the strongly typed case; this class supplies the equivalents that
/// work when the element type is unavailable at compile time, and complements the generic <c>RecursiveSelect</c> with a
/// dynamic-typed version for tree shapes whose nodes only share an <see cref="object" /> base.
/// </para>
/// <para>
/// The API surface is small and pragmatic: <c>CountOrDefault</c> short-circuits to
/// <see cref="System.Collections.ICollection.Count" /> when the source already exposes one, falling back to enumeration
/// only when necessary, and <c>RecursiveSelect</c> walks a node graph where each node may yield further children via a
/// caller-supplied selector — with overloads that accept depth, index, or a controller delegate to influence traversal.
/// </para>
/// <para>
/// <c>CountOrDefault</c> avoids enumerating known collection types and otherwise performs a single eager pass. The
/// <c>RecursiveSelect</c> family is fully deferred and yields nodes in pre-order traversal. Both methods reject a
/// <see langword="null" /> source via <see cref="ThrowHelper" /> rather than failing mid-enumeration.
/// </para>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// // Cheap count when the source already exposes ICollection — falls back to enumeration otherwise.
/// System.Collections.IEnumerable boxed = new ArrayList { 1, 2, 3, 4 };
/// int count = boxed.CountOrDefault(); // => 4
///
/// // Walk a heterogeneous control tree without committing to a generic type.
/// System.Collections.IEnumerable controls = root.GetChildren();
/// foreach (object node in controls.RecursiveSelect(c => ((Control)c).GetChildren()))
///     Console.WriteLine(node);
///]]>
/// </code>
/// </example>
/// </remarks>
public static partial class IEnumerableExtensions
{
}