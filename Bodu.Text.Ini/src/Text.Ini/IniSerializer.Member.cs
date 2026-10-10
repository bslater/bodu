// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IniSerializer.Member.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Reflection;
using Bodu.Text.Serialization;

namespace Bodu.Text.Ini;

public static partial class IniSerializer
{
    /// <summary>
    /// Serializer-local adapter retaining the current member-discovery and naming-policy contract.
    /// </summary>
    private sealed class Member : FlatMemberDescriptor
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Member" /> class.
        /// </summary>
        /// <param name="source">The reflected property or field.</param>
        /// <param name="convertName">The member-naming policy.</param>
        private Member(MemberInfo source, Func<string, string> convertName)
            : base(source, convertName)
        {
        }

        /// <summary>
        /// Creates a descriptor for a property.
        /// </summary>
        /// <param name="property">The property to describe.</param>
        /// <param name="options">The serializer options containing the naming policy.</param>
        /// <returns>A descriptor for <paramref name="property" />.</returns>
        public static Member FromProperty(PropertyInfo property, IniSerializerOptions options) =>
            new(property, options.ConvertName);

        /// <summary>
        /// Creates a descriptor for a field.
        /// </summary>
        /// <param name="field">The field to describe.</param>
        /// <param name="options">The serializer options containing the naming policy.</param>
        /// <returns>A descriptor for <paramref name="field" />.</returns>
        public static Member FromField(FieldInfo field, IniSerializerOptions options) =>
            new(field, options.ConvertName);
    }
}
