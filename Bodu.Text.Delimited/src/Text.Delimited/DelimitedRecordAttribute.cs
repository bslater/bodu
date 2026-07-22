// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DelimitedRecordAttribute.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Delimited;

/// <summary>
/// Marks a partial record POCO for compile-time delimited binding: the <c>Bodu.Text.Formats.Generators</c> source
/// generator emits an <see cref="IDelimitedRecordFactory{TRecord}" /> implementation for the type, exposed through a
/// generated static <c>DelimitedFactory</c> property.
/// </summary>
/// <remarks>
/// <para>
/// The generated factory maps the type's public read/write instance properties in declaration order, honouring
/// <see cref="Bodu.Text.Serialization.PropertyNameAttribute" /> for column names and skipping members annotated with
/// <see cref="Bodu.Text.Serialization.IgnoreAttribute" />. Scalar values convert with the invariant culture.
/// </para>
/// <para>
/// The annotated type must be declared <see langword="partial" /> so the generator can add the factory to it. Passing
/// the generated factory to the <see cref="DelimitedSerializer" /> factory overloads avoids the reflection binder
/// entirely, making the serialization path trimming- and AOT-safe.
/// </para>
/// </remarks>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false, Inherited = false)]
public sealed class DelimitedRecordAttribute : Attribute
{
}
