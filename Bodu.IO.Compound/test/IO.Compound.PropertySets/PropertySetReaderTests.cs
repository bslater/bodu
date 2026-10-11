// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PropertySetReaderTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Compound.PropertySets;

/// <summary>
/// Verifies how <see cref="OlePropertySet.Parse(ReadOnlyMemory{byte})" /> reads the property sets other writers
/// produce, including layouts and values the authoring API never emits.
/// </summary>
/// <remarks>
/// <para>
/// Reading is one operation, so the tests are split by the part of the format they exercise: variant types, strings,
/// dictionaries, vectors, code pages and section bounds.
/// </para>
/// <para>
/// The variant tests write a well-formed set and patch its type word or payload in place, deriving the offsets from the
/// set rather than hard-coding them; the other tests build the bytes with <see cref="PropertySetBytes" />, which leaves
/// every byte, alignment included, to the test.
/// </para>
/// </remarks>
[TestClass]
public partial class PropertySetReaderTests
{
}
