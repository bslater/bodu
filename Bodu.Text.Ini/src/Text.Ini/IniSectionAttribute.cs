// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IniSectionAttribute.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Ini;

/// <summary>
/// Marks a partial section POCO for compile-time INI binding: the <c>Bodu.Text.Formats.Generators</c> source generator
/// emits an <see cref="IIniSectionFactory{TSection}" /> implementation for the type, exposed through a generated static
/// <c>IniFactory</c> property.
/// </summary>
/// <remarks>
/// <para>
/// The generated factory maps the type's public read/write instance properties in declaration order, honouring
/// <see cref="Bodu.Text.Serialization.PropertyNameAttribute" /> for key names and skipping members annotated with
/// <see cref="Bodu.Text.Serialization.IgnoreAttribute" />. Scalar values convert with the invariant culture.
/// </para>
/// <para>
/// The annotated type must be declared <see langword="partial" /> so the generator can add the factory to it. Passing
/// the generated factory to the <see cref="IniSerializer" /> section overloads avoids the reflection binder entirely,
/// making the serialization path trimming- and AOT-safe.
/// </para>
/// </remarks>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false, Inherited = false)]
public sealed class IniSectionAttribute : Attribute
{
}
