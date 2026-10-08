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
/// <see cref="Bodu.Text.Serialization.IgnoreAttribute" />. Scalar values convert with the invariant culture. When
/// writing, the factory leaves out the keys the reflection binder of <see cref="IniSerializer" /> leaves out under each
/// member's own <see cref="Bodu.Text.Serialization.IgnoreAttribute.Condition" />, and under
/// <see cref="Bodu.Text.Serialization.IgnoreCondition.WhenWritingNull" />, the default, for a member without one; the
/// factory never sees <see cref="IniSerializerOptions" />, so another
/// <see cref="IniSerializerOptions.DefaultIgnoreCondition" /> does not apply to it.
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
